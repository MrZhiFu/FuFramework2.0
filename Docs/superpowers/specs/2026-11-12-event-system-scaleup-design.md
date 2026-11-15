# 事件系统规模化改造设计（sender 退场 / 广播侧观测补齐 / 事件编写规范）

- 日期：2026-11-12；2026-11-13 修订——工作流 B 由"事件类生成"降级为"事件类编写规范"（§0），原待确认项消解；同日二次修订——string ID 退场改类型键订阅 T1（D5），事件类 Id 派生收敛至基类
- 状态：全部决策已拍板——基线五判断（1.2）+ 实施决策 D1~D5（1.4）；独立评审已完成；待进 writing-plans
- 关联：与知识仓库《SLG/1.框架层/05-事件系统》全系列对比验证（2026-11-11 会话，两侧源码与文档实读）
- 关联提交：用户已手动修复 `LanguageChangeEventArgs` 两处 public setter 并提交（§9 记为已完成项）

## 0. 修订说明（2026-11-13）：生成器取消的理由

原版工作流 B 为"声明文件 + 批处理生成器"，价值命题是**压缩人类作者**的每事件 30~40 行样板成本。维护者指出关键前提变化：**今后事件类由 AI（Claude Code）按模板编写，不是人手写**。前提消失，逐条重新核账：

| 原收益 | 作者=AI 后的重新评估 |
|--------|---------------------|
| 消灭 30 行/事件模板劳动 | AI 按模板写 40 行是秒级操作，无压缩空间 |
| 消灭属性/Clear/Create 三处漏改 bug | 最强残余论点。漏改非编译错误，但 AI 模板忠实度 + 评审覆盖；若真实出现漏写，届时补百行级校验脚本（解析 EventArgs 检查三处同步）——延迟到首个真实漏写再加，不预造 |
| 声明文件 = 事件目录 | grep / 调试面板 / 类文件检索已覆盖；手维护目录引入"目录与代码漂移"新风险 |
| （反向账） | 对 AI 工作流是额外两步：改声明 → 跑 bat → 核对产物，比直接写类更慢 |

结论：按本项目既定教训（"机制等真实热点出现再加"），**取消生成器**，工作流 B 降级为「事件类编写规范」。原待确认项（生成机制选型、存量 40+ 迁移策略）随生成器取消一并消解——新事件照旧手写即无迁移问题。

## 1. 背景与动机

### 1.1 对比定位结论（已验证）

本项目 EventModule 与 SLG EventBus 是同一性能哲学（主线程单线程、快照隔离分发、逐 handler 异常隔离、零分配导向）下的两条建模路线：

| 维度 | SLG EventBus | 本项目 EventModule |
|------|-------------|-------------------|
| 事件建模 | 事件即信号：const 频道名 + 0~4 泛型参数，参数契约靠注释 | 事件即对象：`GameEventArgs` 子类，`typeof(X).FullName` 作 ID |
| 分发时序 | 完全同步（嵌套递归） | 默认延迟一帧批次分发 + `BroadcastNow` 同步 |
| 订阅自动化 | `[EventMethod]` 代码生成（738 处主路径） | 手写 Subscribe + 账本挂起机制（Suspend/Resume） |
| 防御哲学 | 文档传纪律（13 条实践/反模式表） | 代码锁死：幂等去重、守卫、三层兜底回收 |

规模化短板收敛为两处：**每事件定义成本**（已由"作者=AI"消解，见 §0）与**广播侧观测缺失**（无广播计数、无"广播了没人听"可见性）。

### 1.2 决策基线（维护者已拍板，本设计的五条判断）

1. **事件建模保"一个事件一个类"**：依据是契约验证位置——发布/订阅双方无编译期引用关系，契约强度是唯一正确性保障；const 路线的书写成本优势是作者成本问题（原由生成器消解，现由"作者=AI"消解，判断本身不变），契约弱是设计问题（工具补不回）。性能账已核：两条路线稳态均零 GC，const 模型每次广播省 2 次池操作 + 1 次队列跳（几百纳秒级）+ 值类型载荷不碰堆（仅高频场景兑现，而高频广播本身是双方规范禁止项），均为二阶量级。
2. **sender 从广播 API 与 handler 签名退场**：全库订阅方 handler 的方法体**零处使用 sender**（仅签名与 `<param>` 注释），62 处 Broadcast 调用全为机械传 `this`；SLG 同问题上已做相反选择（Broadcast 无 sender 参数）。"谁发的"是要么进 args 的事实、要么无关的信息。趁规模化前修订，成本最低。
3. **参数传递维持池化类 + 共享语义**：SLG 泛型原语链路无装箱（泛型特化，非 `object[]`/`DynamicInvoke`），"装箱疑虑"排除；真实差异是共享引用 vs 值拷贝，本项目以 `private set` 结构防线 + 所有权契约（`HandleEvent` 无条件回收、不得缓存/转发）覆盖。struct 泛型分发改造不做（统一签名要求公共基类，复杂度不值）。
4. **无参事件排除枚举路线，统一走 class 体系**：枚举无 API 通路（`EmptyEventArgs.Create` 仅 `string` 重载，注释"支持枚举"为 GameFramework 遗留）；`enum.ToString()` 每次广播分配字符串；且现状 string 重载 + `EmptyEventArgs` 全库零业务调用（`EmptyEventArgs.Create` 唯一调用点是重载自身 `EventModule.API.cs:102`），为死代码。
5. **string ID 从公开 API 退场，订阅走类型键（T1 定案）**：现状 string 键下「订阅引错 ID / 引用他人 EventId」编译期不可见（错误模式 A/B），而枚举作 ID 会拆掉「身份单一事实源」——枚举成员与事件类是两套独立标识可失配，广播侧 `Broadcast(枚举, args)` 两参数可各说各话，SLG「字段名≠频道值」坑以新形态复活，故否决枚举。取类型键：`Subscribe<T>(Action<GameEventArgs> handler)`，键 = `typeof(T).FullName`（内部字典仍 string 键、分发机制零变化）；广播侧 `Broadcast(GameEventArgs e)` 键自 `e.Id` 派生，广播↔订阅键天然同源。连带简化：基类统一实现 `Id => GetType().FullName`，事件类删除 per-class `Id` 属性与 `EventId` 静态字段（实现期验证 `FullName` 缓存行为，有顾虑则回退保留 per-class 静态字段形态）。T2（`Action<T>` 全绑定、handler cast 消失）不做：分发 wrapper 簿记（退订对称、面板显示原始方法）复杂度不成比例，错误模式 A 的残余失效形态是 cast 失败留日志（可见非静默）。

### 1.3 规模前提

业务事件将走向数百量级（对齐 SLG 436 常量的实践规模）。现状：框架各模块手写事件类约 40+，游戏侧仅 `BagChangedEventArgs` 一个。**新事件的作者是 AI（Claude Code）按模板编写**（§0 修订的前提）。

### 1.4 实施决策记录（2026-11-13 拍板）

- **D1** handler 委托 = `Action<GameEventArgs>`（否具名委托）；
- **D2** README §5.x 旧示例延后单独一笔处理；
- **D3** 无监听双桶口径确认（不做挂起态精确感知）；
- **D4** 提交切分 = 按工作流三笔（B 体量小可与 C 合并，实现期定）；
- **D5** 订阅 API = 类型键 T1（否决枚举 ID、否决 T2 全绑定），并入工作流 A 执行。

## 2. 目标

1. **sender 退场**：广播 API 与 handler 签名去掉 `object sender`，趁规模化前完成破坏性修订。
2. **事件类编写规范**：README 收敛出唯一事件类模板（含无参形态），新事件按模板落库；无参事件统一 class 体系，`EmptyEventArgs` + string 重载退役。
3. **广播侧观测补齐**：EventModule 增加编辑器期广播统计（次数/最近时间/无监听双桶），`EventModuleWindow` 增加对应展示——补齐排障第一问"有没有人喊、喊了有没有人听"。
4. **契约固化**：`private set` 参数契约、"不得修改收到的 e" 禁令写入 README 与代码注释，修正 README 中与基线冲突的示例。

## 3. 非目标

- **不做事件类生成器与声明文件**（§0 修订）；不做预造性校验脚本（首个真实漏写出现再建）。
- 不做订阅自动化（`[EventMethod]` 类生成）——挂起机制已把订阅成本压到一行一个；待真实摩擦出现再评估。
- 不做生产者 StackTrace 追踪与静态源码扫描——本期只做计数与无监听现形，生产者追踪列为后续阶段。
- 不做 struct 泛型分发、`IReadOnlyList` 包装等参数侧机制化——已知开口（§9）记录不动。
- 不改分发语义——延迟一帧批次分发、快照隔离、幂等去重、异常隔离、回收时机全部维持现状。
- 不改挂起机制（Suspend/Resume/账本）——2026-11-08 设计已定案。

## 4. 反目标（出现即视为失败）

1. **运行时反射**：任何新增机制不得引入运行时反射（铁律；Editor 面板既有反射惯例不受影响）。
2. **玩家包成本**：广播统计的任何字段与调用不得进入玩家包执行路径（验证口径见 §12）。
3. **订阅方无感破坏**：sender 退场后，现有订阅/分发行为（除签名形参外）不得有任何行为变化——同事件同 handler 同轮只调一次、异常隔离、回收时机不变。
4. **两套 ID 制度**：无参事件统一进 class 体系后，不允许"无参事件走字符串、带参事件走类型"的并行态长期存在。

## 5. 工作流 A：sender 退场（签名修订，先行）

### 5.1 新签名

```csharp
// 订阅/退订：string ID 退场，事件类型即键（T1 定案，见 1.2 判断 5）
void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
void Unsubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs

// 广播：sender 形参移除；string 重载随 EmptyEventArgs 一并退役（§6.2）；键自 e.Id 派生
void Broadcast(GameEventArgs e)
void BroadcastNow(GameEventArgs e)
```

- 委托形态定案 `Action<GameEventArgs>`（D1：标准库、零新类型；handler 统一签名不变，调试面板数据源零改动）。
- 内部实现：`FuMultiDictionary<string, Action<GameEventArgs>>` 不变，键 = `typeof(T).FullName`；`BaseEventArgs` 统一实现 `Id => GetType().FullName`（连带删除全部事件类的 `Id`/`EventId` 两成员，随 A 清扫一并执行，grep 兜底残留引用）。
- `BaseEventArgs : EventArgs` 的 `EventArgs` 继承随 `EventHandler` 模式退役一并移除（全库无 `EventArgs` 基类成员引用，一行变更）。
- 事件节点 `Event`（`EventModule.Event.cs`）删除 `Sender` 字段，只承载参数；调试面板队列视图"发送者"列随之移除（信息由 §7 广播统计后续替代，生产者追踪为后续阶段）。

### 5.2 影响面清单（已盘点）

| 层 | 位置 | 改动 |
|----|------|------|
| 模块 API | `EventModule.API.cs`（Subscribe/Unsubscribe/Broadcast×2/BroadcastNow） | 签名修订，string 重载删除 |
| 分发 | `EventModule.Dispatch.cs`（HandleEvent/`handler.Invoke(sender, e)`→`handler(e)`） | 签名修订 |
| 节点 | `EventModule.Event.cs` | 删 Sender 字段与赋值 |
| 注册器 | `EventRegister.cs`（账本类型 + 全部转发方法，`Subscribe/Unsubscribe` 增泛型形态） | 签名修订 |
| 窗口层 | `WinBase.EventRegister.cs`、`BaseModel.cs` 转发方法（同增泛型形态） | 签名修订 |
| Gen 模板 | Comp 层 `Broadcast`/`BroadcastNow` 转发（`CompXxx.Gen.cs:88-90` 同款模板） | 模板更新 + 全量重生成 |
| 广播调用点 | 62 处 `Broadcast(this, ...)` → `Broadcast(...)` | 机械清扫 |
| 订阅调用点 | 全部 `Subscribe(XxxEventArgs.EventId, OnXxx)` → `Subscribe<XxxEventArgs>(OnXxx)`；per-class `Id`/`EventId` 引用残留 grep 兜底 | 机械清扫 |
| 业务 handler | 全部 `void OnXxx(object sender, GameEventArgs e)` → `void OnXxx(GameEventArgs e)` | 机械清扫（已验证零方法体使用 sender） |
| 调试分部 | `EventModule.Debug.cs` `ForEachHandler` 签名 | 同步 |
| 编辑器面板 | `EventModuleWindow.cs`：`HandlerEntry<T>`/`EventEntry<T>` 适配器、`EventHandler<>` 反射绑定（:720-728）、队列行 `m_SenderTypeName` | 签名与展示同步 |

全库 handler 的 sender 方法体使用为零（grep 已验证），清扫为纯机械。

### 5.3 语义保持点

快照隔离、幂等去重、逐 handler 异常隔离、`HandleEvent` 无条件回收、挂起机制、主线程断言——全部不动。本工作流只动签名形参，不动任何分发行为。

### 5.4 启动前置（评审提出）

在途未提交批次（Fui 异步绑定相关 4 文件）先落地提交、设计文档入库、工作树干净后，再启动本工作流——`WinLogin.cs` 等业务文件在清扫名单内，避免双线混改。

## 6. 工作流 B（修订）：事件类编写规范

### 6.1 唯一模板

README §5.1 修复 private set 后即为范本，新事件一律照此形态（AI 编写亦照此，不另立门派）：

```csharp
public sealed class XxxEventArgs : GameEventArgs   // Id 由基类统一派生（T1 连带简化），类内不再声明 Id/EventId
{
    public int Field1 { get; private set; }        // private set 契约（§8）
    public long Field2 { get; private set; }

    public override void Clear() { Field1 = 0; Field2 = 0; }   // 全字段复位

    public static XxxEventArgs Create(int field1, long field2) // 构造期唯一赋值点
    {
        var e = ReferencePool.Acquire<XxxEventArgs>();
        e.Field1 = field1;
        e.Field2 = field2;
        return e;
    }
}
```

三处同步点（属性 / `Clear` / `Create`）由模板 + 评审约束；首个真实漏写出现时再建百行级校验脚本（§0）。

### 6.2 无参事件统一与 EmptyEventArgs 退役

- 无参事件 = 零字段事件类（`Clear()` 空体、`Create()` 无参），与带参事件同一体系；订阅形态 `Subscribe<GameStartEventArgs>(OnGameStart)`。
- `EmptyEventArgs`、string 重载 `Broadcast(object, string)`（`EventModule.API.cs:94`）退役——全库零业务调用（已验证），退役无破坏。
- README 中 EmptyEventArgs 与枚举相关示例同步删除（§8）。

### 6.3 落点与归档

- 延续现状惯例：一事件一文件，按所属模块归入对应 Event 目录。
- 游戏侧新事件归 `Hotfix.Game.Events`（与 `BagChangedEventArgs` 同域）；框架模块事件随各模块现有目录。
- 规模显著（文件数造成真实导航负担）后可按域合并文件——裁量项，不预先定。
- 撞频道：`typeof().FullName` 结构保证与作者无关；新增事件的语义重名靠 review 与目录检索。

## 7. 工作流 C：广播侧观测补齐

### 7.1 统计机制（玩家包零成本）

```csharp
// EventModule.API.cs（无条件编译）：
partial void RecordBroadcast(string eventId);   // Broadcast / BroadcastNow 入口各一行

// EventModule.Debug.cs（整文件 #if UNITY_EDITOR，现状延续）：
partial void RecordBroadcast(string eventId) { /* 计数、最近时间、无监听双桶 */ }
```

- C# partial 方法语义：玩家包下 Debug 分部不编译 → 无实现 → 调用点被编译器剥除；统计字典等字段全部留在 `#if UNITY_EDITOR` 分部内，玩家包零字段、零调用（反目标 2 的落实）。
- 统计项：每事件 ID 的广播次数、最近一次时间、无监听广播计数。
- **无监听双桶口径**（评审发现挂起机制语义交叉后的定案）：挂起机制下窗口关闭 = 总线侧退订、账本保留——若不区分，"关着的窗口"的常态广播会持续污染无监听计数，稀释核心排障信号。处置：`Subscribe` 时在统计侧置单调标志 `hasEverSubscribed[id]`（仅 Editor 分部，一处写入、永不清理、零跨层耦合）；入队时总线无该 ID 订阅则按标志分桶——
  - **从未订阅**（hasEverSubscribed == false）：真孤儿（拼写错/漏订阅），面板重点警示（核心信号）；
  - **有历史订阅但当前无监听**（== true）：挂起中窗口或已退订，面板常规展示（预期态）。
  不做"感知挂起态"的精确区分——需 EventRegister 跨层上报并处理挂起中退订的簿记，复杂度不成比例；双桶已分离"异常"与"预期"，够用且诚实。
- 无监听检测只查键存在（`Contains(TKey)`），不用 O(n) 的 `(key, value)` 重载。

### 7.2 面板扩展（`EventModuleWindow`）

新增「广播统计」段：事件 ID | 当前订阅数 | 广播次数 | 最近广播时间 | 无监听次数（按双桶拆分：从未订阅 / 有历史订阅）；默认按广播次数降序；"广播过但当前无人监听"的 ID 持续可见（即使订阅数为 0）——等价于 SLG 面板 `broadcastStats` 的核心排障价值，且数据源走调试分部公共 API（不重蹈 SLG 反射读私有字段的脆弱耦合）。

## 8. 契约与文档同步

1. `Event/README.md` §4.4 补事件参数契约：**属性一律 `private set`，仅 `Create` 工厂构造期赋值；发布后外部只读；处理函数不得修改、转发或缓存收到的 e**（现状：约定只活在 40+ 文件的惯性里，文档零记载，且 §10.2 最佳实践示例用 `public { get; set; }` 反向示范）。
2. `EventModule.Dispatch.cs` `HandleEvent` 与 `EventModule.API.cs` `Broadcast` 注释补"不得修改"禁令（现状只写"不得转发或缓存"）。
3. README §10.2 最佳实践示例改 private set + Create 形态（并作为 §6.1 唯一模板的落点）；§10.1 删除"枚举（配合 EmptyEventArgs）"推荐与 `ToString()` 示例（与 §1.2 判断 4 冲突且含每广播分配的性能坑）。
4. README §5.x 旧示例（`EventIds` 常量类、`OnDestroy` 手动退订）与现状脱节——**延后单独一笔处理**（D2 定案：不阻塞主链路，示例量大）。

## 9. 已知开口与已完成项

| 项 | 状态 | 处置 |
|----|------|------|
| `LanguageChangeEventArgs` 两处 public setter | **已完成**（用户手动修复并提交） | 关闭 |
| `DownloadAgentHelperUpdateBytesEventArgs.GetBytes()` 暴露可变 `byte[]` | 已知开口 | 记录不动——框架内部事件（DownloadAgentHelper→DownloadAgent），订阅方确定；机制化（只读包装）等真实越界出现 |
| `RedDotChangedEventArgs.m_ChangedKeys` public readonly `List` | 已知开口 | 同上 |
| handler 内共享引用脏读风险 | 结构防线已覆盖（全库 args 仅上述文件有可变面，其余全 `private set`） | 契约注释补全（§8.2）后视为受控 |

## 10. 原待确认项消解记录

原 §10 两项（生成机制：批处理导出器 vs Roslyn 源生成器；存量 40+ 手写类：不动 vs 全量迁移）——随 §0 生成器取消**一并消解**，无遗留决策。

## 11. 给实现者的裁量空间

- 统计内部数据结构与面板 UI 细节；双桶在面板中的具体呈现方式。
- 面板统计段与现有订阅明细/队列段的布局编排。
- 事件类落点与归档细节（§6.3）。

## 12. 验收口径

1. 编译通过 + Play 回归：现有全部事件（订阅/分发/挂起/重放/调试面板）行为与改造前一致——同事件同 handler 同轮只调一次、异常隔离、回收时机不变（反目标 3）。
2. sender 痕迹清零：`object sender` 在事件公共 API、handler 签名、调用点中不再出现（grep 验收）。
3. 玩家包零成本：广播统计相关成员在非 Editor 编译下不存在（反编译或 `#if` 体检验收）。
4. 无参事件演示：手写一个零字段事件类走订阅/广播全链路；`EmptyEventArgs` 与 string 重载删除后编译通过、全库无引用残留。
5. 面板演示：Play 中广播若干事件，统计数字增长、无监听双桶分类正确（构造一个从未订阅 ID 与一个挂起窗口订阅 ID 验证分桶）。
6. 文档同步项逐条核对（§8；§8.4 延后项除外）。
7. 类型键痕迹验收：订阅/退订调用点无 string EventId 残留、事件类无 `Id`/`EventId` 成员声明（grep 验收）。
8. **提交切分（D4 定案）**：按工作流三笔独立提交（A / B / C），每笔编译通过可回归；B 体量小可与 C 合并，实现期定。
