# 09 · HTTP 与 TCP 的分工（为什么 TCP 也能请求-响应，却还要走 HTTP）

> 本篇范围：回答一个新手必踩的疑问——"TCP 明明也支持 Req/Rsp，为什么登录这类操作还要另走 HTTP？" 我们从**两条路的真实代价**出发，落到框架里 `WinLogin`（HTTP）与游戏内业务（TCP）的分工代码。
> 前置：01 篇（一条消息的一生）、03 篇（包头 = 信封）、06 篇（RPC 请求-响应）。

---

## 一、What：先看框架里两条路真实用在哪

框架同时有两条通往服务器的通道，官方定案在 `Framework/Network/README.md:62-84`：

| | 账号登录（`WinLogin`） | 游戏业务（`WinPlayerList` 之后） |
|---|---|---|
| 通道 | HTTP（`WebModule.Instance.Post`） | TCP/WS 长连接（`NetworkModule`） |
| 性质 | 一次性、无状态、低频 | 持续、有状态、高频 |
| 产物 | token + 角色列表 | 数据基线 + 增量推送 |

客户端真实调用（`Hotfix/Game/...`）：

```csharp
// HTTP 侧 —— 登录、建角色、拉列表（低频一次性）
var rspLogin = await WebModule.Instance.Post<RspUserLogin>(            // WinLogin.cs:138
    "http://127.0.0.1:28080/game/api/req_user_login", req, Token);
await WebModule.Instance.Post<RspPlayerCreate>(                          // WinPlayerCreate.cs:103
    "http://127.0.0.1:28080/game/api/req_player_create", m_req, Token);
var webBufferResult = await WebModule.Instance.GetToBytes(textureURL, ...); // CustomLoader.cs:308（下远程图片）

// TCP 侧 —— 进游戏后的实时玩法
await NetworkModule.Instance.Call<RspPlayerLogin>(new ReqPlayerLogin { ... }); // 06 篇的 RPC
```

服务器也分成了**两个独立应用**（`Server/FuFramework.Apps/`，故障域与扩缩容分离）：

```csharp
// Account（HTTP）：ReqUserLoginHttpHandler.OnUserLogin(ReqUserLogin)
//   —— 处理器无连接参数，无状态，扩容 = 加机器
// Player（TCP）：PlayerComponentAgent.OnPlayerLogin(INetworkChannel workChannel, ReqPlayerLogin reqLogin)
//   —— 处理器携带频道，连接即会话
```

所以问题就来了：**这两条路都会"请求-响应"，为什么不能全用 TCP？**

---

## 二、Why：TCP 的请求"贵"，HTTP 的请求"便宜"

### 2.1 能不能做 ≠ 该不该做

TCP 确实支持 Req/Rsp（06 篇的 `Call` + `UniqueId` 配对），但它的请求是**挂在一条长期独占的连接**上的。为了维持这根线，框架要持续付出保活代价（`MessageBase/IHeartBeatReqCreator.cs`）：

```csharp
public virtual float HeartBeatInterval => 5;        // 每 5 秒要发一次心跳
public virtual int MissHeartBeatCountByClose => 5;  // 丢 5 次（25 秒）连接就断
```

所以正确的问题不是"TCP 能不能做"，而是——**为了一次登录，值不值得长期占着这根线**。

### 2.2 核心比喻：打电话 vs 寄平信

```
TCP 长连接 = 煲电话粥   拨通后一直占线，谁有话说谁先说（可推送），但线要一直占着，
                       对方还必须一直守着电话（心跳保活、断线重连）。
HTTP      = 寄平信      即寄即断、零占用；每次寄信都是独立一单（无状态），
                       但永远是"你寄→它回"，它不可能主动打电话给你（无推送）。
```

- **电话能做的事（Req/Rsp）信也能做**——所以"TCP 能做请求-响应"没毛病；
- 但**电话的代价**（长期占线 + 保活）**只有在"要连着聊很久、且必须服务器主动开口"时才划算**；
- 登录、建号、拉列表是**低频一次性**操作，用电话纯属浪费。

### 2.3 除了"便宜"，HTTP 还有 TCP 给不了的附加价值

| HTTP 独有价值 | 说明 |
|---|---|
| 无状态 → 好扩容 | 登录洪峰来了**加机器**即可（README:74），不会挤占游戏服的会话内存 |
| 无状态 → 服务器可随便重启 | **登录服重启不影响在线玩家**；TCP 一断线全场掉线重连 |
| 能访问独立第三方服务 | `CustomLoader.cs:308` 用它 `GetToBytes` 直接下载远程图片纹理 |
| 签名鉴权 / HTTPS | 服务器 `HttpHandler.CheckSign` 签名校验；HTTPS 传账号密码是行业标准 |
| 网关/负载均衡友好 | HTTP 是全世界标准协议，CDN、Nginx、云防火墙原生支持、可限流监控 |

---

## 三、How：两条路如何共用同一套消息体系

### 3.1 身份信息放在哪 —— TCP 用"包头"，HTTP 用"信封"

两条路都要解决同一个问题：**服务器收到字节，必须知道消息类型才能反序列化**。载体不同：

```
TCP（协议内建包头）：  [包头 INetworkMessageHeader] → [包体]        // 03 篇的 14 字节信封
                          MessageId / UniqueId / OperationType / ZipFlag
HTTP（人为信封）：       MessageHttpObject { Id / UniqueId / Body }  // 09 篇主角
```

两边字段几乎一一对应（`Id↔MessageId`、`UniqueId↔UniqueId`）。HTTP 协议没有"游戏包头"这一区段，于是把 `MessageHttpObject` 整个装进 HTTP 请求体，**假扮成包头**。服务器端处理（`Server/.../HttpHandler.cs:58-68`）：

```csharp
bool isProtoBuf = contentType.Equals("application/x-protobuf");  // ① 看 Content-Type 走哪条路
...
MessageHttpObject messageHttpObject = Deserialize<MessageHttpObject>(body); // ② 先解信封（必成功）
Type type = MessageProtoHelper.GetMessageTypeById(messageHttpObject.Id);    // ③ 拿 Id 查类型
message = (MessageObject)Deserialize(messageHttpObject.Body, type);          // ④ 按类型解 Body
```

### 3.2 服务器怎么知道该走哪条路

- **内容类型**：HTTP 请求看 `Content-Type` 是否 `application/x-protobuf`（HTTP 处理器内部再分 JSON / Pb 两种）；
- **进程边界**：`Account`（HTTP）与 `Player`（TCP）是两个独立部署的应用；
- **推送只在 TCP**：增量推送、派发阀门（`SuspendNotifyDispatch`/`ResumeNotifyDispatch`）只挂在 `WinPlayerList` 的 TCP 会话上——**HTTP 是一问一答，不存在推送**（README:84）。

---

## 四、怎么选：一句话决策

| 场景 | 走哪条 |
|---|---|
| 登录/建号/拉列表/领奖签到 | HTTP（低频一次性，零占用，好扩容） |
| 拉一张远程图片/字符串 | HTTP（`GetToBytes`/`GetToString`） |
| 进游戏后的实时玩法、推送 | TCP（持续双向、服务器主动推） |
| 服务器要主动通知你 | **只能 TCP**（HTTP 无推送） |

选路原则浓缩成一句：**能低频就别占线，需要持续/推送才用 TCP。**

---

## 五、自测（随学随考）

### 题目

1. "TCP 也能做请求-响应"对吗？那为什么登录选项还要走 HTTP？
2. TCP 长连接为了维持连接，要付出什么保活代价（框架默认值）？
3. HTTP 相比 TCP 有哪些 TCP 给不了的附加价值？举三个。
4. TCP 的"包头"和 HTTP 的 `MessageHttpObject` 是什么关系？
5. 服务器端如何判断这个 HTTP 请求体是 protobuf 信封不是 JSON？
6. 增量推送为什么只存在于 TCP 会话中？
7. 登录服（Account）重启会影响正在玩的玩家吗？为什么？

### 答案

1. 对——TCP 靠 `Call` + `UniqueId` 配对实现 Req/Rsp。但 TCP 的请求挂在一条长期独占的连接上（心跳保活、断线重连、会话状态），为低频一次性操作占线是浪费；HTTP 即寄即断零占用、无状态好扩容，还附赠第三方访问/HTTPS/签名鉴权。核心是"代价与场景"，不是"能力"。
2. 每 5 秒发一次心跳；连续丢 5 次（约 25 秒）判定连接断开（`NetworkModule.Const` 分部常量）。
3. 无状态→加机器即扩容；无状态→登录服重启不影响在线玩家；能访问独立第三方服务（如下远程图片）；能接 HTTPS/签名/网关/负载均衡。（任举三个即可）
4. 干同一件事的两种载体：都是"先被解析的身份信息"（消息类型 + 单号）。TCP 是协议内建区段（`INetworkMessageHeader`），HTTP 没有此区段，于是用 `MessageHttpObject { Id/UniqueId/Body }` 假扮包头装进请求体。
5. 看 HTTP 请求头的 `Content-Type` 是否等于 `application/x-protobuf`，是则走信封分支，否则按 JSON 走别的分支（`HttpHandler.cs:58`）。
6. 推送是"服务器主动开口"，需要长期双向连接承载；HTTP 一问一答、服务器无法主动联系客户端。
7. 不会。登录服无状态，处理器不携带会话；重启后客户端下次请求照发、重新鉴权即可，不影响已建立 TCP 会话的在线玩家。

---

## 结语

TCP 能做请求-响应，不等于"够了"——**它的请求附带"长期占线"的代价，HTTP 的请求天生独立轻量**。框架的回答是两条路分工：**HTTP 管"你是谁"（身份与低频一次性操作），TCP 管"你在游戏里发生了什么"（实时、持续、可推送）**。而 `MessageHttpObject`，就是把 TCP 那种"包头"装进 HTTP 信封里的那一层——换路不换芯，消息体系里的类型识别、RPC 配对（UniqueId）完全复用同一套。