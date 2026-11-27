---
paths:
  - "Unity/Assets/Scripts/**/Framework/**"
---

# 事件系统

规模化改造已完成（2026-11-16），以下是**当前 API 基线**，写业务代码照此。

## 订阅与广播

```csharp
// 订阅（类型键，无事件 Id）
Subscribe<T>(handler);              // 泛型
Subscribe(Type, handler);           // 运行时（红点等动态场景）

// handler 签名：无 sender
void OnXxx(GameEventArgs e)

// 广播（无 sender）
Broadcast(e);                       // 按帧排队
BroadcastNow(e);                    // 立即派发
```

## 事件类编写模板

```csharp
public sealed class XxxEventArgs : GameEventArgs
{
    public int Source { get; private set; }   // private set 一律

    public static XxxEventArgs Create(...) { ... }   // Create 是唯一赋值入口
    public override void Clear() { /* 全字段复位 */ }
}
```

- 事件类**必须 `sealed`**。
- **不再有 Id / EventId 字段**——基类 `Id => GetType().FullName` 统一派生（实测 1M 次零分配）。
- **来源事实进 args 的 `Source` 字段**（Create 注入，`IsOwnEvent` 判 Source）——并发多播中区分"谁发的"的唯一通路。
- **不得修改转发缓存 `e`**。

## 生命周期

- 订阅方在 `OnOpen` 订阅、`OnClose` 退订，**成对**。幽灵订阅会导致重连/重进界面时重复执行事务。
- 窗口挂起/恢复由框架控制（`Suspend`/`Resume` 已收紧 `internal`，业务不得手动调）。
- 挂起期间的事件入缓冲，恢复后按序回放。

## 广播统计（Editor）

Editor 面板提供广播统计：无监听双桶——**从未订阅 = 红（真孤儿）**，**有历史订阅 = 黄（挂起预期态）**。按颜色判断是遗漏还是正常。

## 已裁决、勿重提

- **跨所有者共享委托静默失联 = 设计闭环，不修**。立场是单一所有者 + 订阅侧警告；引用计数方案已实证裁撤（零共享 + 事故未救场），不要再提议恢复。
- 不要恢复"sender 参数"或"事件 Id 字段"这类第二载体。
