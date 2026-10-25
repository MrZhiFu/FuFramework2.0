# 02 · Socket 与 TCP（传输层）

> 本篇范围：TCP 到底给了什么承诺、不承诺什么（粘包的根源）；`SystemNetSocket` 是什么；`BeginReceive` 为什么"等待不占线程"；项目为什么在持续收发上用 APM 而不是 async/await；收发两侧的蓄水池与队列如何分工。

---

## 一、What：TCP、Socket、粘包——先有世界观

### 1.1 TCP 的三个承诺（和不承诺）

网络传输两大流派：**UDP = 寄明信片**（快，但可能丢、可能乱序），**TCP = 打电话**（先接通，再一句接一句）。本模块走 `tcp://ip:port`。

| 承诺 | 含义 | 类比 |
|------|------|------|
| 可靠 | 发出的字节一定到达，丢失自动重发 | 说过的每句话对方都听见 |
| 有序 | 到达顺序 = 发送顺序 | 不会前后颠倒 |
| 字节流 | 像水管里的水，连续不断 | ——（坑在这） |

**TCP 不承诺"消息边界"**。它只说"字节按序到达"，从不说"一条消息到齐了"。假设依次发 A（4 字节）、B（2 字节）、C（6 字节），对端一次 `Receive` 可能拿到任意组合：

```
发件人视角：   [AAAA][BB][CCCCCC]
                              ↓ 进入 TCP 水管（杯子被打散，没有消息边界）
命运①： 第 1 次 [AAAAB]       ← A 整条 + B 的前 1 字节（粘包 + B 被切半截）
         第 2 次 [BCCCCCC]     ← B 后 1 字节 + C 整条
命运②： 一次   [AAAABBCCCCCC]  ← 三条全挤一坨（纯粘包）
命运③： 一次   [AA]            ← A 都没到齐（半包）
```

**数据守恒校验**：不管哪种命运，把收到的字节拼起来永远是 `AAAA BB CCCCCC` 这 12 个字节——不多、不少、不倒。TCP 三个承诺一条没违约，它只是对"消息到齐"装不知道。**因为水管里根本没有"消息"这个概念，只有连续字节。消息边界是你和服务器之间的约定，必须自己造"信封"来砌**（第 3 篇）。

### 1.2 `SystemNetSocket` 是什么

`Socket/SystemNetSocket.cs`（约百行）是 `System.Net.Sockets.Socket` 的**薄封装**——每个方法基本是一行转发：

```csharp
public int EndSend(IAsyncResult asyncResult, out SocketError error)
    => m_socket.EndSend(asyncResult, out error);
```

为什么要包一层？看接口：

```
INetworkSocket（接口：收/发/连/关 —— 频道眼中的"电话线"）
  ├── SystemNetSocket      ← TCP 走系统 Socket
  └── WebSocketNetSocket   ← WebSocket 走 UnityWebSocket 插件
```

对比两份实现会发现 `WebSocketNetSocket.LocalEndPoint => null`——WebSocket 根本没有"本地端点"概念，接口的抽象就在这种细节显形。**频道只认接口，TCP 换 WS 频道逻辑零改动。**

---

## 二、Why：`BeginReceive` 为什么"等待不占线程"；为什么热路径用 APM

### 2.1 APM 是什么，等待为什么零线程

APM（Asynchronous Programming Model，.NET 最老异步模型）——**回执单模型**：

```csharp
IAsyncResult ar = socket.BeginReceive(buffer, 0, len, callback, state);
// ① 下单：把"数据到写这个缓冲区、然后叫我"整体交给操作系统（Windows = IOCP 完成端口）
//      几微秒返回 —— 之后没有任何线程在等数据
// ② 数据到了：OS 把"完成"放进完成队列 → 唤醒 .NET 线程池 → 派一个线程跑 callback
// ③ 回调里 EndReceive(ar) 取结果：实际收到的字节数（不由你决定，0 ~ 上限任意）
```

关键：**等待阶段零线程**。如果每个 Socket 配一个专职线程等数据，几千连接就是几千个睡着的线程，光栈内存就爆；IOCP 用**少量线程服务海量连接**——没事不占资源，谁的数据到谁被唤醒干活。

点外卖类比：同步 `Receive()` = 一直举着电话问"到了吗"（线程被占死）；`BeginReceive` = 挂电话该干嘛干嘛，外卖到门口骑手（线程池线程）按门铃——骑手只管送的那一下，不是一直蹲在门口。

### 2.2 为什么持续收发用 APM 而不是 async/await

async/await 是同一件事的编译器糖（底层还是 OS 异步 + 线程池 + 回调），但有一个硬约束：**await 的续体绑定线程**（Unity 里封送回主线程）。

持续接收有个硬需求：数据源源不断，循环必须"收 → 入队 → 再收"连轴转。看真实代码 `ReceiveCallback` 的收尾：

```csharp
finally
{
    // 无论成功、解析失败还是异常，都必须续接接收，避免连接静默卡死。
    ReceiveAsync();   // 回调自己发起下一轮 BeginReceive → 后台永动机
}
```

如果用主线程 `while(true) await socket.ReceiveAsync...`，主线程节奏被**对端**决定而非帧率，游戏主循环被冲乱。而 APM 天然适配"后台连轴转取数据 + 主线程按帧取货"的两线程结构。

**但项目不是排斥 await**——同一处网络里，"连接"这个一次性等待用 `UniTask`（`WebSocketNetSocket.ConnectAsync`，签名 `async UniTask ConnectAsync(CancellationToken)`，必传取消令牌）：

| 操作 | 频率 | 线程诉求 | 模型 |
|------|------|---------|------|
| 连接 | 一次 | 等一个结果，主线程 await 正合适 | ✅ UniTask（**仅 WebSocket 连接与 DNS 解析**；TCP 连接同为 APM `BeginConnect`） |
| 持续收发 | 每包 | 后台连轴转 + 主线程按帧取货 | ✅ APM |

**结论：不是"不用 await"，而是"每一处放对技术"。**（呼应第 1 篇"频道层层抽象"。）

---

## 三、How：蓄水池、两套队列与"指针追总量"算法

### 3.1 两条 FIFO 队列（`FuLinkedList` + 锁）

`FuLinkedList` 的四个关键操作 = **食堂排队**：`AddLast(x)` 入队尾 / `First` 看队头（`.Value` 才是消息）/ `RemoveFirst()` 弹队头 / `Clear()` 清空。

| 队列 | 生产者 | 消费者 | 锁 |
|------|-------|--------|----|
| `m_SendPacketPool` | 主线程入队发送 | 主线程每帧取货 | `lock(m_SendPacketPool)` |
| `m_waitingHandleMsgLkList` | **后台回调** `AddLast` | **主线程** `First/RemoveFirst` | `m_handleMsgLock` |

为什么用链表？`AddLast`（尾）与 `RemoveFirst`（头）都是 O(1)；`List` 头部移除是 O(n) 拷贝——热路径每个包各一次入队/出队，链表是刻意选择。

### 3.2 发送侧为什么会"续发"

一条消息先序列化进 `SendState.Stream`（Length=整条字节数，Position 归 0 后开始发）：

```
BeginSend（从 Position 发到 Length）→ 一轮可能只发出去一部分
SendCallback:  Position += bytesSent
  - Position < Length  → 还有货没发完 → 续发 SendAsync()（从新 Position 接着发）
  - Position == Length → 全部发完 → Reset() 清箱
```

`ProcessSend` 开头还有一道**闸门**（`NetworkChannelBase`）：

```csharp
// 同批只允许一批在途：
// ① Stream.Length > 0：上一批还没发完（发完才 Reset 清零），不能再灌新消息，避免两批混在一个流里；
// ② 发送队列已空：没有可发的消息。
if (m_SendState.Stream.Length > 0 || m_SendPacketPool.Count <= 0) return false;
```

`Length` 与 `Position` 两句人话：
- **`Length` = 这箱货总共多重；`Position` = 已经卸下多少。**
- `Position < Length` 说明没卸完接着卸；`Position == Length` 说明卸空装下一箱。
- 发送侧与接收侧共用同一套"指针追总量"算法（接收侧见第 3/4 篇的 `Position < Length` 攒包判断）。

### 3.3 线程分工全景（修正版）

```
【后台线程池线程】                    【主线程】每帧（Update）
ReceiveCallback                       DrainLifecycleEvents
  EndReceive → bytesReceived          if (m_Socket == null || !IsOpen) return
  ReceiveState.Stream 水位推进         ProcessSend()          （从发送队列取货）
  ProcessReceivedBytes：切包/反序列化   ProcessReceive()        （基类占位）
  lock 里执行链表 .AddLast(完整消息)    m_rpcState.Update()    （RPC 挂起表超时轮询）
  finally: ReceiveAsync() 续接         ProcessHeartBeat()      （心跳）
                                       ProcessReceivedMessage()（从链表取货派发）
```

**关键**：切包、反序列化**全在后台回调线程**完成；跨线程传的不是字节，而是**解析好的完整消息对象**。主线程只做轻量取货派发。

---

## 四、自测（随学随考）

### 题目

1. TCP 给你三个承诺；它**不**给你什么？粘包/半包为什么必然出现？
2. 客户端连发 A=4、B=2、C=6 字节，对端一次 `EndReceive` 返回 `AAAAB`，代表什么？下一次返回 `BCCCCCC`，怎么证明数据没丢没重？
3. `BeginReceive` 的"等待"是谁在等？为什么不用给每条 Socket 配专职线程？
4. "APM 是爷爷辈，async/await 是孙子辈"——那本项目为什么还在热路径上用 APM？
5. `m_waitingHandleMsgLkList` 的 `AddLast` 和 `RemoveFirst` 分别在哪个线程执行？为什么这里必须加锁？
6. `SendCallback` 里为什么 `Position < Length` 时还要继续 `SendAsync()`？（起点若写死 0 会怎样？）
7. 一帧里`ProcessSend` 的闸门 `Stream.Length > 0` 在防什么？

### 答案

1. TCP 承诺可靠、有序、字节流；**不承诺消息边界**（也不知道"消息"）。"一次 Receive 返回多少"对端自由决定 → 多条消息可能挤在一次返回（粘包）、一条被切成多次（半包）都合法。
2. `AAAAB` = A 整条 + B 的前 1 字节（粘包 + 半包叠加）；下一次 `BCCCCCC` = B 剩余 1 字节 + C 整条。拼接 `AAAAB + BCCCCCC = AAAA BB CCCCCC`，与发送内容完全一致——证明没丢、没重、没乱序（TCP 的三个承诺没违约）。
3. **没有人在等**。请求挂到操作系统异步完成机制（IOCP）上，等待期间零线程占用；数据到达后由线程池派一个线程跑回调。专职线程方案在几千连接下栈内存爆炸。
4. 因为"持续接收"需要一个后台连轴转的循环，而 await 的续体绑线程、Unity 还封送回主线程——await 塞不进"后台持续收 + 主线程按帧取货"的两线程结构。项目对"一次性连接"仍用 UniTask（每处放对技术）。
5. `AddLast` 在后台回调线程；`RemoveFirst` 在主线程。两个线程同时摸同一个链表会把结构撕坏，故必须用 `m_handleMsgLock` 互斥。
6. 因为 TCP 一次 `Send` 不保证发完（发送缓冲区可能满），`bytesSent` 可能小于剩余量，必须"发一点、看进度、没完续发"。起点若写死 0，前 40 字节会被重复发送 → 接收方按信封长度切包会切出"缝合怪"，消息永久损坏。
7. 防止两批消息混进同一个 `SendState.Stream`。只有发完（`Position==Length` 触发 `Reset` 把 Length 清零）才能灌下一批；否则两批货混在一个集装箱，对端按 `PacketLength` 切包就全乱套。

> 下一站：那个 14 字节"信封"长什么样、每字节为何存在、以及字节序（网络大端 vs 内存小端）的真相。