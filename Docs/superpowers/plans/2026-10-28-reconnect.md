# 客户端断线自动重连 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 运行中断线后自动弹"重连中"框按退避序列重拨，拨通后静默重登让玩家原地复活；重试耗尽交给玩家手选。

**Architecture:** 框架层沉淀三件套（关闭原因 `ENetworkCloseReason`、可等待拨号 `ConnectAsync`、退避重拨 `ReconnectAsync`），Game 层新增 `ReconnectManager`（事件分流 + 被顶标记 + 重连事务编排）与两个静态流（`PlayerLoginFlow` / `LogoutFlow`），UI 复用 `WinDialogMessageBox` + 新增 `WinReconnecting`。服务器零改动。

**Tech Stack:** Unity 2022 / C# / UniTask / FairyGUI / protobuf-net 生成物 / 自研 ModuleManager-Singleton 体系

**Spec:** `Docs/superpowers/specs/2026-10-28-reconnect-design.md`（决策表 §4 与现状事实 §3 是本计划的论证基础，executor 必读）

## Global Constraints

- 项目铁律：杜绝原生 `Task`（用 UniTask）、杜绝协程、杜绝 LINQ、运行时杜绝反射、**异步 API 的 `CancellationToken` 参数必传（无默认值）**。
- 提交纪律（用户明文）：**每个提交步骤执行前必须先询问用户，获准后才 commit**；commit message 格式 `[AI]feat: ...` / `[AI]refactor: ...`（中文）。
- 代码缩进用 Tab；字段前缀 `m_`（实例）/`s_`（静态可变）/`sr_`（静态只读）；常量 ALL_CAPS；枚举 `E` 前缀。
- 每个任务的验证 = `unity-cli tool call refresh_assets` 后轮询 `unity-cli tool call get_compilation_state` 至 `errorCount: 0`（下称**编译门**）；Unity 工程无单测设施，行为验证用标注的 Editor 手测步骤。
- 生成物（`*.gen.cs`、`AutoGen/Proto/*`）不得手改；新增 `[NotifyMessageHandler]` 后必须跑菜单 `FuFramework/网络/重新生成推送注册表`。

## Review Focus

设计稿隐含但任务测试不直接覆盖的五个失败模式，reviewer 逐条核对：

1. **重连过程中再次断线**（重入）——同一时间只能有一个重连事务；钉在 Task 6 的重入防护与 Task 7 的事务生命周期。
2. **幽灵订阅双事务**——`WinPlayerList` 进游戏后窗口 Close 而订阅存活，拨通广播触发第二次登录；钉在 Task 4 的 OnOpen 重订 + OnClose `UnSubscribeAll()`。
3. **顶号乒乓**——被顶标记后到达的 `Closed(Lost)` 不得触发重连；钉在 Task 6 的标记检测顺序（先查标记再启动事务）。
4. **登录失败阀门残留**（游戏"聋"）——任何失败路径必须 `ClearNotifyBuffer` 且不遗留 Suspend；钉在 Task 4 的 catch 收尾。
5. **重连期间业务误触**——转圈框必须全屏模态遮罩（防误点吃"频道未创建"异常）；钉在 Task 7 的窗口形态。

---

### Task 1: 框架件 1——关闭原因 `ENetworkCloseReason`

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Network/Misc/ENetworkCloseReason.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/INetworkChannel.cs:51`、`Channel/NetworkChannelBase.cs:196,239`、`Channel/NetworkChannelBase.HeartBeat.cs:90`、`Channel/NetworkChannelBase.Connection.cs:45,92,100`、`Channel/SystemTcpNetworkChannel.cs:137,414`、`Channel/WebSocketNetworkChannel.cs:88,93`、`Channel/NetworkChannelHelper.cs:356`、`Event/NetworkClosedEventArgs.cs`、`NetworkModule.cs:85`、`NetworkModule.API.cs:93`

**Interfaces:**
- Produces: `enum ENetworkCloseReason { Active, Lost }`；`void Close(ENetworkCloseReason reason)`（接口+基类，旧无参 `Close()` **删除**）；`NetworkClosedEventArgs.Create(INetworkChannel networkChannel, ENetworkCloseReason closeReason)` 与 `public ENetworkCloseReason CloseReason { get; private set; }`（`Clear()` 同步清零）。Task 2/3/5/6 消费。

- [ ] **Step 1: 新建枚举文件**

`Misc/ENetworkCloseReason.cs`：`Active`（主动关闭：登出、销毁、手动放弃）、`Lost`（异常断线：心跳判死、收发异常、Socket 断开、服务器踢人/顶号）。注释写明「顶号靠 `NotifyPrompt` 消息标记区分，不靠本枚举」（见设计稿 §5 件 1 修正注）。

- [ ] **Step 2: 改接口与基类，标注全部调用点**

`INetworkChannel.Close(ENetworkCloseReason reason)`；`NetworkChannelBase.Close(reason)` 存入私有字段 `m_closeReason`（默认 `Lost`），入队 `Closed` 事件时随 `EnqueueLifecycleEvent` 携带到 `DrainLifecycleEvents` → `m_NetworkChannelClosed` 委托 → `NetworkClosedEventArgs.Create(channel, reason)`。调用点 reason 分配（逐一改，漏一处编译不过）：

| 调用点 | reason | 语义 |
|---|---|---|
| `NetworkChannelBase.cs:239`（Dispose 内） | `Active` | 销毁 |
| `NetworkChannelBase.Connection.cs:45`（Connect 清旧 Socket） | `Active` | 重建前清理 |
| `NetworkChannelBase.Connection.cs:92,100`（域名解析失败） | `Lost` | 异常 |
| `NetworkChannelBase.HeartBeat.cs:90`（判死） | `Lost` | 异常 |
| `SystemTcpNetworkChannel.cs:137`（bytesReceived<=0） | `Lost` | 对端断开 |
| `SystemTcpNetworkChannel.cs:414`（连接失败 catch） | `Lost` | 异常 |
| `WebSocketNetworkChannel.cs:88`（底层 CloseCallback） | `Lost` | 对端断开 |
| `WebSocketNetworkChannel.cs:93` override `Close` | 透传 reason 给 `base.Close(reason)` | |
| `NetworkChannelHelper.cs:356`（Error 事件回调） | `Lost` | 收发异常 |
| `NetworkModule.API.cs:93` 门面 | 改签名 `Close(ENetworkCloseReason reason = ENetworkCloseReason.Active)` 转发 | 业务主动 |

`NetworkModule.cs:85` `OnNetworkChannelClosed` 增加 reason 参数转发进 `NetworkClosedEventArgs.Create`。

- [ ] **Step 3: 编译门**

Run: `unity-cli tool call refresh_assets` → 轮询 `get_compilation_state`。Expected: `errorCount: 0`。

- [ ] **Step 4: 提交（先询问用户）**

`git add` 相关文件 → 询问用户 → commit `[AI]feat: 网络频道关闭原因区分（Active/Lost），随 NetworkClosedEventArgs 广播`

### Task 2: 框架件 2——可等待拨号 `ConnectAsync`

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/NetworkModule.API.cs`（#region 频道生命周期内新增）

**Interfaces:**
- Consumes: Task 1 的 `NetworkClosedEventArgs.CloseReason`。
- Produces: `public async UniTask<INetworkChannel> ConnectAsync(Uri address, CancellationToken cancellationToken, int timeoutMilliseconds = 15000)`——内部自建 `new NetworkChannelHelper()` 并 `CreateChannel`（helper 无派生者，硬编码可接受），返回已连接频道；超时抛 `TimeoutException`；拨号失败经既有 `NetworkErrorEventArgs` 路径抛 `InvalidOperationException`。Task 3 消费。

- [ ] **Step 1: 实现 ConnectAsync**

事件桥：订阅 `NetworkConnectedEventArgs`（`ne.NetworkChannel == 频道` → TCS 成功）、`NetworkErrorEventArgs`（同匹配 → TCS 失败）、`NetworkClosedEventArgs`（同匹配 → TCS 失败）三事件到 `UniTaskCompletionSource<bool>`；`Connect(address)` 后 await TCS 与「`timeoutMilliseconds` 超时 + `cancellationToken`」竞争（建议：链接 CTS 源 `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` + `CancelAfter`，把融合 token `AttachExternalCancellation` 到 TCS.Task）。`finally` 全量退订。TCS 的 `TrySetXxx` 天然幂等，多事件竞争安全。

- [ ] **Step 2: 编译门**

Expected: `errorCount: 0`。

- [ ] **Step 3: Editor 手测两条路径**

Play 模式临时调用（或临时改 `WinPlayerList.OnBtnLoginClick`）：连真实服务器 → `ConnectAsync` 正常返回；连不存在的 IP → 15s 后抛 `TimeoutException`。测完还原调用点。

- [ ] **Step 4: 提交（先询问用户）**

commit `[AI]feat: NetworkModule.ConnectAsync 可等待拨号（超时/失败以异常返回）`

### Task 3: 框架件 3——退避重拨 `ReconnectAsync`

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Network/NetworkModule.Reconnect.cs`（partial class，含 `ReconnectOptions` + `ReconnectAsync`）

**Interfaces:**
- Consumes: Task 2 的 `ConnectAsync`。
- Produces: `public sealed class ReconnectOptions { public Uri Address { get; init; } public int[] BackoffScheduleSeconds { get; init; } = { 2, 3, 5, 8, 13 }; }`（**数组长度即最大尝试次数**，不设独立 MaxAttempts 字段）；`public async UniTask<INetworkChannel> ReconnectAsync(ReconnectOptions options, CancellationToken cancellationToken)`——重试耗尽抛 `TimeoutException`。Task 7 消费。

- [ ] **Step 1: 实现 ReconnectAsync**

循环形状（设计稿 §5 件 3 定案）：

```csharp
for (var attempt = 0; attempt < options.BackoffScheduleSeconds.Length; attempt++)
{
    await UniTask.Delay(options.BackoffScheduleSeconds[attempt] * 1000, cancellationToken: cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    DestroyChannel();                                  // 旧频道整个销毁，装配状态干净
    try { return await ConnectAsync(options.Address, cancellationToken); }
    catch (OperationCanceledException) { throw; }      // 取消不吞
    catch { /* 记日志后进入下一轮退避 */ }
}
DestroyChannel();                                      // 耗尽后清干净再抛
throw new TimeoutException($"重连失败：已重试 {options.BackoffScheduleSeconds.Length} 次。");
```

- [ ] **Step 2: 编译门**

Expected: `errorCount: 0`。

- [ ] **Step 3: Editor 手测**

临时调用：断开服务器进程 → 调 `ReconnectAsync`（Backoff 换成 `{2,2,2}`）→ 观察日志三轮间隔 2s；重启服务器 → 某一轮拨通返回频道。测完删临时调用。

- [ ] **Step 4: 提交（先询问用户）**

commit `[AI]feat: NetworkModule.ReconnectAsync 退避重拨编排（默认 {2,3,5,8,13}s）`

### Task 4: 登录事务抽取 `PlayerLoginFlow` + 幽灵订阅修复

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Game/Network/PlayerLoginFlow.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinPlayerList.cs`

**Interfaces:**
- Produces: `public static class PlayerLoginFlow { public static async UniTask<RespPlayerLogin> EnterGameAsync(long playerId, CancellationToken cancellationToken) }`——重入防护（`private static bool s_isEntering`，try/finally 复位；重入抛 `InvalidOperationException`）→ `SuspendNotifyDispatch()` → `Call(new ReqPlayerLogin { Id = playerId })` → `PlayerManager.Instance.PlayerInfo = resp.PlayerInfo` → `ResumeNotifyDispatch()` → 返回 resp；**catch 收尾 `ClearNotifyBuffer()` + `ResumeNotifyDispatch()` 后 rethrow**（Review Focus 4：阀门不留残留）。Task 7 消费。
- 说明：`PlayerManager` 现居 `Manager_ToDelete/`（过渡目录），按现状使用，不迁移。

- [ ] **Step 1: 实现 PlayerLoginFlow**

按 Interfaces 契约直实现；`cancellationToken` 透传进 `Call` 的等待链（`NetworkModule.Call` 现无 ct 参数——用 `.AttachExternalCancellation(cancellationToken)` 挂到返回的 UniTask 上，取消时抛 `OperationCanceledException`）。

- [ ] **Step 2: 改造 WinPlayerList（挪订阅 + 换事务 + 生命周期 CTS）**

三处改动：
1. **订阅挪位**：`InitEvent()` 里的 `Subscribe(NetworkConnectedEventArgs.EventId, ...)` / `Subscribe(NetworkClosedEventArgs.EventId, ...)` 挪到 `OnOpen()`（池化窗口每次打开重订），`OnClose()` 调 `UnSubscribeAll()`（`WinBase.EventRegister.cs:91` 现成 API）——修复幽灵订阅（Review Focus 2）。UI 监听（`AddUIListener`）留 `OnInit` 不动。
2. **生命周期 CTS**：字段 `private LifecycleCancellationSource m_lifetimeCts;`——`OnOpen()` 创建、`OnClose()` `Cancel` 后置 null；`OnNetworkConnected` 里 `PlayerLoginFlow` 用 `m_lifetimeCts.Token`。
3. **换事务**：`PlayerLoginAsync` 方法体替换为 `await PlayerLoginFlow.EnterGameAsync(m_selectedPlayerInfo.Id, m_lifetimeCts.Token)` 后接原有 `OpenAsync<WinMain>` + `Close(this)`；原阀门/Call/赋值代码全部删除（已入 Flow）；`catch` 里原 `ClearNotifyBuffer(); throw;` 删除（Flow 已做），保留向上抛。

- [ ] **Step 3: 编译门**

Expected: `errorCount: 0`。

- [ ] **Step 4: Editor 手测（首次登录回归）**

Play：正常登录全流程（选角色 → 进主界面 → 背包打开）行为不变；登录窗口二次打开（返回后再进）事件仍触发。

- [ ] **Step 5: 提交（先询问用户）**

commit `[AI]refactor: 登录事务抽为 PlayerLoginFlow（阀门收尾兜底）+ WinPlayerList 订阅生命周期修复`

### Task 5: 最小登出流程 `LogoutFlow`

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Game/Network/LogoutFlow.cs`

**Interfaces:**
- Consumes: Task 1 的 `Close(Active)`（`DestroyChannel` → `Dispose` → `Close(Active)` 已在 Task 1 标注）；Task 6 将引用其取消重连的前置钩子。
- Produces: `public static class LogoutFlow { public static async UniTask LogoutAsync(CancellationToken cancellationToken) }`——顺序：`ReconnectManager.Instance.CancelReconnect()`（Task 6 提供，本任务先按此签名调用，Task 6 落地）→ `PlayerManager.Instance.PlayerInfo = null` → `NetworkModule.Instance.ClearNotifyBuffer()` → `NetworkModule.Instance.DestroyChannel()` → `GlobalModule.UIModule.CloseAll()` → `await GlobalModule.UIModule.OpenAsync<WinLogin>()`。Task 6/7 消费。

- [ ] **Step 1: 实现 LogoutFlow**

按顺序直实现。`CloseAll()` 后 `OpenAsync<WinLogin>`——常驻窗口（如 `WinGlobalLoading`）若被误关，手测暴露后改用逐窗口关闭或豁免清单（builder 现场决定，注释说明）。

- [ ] **Step 2: 编译门**

Expected: `errorCount: 0`（`CancelReconnect` 此刻可为 `ReconnectManager` 上的空壳方法——Task 6 补实体；若不想提前建 `ReconnectManager` 空类，可本任务先不调用该行，Task 6 补）。

- [ ] **Step 3: 提交（先询问用户）**

commit `[AI]feat: 最小登出流程 LogoutFlow（清状态+关频道+回登录界面）`

### Task 6: `ReconnectManager`——事件分流 + 被顶标记

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Game/Network/ReconnectManager.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs`（约 :150 游戏服务装配区加 `ReconnectManager.Instance.Initialize();`）

**Interfaces:**
- Consumes: Task 1 `NetworkClosedEventArgs.CloseReason`；Task 4 `PlayerLoginFlow`（间接）；Task 5 `LogoutFlow`。
- Produces（Task 7 消费）:
  - `public class ReconnectManager : Singleton<ReconnectManager>`；`Initialize()` 订阅 `NetworkClosedEventArgs`；`Dispose()` 退订（若项目 Singleton 无 Dispose 惯例则提供 `Uninitialize()`）。
  - `public void CancelReconnect()`——Cancel `m_reconnectCts`。
  - `private async UniTaskVoid StartReconnectFlow(Uri address)`——**本任务空壳 + TODO 注释**（Task 7 填充），保证编译与事件链完整。
  - `[NotifyMessageHandler(typeof(NotifyPrompt), nameof(OnNotifyPrompt))] internal void OnNotifyPrompt(NotifyPrompt msg)`——`msg.Type == 5` 时置 `m_isKickedByTopLogin = true` 并记录 `msg.Content`（其余 Type 忽略不动——现有行为为零，不扩散）。
  - `OnNetworkClosed(object sender, GameEventArgs e)` 分流（**顺序敏感**，Review Focus 1/3）：`reason != Lost` → 忽略；`PlayerManager.Instance.PlayerInfo == null`（未进游戏）→ 忽略；`m_isKickedByTopLogin` → 重置标记 → 弹 `WinDialogMessageBox`（Content 兜底文案"您的账号已在其他设备登录"）→ `LogoutAsync`；否则 → 重置 `m_reconnectCts` 为新 `LifecycleCancellationSource` → `StartReconnectFlow(address)`（address = `new Uri($"tcp://{WinPlayerList.serverIp}:{WinPlayerList.serverPort}")`，现状公开静态配置，最省；将来多服再改登记制）。
  - 重入防护：`StartReconnectFlow` 启动前若 `m_reconnectCts != null && !m_reconnectCts.IsCancellationRequested` → 直接返回（已有事务在跑）。

- [ ] **Step 1: 实现 ReconnectManager**

按 Interfaces 直实现。`OnNotifyPrompt` 标注 `[NotifyMessageHandler]` 后须完成 Step 3 的注册表再生。

- [ ] **Step 2: HotfixLauncher 接线**

`ReconnectManager.Instance.Initialize();` 放在 `LocalizationModule` 接线（:150）同区域。

- [ ] **Step 3: 再生推送注册表**

Run: unity-cli `execute_menu_item` 菜单 `FuFramework/网络/重新生成推送注册表`（或 `Protobuf/gen-proto-registry.bat`）。Expected: `MessageRegistry.gen.cs` 出现 `BindNotify(typeof(NotifyPrompt), ...)` 条目；编译期守卫无报错。

- [ ] **Step 4: 编译门**

Expected: `errorCount: 0`。

- [ ] **Step 5: 提交（先询问用户）**

commit `[AI]feat: ReconnectManager 事件分流与被顶标记（NotifyPrompt Type=5 不自动重连）`

### Task 7: 重连事务编排 + 转圈框 UI

**前置（人工，非代码）：** 用户在 FGUI 编辑器 Common 包新建 `WinReconnecting` 组件（已定案：按钮居中排布局），导出并跑 CSharpCodeGen 生成 `Gen` 部分。**资源未就绪前本任务只能做到 Step 2。**

制作规格（层次自底向上）：① 全屏纯黑遮罩图（透明度 ~60%，根容器填满全屏以阻挡底层点击）；② 转圈动画（屏幕中偏上，FGUI Transition 做 rotation 循环，命名 `spin`）；③ `txtTitle`"重连中..."（较大字号）；④ `txtDesc`"网络不稳定，正在尝试重新连接..."（小一号灰色）；⑤ `btnCancel`"取消"（居中，间隔 ~100px）。**命名必须一致**（代码按 `btnCancel`/`txtTitle`/`txtDesc`/`spin` 接线，对照 `WinDialogMessageBox` 的 btn/txt 惯例）。

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Game/UI/Common/WinReconnecting.cs`（手写层：`OnInit` 绑 `btnCancel.onClick → ReconnectManager.Instance.CancelReconnect()`；**全屏模态遮罩**——Review Focus 5）
- Modify: `Unity/Assets/Scripts/Hotfix/Game/Network/ReconnectManager.cs`（填充 `StartReconnectFlow`）

**Interfaces:**
- Consumes: Task 3 `ReconnectAsync` / `ReconnectOptions`；Task 4 `PlayerLoginFlow.EnterGameAsync`；Task 5 `LogoutFlow.LogoutAsync`；Task 6 全部。

- [ ] **Step 1: WinReconnecting 窗口类**

FGUI Gen 生成后补手写层。取消按钮回调：`CancelReconnect()`（窗口关闭由事务的取消路径统一处理）。

- [ ] **Step 2: 填充 StartReconnectFlow**

```csharp
// 事务形状（设计稿 §6 定案；具体语句 executor 依现有 API 落）
private async UniTaskVoid StartReconnectFlow(Uri address)
{
    var playerId = PlayerManager.Instance.PlayerInfo.Id;   // 断线未登出，PlayerInfo 仍在
    await GlobalModule.UIModule.OpenAsync<WinReconnecting>();
    try
    {
        await NetworkModule.Instance.ReconnectAsync(
            new ReconnectOptions { Address = address }, m_reconnectCts.Token);
        await PlayerLoginFlow.EnterGameAsync(playerId, m_reconnectCts.Token);
        GlobalModule.UIModule.CloseAll();
        await GlobalModule.UIModule.OpenAsync<WinMain>();
    }
    catch (OperationCanceledException) { await LogoutFlow.LogoutAsync(m_reconnectCts.Token); }
    catch (Exception)
    {
        GlobalModule.UIModule.CloseAll();
        GlobalModule.UIModule.OpenAsync<WinDialogMessageBox>(new WinDialogMessageBox.DialogMessageBoxData(
            "重连失败，请重试或返回登录。",
            onEnter:  () => StartReconnectFlow(address).Forget(),   // 再试一次：从拨号重头，计数天然重置
            onCancel: () => LogoutFlow.LogoutAsync(CancellationToken.None).Forget())).Forget();
    }
    finally { GlobalModule.UIModule.Close(typeof(WinReconnecting)); /* 按项目关窗 API 落地 */ }
}
```

- [ ] **Step 3: 编译门**

Expected: `errorCount: 0`。

- [ ] **Step 4: Editor 手测（需双端）**

① 进游戏后断网（拔线/关服务器）→ 转圈框出现且全屏模态（点不到主界面）→ 恢复网络 → 自动回主界面，无选角闪现；② 断网不恢复 → 5 次退避（间隔 2/3/5/8/13s 看日志）→ 失败框两按钮各自正确；③ 重连中点取消 → 回登录界面；④ 转圈框弹出期间连点主界面按钮 → 无异常日志。

- [ ] **Step 5: 提交（先询问用户）**

commit `[AI]feat: 重连事务编排与重连中转圈框（自动重登回主界面/失败交玩家手选）`

### Task 8: 端到端验收 + 文档与记忆同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/README.md`（组件表加 `ReconnectOptions`；目录树加 `NetworkModule.Reconnect.cs`；API 示例加重连段）
- Modify: `Docs/网络模块学习笔记/04-频道核心.md`（新增「重连」小节：三件套 + 编排链路 + 被顶标记）；`Docs/网络模块学习笔记/README.md`（核心文件清单补 `ReconnectManager`）
- 记忆：`network-single-channel-model.md` 追加重连落地条目；MEMORY.md 无需新行

- [ ] **Step 1: 端到端验收（设计稿 §9 全 7 条）**

逐条执行并记录结果：①断线自动重连回主界面；②5 次退避后失败框；③正常登出不弹框（用 `LogoutFlow` 实测）；④登录失败缓冲不回放；⑤首次登录回归；⑥被顶号弹提示不重连（双开客户端顶号实测）；⑦拨通后无第二次登录事务（日志验证单事务）。

- [ ] **Step 2: 文档与记忆更新**

按 Files 列表同步；笔记小节保持既有行文风格（What/Why/How + 自测题可选）。

- [ ] **Step 3: 编译门 + 提交（先询问用户）**

commit `[AI]docs: 重连机制文档与笔记同步`
