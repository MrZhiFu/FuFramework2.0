# FuFramework Event Module

## 1. 简介

FuFramework Event 模块是一个高性能的事件管理系统。它提供了灵活的事件订阅/发布机制，集成引用池技术以减少 GC 压力。模块仅允许主线程访问；跨线程产生的事件（如网络 Socket 回调）由网络频道封送到主线程后再 Broadcast。

**核心身份模型（类型键）**：事件参数类型即事件标识——`XxxEventArgs` 类的 `typeof(T).FullName` 就是它的 ID（由基类 `BaseEventArgs.Id` 统一派生），订阅/退订走泛型形态 `Subscribe<T>(handler)`，广播走无 sender 形态 `Broadcast(e)`。string ID 已从公开 API 退场，仅在框架簿记层（挂起/重放）内部使用。

***

## 2. 特性

- 类型键订阅 ：事件参数类型即事件 ID，编译期防拼写错/防引错事件；广播与订阅的键天然同源
- 仅主线程   ：公共 API 仅允许主线程调用，开发期（UNITY_ASSERTIONS）有断言拦截跨线程误用
- 延迟处理   ：事件默认在下一帧统一处理，避免在处理事件时修改订阅列表导致的异常
- 多播支持   ：支持一个事件对应多个处理函数；同一处理函数重复订阅被幂等去重（记录警告日志），每事件只调用一次
- 对象池集成 ：事件参数和事件节点都通过引用池管理，减少 GC 压力
- 模块级管理 ：`EventRegister` 提供模块级的事件订阅管理，窗口关开自动挂起/重放（业务零手动配对）
- 广播统计   ：编辑器期按事件 ID 计数/最近时间/无监听双桶（真孤儿 vs 预期态），调试面板可见

***

## 3. 核心概念

### 3.1 事件参数基类

```csharp
public abstract class BaseEventArgs : IReference
{
    /// 事件 ID（基类统一派生：具体事件类的 GetType().FullName，事件类无需再声明 Id/EventId）
    public virtual string Id => GetType().FullName;

    /// 清理引用（归还引用池时调用）
    public abstract void Clear();
}

/// 游戏逻辑事件基类（划定订阅边界：EventHandler 只放行游戏事件）
public abstract class GameEventArgs : BaseEventArgs { }
```

***

## 4. 核心类说明

### 4.1 EventModule

事件管理模块，继承自 `ModuleBase`，是事件系统的核心管理类。

主要方法：

```csharp
// 订阅/取消订阅事件（类型键）
void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
void Unsubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs

// 订阅/取消订阅事件（运行时类型形态：事件类型编译期未知时使用，如红点触发事件集合）
void Subscribe(Type eventType, Action<GameEventArgs> handler)
void Unsubscribe(Type eventType, Action<GameEventArgs> handler)

// 抛出事件（延迟到下一帧主线程分发；无 sender）
void Broadcast(GameEventArgs e)

// 立即抛出事件（同步处理，仅限主线程调用；无 sender）
void BroadcastNow(GameEventArgs e)
```

> `Subscribe` 幂等去重：同一事件同一处理函数重复订阅只保留首次登记并记录警告日志，分发时只调用一次；
> `Unsubscribe` 立即整体摘除条目（缺失条目安全空操作）。本框架中同一处理函数只有一个所有者，不存在跨订阅者共享场景。

> 内部另有 `internal Subscribe/Unsubscribe(string id, Action<GameEventArgs>)` 簿记入口：
> 仅供挂起/重放按 (id, handler) 对遍历账本使用（泛型无法从运行时 id 反推），业务勿直接调用。

***

### 4.2 EventRegister

事件注册器，用于模块级的事件订阅管理，实现 `IReference` 接口支持引用池。

功能特点：

- 集中管理一个模块的所有事件订阅
- 支持一键取消所有订阅（`UnSubscribeAll`）
- 自动与 `EventModule` 交互

主要方法：

```csharp
// 创建
static EventRegister Create()

// 订阅/取消订阅（类型键）
void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
void UnSubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
void UnSubscribeAll()                   // 取消所有订阅

// 派发事件
void Broadcast(GameEventArgs eventArgs)
void BroadcastNow(GameEventArgs eventArgs)

// 挂起/重放（由框架在窗口生命周期自动调用，业务勿手动调）
void Suspend()                          // 窗口关闭时：按账本逐条总线退订，账本保留即重放清单
void Resume()                           // 窗口打开时（先于业务 OnOpen）：按账本重放订阅

// 释放
void Clear()                            // 清理（自动调用 UnSubscribeAll，并复位挂起标志）
void Release()                          // 归还引用池
```

生命周期契约（窗口订阅挂起）：

- 窗口关闭（`WinBase._OnClose`）时框架调用 `Suspend()`：按账本逐条从总线退订，账本保留即重放清单；同时窗口计时器终止（`StopAllTimers`，破坏性不重放）
- 窗口打开（`WinBase._OnOpen`）时框架调用 `Resume()`：按账本逐条重放订阅，先于业务 `OnOpen` 执行
- 挂起态下的 `Subscribe` 只记账本不进总线（重开时由 `Resume` 统一重放）；`Subscribe` 幂等去重（重复登记警告+忽略），保证账本与总线 1:1、挂起/重放严格对称
- **窗口类业务代码勿在 `OnClose` 调 `UnSubscribeAll`**（全清语义会连带框架自身的语言订阅等，且池复用路径不重订）——窗口订阅生命周期已由挂起/重放托管，业务无需手动退订
- `Clear()` 复位挂起标志：寄存器经引用池回池复用且 Acquire 不重置字段，漏复位会让新窗口的订阅只进账本不进总线（事件静默失效）

***

### 4.3 EventModule 内部机制

核心容器（原独立事件池 `EventPool<T>` 已并入 `EventModule`），管理事件的订阅、发布和处理。

核心机制：

- 事件队列  ：使用 `Queue<Event>` 存储待处理事件，仅主线程访问
- 多值字典管理订阅  ：`FuMultiDictionary<string, Action<GameEventArgs>>` 存储事件处理函数（键为事件参数类型 FullName）
- 幂等订阅  ：同一事件同一处理函数重复订阅只保留首次登记（分发时只调用一次），重复订阅记录警告日志——
  本框架中同一处理函数只有一个所有者，重复订阅几乎必然是「池化对象重复订阅未退订」类误用
- 即时退订  ：`Unsubscribe` 立即从订阅字典移除条目，未订阅过的条目静默忽略；
  分发前快照保证分发过程中的订阅/退订不影响本轮（被退订的 handler 本轮仍收到当次事件）
- 主线程契约  ：无锁设计，公共 API 仅允许主线程调用，开发期（UNITY_ASSERTIONS）断言拦截跨线程误用

工作流程：

1. 订阅阶段  ：`Subscribe<T>` 将处理函数按 `typeof(T).FullName` 添加到多值字典（已存在则警告并忽略，不重复入字典）
2. 取消订阅阶段  ：`Unsubscribe<T>` 立即从多值字典移除条目
3. 事件处理阶段  ：`OnUpdate` 从队列取出事件，快照该事件的 handler 列表后逐个调用
4. 清理阶段  ：事件处理完成后，通过引用池释放事件参数

> 注意：事件参数在分发结束（`Broadcast` 的下一帧分发或 `BroadcastNow` 立即分发）后即被回收并 `Clear`，
> 处理函数**不得修改、转发或缓存收到的 `e`**——修改会让同轮后续 handler 读到脏数据；缓存/转发会观测到已清空的数据（回收后可能被复用形成脏读）；
> 需要转发时请新建事件参数对象。

***

### 4.4 事件参数类契约（唯一编写模板）

新事件参数类一律按此模板编写（AI/人工同规）：

1. **属性一律 `private set`**：仅 `Create` 工厂在构造期赋值；发布后外部只读（结构防线：handler 想改也编译不过）
2. **不声明 `Id`/`EventId`**：基类 `BaseEventArgs.Id => GetType().FullName` 统一派生
3. **`Clear()` 全字段复位**：漏一项 = 池化复用后带脏数据
4. **`Create(object source, ...)` 注入来源**（仅并发多播需区分「谁发的」的事件需要）：sender 已退场，来源是事实就进 args（参考 `DownloadAgentHelper*EventArgs.Source`）
5. `sealed` 类：子类化会破坏类型键（`Subscribe<基类>` 广播子类实例会进两个不同频道）

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

public sealed class XxxEventArgs : GameEventArgs   // Id 由基类统一派生，类内不再声明
{
    /// <summary>
    /// 字段说明。
    /// </summary>
    public int Field1 { get; private set; }        // private set 契约

    /// <summary>
    /// 字段说明。
    /// </summary>
    public long Field2 { get; private set; }

    /// <summary>
    /// 清理引用(归还引用池时调用)。全字段复位，漏一项 = 池化复用后带脏数据。
    /// </summary>
    public override void Clear()
    {
        Field1 = 0;
        Field2 = 0;
    }

    /// <summary>
    /// 创建事件参数（构造期唯一赋值点）。
    /// </summary>
    /// <param name="field1">字段说明。</param>
    /// <param name="field2">字段说明。</param>
    /// <returns>创建的事件参数。</returns>
    public static XxxEventArgs Create(int field1, long field2)
    {
        var e = ReferencePool.Acquire<XxxEventArgs>();
        e.Field1 = field1;
        e.Field2 = field2;
        return e;
    }
}
```

***

## 5. 使用示例

### 5.1 定义事件

按 4.4 模板新建 `XxxEventArgs.cs`，放在所属模块的 Event 目录下（游戏侧新事件归 `Hotfix.Game.Events`）。
事件无需单独的 ID 常量——类型即 ID。

### 5.2 订阅事件

```csharp
using Hotfix.Framework.Event;

public class PlayerController
{
    private EventRegister m_eventRegister;

    private void InitEvent()
    {
        // 订阅放 OnInit/构造（每实例一次），关开生命周期由挂起机制托管，业务无需配对退订
        m_eventRegister.Subscribe<PlayerDamageEventArgs>(OnPlayerDamage);
        m_eventRegister.Subscribe<PlayerLevelUpEventArgs>(OnPlayerLevelUp);
    }

    private void OnPlayerDamage(GameEventArgs e)
    {
        if (e is not PlayerDamageEventArgs args) return;

        // 处理事件（只读 args；不得修改/缓存/转发 e）
    }
}
```

> 非池化对象（MonoBehaviour 等）退订：`Unsubscribe<PlayerDamageEventArgs>(OnPlayerDamage)`。
> 缺失条目安全空操作，无需判存。

### 5.3 发布事件

```csharp
// 创建并发布（事件参数会在处理完成后自动归还引用池，无需手动释放）
Broadcast(PlayerDamageEventArgs.Create(damage, attacker, hitPosition));

// 立即分发（同步执行，仅限主线程）
BroadcastNow(PlayerLevelUpEventArgs.Create(newLevel, oldLevel));
```

### 5.4 使用 EventRegister 进行模块级事件管理

```csharp
public class UIModule
{
    private EventRegister m_eventRegister;

    private void Start()
    {
        // 创建事件注册器
        m_eventRegister = EventRegister.Create();

        // 订阅（类型键）
        m_eventRegister.Subscribe<PlayerDamageEventArgs>(OnPlayerDamageUI);
        m_eventRegister.Subscribe<PlayerLevelUpEventArgs>(OnPlayerLevelUpUI);
    }

    private void OnPlayerDamageUI(GameEventArgs e)
    {
        if (e is not PlayerDamageEventArgs args) return;

        ShowDamageNumber(args.Damage, args.HitPosition);
    }
}
```

***

## 6. 事件系统

### 6.1 事件处理流程

```
1. 订阅阶段
   Subscribe<T>(handler) -> 按 typeof(T).FullName 添加到 m_actionHandlerMultiDict（已存在则警告并忽略）

2. 取消订阅阶段
   Unsubscribe<T>(handler) -> 立即从 m_actionHandlerMultiDict 移除条目

3. 发布阶段
   Broadcast(e) -> 创建 Event 节点 -> 加入 m_eventQueue

4. 处理阶段（OnUpdate）
   从 m_eventQueue 取出事件
   -> 快照该事件的 handler 列表（隔离分发过程中的订阅/退订）
   -> 调用所有匹配的 handler
   -> ReferencePool.Recycle(args) 释放事件参数
```

### 6.2 主线程契约说明

事件模块仅允许主线程访问：公共 API 入口均有 `AssertMainThread` 断言（`[Conditional("UNITY_ASSERTIONS")]`，仅 Editor/Development 构建生效，发布版被剥离、零运行时开销），跨线程调用在开发期立即报错定位。

| 方法                                                                                                       | 处理时机 | 说明 |
| -------------------------------------------------------------------------------------------------------- | ---- | --------- |
| `Broadcast(e)`    | 下一帧  | 事件入队，`Update` 时统一分发，通用场景，推荐   |
| `BroadcastNow(e)` | 立即（同步）   | 仅限主线程，需要同步处理的场景 |
| `Subscribe<T>` / `Unsubscribe<T>` | 立即    | 仅限主线程   |
| `EventCount` / `EventHandlerCount` / `ForEachHandler` / `ForEachEvent` / `ForEachBroadcastStat` / `ClearBroadcastStats`（仅 Editor，见 `EventModule.Debug.cs`） | 立即    | 仅限主线程；遍历回调内可重入触发分发/遍历，重入时使用局部快照互不干扰 |

跨线程产生的事件（如网络 Socket 回调）不得直接调用事件模块，由网络频道封送到主线程后（channel.Update 排水）再 `Broadcast`。

实现机制：

- 无锁设计  ：单线程契约下无需加锁，事件队列与处理器字典均仅主线程访问
- 分发前快照 handler 列表，分发过程中的订阅/退订不影响本轮（快照隔离）

***

## 7. 编辑器功能

### 7.1 事件调试面板

`EventModule` 的调试面板（`EventModuleWindow`，位于 `Assets/Editor/FuFramework/Event/`），仅在 Play 模式下可用。

功能：

- 数据源  ：`EventModule.Debug.cs` 调试分部（`#if UNITY_EDITOR` 整文件包裹，真机包不编译）提供 `EventCount` / `EventHandlerCount` / `ForEachHandler` / `ForEachEvent` / `ForEachBroadcastStat` / `ClearBroadcastStats`
- 模块总览  ：显示待分发事件数、订阅事件 ID 数、handler 总数
- 订阅明细  ：按事件 ID 分组，展示每个 handler 的方法名/所属类型/静态或实例标记，支持跳转源码
- 事件队列  ：显示待分发队列中的每条事件，支持一键清空
- 广播统计  ：按事件 ID 展示广播次数/最近广播时间/无监听双桶——「从未订阅」红行（真孤儿：拼写错/漏订阅）、「有历史订阅」黄行（挂起中窗口或已退订，预期态），支持清零统计
- 交互  ：搜索过滤（事件 ID / 方法名 / 所属类型）、自动刷新、全部展开/折叠

> 排障口诀：「事件没反应」先看广播统计——没广播次数=发送方没发/发错事件类；有次数但订阅明细里没你=订阅没生效（宿主没建 Register/挂起中）；都有但行为不对=查 handler 逻辑与参数类型（`is not XxxEventArgs` 早退）。

使用方法：

1. 在编辑器中运行游戏（Play 模式）
2. 打开菜单 `FuFramework/调试/事件调试面板`

***

## 8. 目录结构说明

```text
Event/
├── EventModule.cs               # 事件管理模块（模块生命周期 + 状态字段 + 清理/断言）
├── EventModule.API.cs           # 事件管理模块公共 API（类型键订阅/退订/无 sender 抛出）
├── EventModule.Dispatch.cs      # 帧分发机关（OnUpdate 批次排水 + HandleEvent 单事件分发）
├── EventModule.Event.cs         # 事件节点定义
├── EventModule.Debug.cs         # 调试分部（#if UNITY_EDITOR：面板数据源 + 广播统计实现）
├── EventRegister.cs             # 事件注册器（账本 + 挂起/重放簿记）
├── Event/                       # 事件参数
│   ├── BaseEventArgs.cs         # 事件参数基类（Id 统一派生）
│   └── GameEventArgs.cs         # 游戏事件参数基类
└── README.md                    # 本文档
```

***

## 9. 依赖

- Unity  : 2021.3 LTS 或更高版本
- Hotfix.Framework.Core  : 框架核心模块
- Hotfix.Framework.ReferencePool  : 引用池模块

***

## 10. 注意事项

1. 事件参数契约  ：属性一律 `private set`、仅 `Create` 构造期赋值、`Clear()` 全字段复位；处理函数**不得修改、转发或缓存收到的 `e`**（见 4.4 契约）
2. 主线程限制  ：所有公共 API 仅允许主线程调用（开发期有断言拦截跨线程误用）；跨线程产生的事件由网络频道封送主线程后再 Broadcast
3. 事件处理顺序  ：同一事件的多个处理函数按订阅顺序调用
4. 异常处理  ：事件处理函数中的异常会被捕获并记录，不会影响其他处理函数
5. 对象池  ：事件参数对象会自动通过引用池管理，无需手动释放，但需正确实现 `Clear` 方法
6. 即时退订  ：退订立即生效；仅分发过程中（handler 内）的退订因快照语义，本轮仍会收到当次事件
7. 类型即 ID  ：事件参数类必须 `sealed`——子类化会让 `Subscribe<基类>` 与子类实例广播分属两个频道
8. 生命周期边界  ：事件模块关停（`OnDispose`）后不得再调用 `Broadcast`——事件节点「取出 → 入队」之间存在极小竞态窗口，滞留事件将永不分发、永不回收
9. 遍历接口  ：`ForEachHandler` / `ForEachEvent` / `ForEachBroadcastStat` 仅限主线程调用；`ForEachEvent` 回调内不得修改或回收未分发的事件参数
