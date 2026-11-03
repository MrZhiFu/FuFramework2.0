# 网络调试面板设计稿

> 日期：2026-11-01
> 状态：待审批
> 功能参考：`C:\Users\Administrator\Desktop\网络调试面板`（SLG 项目整建制工具，2362 行/7 文件）
> 类结构参考：`Unity/Assets/Editor/FuFramework/ReferencePool/ReferencePoolModuleWindow.cs`（DebugWindowBase 反射面板体系）

## 1. 意图与背景

为 NetworkModule（TCP 链路）建立**网络调试面板**：实时查看运行时的消息收发历史，支撑联调排错（请求发出去没回来、耗时在哪、错误码是什么、推送有没有到）。

桌面的网络调试面板是从 SLG 项目搬来的完整工具（列表/筛选/详情/Hex/重发/HTTP 六大功能），但其接入点是 SLG 体系（ProtoMap/SendAndReceiveAsync），不能直接使用——本设计将它的**UI 形态与功能集**映射到本项目的框架上，**类结构**则落在本项目现成的 DebugWindowBase 面板体系里。

### 1.1 用户拍板的决策

| 决策点 | 结论 |
|---|---|
| 核心用途 | **调试排错为主**（兼顾学习观察） |
| 功能档位 | **全量照搬**：列表 + 筛选搜索 + 详情 + 暂停清空 + 消息重发 |
| 监控范围 | **请求/响应 + 推送（Notify）**（参考工具只有 Req/Resp） |
| HTTP 监控 | **不含**（后定：`WebModuleWindow` 已完整覆盖 HTTP 调试——统计/实时快照/最近记录/手动测试请求/急救，避免两面板重复展示同一份数据；详见 §8） |
| 重发安全 | 初期定默认关总开关，**验收期推翻：默认可用无开关**（Editor 开发工具本就只在开发环境用，见 §6 决策演变） |

## 2. 架构总览

**关键约束**：`Unity.Editor.asmdef` 不引用 HotFix（热更程序集）——Editor 面板只能经 `HotfixReflection` 反射访问 Hotfix 侧数据。这是本项目全部 7 个调试面板的既定模式，也是"类结构参考引用池面板"的含义。

```
┌─ Editor 层（Assets/Editor/FuFramework/Network/）─────────────────┐
│  NetworkModuleWindow : DebugWindowBase   主窗口（列表/筛选/详情）   │
│  NetworkJsonViewWindow                   JSON 折叠查看器（子窗口）  │
│  NetworkHexViewWindow                    Hex 二进制查看器（子窗口）  │
│  NetworkResendWindow                     重发编辑窗口（子窗口）      │
│  全部经 HotfixReflection 反射访问 Hotfix 侧 NetworkDebug         │
└──────────────────────────────────────────────────────────────┘
                    ▲ 反射（低频：拉快照/触发操作）
┌─ Hotfix 层（Scripts/Hotfix/Framework/Network/）──────────────────┐
│  NetworkDebug（静态类，#if UNITY_EDITOR 条件编译，不进包）        │
│  ├─ 循环队列 2000 条 NetworkMessageRecord（线程安全）              │
│  ├─ Enabled 开关（面板打开才录制，关闭即停——照 WebModule.Debug 模式）│
│  └─ 查询接口：GetSnapshot / GetRecord / Clear / SetEnabled / Resend│
│  埋点（同程序集直调，开关关闭零开销）：                             │
│  ├─ 发送：NetworkChannelBase.ProcessSendMessage 成功后            │
│  ├─ 接收：NetworkChannelBase.ProcessReceived（TryReply 前先记录）  │
│  └─ RPC 终点：RpcState.Update 超时 / Dispose 断线取消              │
└──────────────────────────────────────────────────────────────┘
```

## 3. 数据采集层（Hotfix）

### 3.1 NetworkMessageRecord（记录条目）

通用字段，不依赖具体消息类型（规避消息对象池化/生命周期问题——**Record 只存生成好的显示数据，不存消息对象引用**）：

| 字段 | 说明 |
|---|---|
| Seq | 自增序号（横跨所有类型，利于排序与引用） |
| Kind | 方向/类型：Request / Response / Notify |
| TimeUtc | 记录时刻 |
| UniqueId | 消息唯一号（Req/Resp 精确配对——优于参考工具的 moduleId+cmdId 配对） |
| MessageId | 消息号（`MessageIdRegistry` 反查） |
| MessageName | 消息类型名（`msgObj.GetType().Name`，埋点时取） |
| Status | 状态：已发待回 / 已回 / 超时 / 取消（断线）/ 未知（Notify 无状态概念，恒为已终结） |
| ElapsedMs | 耗时（响应到达或超时/取消时补写；来源 `RpcMessageData.ElapseTime` 或时间差） |
| BodyBytes | **未压缩包体字节**快照（埋点时**复制**，供 Hex 查看；上限见 3.4）。两侧对称均为未压缩形态：发送侧在压缩逻辑前抓取（或对 msgObj 重新序列化，实现落点留 §9），接收侧解码后天然已解压 |
| BodySize | 包体字节数 |
| JsonSnapshot | `UtilityAOT.Json.ToJson(msgObj)` 生成的内容快照（上限见 3.4） |
| ResendSource | 标记本条是否由重发产生（与原始记录区分） |

### 3.2 NetworkDebug（静态采集器）

照 `WebModule.Debug.cs` 的既有模式（同项目同惯例，注释同样写明"仅供编辑器调试面板反射读取"）：

- **开关**：`SetEnabled(bool)` + `Enabled` 属性。开启瞬间清空既有历史（与 WebModule.SetDebugRecording 一致）。**埋点全部以 `if (!Enabled) return;` 短路**——正式运行零开销。
- **容量**：循环队列 2000 条（同参考工具；WebModule.Debug 是 200，TCP 消息频率更高故放大）。
- **线程安全**：收发链路跨线程（接收回调在线程池线程），锁保护。
- **暂停**：`IsPaused`——暂停期间埋点仍短路（不新增记录），已有记录保留可查。
- **查询接口**：
  - `GetSnapshot() → NetworkMessageRecord[]`（全量快照，面板每 100ms 拉一次）
  - `Clear()`（逐槽置 default 释放字符串/字节引用，照 WebModule.ClearDebugHistory 的逐槽清理）
- **重发入口**：`Resend(long seq, string jsonBody)` —— Editor 触发后 Hotfix 侧 `Forget()` 执行完整 UniTask 链（JSON → 反序列化 → `NetworkModule.Instance.Call`），响应/超时/取消照常进入记录列表。**Editor 不碰异步**。仅当总开关开启时执行（见 §6）。

### 3.3 埋点位置（全部为现有代码咽喉点，改动均为增量）

| 埋点 | 位置 | 可得数据 | 说明 |
|---|---|---|---|
| 请求记录 | `NetworkChannelBase.ProcessSendMessage` 成功后 | msgObj、`m_NetworkChannelHelper.LastSentMessageId`、**压缩前包体字节**（重新序列化或压缩前钩点，落点留 §9） | 现有 `ENABLE_NETWORK_REQ_LOG` 调试日志就是此处的范式（含忽略列表），新增监控调用与之并列 |
| 接收记录 | **解码点成条**（`SystemTcpNetworkChannel` 解码回调处，与 `DebugReceiveLog` 同位） | msgObj、MessageId、包体字节（本地 buffer） | `LastReceivedMessageId` 是解码线程更新的单字段，主线程派发滞后读会错位（爆发期读到后续包的 id）；解码点 msgObj+消息号+包体字节一把抓全。Seq 反映**到达序**（解码序）。按 `msgObj is IResponseMessage`（响应）与否区分 Response / Notify |
| 响应配对 | `RpcState.TryReply` 配对成功处 | UniqueId → 回写请求记录 Status=已回 + ElapsedMs | |
| 响应超时 | `RpcState.Update` 超时移除处 | UniqueId → 回写 Status=超时 | |
| 断线取消 | `RpcState.Dispose` 取消处 | UniqueId → 回写 Status=取消 | |

- **心跳排除条件**：仅排除**非 `INotifyMessage` 的纯保活心跳**（量大且无业务价值）；双实现心跳（如 `NotifyHeartBeat` 校时通知）**正常记录**——服务器时间同步是当前重点调试对象，校时链路必须可见。

- **请求↔响应各自独立成条**（同参考工具），靠 UniqueId 关联；响应记录到达时同时回写对应请求记录的状态与耗时（状态在记录条目上，列表"请求"页即见全部未回/超时请求）。
- **包体字节复制时机**：发送侧在 `ProcessSendMessage` 处拷 `msgBodyBuffer`；接收侧在解码得包体字节处拷贝（同参考工具"Response 二进制复制保存，避免池回收导致数据丢失"）。
- **JSON 生成时机**：埋点时立即 `ToJson`（对象此时必然存活），**绝不在 Record 里存 msgObj 引用**。

### 3.4 容量与截断

- 记录条数：2000 条循环覆盖。
- 单条 JsonSnapshot 上限 **32KB**，超出截断并标注"…（已截断，共 N 字符）"。
- 单条 BodyBytes 上限 **64KB**，超出仅存前 64KB 并在记录上标注"已截断"（Hex 查看以头部为主，完整包极罕见）。
- 参考工具实测：2000 条记录内存占用约几 MB，可接受。

### 3.5 HTTP 链路：不在本面板范围

HTTP 监控已由 `WebModule.Debug.cs`（埋点）+ `WebModuleWindow`（面板，1596 行）完整实现：统计概览、实时请求快照（等待/发送中）、最近记录列表、手动测试请求（HTTP 版"重发"）、急救取消。网络调试面板**不做 HTTP Tab**——避免两个面板重复展示同一份数据；HTTP 调试请使用 Web 模块调试面板（`FuFramework/调试/Web模块调试面板`）。

## 4. Editor 窗口层

### 4.1 主窗口 NetworkModuleWindow : DebugWindowBase

- 菜单：`FuFramework/调试/网络调试面板`，`FuMenuPriority` 新增 `DEBUG_PANEL_NETWORK` 段（分段管理惯例）。
- **基座微量扩展**：`DebugWindowBase` 新增 `protected virtual void OnPanelEnabled() / OnPanelDisabled()`（基类 `OnEnable/OnDisable` 末尾各调一行，默认空实现）——7 个现有面板零影响。本面板覆写它们实现"打开才录制、关闭即停"的开关接线（基类 OnEnable/OnDisable 为 private 非虚，子类不可直接介入）。
- 基座复用：Play 守卫、反射守卫（`HotfixReflection.TypeNames` 增补 NetworkDebug 条目）、自动刷新（覆写 `RefreshInterval => 0.1f`，对齐参考工具 10FPS）、`HasExpandCollapse => false`。
- 工具栏：基类搜索框（按消息名/消息号/UniqueId 过滤）+ `DrawToolbarExtraButtons` 放【触发断线】（模拟意外断线：确认对话框 → `NetworkModule.TriggerDisconnectForDebug()` → `Close(Lost)` → 自动重连编排，验收期新增）【暂停/继续】【清除忽略】【清空】。
- 概览区（`DrawOverview`）：Tab 筛选（全部/请求/响应/推送/心跳/错误，按钮上带消息计数）+ **重发默认可用**（无总开关，见 §6）。
- 列表（`DrawContent`）：虚拟化列表（照参考工具 DisplayRow 拍平方案，行高固定 25px，只绘制可见行）；列：序号/时刻/类型/消息名/UniqueId/状态/耗时/大小；颜色标识照参考（请求橙红/响应黄绿/推送粉红，错误标红）。
- 详情：选中记录后在列表下方固定高度区绘制基本信息 + 三个按钮【查看 JSON 结构】【查看二进制数据】【再次发送（请求类可用）】。
- 错误页：Status ∈ {超时, 取消} 的记录（响应异常）。
- 窗口侧忽略过滤（参考工具同款）：按消息号忽略展示，`EditorPrefs` 持久化。仅影响显示，不影响采集。

### 4.2 子窗口

| 窗口 | 对应参考 | 形态 |
|---|---|---|
| `NetworkJsonViewWindow` | ProtobufViewWindow(+Render) | 对 JsonSnapshot 文本做结构化折叠渲染：`{}`/`[]` 容器行首箭头切换折叠/展开，支持复制全文 |
| `NetworkHexViewWindow` | HexViewWindow | 每行 16 字节：偏移量 + Hex + ASCII 混排，支持复制 |
| `NetworkResendWindow` | ProtoResendWindow | 预填请求 JSON（可编辑），显示目标消息名与 UniqueId；确认后反射调 `NetworkDebug.Resend(seq, json)`；发送后提示"已发起，结果见列表" |

子窗口数据传递：打开时把目标记录的 JsonSnapshot / BodyBytes 值传入窗口静态字段（窗口间传值用最简方式，照参考工具做法）。

### 4.3 反射边界

- Editor 面板只反射 `NetworkDebug`（静态类）与 `WebModule`（Debug 查询接口）的**公开静态成员**。
- `HotfixReflection.TypeNames` 增补 NetworkDebug 条目（改热更类型名只需同步此处的既有惯例）。
- 铁律说明：铁律 4（运行时杜绝反射）指运行时游戏逻辑；Editor 调试面板反射是项目既定模式（7 个面板同款），本设计沿用。

## 5. 重发链路（Hotfix 侧）

```
Editor：NetworkResendWindow 确认
  → 反射调 NetworkDebug.Resend(seq, jsonBody)     [默认可用，无开关]
Hotfix（Forget() 全链，主线程上下文）：
  1. 按 seq 找原请求记录 → 拿 MessageId → MessageIdRegistry 反查消息类型
  2. JSON 反序列化为消息对象（UniqueId 重新分配，不沿用旧值避免撞挂起表）
  3. 经频道/NetworkModule 新增的 internal 非泛型重发辅助发出
     （Call<TResponse> 泛型无法用运行时 Type 调用；辅助方法内部走 Send + RpcState 挂表，
       #if UNITY_EDITOR 条件编译不进包——用户拍板：加非泛型辅助，不用反射、不动生成器）
  4. 响应/超时/取消经既有 RpcState 终点自然落入记录列表（ResendSource 标记）
失败路径（类型查不到/JSON 非法/未连接）：同步校验段 Resend 返回 bool；
  异步段异常记一条 Status=错误 的记录，编辑器列表可见
```

## 6. 重发可用性（决策演变）

- **初期决策**：加默认关闭的总开关（`NetworkDebug.ResendEnabled` + EditorPrefs）防误触。
- **验收期推翻**（用户裁决）：Editor 开发工具本就只在开发环境使用，重发**默认可用、无开关把守**——总开关链路已整体移除（Hotfix 属性/面板 Toggle/重发窗口判定）。
- 保留的安全边界：仅 Request 可重发；重发窗口醒目 Warning 提示"重发会真实发送到服务器"。

## 7. 约束（铁律）

1. **不进包**：Hotfix 侧采集层全部 `#if UNITY_EDITOR` 条件编译；Editor 侧天然 Editor-only。
2. **UniTask**：Hotfix 侧重发链路用 UniTask + `Forget()`，生命周期由 NetworkDebug 静态自持（编辑器调试工具，无窗口生命周期概念；与 NTP 链的静态自持同款）。
3. **无 LINQ**：采集层与面板代码均手写循环（Editor 面板非热路径，但仍守铁律）。
4. **运行时零反射**：埋点是同程序集直调；反射只在 Editor 层。
5. **不改动既有语义**：埋点为纯增量调用（照 WebModule.Debug 的"纯增量埋点"承诺），收发/超时/取消逻辑一行不动。
6. **Tab 缩进**、FuMenuPriority 分段、注释密度对齐现有代码。

## 8. 非目标（本期不做）

- **HTTP 链路监控**：`WebModule.Debug.cs` + `WebModuleWindow` 已完整覆盖（统计/实时快照/最近记录/手动测试请求/急救），两个面板重复展示同一份数据无意义——HTTP 调试用 Web 模块调试面板
- HTTP 手动测试请求（Web 模块调试面板已有，不重复建设）
- 真机/打包后监控（Editor-only 是参考工具与本项目面板的共同边界）
- 导出 JSON/CSV、时间范围筛选、统计图表、自定义过滤规则（参考工具"未来改进"清单，按需后补）
- HTTP 手动测试请求（Web 模块调试面板已有，不重复建设）
- 多频道区分展示（当前单频道门面模型，一条链路；多频道化后再加 Socket 列）
- 持久化存储（运行时有效，同参考工具）

## 9. 留给实现阶段的自由裁量

- 虚拟化列表的精确绘制实现（EditorGUI.GetContentRect / GUILayout 行高计算）
- Tab 与统计摘要的布局微调
- MessageIdRegistry 消息号→名称反查的具体方法名（以实际 API 为准）
- 接收侧包体字节复制的精确落点（NetworkChannelHelper 解码路径中包体字节的持有窗口）
- 颜色/样式常量的具体取值（对齐参考工具或项目现有风格）

## 10. 审查定案记录（builder check 后的裁决归属）

审查核实设计稿与代码吻合（WebModule.Debug 模式、HotfixReflection、MessageIdRegistry 双向反查、`UtilityAOT.Json.ToObject(Type, string)` 非泛型重载均真实存在），提出 6 问，处置如下：

| # | 问题 | 处置 | 谁定的 |
|---|---|---|---|
| 1 | 重发调用路径（泛型 Call 无法运行时调） | 加 internal 非泛型重发辅助（§5） | **用户拍板** |
| 2 | Hex 字节压缩前/后矛盾 | 统一存未压缩包体，两侧对称（§3.1/§3.3/§10.4） | 我 |
| 3 | 接收记录创建点错位 | 挪到解码点成条，Seq=到达序（§3.3） | 我（采纳审查技术证据） |
| 4 | 基座缺打开/关闭钩子 | DebugWindowBase 加 2 个 protected virtual 钩子（§4.1） | 我（向后兼容微量扩展） |
| 5 | HTTP 开关与 Web 面板写冲突 | HTTP Tab 砍掉，问题随之消解（§3.5/§8） | 我（后经用户确认砍掉 HTTP Tab） |
| 6 | 双实现心跳被一刀切排除 | 排除条件改"非 INotifyMessage 纯保活心跳"（§3.3） | 我（校时链路必须可见） |

## 11. 验收口径

1. Play 模式登录流程中，请求/响应/推送三类消息实时出现在列表，UniqueId 配对正确、耗时正确。
2. 断线（拔线/停服）后：未回请求显示超时或取消状态；重连成功后新会话消息继续记录。
3. 暂停后列表冻结，继续后恢复；清空后列表为空且再次录制冷启动正常。
4. JSON 折叠视图可展开/折叠/复制；Hex 视图字节为**未压缩包体**，与重新序列化的 proto 字节一致（可逐字节对照 JSON 字段）。
5. 选中请求改 JSON 字段重发，服务器收到并正常响应，列表出现 IsResend 标记的新请求与对应响应。
6. 面板关闭后录制停止（日志无新增记录），重新打开后历史清空重新录制。
7. Build 后包体不含 NetworkDebug 与任何埋点代码。
