---
paths:
  - "**/*.cs"
---

# UniTask 异步与取消

对应铁律 1、2、5。详见 `coding/coding-standards.md`。

## 三条硬性

1. 禁止原生 `Task`，一律 `UniTask`。
2. 禁止 `Coroutine` / `StartCoroutine`，一律 `UniTask`。
3. 每条异步链必须有**生命周期所有者**。

## 取消契约（实测确立，勿凭直觉改）

**取消必须进入业务 await 点，不能靠外层包装。**

- 网络/资源类 API 的 `CancellationToken` **必须传参且无默认值**。
- 延时必须用 `UniTask.Delay(ms, cancellationToken: token)`——**第二个位置参数是 `ignoreTimeScale`，Token 必须用命名参数传**。
- 顶层对象（窗口 `OnClose`、模块 `OnDispose`）持有 `CancellationTokenIssuer` 并 `Cancel()`。
- helper 方法**透传调用方的 `CancellationToken`**，不自持、不自建 CTS。

## AttachExternalCancellation 的两个陷阱（实测）

1. **只切断它所包裹的那一层 await**。被包裹任务内部的子链（如裸 `UniTask.Delay(5000)`）作为孤儿照跑到完。叶子任务上测出的"取消成功"是假象。
2. **已取消 Token 下调用它**会走 `IsCancellationRequested → task.Forget(); return FromCanceled(...)` 分支：业务 async 方法同步段抛出的异常被 **`Forget()` 送进未观察通道静默吞掉**，真实 bug 借此隐身。

结论：`AttachExternalCancellation` 不能用于"窗口关闭自动取消在途处理器"。网络模块内部有使用，同样只断外层，改动时注意。

## fire-and-forget

调用无等待的异步方法必须 `.Forget()`：

```csharp
LoginAsync().Forget();
```

禁止静默 `_ = XxxAsync()`（既无等待也无 Forget，异常无落点）。

## 排查取消不生效的套路

1. 先做**裸组合隔离实验**（Delay 直接 attach vs 全链路），确认是包装层还是业务层的问题。
2. 再插桩打印 `token.IsCancellationRequested` 状态。
3. 纯逻辑推演不可靠——UniTask 源码 `UniTaskExtensions.cs` 的 `AttachExternalCancellationSource` 可作最终依据。

## 事务自有生命周期

长链操作（如"登录成功 → 加载场景 → 开主界面"）用**事务自有 Token**，不能沿用发起它的窗口 Token——窗口在事务中途关闭，其 Token 取消不应中断事务。参见 `GameSessionFlow.TransactionToken`。
