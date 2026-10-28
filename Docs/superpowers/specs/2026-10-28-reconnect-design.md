# 网络断线自动重连（客户端）设计稿

> 日期：2026-10-28
> 状态：待批准
> 决策方式：brainstorming 对话收拢，用户逐项拍板
> 参考：SLG 知识库 13 篇（断线重连状态机）；服务器现状调查（见 §3）

## 1. 背景与动机

当前框架**没有任何自动重连**：断线后唯一的恢复路径是玩家手动点登录按钮 → 销毁重建频道 → 重新登录。开发期可接受，但上线后体验不可接受（切后台回来、电梯里信号抖动，玩家都要手动重来）。

SLG 对比评分中"断线重连"是差距最大的维度（9.5 vs 4）。本设计补齐客户端侧重连闭环：**断线 → "重连中"转圈框 → 原地复活继续玩**。

## 2. 目标 / 非目标 / 反目标

**目标**
- 运行中断线（心跳判死、收发错误、Socket 断开）后自动进入重连流程：弹"重连中"框，按退避序列重拨，拨通后静默自动重登，玩家回到主界面原地复活。
- 重试耗尽后交还控制权：弹"重连失败"框，玩家手选【再试一次】或【返回登录】。
- 框架层沉淀三个可复用原语（见 §5），重连编排的 UI 部分留在 Game 层（见 §6）。

**非目标（本设计明确不做）**
- 服务器侧任何改动（宽限期协议化、`LoginState.isReconnect` 接线、`MessageOperationType.Reconnect` 处理、`OnRoleOnline/OnRoleOffline` 派发）——服务器现状已够用（§3），协议级重连语义留给将来。
- 登录鉴权补全（`ReqPlayerLogin` 直传 Id 无校验是已知安全问题，单独立项）。
- 窗口栈快照恢复（方案 2/3）——本设计采用"统一回主界面"（§4 决策 5），将来平滑升级豁免机制。
- 账号段（HTTP）登录的重试——HTTP 一问一答自带重试语义，不在范围。
- 双路由/多频道。

**反目标（出现即失败）**
- 重连流程把玩家弹回选角色界面（WinPlayerList）——体验破坏。
- 主动登出触发自动重连——死循环。
- 重连后旧会话的增量缓冲被回放——数据串号。现有 `ClearNotifyBuffer` 失败路径必须保住。

## 3. 现状事实（builder 必读）

**客户端**（本设计改动面）：
- 断线信号已收口：心跳判死 / 收发错误 / Socket 断开，最终都走 `NetworkChannelBase.Close()` → 入队 `Closed` 生命周期事件 → 主线程派发 → `NetworkModule` 广播 `NetworkClosedEventArgs`。**收口点已存在，重连触发器就挂这里。**
- 但 `Close()` 无原因区分：主动登出与异常断线的事件长得一样（件 1 要解决的）。
- `Connect()` 是发完即忘：拨号结果只能靠业务订阅 `NetworkConnectedEventArgs` / `NetworkErrorEventArgs` 两个事件自行桥接（件 2 要解决的）。
- `WinPlayerList.OnBtnLoginClick` 已有"销毁旧频道 → 重建 → Connect"的完整做法，`PlayerLoginAsync` 已有完整登录事务（`SuspendNotifyDispatch` → `Call(ReqPlayerLogin)` → `ResumeNotifyDispatch`，失败路径 `ClearNotifyBuffer` + throw）。重连复用这两块。
- 心跳：新频道创建时生成物自动注册 `HeartBeatProvider`，无需额外处理。

**服务器**（本设计不动，但决定可行性）：
- 断线 → `OnDisconnected` → `SessionManager.Remove`（会话立即删除）→ `SessionRemove` 事件 → `PlayerComponentAgent.OnLogout`（仅退在线集合 + `SetAutoRecycle(true)`）。
- **玩家实体不随断线销毁**：Actor + 组件 + 内存 State 在"登出后再闲置 15 分钟"后才被 `CheckIdle` 回收；期间重连走全新 `ReqPlayerLogin`，`ActorMap.GetOrAdd` 命中同一 Actor，内存 State 直接续用——**数据不毁灭，"原地复活"在服务器现状下天然成立**。
- 超过 15 分钟 Actor 被回收：重连后从 Mongo 重读最后落库数据，依然可进游戏（数据回档到上次落库点，可接受，无需特殊处理）。
- 顶号：`SessionManager.UpdateSession` 已实现"同 RoleId 旧会话关闭并替换"。重连登录语义上是新会话，与顶号逻辑自然兼容，无需特判。

## 4. 已定决策（用户拍板，builder 不得更改）

| # | 决策 | 内容 |
|---|------|------|
| 1 | 体验目标 | 断线 → "重连中"转圈框 → 原地复活 |
| 2 | 服务器 | 完全不动 |
| 3 | 放弃行为 | 5 次退避全失败 → "重连失败"框，玩家手选【再试一次】/【返回登录】 |
| 4 | 归属 | B 全套：框架层件 1+2+3，Game 层 UI 编排 |
| 5 | UI 恢复 | 统一回主界面：重登成功 → 关闭全部窗口 → 重新打开主界面 |
| 6 | 退避序列 | `{2,3,5,8,13}s × 5 次`（SLG 实证值） |
| 7 | 重连期间 Send/Call | 现有语义不变（Call 已被断线 Cancel 唤醒；Send 走 Error 事件） |
| 8 | 登出区分 | 主动登出传 `CloseReason.Active` 不触发重连；自动重连只认 `Lost` |
| 9 | 心跳 | 新频道生成物自动注册，零处理 |
| 10 | 顶号 | **不自动重连**：新增 `NotifyPrompt(Type=5)` 处理器，收到后标记"被顶"，随后的断线不触发重连，弹"账号已在其他设备登录"提示（builder 发现：服务器踢人客户端只能标 `Lost`，靠 CloseReason 无法区分被顶——suppress 必须走消息标记） |
| 11 | 返回登录落点 | 回 `WinLogin` 重走 HTTP 账号登录；**需新建最小登出流程**（builder 查实：当前代码不存在任何"从游戏内回登录"的路径） |
| 12 | FGUI 资源分工 | "重连中"框资源由用户在 FGUI 编辑器制作并导出（前置任务），代码侧接线；"重连失败"框复用现有 `WinDialogMessageBox` |

## 5. 框架层三件套

### 件 1：关闭原因（`CloseReason`）

- `NetworkChannelBase.Close()` 增加原因参数：`Close(ENetworkCloseReason reason)`（枚举命名遵循项目 `E` 前缀惯例，具体枚举名与成员命名留给 builder 定，语义必须为：`Active`=主动关闭（登出、销毁、手动放弃），`Lost`=异常断线（心跳判死、收发异常、Socket 断开、**服务器踢人/顶号**））。
- **注意（builder 审稿修正）**：顶号是服务器踢人，客户端只能走被动路径标 `Lost`——靠 CloseReason **无法**区分被顶。被顶的 suppress 依赖决策 10 的 `NotifyPrompt(Type=5)` 消息标记，不依赖本枚举。
- `NetworkClosedEventArgs` 增加 `CloseReason` 字段，随事件广播。
- 全部现有 `Close()` 调用点逐一标注：主动路径（`NetworkModule.Close`、`DestroyChannel`）传 `Active`；被动路径（心跳判死、收发错误、`bytesReceived <= 0`、WebSocket 底层关闭回调）传 `Lost`。
- WebSocket 的底层 `CloseCallback`（服务器/网络主动断）语义为 `Lost`；客户端主动 `Dispose` 为 `Active`。

### 件 2：可等待的拨号（`ConnectAsync`）

- `NetworkModule` 增加门面方法 `ConnectAsync(Uri address, int timeoutMilliseconds, CancellationToken cancellationToken)`（签名细节留给 builder），内部：创建频道（编译宏决定 TCP/WS，同现有 `CreateChannel`）→ `Connect` → 订阅 `NetworkConnectedEventArgs` / 连接失败路径 → `UniTaskCompletionSource` 桥接 → 拨通返回频道、超时/失败抛异常（`TimeoutException` / 既有错误事件路径）。
- **拨号失败必须有明确结果**：现状拨号失败混在 `NetworkErrorEventArgs`（ConnectError）里，件 2 要把它变成可 await 的失败（异常或 bool），不再要求业务写一次性事件桥。
- 生命周期：`CancellationToken` 必传（铁律 5），取消时清理事件订阅（防泄漏）。
- 现有 `CreateChannel` / `Connect` 同步 API **保留不动**（`WinPlayerList.OnBtnLoginClick` 等既有调用方不迁移，风险面最小）；`ConnectAsync` 是新增的并行入口。

### 件 3：退避重拨编排（`ReconnectAsync`）

- `NetworkModule` 增加门面方法 `ReconnectAsync(ReconnectOptions options, CancellationToken cancellationToken)`：
  ```
  循环 attempt = 1..options.MaxAttempts:
      等待退避间隔（options.BackoffSchedule[attempt-1]）
      DestroyChannel（旧频道整个销毁，装配状态干净——对齐 OnBtnLoginClick 现状）
      CreateChannel + ConnectAsync（件 2 复用）
      拨通 → 返回新频道；失败 → 下一轮
  重试耗尽 → 抛失败异常（类型与消息留给 builder），由 Game 层决定收尾
  ```
- `ReconnectOptions`：退避序列（默认 `{2,3,5,8,13}s`）+ 最大次数（默认 5）。**不要过度普适**：不加回调、不加中间状态机——进度上报由 Game 层轮询或后续按需加。
- 生命周期：`CancellationToken` 必传。取消语义：立即停止重拨（不再发起新一轮），已发起的拨号等待随之取消。
- 重连期间 `NetworkModule.Channel` 处于"旧频道已销毁、新频道未建"的窗口——业务侧 `Send`/`Call` 会得到现有的"频道未创建"异常，语义自洽，不额外防护。

## 6. Game 层编排

新增一个 Game 层重连编排者（命名与落位留给 builder，建议 `Hotfix.Game.Network.ReconnectManager` 或挂在现有登录流程旁）：

```
订阅 NetworkClosedEventArgs:
    reason == Lost 且当前处于"已进游戏"状态（非登录期）且未被标记"被顶":
        打开"重连中"框（全屏模态遮罩，FGUI 新窗口含取消按钮；资源由用户编辑器制作——决策 12）
        启动重连事务（持有 LifecycleCancellationSource，铁律 5；同一时间仅一个事务，重入防护）：
            await NetworkModule.ReconnectAsync(options, ct)
            ├─ 成功 → 静默重登（见下）
            │         ├─ 成功 → 关闭全部窗口 → OpenAsync<WinMain> → 关"重连中"框
            │         └─ 失败（RPC 超时/再断线被 Cancel）→ 折叠进"重连失败"框收尾
            │                    （阀门收尾：ClearBuffer + Resume，状态清干净）
            └─ 失败/取消 → 打开"重连失败"框（复用 WinDialogMessageBox）
                            ├─【再试一次】→ 重新启动重连事务（从拨号重头，计数重置）
                            └─【返回登录】→ 新建的最小登出流程（见下）

订阅 NotifyPrompt（Type=5"账号已在其他设备登录"）:
    标记"被顶"状态 → 随后到达的 Closed(Lost) 不触发重连
    → 弹"账号已在其他设备登录"提示 → 走登出流程回 WinLogin
```

**静默重登**：复用 `WinPlayerList.PlayerLoginAsync` 的事务（挂阀门 → `Call(ReqPlayerLogin)` → 恢复派发），但**不得弹回选角界面**——把该事务从窗口代码抽为可复用方法（如 `PlayerLoginFlow.EnterGameAsync`），登录入口（WinPlayerList）与重连入口（ReconnectManager）共用；失败路径 `ClearNotifyBuffer` 语义必须保住（反目标 3）。**重入防护必须加**：同一时间只允许一个登录事务（防双事务竞态）。

**幽灵订阅修复（builder 发现）**：`WinBase` 关闭不退订事件，`WinPlayerList.OnNetworkConnected → PlayerLoginAsync` 的订阅在进游戏后长期存活——重连拨通时全局广播 `NetworkConnectedEventArgs` 会触发**幽灵订阅**用过期数据再跑一次登录（双事务竞态）。修法（局部，不动 `WinBase` 全局语义）：`WinPlayerList.OnClose` 退订网络事件 + 登录事务重入防护。`WinBase` 关闭不退订的框架级问题记录待办，不在本设计扩 scope。

**最小登出流程（新建，builder 查实当前不存在）**：【返回登录】/ 被顶 / 取消重连 共用：
清玩家本地状态（`PlayerManager.PlayerInfo` 等）→ `ClearNotifyBuffer` → 关闭频道（`Close(Active)` 或 `DestroyChannel`）→ 关闭全部窗口 → `OpenAsync<WinLogin>`。

**转圈框的取消按钮**：玩家主动放弃 → Cancel 重连事务 → 走登出流程（决策 11 落点）。

**登出竞态**：玩家在重连过程中点登出 → 先 Cancel 重连事务再走登出；登出流程关闭频道用 `Active`，天然不触发新一轮重连。

## 7. 边界情况表

| 情况 | 行为 | 依据 |
|---|---|---|
| 断线发生在登录期（还没进游戏） | 不触发自动重连（`WinPlayerList` 阶段 Channel 可能为 null 或刚建），由玩家重试按钮兜底 | 决策 8 的推广：只在"已进游戏"状态触发 |
| 重连拨通但 Actor 已被服务器回收（>15min） | 正常走重登，服务器从 Mongo 重读——无需客户端特判 | §3 服务器事实 |
| 重连期间被顶号 | 重登成功即顶掉对方（服务器顶号语义），本端继续；对方走它自己的断线流程（若对方也是本客户端且在游玩中，按决策 10 弹提示不重连） | §3、决策 10 |
| 重连过程中再次断线（拨通后又断） | `Closed(Lost)` 再次到达——编排者需保证同一时间只有一个重连事务（重入防护） | builder 实现要点 |
| 5 次重试期间玩家挂在"重连中"框 | 框持续显示，可取消 | 决策 3 |
| 游戏切后台挂起超长（心跳判死堆积） | 真实时钟心跳回前台立即判死 → 进重连流程，退避序列自然吸收 | 现有 `UpdateByRealTime` 行为 |

## 8. 留给 builder 的实现自由度

- 枚举名与成员命名（`ENetworkCloseReason`？）、`ReconnectOptions` 的具体形态（class/struct、字段命名）。
- `ConnectAsync` 的事件桥接实现（完成源、订阅清理、超时实现方式）。
- Game 层编排者的落位与命名、窗口类名（FGUI 新窗口 `WinReconnecting` / `WinReconnectFailed` 之类）、"已进游戏"状态的判定方式。
- 登录事务抽取的具体形状（静态流/帮助类/服务类）。
- 全部新代码遵守项目铁律：UniTask（无 Task/协程）、无 LINQ、无运行时反射、异步链生命周期所有者 + CancellationToken 必传。

## 9. 验收口径

1. 心跳判死（拔网线/断网模拟）→ 自动弹"重连中"框 → 恢复网络后自动回到主界面，数据为重登后的全量基线，无选角界面闪现。
2. 断网不恢复 → 5 次重试（间隔 2/3/5/8/13s）后弹"重连失败"框，两按钮行为正确。
3. 正常登出（关闭客户端会话）→ 不出现"重连中"框。
4. 重连成功后旧会话增量缓冲不回放（登录失败路径 `ClearNotifyBuffer` 可验证）。
5. 既有登录流程（首次登录）行为不变。
6. 被顶号（服务器推 `NotifyPrompt Type=5` 后断开）→ 不弹"重连中"框，弹"账号已在其他设备登录"提示，回 `WinLogin`。
7. 重连拨通后不存在第二次登录事务（幽灵订阅已被退订；重入防护生效）。
