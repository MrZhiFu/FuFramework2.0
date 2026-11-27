---
paths:
  - "**/*.cs"
---

# 运行时性能测量

本项目标榜性能（禁 LINQ、热路径不分配），但"感觉快"不算证据。这里给可复现的测量方法。

**三层取数，从快到慢**：

**① `eval_csharp` 直接求值**（最快，免编译免改码）：

```bash
Tools/UnityCli/unity-cli.exe tool call eval_csharp --json '{"code":"System.Diagnostics.Stopwatch.StartNew().ElapsedMilliseconds"}'
```

适合临时读一个值、算一段耗时、调一个方法。

**② 项目自带 Profiler**（结构化指标）：

```bash
# 1. 枚举可用指标
Tools/UnityCli/unity-cli.exe tool call profiler_get_metrics --json '{"listAvailable":true}'
# 2. 开采集 → 跑操作 → 读指标 → 停
Tools/UnityCli/unity-cli.exe tool call profiler_start --json '{}'
Tools/UnityCli/unity-cli.exe tool call profiler_get_metrics --json '{"metrics":["<指标名>"]}'
Tools/UnityCli/unity-cli.exe tool call profiler_stop --json '{}'
```

**③ `[MenuItem]` 探针**（前两者都不够时——需要多次迭代、跨帧、或控制台门控）：

只有 Profiler 给不出你要的粒度（例如某方法内的纳秒级对比、需要固定迭代次数取最小值）时，才写探针 + `execute_menu_item` 触发（见 `tooling/unity-cli.md`）。

## CPU 耗时（纳秒级）

```csharp
#if UNITY_EDITOR
[MenuItem("Probe/MeasureTimerAdd")]
private static void MeasureTimerAdd()
{
    const int ITERATIONS = 10000;
    var sw = new System.Diagnostics.Stopwatch();
    long minTicks = long.MaxValue;

    for (int round = 0; round < 5; round++)   // 多轮取最小值
    {
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            // 被测代码
        }
        sw.Stop();
        if (sw.ElapsedTicks < minTicks) minTicks = sw.ElapsedTicks;
    }

    double nsPerOp = minTicks * (1_000_000_000.0 / System.Diagnostics.Stopwatch.Frequency) / ITERATIONS;
    System.IO.File.WriteAllText(
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "probe-timer.json"),
        $"{{\"nsPerOp\":{nsPerOp:F2}}}");
}
#endif
```

**为什么多轮取最小**：JIT 首次编译、GC 抖动、系统调度都会抬高耗时。最小值最接近"纯执行成本"。

**必须扣基线**：空循环跑同样的迭代次数，把它的耗时减掉。

## GC 分配

| 测什么 | 方法 |
|---|---|
| 单次调用分配量 | `long before = GC.GetTotalMemory(false); 调用; long diff = GC.GetTotalMemory(false) - before;` |
| GC 触发次数 | `int g0 = GC.CollectionCount(0); 调用; int delta = GC.CollectionCount(0) - g0;` |

**注意**：
- `GC.GetTotalMemory(false)` 是近似值，单次小分配（<1KB）会被噪声淹没——**只有几十 KB 以上的分配才可信**。
- 测分配前先 `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();` 让基线稳定。
- 迭代多次取平均，单次测量不可信。

**热路径的验收标准是"零分配"**，不是"分配得少"。测出非零就先怀疑：闭包、装箱、字符串拼接、临时集合。

## 触发与取数

```bash
Tools/UnityCli/unity-cli.exe tool call execute_menu_item --json '{"menuPath":"Probe/MeasureTimerAdd"}'
# 读结果文件（不要靠控制台）
cat "$TEMP/probe-timer.json"
```

**结果写文件，不要读控制台**：
- `read_console` 是消费式读取（读一次即清零）
- 探针里的 `Debug.Log` 会被捕获为 `Warning` 类型，按 `Log` 过滤读不到

## 探针纪律

1. 探针代码用 `// ==== 临时探针（用完删） ====` 包裹，`#if UNITY_EDITOR` 包裹。
2. 菜单名用 **ASCII**（中文菜单名在 `execute_menu_item` 里不稳）。
3. 测完**删除探针**并清理孤儿 `.meta`，`git status --porcelain -uall | grep -i probe` 复查。
4. 别把探针留在提交里。

## 什么时候该测

- 优化前后对比（用同一探针，前后两次数据）
- 怀疑某条热路径有分配
- 改了池化/缓存/循环结构后

**没有测量数据的"优化"不是优化，是猜测。**
