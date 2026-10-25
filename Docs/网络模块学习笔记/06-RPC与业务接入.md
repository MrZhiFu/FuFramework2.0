# 06 · RPC 与业务接入（请求-响应）

> 配套图：[RPC 时序图](../.archify/sequence-rpc-call-20261019-222217/rpc-call.html)
> 本篇回答：`await NetworkModule.Instance.Call(new ReqPlayerLogin { ... })` 这一句**各自在做什么**，响应用什么"钥匙"配回来，超时/断线为什么"必然唤醒"而不是永久悬挂，以及业务侧怎么写才符合铁律 5。

---

## 一、What：RPC 是什么，`Call` 三个词各干嘛

**RPC = Remote Procedure Call（远程过程调用）**——把"让服务器干活"伪装成"本地函数调用"：

```
你想做的（像调本地函数）：   RespPlayerLogin resp = 请服务器让角色进游戏(角色ID);
RPC 实际发生的：
  ① 把"请登录"变成一条消息 req 发过去           ← "打电话"
  ② await：我什么都不干，只等回信               ← "等对方回话"
  ③ 收到回复 resp → 当"返回值"拿回来            ← "对方回话，挂断"
```
心电、`Send` 都不是 RPC（它们不等回复）；"**发了还要 await 等回话**"才是 RPC。项目实现（`NetworkChannelBase.API.cs` 的 Call）就三行：

```csharp
public async UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> msgObj) where TResponse : MessageObject, IResponseMessage
{
    msgObj.NotNull(nameof(msgObj));
    Send((MessageObject)msgObj);                        // ① 发包（第 2/3 篇全链路）
    var result = await m_rpcState.Call(msgObj);   // ② 挂起表登记 + 等配对
    return result;                                      // ③ 泛型返回：编译期就定死响应类型，响应不符直接抛异常（非静默 null）
}
```

---

## 二、Why：为什么用 `UniqueId` 配对；为什么必须"主动终结"

### 2.1 为什么不用消息 ID 配对

消息 ID（如 `19660810`）是"发货渠道"，同类型共用；而 `UniqueId` 是"每份独一无二的快递单号"（`MessageObject` 构造时 `IdGenerator` 生成）。如果靠消息 ID：

- 挂起表里同时只能有一条同类型请求，第二条覆盖第一条；
- 两个 `await` 会拿到**同一个**响应（错配）。

只有 `UniqueId` 能让"同时挂 N 条同类型请求、各配各的"成立（字典 key 各不同）。

### 2.2 为什么 `UniqueId` 走"侧信道"

`MessageObject.UniqueId` 是 `[JsonIgnore]`、**不参与 proto 序列化**。与其给每条消息定义 `unique_id` 字段、序列化浪费字节，不如塞进本来就要发的 14 字节信封第 4 字段——**零额外成本**，收端 `ReadInt` 一个字段读回。这条"绕过 proto、只活在自己定义里的号码"就称**侧信道**（正式内容走"主信道"=信纸）。三个词的人话翻译：

> "UniqueId 不写进正式消息里，而是贴在快递箱外面的单据上（14 字节信封）；靠着它，await 才能把千里之外的响应认领回它等的那个请求。"

### 2.3 为什么断线/超时必须"主动终结"而不是 `Clear()` 了事

`await` 最恶的坑是**永久悬挂**——不占线程，但调用方永远醒不来。`Clear()` 只清了登记表，**却不管每个挂起的 Task**（它们始终未完成）。所以 `RpcState.Dispose` 必须对每个挂起 `Cancel()`（`TrySetException(OperationCanceledException)`）——**表可以清，任务必须有人叫醒**。

---

## 三、How：完整时序 + 三种结局 + 回包分流 + 业务写法

### 3.1 完整时序（见 RPC 时序图）

```
业务   await NetworkModule.Instance.Call(new ReqPlayerLogin { ... })（响应类型自动推断）
           ↓ NetworkModule/Channel.Call: Send(req) → RpcState.Call → 挂起表登记（key=req.UniqueId）
        —— 到这里调用方就"悬起"了，什么都不占，等 Task 恢复 ——
           ↓ 服务器处理，响应 RespPlayerLogin 回来 → 收包全链路 → 信封读回 UniqueId → SetUniqueId
主线程 ProcessReceivedMessage:
   ↓ （心跳回包已在此前按 is IHeartBeatMessage 截流，不进业务派发）
   └─ m_rpcState.TryReply(resp)
        ├─ 判断是不是 IResponseMessage（is 模式匹配，无缓存）
        ├─ TryRemove(resp.UniqueId) 在挂起表找配对
        ├─ 配上 → data.Reply(resp) → UniTaskCompletionSource.TrySetResult → await 恢复
        │        （错误码由响应体 Code / NotifyErrorCode 旁路推送承载，无独立回调）
        └─ 没配上 → DispatchNotifyMessage（当普通推送走 [NotifyMessageHandler]）
        ↓
   await 恢复：返回强类型 RespPlayerLogin → 调用方拿到响应 ✅
```

### 3.2 三种结局

`RpcMessageData`（挂起记录）内部持一个 `UniTaskCompletionSource`，`await` 悬的就是它的 `Task`。三种结局都落在"对这个 Task 做一次 SetXxx"上：

| 结局 | 触发方 | 机制 | await 表现 |
|------|--------|------|-----------|
| 正常响应 | 收包主线程 | `TryReply` → `m_tcs.TrySetResult(resp)` | 恢复返回 RespPlayerLogin |
| 超时 | `RpcState.Update` 每帧 | 逐项 `IncrementalElapseTime` → 超时 `TrySetException(TimeoutException)` | 抛超时异常 |
| 断线 | 频道 Close | `RpcState.Dispose` → 每个挂起 `Cancel()` → `TrySetException(OperationCanceledException)` | 抛取消异常 |

代码注释原话把动机写得很直白（`RpcState.cs`）：*断线/销毁时必须让所有挂起的请求以异常结束，否则 await 会永久悬挂（原实现只做了字典 Clear）。*

### 3.3 回包分流（TryReply 优先）

```csharp
if (m_rpcState.TryReply(messageObject)) continue;   // 配上了 → 不再派发
DispatchNotifyMessage(messageObject);               // 没配上 → 当 [NotifyMessageHandler] 普通推送
```
落到 `DispatchNotifyMessage` 的三种情况：① 根本不是 `IResponseMessage`；② 是响应但挂起表里没有配对（服务器主动推送）；③ 迟到的 RPC 响应（请求已超时/断线，响应晚到无人认领）。（顺带一提：RPC 用的是「回包分流」，而登录那段 HTTP/TCP 是「双段分层」——两个"双"字不是一回事，见 3.5。）

### 3.4 业务侧怎么写（铁律 5 的实际应用）

```csharp
try
{
    var resp = await NetworkModule.Instance.Call(new ReqPlayerLogin { Id = playerId });
    if (resp.ErrorCode == 0) { /* 进游戏 */ }
    else { /* 弹错误码 */ }
}
catch (OperationCanceledException) { /* 断线：提示重连 */ }
catch (TimeoutException)           { /* 超时：提示网络差 */ }
```

- 每个异步链必须有**生命周期所有者**：这个 `Call` 的调用方（登录窗口）负责 try/catch 接手异常，而不是放任 await 悬着。
- fire-and-forget 起步请 `.Forget()`；**不要**在 RPC 里做"等等等 + 不取消"的长等待——断线那下 `Cancel` 就是给你兜底的。
- 响应能同时被 `[NotifyMessageHandler]` 订阅吗？**不能**：特性构造期校验消息必须实现 `INotifyMessage`，Resp 类型挂不上（Resp 的消费者是 `Call` 的 await）。Resp 配不上对（无宿主等待）时会在派发链上查无绑定、警告后丢弃。

### 3.5 登录为什么分两段：HTTP 管账号，TCP 管游戏

业务接入还有一个前置问题：**为什么 `WinLogin`（账号登录）走 HTTP，`WinPlayerList`（进游戏）才走 TCP 频道**？——两种工作的性质根本不同：

| | 账号登录 | 游戏业务 |
|---|---|---|
| 模式 | **一次性**：提交凭证 → 拿回结果，结束 | **持续**：服务器持续推增量、双向高频交互 |
| 状态 | **无状态**：失败了重试即可，没有会话要恢复 | **有状态**：服务器记住"此人在线"、数据在内存 |
| 频率 | 低频（一人登录一次） | 高频（一局游戏成百上千条消息） |
| 基础设施 | HTTPS 证书、负载均衡、限流、WAF 全是现成的 | 自定义协议、自己做心跳/加密/重连 |
| 故障影响 | 登录服挂了**不影响已在线玩家** | 游戏服挂了才影响会话 |

类比：登录 = **买票**（办完就走，售票窗口不够就加窗口）；游戏会话 = **上车**（车一直开着，乘客司机持续互动）。用买票的方式开车、用开车的窗口卖票，都别扭。

服务器印证——账号与玩家是两个独立 App（`Server/FuFramework.Apps/`），两个处理器签名的差别就是全部设计：

- `Account`（HTTP）：`ReqUserLoginHttpHandler.OnUserLogin(ReqUserLogin)` —— 签名**无连接参数**，无状态，谁来查库都一样，扩容 = 加机器；
- `Player`（TCP）：`PlayerComponentAgent.OnPlayerLogin(INetworkChannel workChannel, ReqPlayerLogin reqLogin)` —— 第一个参数就是**频道**，从此这个人的所有推送都从这条连接出去，**连接即会话**。

如果登录也走 TCP 会失去什么：① 开服登录洪峰挤占游戏服的会话内存（每条连接+运行时数据都是珍贵资源，而无状态的登录服加机器就能扩）；② 故障域黏连（登录服重启/被刷，在线玩家跟着遭殃）；③ HTTPS 防刷限流全要重造轮子；④ 客户端从打开 App 到点进游戏之间，白养一条连接的心跳/重连。

与本篇阀门的呼应：**增量推送只存在于 TCP 会话中**（HTTP 一问一答，不存在"响应没回来时服务器主动推东西"），基线竞态也从 `Call(ReqPlayerLogin)` 才开始存在——所以 `SuspendNotifyDispatch` 挂在 `WinPlayerList.PlayerLoginAsync` 的基线窗口；`WinLogin.UserLoginAsync` 里没有它的位置——那个阶段 `NetworkModule.Channel` 还是 null，TCP 连接要到选完角色才创建。
**失败分支**：登录失败（Call 抛超时/取消）时，基线没建立、缓冲里的增量全是脏数据——`catch { ClearNotifyBuffer(); throw; }` 清缓冲而非回放，异常原样抛给上层上重连提示。

将来多服/分区时的完整形态（网关分发，proto 已预留 `RespConnectServer` 的 TargetIP/TargetPort）：

```
HTTP 登录账号 → 服务器告知"该去哪台游戏服" → TCP 连那台 → Call(ReqPlayerLogin) 进游戏
```

一句话收束：**HTTP 管"你是谁"，TCP 管"你在游戏里发生了什么"**——前者无状态可随意扩缩，后者有状态必须长连，两者各取所长。

---

## 四、自测（随学随考）

### 题目

1. 为什么配对用 `UniqueId` 而不用消息 ID？
2. 客户端收到的响应 `RespPlayerLogin`，它的 `UniqueId` 是怎么来的？（指出是哪一行代码填的）
3. 为什么断线必须 `Cancel` 而不是只 `Clear()` 字典？
4. 同一条响应，`TryReply` 和 `DispatchNotifyMessage` 谁先执行？什么时候会落到后者？
5. "RPC 是远程过程调用" —— 用三句话给一个没听过的人解释。
6. 业务侧为什么必须捕获 `OperationCanceledException` 与 `TimeoutException`？
7. 为什么账号登录走 HTTP、进游戏才走 TCP？（说出至少两条 HTTP 承担登录的好处）

### 答案

1. 消息 ID 是发货渠道，同类型共用 → 靠它配对会错配（两条同类型请求拿同一个响应）；`UniqueId` 是每实例唯一单号 → 挂起表 key 各不同，能同时挂 N 条同类型请求、各配各的。
2. 从响应信封的侧信道读回：收包时 `MessageObject.SetUniqueId(PacketReceiveHeaderHandler.UniqueId)`（`SystemTcpNetworkChannel.ProcessPackBody`）。服务器在响应信封里回显了请求的同一个 UniqueId，客户端读回来赋给响应的 MessageObject，`TryReply` 就能拿它去挂起表配对。proto 里没有这个字段（`[JsonIgnore]`），它只活在自定义包头里。与 `UpdateUniqueId()`（发侧生成新号）分工：一个生成、一个还原，方法名各自对应方向。
3. `Clear()` 只清登记表，不清每个挂起的 Task——`await` 仍悬在未完成状态，调用方永久醒不来（还要泄漏一坨继续堆栈）。`Cancel()` 对每个挂起 `TrySetException(OperationCanceledException)` → await 抛出取消异常，调用方 catch 后走重连提示。
4. **先 `TryReply`，配上了 `continue`；配不上才 `DispatchNotifyMessage`**。落到后者的三种情况：不是 IResponseMessage；是响应但挂起表无配对（主动推送）；迟到的 RPC 响应（请求已超时/断线）。
5. RPC = 远程过程调用。① 你把"请服务器干一件事"变成一条消息发过去；② 然后空手 await，等它回信；③ 收到回信把它当"返回值"用——整个过程像在调用一个本地函数。
6. `Call` 的 Task 有三种结局（正常/超时/断线），两处异常路径会以 `TimeoutException`/`OperationCanceledException` 抛给 await；不捕获就可能逃逸到主循环无人处理，或让登录流程卡住。捕获它们才能优雅地上重连提示、清理 UI 状态（这符合铁律 5 的"异步链必须有生命周期所有者"）。
7. ① 登录是一次性、无状态、低频的凭证交换，HTTP 请求-响应模型天然匹配，且 HTTPS/负载均衡/限流等基础设施现成；② 无状态服务加机器即可扩容，开服登录洪峰不会挤占游戏服的会话内存；③ 故障域隔离：登录服重启/被刷不影响已在线玩家的 TCP 会话。反面印证：推送增量只存在于 TCP 会话，基线竞态只在 TCP 段存在，所以派发阀门只挂 `WinPlayerList.PlayerLoginAsync`。

---

## 结语

六篇至此闭环：

- **01 全景**：一条消息怎么从业务走到网线再回来；频道=一条线路的完整封装。
- **02 Socket 与 TCP**：TCP 承诺与不承诺、粘包根源、BeginReceive 零线程等待、为什么热路径用 APM、两个蓄水池与"指针追总量"。
- **03 包头/包体与压缩**：14 字节信封逐字段、int/uint/byte 与占格、大端字节序真相、压缩策略、空消息特例。
- **04 频道核心**：Update 状态机、发送/接收状态机、心跳保活与断线判定、锁的领地。
- **05 装配系统**：零反射动机、生成期前移、三张表、internal 可见性、后注册覆盖。
- **06 RPC**：Call 三合一、UniqueId 侧信道配对、超时/断线必然唤醒、回包分流与业务写法、账号 HTTP/业务 TCP 双段分层。

现在，你可以从一个登录按钮讲到一条消息的字节排布、再讲回 `await` 的三种结局——这就是这个网络模块的完整心智模型。