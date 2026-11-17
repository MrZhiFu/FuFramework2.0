# 取消类重命名设计

- 日期：2026-11-18
- 状态：待批准

## 背景与动机

`Core/Async/` 下两个取消工具类命名有缺陷：

1. `LifecycleCancellationSource` 太长，书写与阅读成本高；
2. `CancellationScope` 无法见名知意——"Scope" 传达不出"记账（在途计数）+ 排水（等收尾再返回）"的能力；
3. 两者统一以 `Cancellation` 开头，在补全列表中成对出现、选型直观。

## 决策（用户已定案：方案A）

| 旧名 | 新名 | 命名理由 |
|------|------|----------|
| `LifecycleCancellationSource` | `CancellationTokenIssuer` | Issuer = 发令牌者；核心动作 Recreate 换新令牌，"发"字命中职责 |
| `CancellationScope` | `CancellationBarrier` | Barrier = 栅栏；"取消后挡住、等在途全部收尾才放行"，直指框架重启排水场景 |

## 范围（要改的）

- 类名 + 文件名重命名（含 `.meta`，`git mv` 保持 GUID 与历史）：
  - `LifecycleCancellationSource.cs` → `CancellationTokenIssuer.cs`
  - `CancellationScope.cs` → `CancellationBarrier.cs`
- 两个类自身的 XML 注释同步（含互相提及对方名字处）
- 全部调用点 `.cs`（WinBase、CustomLoader、ModuleManager、各 Module、Guide、Network、GameDriven、AntiCheating、UIModule 等）
- 6 个 FGUI `.Gen.cs` 手改 + 插件模板 `FairyGUIProject/plugins/CSharpCodeGen/Template/CompGenTemplate.txt` 同步（沿用 TimerRegister 改名的"模板+Gen 同步"惯例）
- `CLAUDE.md` 铁律第 5 条中点名的类名
- 7 个模块 README：Core / UI / Scene / Sound / Entity / Asset / Web

## 非目标（裁决，可纠正）

- `ICancelAsync` 接口名与 `Begin()/BeginScope/Recreate()/CancelAsync()/Cancel()` 等成员名不动——用户诉求只针对类名，这些成员名不违反"太长/不见名知意/统一前缀"三条中的任何一条。
- `Docs/superpowers/` 下带日期的历史 specs/plans 不回改——它们是当时决策的存档，回改等于伪造历史。
- 服务端 `AppSession.cs` 不动——grep 命中的是 `ILogger.BeginScope`（Microsoft.Extensions.Logging），假阳性，已核实。
- `HotfixReflection.cs` 无需同步——已核实其 TypeNames 不含这两个类型名。

## 验证

1. unity-cli 编译通过；
2. 全仓 grep `LifecycleCancellationSource|CancellationScope` 只剩 `Docs/superpowers/` 历史存档；
3. 两类内部互相提及的注释、模板与 Gen 文件内容一致。
