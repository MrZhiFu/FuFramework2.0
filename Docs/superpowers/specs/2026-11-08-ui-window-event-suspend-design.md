# UI 窗口事件订阅"关窗挂起/开窗重放"机制设计

- 日期：2026-11-08
- 状态：设计已与维护者（用户）对齐，方案 A 拍板
- 关联提交：`2192574f`（WinPlayerList 误杀语言订阅的临时修复，本设计落地后其手动配对整体作废）
- 关联变更：EventPool 订阅语义已由 `(id, handler)` 引用计数改为幂等去重（重复订阅记录警告日志）、退订即时摘除（待移除表/延迟摘除机关整体移除，`Check` 删除——零调用方，不保留查询 API）；独立事件池 `EventPool<T>` 已整体并入 `EventModule`（泛型参数全库零第二使用，纯转发门面层消除）；事件模块与 EventRegister 的全部守卫已改为 FuLogger 错误日志（不抛异常；`EventRegister.Create` 在模块不可用时返回「未绑定订阅器」，其上操作记日志并忽略）——本文档涉及时表述已同步

## 1. 背景与动机

### 1.1 现状（全部为已验证代码事实）

- 窗口事件订阅的生命周期绑定**池实例**而非**打开会话**：三个注册器只在首次创建时建立（`WinBase.cs:171-174`），池复用路径 `Init` 早退不重订（`WinBase.cs:158`）。
- `Close → _OnClose → _OnRecycle → 回池`全程**不退订**；只有真销毁 `_OnDispose → ReleaseAllRegisters` 才释放。
- 池参数：容量 16、闲置 60s 过期、60s 检查（`UIModule.cs:76-86`）——"关着订阅活着"是 0~60 秒的暂态，随真销毁消失。
- 事件模块按 `(id, handler)` 幂等去重：重复订阅只保留首次登记并记录警告日志，不分发翻倍（`EventModule.Subscribe/Unsubscribe`）。
- 计时器注册在全局 `TimerModule`，关闭路径无任何暂停动作，窗口关着照常走。

### 1.2 后果（已发生的两类事故）

1. **幽灵触发**：关闭的池内窗口仍收事件。`WinPlayerList` 曾因重连广播打进已关闭窗口，用过期数据重跑登录事务（双事务竞态），被迫手动 `OnOpen` 订阅 / `OnClose` 退订配对。
2. **误杀订阅**：业务退订唯一现成工具是全清语义的 `UnSubscribeAll`，会把框架在 `Init` 时订阅的语言变更事件一并退掉，且池复用不重订 → 二次打开后语言切换失效（已由 `2192574f` 临时修复）。

### 1.3 根因

框架提供了机制（注册器/幂等去重/池销毁兜底）但没给立场："隐藏窗口要不要继续处理事件"由每个业务开发者自行拼装，拼错代价是幽灵触发或误杀。

### 1.4 拍板

**方案 A：关窗自动挂起、开窗自动重放。** 维护者确认业务当前与可预见范围内**不存在**"窗口关着也要持续收事件做后台处理"的场景，现有 Refresh-on-open 模式可覆盖全部数据新鲜度需求。

## 2. 目标

1. 框架级立场与既有 `m_cancellation` 同构：**窗口关闭 = 这个窗口的一切暂停**（异步取消、事件挂起、计时器终止）；**开窗 = 恢复**。
2. 业务零手动配对：不再需要 OnOpen 订阅 / OnClose 退订。
3. 消除 `UnSubscribeAll` 误杀风险的业务暴露面（业务今后不需要在 OnClose 里退订）。
4. 业务订阅心智模型唯一化：**订阅放 `OnInit`（每实例一次），`OnOpen` 只做数据刷新**。

## 3. 非目标

- 不改组件（Comp Gen 层注册器）生命周期——组件注册器独立于窗口，随窗口实例销毁，维持现状。
- 不改 `Pause / BeCover / Reveal` 语义——隐藏但不关闭的窗口**不挂起**（窗口仍在组内活跃，与 `Visible` 守卫模式一致）。
- 不改 `UIEventRegister`（FGUI 控件回调）——随 GObject 生命周期，无全局登记。
- 不改 `UnSubscribeAll` 的公开语义（全清仍是全清）。
- 不做派发侧守卫（EventPool 不感知窗口状态）。
- 不为"关着也要收事件"提供豁免开关——维护者确认无此场景；将来若出现再评估。

## 4. 反目标（出现即视为失败）

1. **分发翻倍**：挂起/重放不得造成同 handler 同帧或同事件被调用多次（EventModule 幂等去重不可破坏：重复订阅不分发翻倍）。
2. **真销毁残留**：挂起态下销毁窗口，总线必须无残留订阅（`EventModule.Unsubscribe` 对缺失条目是安全空操作，见 5.4）。
3. **订阅账本无界膨胀**：挂起不清账本，业务若沿用在 `OnOpen` 里订阅的旧习惯，每次开窗账本加一条（分发仍单次——池侧幂等去重，重复订阅直接触发警告日志，误用信号可见）——以约定 + 模板注释约束，并作为已知误用症状写入调试面板可观测范围。
4. **API 膨胀**：不新增业务必须学习的第二种订阅方式。

## 5. 设计决策（已定）

### 5.1 挂起范围

仅窗口自身的 `WinBase.EventRegister`（业务订阅与框架订阅一起挂起）。语言变更订阅挂起后由 `_OnOpen` 既有的 `GObject.RefreshAllL10n` + 业务 `OnOpen` 刷新覆盖，行为正确。

### 5.2 计时器

`_OnClose` 调 `StopAllTimers()`（**破坏性终止，不重放**）——与 `m_cancellation.Cancel()` 同构。当前业务层零计时器使用（已全量核查），无迁移成本；将来"需跨开窗存续"的计时器在 `OnOpen` 重启即可。

### 5.3 EventRegister 新增挂起机制

```text
m_suspended: bool（默认 false）

Suspend():
    幂等守卫：已挂起直接返回
    置位 m_suspended
    遍历自身账本 m_eventHandlerDict，逐条 m_eventModule.Unsubscribe(id, handler)
    （EventModule 对不存在的条目天然忽略，不抛异常；账本保留不清空）

Resume():
    幂等守卫：未挂起直接返回
    复位 m_suspended
    遍历账本逐条 m_eventModule.Subscribe(id, handler) 重放

Subscribe(id, handler)：
    账本无条件登记（现状不变）
    仅当 !m_suspended 才向总线 Subscribe（挂起态只记账，重开时由 Resume 统一重放）

UnSubscribe(id, handler)：
    账本移除（不存在记错误日志并忽略，不抛异常）
    向总线退订走 m_eventModule.Unsubscribe（模块侧幂等，挂起态下总线无此条目则自然忽略）

UnSubscribeAll()：
    现实现天然兼容挂起态：逐条退订（模块侧幂等忽略）+ 清账本，无需改动

Clear()：
    UnSubscribeAll 之外必须复位 m_suspended = false——寄存器是 IReference，经
    ReferencePool 回池复用且 Acquire 不重置任何字段（ReferenceCollection.cs:77-107），
    漏复位会让挂起态寄存器被新窗口领走，其订阅全部只进账本不进总线（事件静默失效）
```

### 5.4 挂起态销毁链路安全性（评审修订）

挂起后窗口在池中过期/超容量被真销毁：`DisposeObjectInternal → WinObject.OnDispose → _OnDispose → ReleaseAllRegisters → EventRegister.Clear → UnSubscribeAll`。逐条 `m_eventModule.Unsubscribe` 对已在总线侧移除的条目天然忽略（`FuMultiDictionary.Remove` 安全空操作），账本随后清空，无残留、无异常。

**补一处复位**（评审发现的设计漏洞，已修订 5.3）：`EventRegister.Clear()` 必须在 `UnSubscribeAll` 之外复位 `m_suspended = false`。寄存器经 `ReferencePool` 回池复用，`Acquire` 不重置字段（`ReferenceCollection.cs:77-107`）——漏复位则挂起态寄存器被新窗口领走后事件静默失效。

### 5.5 调用时机

- `_OnClose`：`m_cancellation.Cancel()` 之后追加 `EventRegister.Suspend()` 与 `StopAllTimers()`（`WinUI == null` 的 teardown 分支同样执行）。
- `_OnOpen`：`m_cancellation.Recreate()` 之后、业务 `OnOpen()` 之前执行 `EventRegister.Resume()`（保证业务 OnOpen 期间订阅已就绪）。
- `_OnRecycle` / `_OnDispose` 不新增动作（销毁链路见 5.4）。

### 5.6 业务约定（随本机制生效）

- **订阅一律放 `OnInit`**（每实例一次）；`OnOpen` 只做数据刷新，不再订阅。
- 关窗后需要重置的状态在 `OnOpen` 刷新，而不是靠"关窗时退订重订"实现。
- 模板（Gen 层示例注释）与 UI README 同步更新此约定。

### 5.7 已知接受边界

- 事件分发过程中窗口被关闭（handler 内 `CloseSelf`）：EventPool 分发基于快照，同事件其余 handler（含被挂起窗口的）仍会执行完本轮——与现状退订语义一致，接受。
- 挂起态下业务误调 `UnSubscribe` 未订阅的条目记错误日志并忽略（不抛异常）。

## 6. 迁移

1. `WinPlayerList`：`InitEvent()` 移入 `OnInit`；删除 `OnOpen` 内订阅调用与 `OnClose` 整个方法（手动配对作废）。
2. 其余窗口零改动（全量核查过：业务订阅点仅 3 处，另 2 处在组件注册器，不受影响）。
3. 回滚：机制集中在 `EventRegister` 两方法 + `WinBase.Life` 两调用点，`git revert` 即可整体回退。

## 7. 约束

- 遵守 `CLAUDE.md` 五项铁律（无 Task/协程/LINQ/反射；异步链有生命周期所有者）与 `Docs/代码风格规范.md`（Tab 缩进等）。
- 不引入反射、不改动 EventPool/EventModule 公开契约。
- 注释风格对齐代码库现状：防御式守卫逐条说明"为什么"，陷阱写进注释防止回退。

## 8. 留给实施者

1. `Suspend/Resume` 的具体实现与防御式注释（异常处理风格对齐库内现有防御写法）。
2. `WinBase.Life.cs` 两处调用点插入与注释。
3. Gen 模板示例注释更新（"订阅放 OnInit，框架自动挂起/恢复"）。
4. `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md` 对应章节更新。
5. 验证（编译 + Play 模式插桩实测）：
   - 开窗 → 广播事件 → handler 收到；
   - 关窗 → 广播 → 不收到（挂起生效）；
   - 重开 → 广播 → 收到（重放生效）；
   - 开着窗口切换语言 → 刷新正常（语言订阅未被误杀）；
   - 关窗后等待池过期销毁（或调小过期时间）→ `EventModule.EventHandlerCount` 对账无残留；
   - 过期销毁 → 寄存器经引用池复用给新窗口 → 开窗 → 广播可达（验证 `Clear` 复位挂起标志，防静默失效）；
   - `WinPlayerList` 走一遍登录流程回归。

## 9. 明确不做

- 组件层的同类挂起机制（组件常驻窗口实例内，无"开关会话"概念，现状即正确）。
- 账本去重/防膨胀的框架强制（约定 + 可观测性已够；机制强制会破坏 N 订 N 退的对称契约）。
- "关窗也要收事件"的豁免开关（见非目标）。
