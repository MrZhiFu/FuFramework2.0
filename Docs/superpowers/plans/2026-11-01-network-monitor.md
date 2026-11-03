# 网络监控器实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 NetworkModule 建立网络监控器调试面板：TCP 链路（请求/响应/推送）的实时列表、筛选搜索、详情（JSON 折叠/Hex）、暂停清空、消息重发（默认关总开关）。

**Architecture:** Hotfix 层新增 `NetworkMonitor` 静态采集器（`#if UNITY_EDITOR` 不进包，循环队列 2000 条，埋点纯增量不动语义）+ 收发链路三处埋点 + RPC 三终点回写；Editor 层 `DebugWindowBase` 体系新建主窗口与三个子窗口，经 `HotfixReflection` 反射访问（Editor 程序集不引用 HotFix 程序集的既定模式）。

**Tech Stack:** Unity IMGUI / HybridCLR 双程序集（Editor 反射访问 Hotfix）/ UniTask / `UtilityAOT.Json`。

**Spec:** `Docs/superpowers/specs/2026-11-01-network-monitor-design.md`

## Global Constraints

- 全部监控代码 `#if UNITY_EDITOR` 条件编译：**定义与调用处都包**（Build 后零残留，验收 7）。
- 埋点为纯增量调用，不改收发/超时/取消任何既有语义。
- 项目铁律：UniTask（无原生 Task/协程）、无 LINQ、Hotfix 层无反射（Editor 面板反射是既定模式）。
- `MessageObject.UniqueId` 是 **int**（非 long）。
- 缩进用 Tab，注释密度对齐现有代码，FuLogger 记日志。
- 提交规范 `Docs/Git提交规范.md`（`[AI]feat:` 前缀）；**执行时每次提交前先问用户**（用户纪律）。
- 编译验证统一命令：`unity-cli tool call get_compilation_state`（期望 `errorCount: 0`；改 C# 后先 `unity-cli tool call refresh_assets` 触发刷新）。

## Review Focus

规格暗示但任务测试不覆盖、最可能咬人的五类输入（每条已在对应任务加了钉注测试）：

1. **断线瞬间的挂起请求**：`RpcState.Dispose` 取消后，列表中该请求应显示「取消」而非永久悬留「已发待回」→ Task 2 步骤 5 钉注。
2. **环形队列滚动后回写找不到原记录**：请求已滚出 2000 条后才收到响应/超时，回写必须静默忽略不抛异常 → Task 1 步骤 5 钉注。
3. **接收线程并发录制**：解码回调在线程池线程高频调 `RecordReceived`，锁内做环形写入 + pending 表更新，不得死锁/竞态 → Task 2 步骤 5 钉注（Play 模式高压观察）。
4. **心跳豁免边界**：纯保活心跳（`IHeartBeatMessage` 且非 `INotifyMessage`）不产生记录；`NotifyHeartBeat`（双实现）必须出现 → Task 2 步骤 5 钉注。
5. **巨型消息截断**：JSON 超 32KB / 包体超 64KB 截断后，详情查看不炸、标注「已截断」→ Task 1 步骤 5 钉注。

---

### Task 1: 数据层——NetworkMessageRecord 与 NetworkMonitor 采集器

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Network/Monitor/NetworkMessageRecord.cs`
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Network/Monitor/NetworkMonitor.cs`

**Interfaces:**
- Consumes: `MessageObject`（`UniqueId` int、`GetType().Name`）、`MessageIdRegistry`、`UtilityAOT.Json.ToJson(object)`、`FuLogger`
- Produces（后续任务依赖的确切签名，全部 `public static`，类整体 `#if UNITY_EDITOR`）：
  - `enum ENetworkMessageKind { Request, Response, Notify }`
  - `enum ENetworkMessageStatus { Sent, Replied, Timeout, Canceled, Error }`
  - `sealed class NetworkMessageRecord { long Seq; ENetworkMessageKind Kind; DateTime TimeUtc; int UniqueId; int MessageId; string MessageName; ENetworkMessageStatus Status; long ElapsedMs; byte[] BodyBytes; int BodySize; string JsonSnapshot; bool IsResend; }`
  - `static class NetworkMonitor`：
    - `bool Enabled { get; private set; }` / `void SetEnabled(bool enabled)`（开启瞬间清历史）
    - `bool IsPaused { get; set; }`
    - `void RecordRequest(MessageObject msgObj, int messageId, byte[] bodyBytes)`
    - `void RecordReceived(MessageObject msgObj, int messageId, byte[] bodyBytes)`
    - `void OnRpcSettled(int uniqueId, ENetworkMessageStatus status, long elapsedMs)`
    - `NetworkMessageRecord[] GetSnapshot()`
    - `void Clear()`
    - `bool ResendEnabled { get; private set; }` / `void SetResendEnabled(bool)`
    - `bool Resend(long seq, string jsonBody)`（本任务只做编排骨架：seq→记录→类型反查→JSON 反序列化→调 `NetworkModule.Instance.ResendForMonitor(msgObj)`，该方法 Task 3 提供——本任务先留 `// Task 3 接入` 注释返回 false）

- [ ] **Step 1: 写 `NetworkMessageRecord.cs`**

枚举两枚 + 密封类，纯数据载体。`BodySize => BodyBytes?.Length ?? 0`。类注释写明「Record 只存生成好的显示数据，绝不持有消息对象引用（规避池化/生命周期问题）」。

- [ ] **Step 2: 写 `NetworkMonitor.cs` 采集器**

实现要点（照 `WebModule.Debug.cs` 既有模式）：
- 循环队列：`NetworkMessageRecord[2000]` + `m_head` + `m_count`（环形覆盖最旧）；逐槽 `default` 清理（`Clear` 与满覆盖时释放字符串/字节引用）。
- `object` 锁保护（收发跨线程）；锁内不做 JSON 序列化（序列化在调用方线程先做完再进锁）。
- 自增序号 `s_seq`（long）。
- pending 表：`Dictionary<int, long> m_pendingRequestSeqs`（UniqueId→请求记录 Seq）；`OnRpcSettled` 按 UniqueId 查 Seq → 环形队列内定位该记录回写 `Status`/`ElapsedMs`；**查不到静默 return**（记录已滚出）。
- `RecordRequest`：纯保活心跳排除（`msgObj is IHeartBeatMessage && msgObj is not INotifyMessage` 直接 return）→ 生成 JSON（截断 32KB 标注）→ 拷贝字节（截断 64KB 标注）→ 进环形 + 登记pending。状态 `Sent`。
- `RecordReceived`：同一心跳排除 → `msgObj is IResponseMessage` ? 建 `Response` 记录（Status 恒 `Replied`）: 建 `Notify` 记录。
- `Resend` 编排：`ResendEnabled` false 返回 false → 按 seq 找原请求记录 → `MessageIdRegistry.GetTypeById` 反查类型（null 返回 false）→ `UtilityAOT.Json.ToObject(type, jsonBody)` 转 `MessageObject`（异常捕获记 `Error` 记录返回 false）→ `UniqueId` 保持新分配值（不重置）→ 调 `NetworkModule.Instance.ResendForMonitor(msgObj)`（Task 3 接入，先留注释）。

- [ ] **Step 3: 编译验证**

Run: `unity-cli tool call refresh_assets && unity-cli tool call get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Network/Monitor/
git commit -m "[AI]feat: 网络监控器数据层（NetworkMessageRecord/NetworkMonitor 循环队列采集器）"
```
（执行时先问用户）

- [ ] **Step 5: Review Focus 钉注自查**

确认三处已实现：环形滚动后 `OnRpcSettled` 查不到静默忽略（Review Focus 2）、JSON/字节截断标注（Review Focus 5）、心跳排除条件（Review Focus 4 的数据侧）。

### Task 2: 埋点——发送/接收/RPC 三终点

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelHelper.cs`（`SerializeAndWriteToCacheStream`，约 209 行 `SerializerHelper.Serialize` 之后、215 行压缩之前）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/SystemTcpNetworkChannel.cs`（`ProcessPackBody`，`msgObj.SetUniqueId(...)` 之后、入队之前）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/State/RpcState.cs`（`TryReply` 配对成功处 / `Update` 超时移除循环 / `Dispose` 取消循环）

**Interfaces:**
- Consumes: Task 1 的 `NetworkMonitor.RecordRequest / RecordReceived / OnRpcSettled`
- Produces: 无（纯增量调用，不改任何既有语义）

- [ ] **Step 1: 发送埋点**

`SerializeAndWriteToCacheStream` 内，`msgBodyBuffer = SerializerHelper.Serialize(msgObj);` 之后、`if (msgBodyBuffer.Length > LIMIT_COMPRESS_LENGTH)` 之前插入：

```csharp
#if UNITY_EDITOR
			// 网络监控器发送埋点：抓压缩前的原始包体字节（与接收侧解压后字节对称，Hex 视图可与 JSON 对照）
			NetworkMonitor.RecordRequest(msgObj, m_currentSentId, msgBodyBuffer);
#endif
```

- [ ] **Step 2: 接收埋点**

`ProcessPackBody` 内，`msgObj.SetUniqueId(...)` 块之后、`#if ENABLE_NETWORK_RSP_LOG` 之前插入：

```csharp
#if UNITY_EDITOR
			// 网络监控器接收埋点：解码点成条——LastReceivedMessageId 是解码线程更新的单字段，
			// 主线程派发时读会错位；此处 msgObj/消息号/未压缩包体一把抓全，Seq 反映到达序
			NetworkMonitor.RecordReceived(msgObj, m_NetworkChannelHelper.LastReceivedMessageId, buffer);
#endif
```

- [ ] **Step 3: RPC 三终点回写**

- `TryReply`：`m_waitingReplyDict.TryRemove(...)` 成功分支内（`Reply` 前）插 `#if UNITY_EDITOR` 包裹的 `NetworkMonitor.OnRpcSettled(msgObj.UniqueId, ENetworkMessageStatus.Replied, rpcMessageData.ElapseTime);`
- `Update` 超时移除循环：移除处插 `#if UNITY_EDITOR` 包裹的 `NetworkMonitor.OnRpcSettled(objectId, ENetworkMessageStatus.Timeout, m_waitingReplyDict[objectId].ElapseTime);`（注意在 `TryRemove` 前取值，避免竞态；实现时以实际循环结构为准保持最小侵入）
- `Dispose` 取消循环：`Cancel()` 处插 `#if UNITY_EDITOR` 包裹的 `NetworkMonitor.OnRpcSettled(uniqueId, ENetworkMessageStatus.Canceled, rpcMessageData.ElapseTime);`（foreach 变量名以现有代码为准）

- [ ] **Step 4: 编译验证**

Run: `unity-cli tool call refresh_assets && unity-cli tool call get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 5: Play 模式冒烟（可选，服务器可用时）**

打开网络监控器前置条件尚未建（Task 5），先用临时日志替代：`SetEnabled(true)` 后登录一次，`GetSnapshot().Length > 0` 且含 Request/Response 两种 Kind；纯心跳无记录、`NotifyHeartBeat` 有记录（Review Focus 4）。无法实测则留待 Task 7 验收。

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelHelper.cs Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/SystemTcpNetworkChannel.cs Unity/Assets/Scripts/Hotfix/Framework/Network/State/RpcState.cs
git commit -m "[AI]feat: 网络监控器埋点（发送压缩前字节/解码点成条/RPC 三终点回写）"
```
（执行时先问用户）

### Task 3: 重发链路——非泛型辅助 + Resend 接入

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Network/NetworkModule.Monitor.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Monitor/NetworkMonitor.cs`（Resend 编排里 Task 1 留的 `// Task 3 接入` 注释替换为真实调用）

**Interfaces:**
- Consumes: `NetworkChannelBase.API.cs` 的私有 `m_rpcState`（同 partial 类可访问）、`NetworkModule` 现有频道访问方式（照 `NetworkModule.API.cs` 中 Call 转发取频道的写法）、Task 1 `NetworkMonitor.Resend` 骨架
- Produces:
  - `NetworkModule` 新增（`#if UNITY_EDITOR` 包裹，`internal`）：`internal void ResendForMonitor(MessageObject msgObj)` —— 取当前频道，频道未连接时记 `Error` 记录并 return；否则 `m_currentChannel.CallForMonitor(msgObj).Forget()` 异常落入既有记录（响应/超时/取消经 RpcState 终点自然入列表）。频道类上补 `internal async UniTask<IResponseMessage> CallForMonitor(MessageObject msgObj)`：`Send(msgObj); return await m_rpcState.Call(msgObj);`（放 `NetworkChannelBase.API.cs` 末尾，`#if UNITY_EDITOR` 包裹）
  - `NetworkMonitor.Resend` 闭环可用

- [ ] **Step 1: 写 `NetworkChannelBase.API.cs` 末尾的 `CallForMonitor`**

`#if UNITY_EDITOR` 包裹，方法注释写明「仅供 NetworkMonitor 重发使用：运行时 Type 无法调泛型 Call，此为非泛型等价路径（Send + 挂表），UniqueId 沿用消息对象新分配值」。

- [ ] **Step 2: 写 `NetworkModule.Monitor.cs`**

`public sealed partial class NetworkModule`（对齐 NetworkModule.Reconnect.cs 的 partial 声明方式），`#if UNITY_EDITOR` 整体包裹。`ResendForMonitor` 内部：取频道（空/未连接→ `FuLogger.LogWarning` + return）→ `CallForMonitor(msgObj).Forget()`。取频道的具体写法照 `NetworkModule.API.cs` 现有 Call 转发实现（实现者读该文件）。

- [ ] **Step 3: NetworkMonitor.Resend 接线**

`// Task 3 接入` 注释处替换为 `NetworkModule.Instance.ResendForMonitor(msgObj); return true;`。重发请求的记录随发送埋点自然产生（`IsResend` 标记在 Resend 编排中构造记录时设置——注意发送埋点产生的是新记录，IsResend 无法从埋点侧得知：改为 Resend 编排里 `NetworkMonitor.MarkNextResend()` 置一次性标志，`RecordRequest` 消费后复位）。

- [ ] **Step 4: 编译验证**

Run: `unity-cli tool call refresh_assets && unity-cli tool call get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 5: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Network/NetworkModule.Monitor.cs Unity/Assets/Scripts/Hotfix/Framework/Network/Monitor/NetworkMonitor.cs Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelBase.API.cs
git commit -m "[AI]feat: 网络监控器重发链路（非泛型 CallForMonitor + Resend 编排闭环）"
```
（执行时先问用户）

### Task 4: Editor 基座三小改

**Files:**
- Modify: `Unity/Assets/Editor/FuFramework/Common/Debug/DebugWindowBase.cs`（`OnEnable`/`OnDisable` 末尾各加一行虚调用）
- Modify: `Unity/Assets/Editor/FuFramework/Common/Debug/HotfixReflection.cs`（加一个属性）
- Modify: `Unity/Assets/Editor/FuFramework/Common/FuMenuPriority.cs`（`DEBUG_PANEL_TIMER` 之后加一段）

**Interfaces:**
- Produces:
  - `DebugWindowBase`：`protected virtual void OnPanelEnabled() { }` / `protected virtual void OnPanelDisabled() { }`（基类 `OnEnable` 末尾调前者、`OnDisable` 末尾调后者——注意基类 OnEnable/OnDisable 是 private，本改动是唯一介入点）
  - `HotfixReflection.NetworkMonitor`（`public static System.Type`，`Resolve("Hotfix.Framework.Network.NetworkMonitor")`）
  - `FuMenuPriority.DEBUG_PANEL_NETWORK = DEBUG_PANEL_BASE + 7`

- [ ] **Step 1: 三处小改**（每个改动一行到数行，虚方法默认空实现，7 个现有面板零影响）

- [ ] **Step 2: 编译验证**

Run: `unity-cli tool call refresh_assets && unity-cli tool call get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Editor/FuFramework/Common/Debug/DebugWindowBase.cs Unity/Assets/Editor/FuFramework/Common/Debug/HotfixReflection.cs Unity/Assets/Editor/FuFramework/Common/FuMenuPriority.cs
git commit -m "[AI]feat: 调试面板基座扩展（面板开闭钩子/网络监控器菜单段位/反射登记）"
```
（执行时先问用户）

### Task 5: 主窗口 NetworkModuleWindow

**Files:**
- Create: `Unity/Assets/Editor/FuFramework/Network/NetworkModuleWindow.cs`

**Interfaces:**
- Consumes: Task 1 `NetworkMonitor` 全部查询接口、Task 4 三基座扩展
- Produces: 主窗口（Tab/统计/列表/详情区/工具栏操作/重发开关）；子窗口打开入口（传值方式：子窗口公开 `static` 接收字段，Task 6 定义）

- [ ] **Step 1: 写窗口骨架与反射层**

`sealed class NetworkModuleWindow : DebugWindowBase`，命名空间 `FuFramework.Network.Editor`，菜单 `FuFramework/调试/网络监控器` + `FuMenuPriority.DEBUG_PANEL_NETWORK`，`minSize (800, 600)`，居中打开（照 ReferencePoolModuleWindow）。覆写：`ModuleDisplayName => "NetworkMonitor"`、`RefreshInterval => 0.1f`、`HasExpandCollapse => false`、`OnPanelEnabled/OnPanelDisabled`（反射调 `NetworkMonitor.SetEnabled(true/false)`）。`EnsureReflection` 绑定：`Enabled/IsPaused/GetSnapshot/Clear/ResendEnabled/SetResendEnabled/Resend` 的 Property/Method（照 ReferencePoolModuleWindow 的反射缓存模式，`HotfixReflection.NetworkMonitor` 取类型）；`ResetReflection` 全清。窗口关闭即停录制。

- [ ] **Step 2: 概览区与工具栏**

`DrawOverview`：Tab 按钮 5 枚（全部/请求/响应/推送/错误，`EditorStyles.toolbarButton` 高亮当前）；统计摘要一行（总/请求/响应/推送/超时取消计数，手写循环数）；**重发总开关 Toggle**（默认关，`EditorPrefs` 键 `"NetworkMonitor_ResendEnabled"` 持久化，`OnEnable` 读取并反射 `SetResendEnabled`）。`DrawToolbarExtraButtons`：【暂停/继续】Toggle（反射 `IsPaused`）+【清空】按钮（反射 `Clear()`）。

- [ ] **Step 3: 虚拟化消息列表**

`DrawContent`：照参考工具 DisplayRow 拍平方案——筛选+搜索（基类 `m_searchFilter`，匹配 MessageName/MessageId 字符串/UniqueId 字符串，忽略大小写）→ 全量记录按 Tab 过滤 → **窗口侧忽略过滤**（按 MessageId 忽略展示，`HashSet<int>`，`EditorPrefs` 键 `"NetworkMonitor_IgnoredMessages"` 持久化，详情区或行右键菜单提供「忽略此消息号」，同参考工具）→ 只绘制可见行（`GUILayout` 行高 25px，`scrollPos.y` 推算首行索引）。列：序号/时刻(HHmmss.fff)/类型/消息名/UniqueId/状态/耗时/大小，列间 `DrawColumnSeparator()`。颜色：请求橙红 `#EE4000`、响应黄绿 `#B3EE3A`、推送粉红 `#F709C7`、状态非 Replied/Sent 标红。点击行选中（`_selectedIndex`），双击或选中即刷新详情区。

- [ ] **Step 4: 详情区**

列表下方固定高度区（`EditorGUILayout.BeginVertical("box")` + 内嵌滚动）：选中记录的基本信息（字段逐行 LabelField）+ 三按钮：【查看 JSON 结构】→ `NetworkJsonViewWindow.Show(json, title)`；【查看二进制数据】→ `NetworkHexViewWindow.Show(bytes, title)`（两者 Task 6 提供，本任务先以 `// Task 6 接入` 注释占位）；【再次发送】（仅 `Kind == Request` 且 `ResendEnabled` 时可用，否则禁用+提示）→ `NetworkResendWindow.Show(record)`。

- [ ] **Step 5: 编译验证**

Run: `unity-cli tool call refresh_assets && unity-cli tool call get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Editor/FuFramework/Network/NetworkModuleWindow.cs
git commit -m "[AI]feat: 网络监控器主窗口（Tab 筛选/虚拟化列表/详情区/重发总开关）"
```
（执行时先问用户）

### Task 6: 子窗口三件套

**Files:**
- Create: `Unity/Assets/Editor/FuFramework/Network/NetworkJsonViewWindow.cs`
- Create: `Unity/Assets/Editor/FuFramework/Network/NetworkHexViewWindow.cs`
- Create: `Unity/Assets/Editor/FuFramework/Network/NetworkResendWindow.cs`

**Interfaces:**
- Consumes: Task 1 `NetworkMonitor.Resend`、Task 5 的占位调用点
- Produces:
  - `NetworkJsonViewWindow.Show(string json, string title)`（static 入口，`EditorWindow.GetWindow` 显示；`{}`/`[]` 容器行折叠展开，行首箭头，复制全文按钮）
  - `NetworkHexViewWindow.Show(byte[] bytes, string title)`（每行 16 字节：偏移 + Hex + ASCII 混排，复制按钮）
  - `NetworkResendWindow.Show(object record)`（反射读记录的 MessageName/MessageId/JsonSnapshot；JSON 编辑区 `TextArea`；确认按钮：`ResendEnabled` 关闭时禁用并提示，开启时反射调 `Resend(seq, json)`，提示「已发起，结果见列表」）
  - 接线：替换 Task 5 详情区的 `// Task 6 接入` 占位为真实调用

- [ ] **Step 1: JSON 折叠查看器**

解析 JSON 文本为行树（手写轻量解析：按 `{}`/`[]` 配对缩进分层，不引第三方库）；每行一个 Foldout 式按钮行（容器行可折叠，叶子行直接显示）；`OnGUI` 只绘制可见区（复用 Task 5 拍平思路）；标题栏带【复制全文】。

- [ ] **Step 2: Hex 查看器**

标准 Hex 视图：每行偏移(8 位 hex) + 16 字节 Hex + 右侧 ASCII（不可见字符 `.`）；超长滚动；【复制】导出纯 Hex 文本。

- [ ] **Step 3: 重发窗口**

只读信息区（消息名/消息号/原序号）+ 可编辑 `TextArea`（预填 `JsonSnapshot`）+ 确认/取消。确认：反射 `Resend(seq, editedJson)` 返回 bool——false 时 `EditorUtility.DisplayDialog` 提示原因（开关未开/类型查不到/JSON 非法）；true 时关窗并提示结果走列表。

- [ ] **Step 4: 接线与编译验证**

替换 Task 5 两处占位；Run: `unity-cli tool call refresh_assets && unity-cli tool call get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 5: Commit**

```bash
git add Unity/Assets/Editor/FuFramework/Network/NetworkJsonViewWindow.cs Unity/Assets/Editor/FuFramework/Network/NetworkHexViewWindow.cs Unity/Assets/Editor/FuFramework/Network/NetworkResendWindow.cs Unity/Assets/Editor/FuFramework/Network/NetworkModuleWindow.cs
git commit -m "[AI]feat: 网络监控器子窗口（JSON 折叠视图/Hex 视图/重发编辑窗口）"
```
（执行时先问用户）

### Task 7: 端到端验收 + 文档同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/README.md`（新增「4.9 网络监控器」小节）
- Modify: 本计划勾选 + 记忆同步（会话内完成，不占代码提交）

**Interfaces:**
- Consumes: 全部前序任务

- [ ] **Step 1: Play 模式实测（需用户配合启动服务器）**

对照设计稿 §11 七条逐项验收：
1. 登录流程中 Request/Response/Notify 三类实时入列，UniqueId 配对、耗时正确
2. 停服断线后未回请求显示超时/取消；重连后新会话继续记录（Review Focus 1）
3. 暂停冻结/继续恢复/清空后重新录制冷启动正常
4. JSON 折叠视图展开折叠复制正常；Hex 与重新序列化字节一致
5. 重发开关关→按钮禁用；开→改 JSON 字段重发，列表出现 IsResend 新请求+对应响应（Review Focus 5 顺带：构造大响应看截断）
6. 关窗后录制停止（日志无新增）；重开历史清空重新录制
7. `ENABLE_NETWORK_REQ_LOG` 宏对照：录制开启时既有调试日志行为不受影响
（高压场景顺带验证 Review Focus 3：重连回放/推送爆发时列表滚动正常不卡死）

- [ ] **Step 2: README 同步**

`Network/README.md` 4.8 之后加 4.9：监控器开启方式（菜单+Play 模式）、录制开关语义（面板开才录）、三类消息与 RPC 三终点状态流转、重发总开关、心跳豁免边界（纯保活不记、校时通知记录）、Editor-only 不进包。

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Network/README.md
git commit -m "[AI]docs: 网络模块 README 新增 4.9 网络监控器章节"
```
（执行时先问用户）
