# FuFramework Coroutine Module

## 简介
FuFramework Coroutine 模块是对 Unity 原生协程机制的增强封装。它提供了一个中心化的管理器 `CoroutineModule`，用于更安全、更可控地启动和停止协程。该模块解决了原生协程难以追踪、难以批量管理以及容易造成内存泄漏的问题。

## 特性
- **统一管理**：所有协程由 `CoroutineModule` 统一托管，模块销毁时自动清理。
- **双向映射**：内部维护了 `IEnumerator` 到 `Coroutine` 的映射，支持通过原始迭代器停止协程。
- **可视化调试**：在 Inspector 面板实时显示当前运行的协程数量及详细信息，方便排查泄漏。
- **便捷接口**：提供了 `WaitForEndOfFrameFinish` 等常用功能的封装。

## 核心类说明

### CoroutineModule
协程管理器，继承自 `FuModule`。
- **StartCoroutine**: 启动协程并记录。
- **StopCoroutine**: 停止指定协程（支持通过 `IEnumerator` 或 `Coroutine` 对象停止）。
- **StopAllCoroutines**: 停止所有由管理器启动的协程。
- **WaitForEndOfFrameFinish**: 在当前帧渲染结束后执行回调。

## 使用示例

### 1. 获取管理器
```csharp
var corModule = ModuleManager.GetModule<CoroutineModule>();
```

### 2. 启动协程
```csharp
IEnumerator MyTask()
{
    FuLogger.LogInfo("Task Started");
    yield return new WaitForSeconds(1.0f);
    FuLogger.LogInfo("Task Finished");
}

// 启动并保存引用（如果需要后续停止）
var enumerator = MyTask();
corModule.StartCoroutine(enumerator);
```

### 3. 停止协程
```csharp
// 方式一：通过迭代器引用停止（推荐）
corModule.StopCoroutine(enumerator);

// 方式二：通过 Coroutine 对象停止
// var coroutine = corModule.StartCoroutine(enumerator); // 注意：当前接口暂未直接返回 Coroutine 对象，需自行封装或使用方式一
```

### 4. 帧结束回调
常用于截屏或需要等待渲染完成的操作。
```csharp
corModule.WaitForEndOfFrameFinish(() =>
{
    FuLogger.LogInfo("当前帧渲染结束，可以进行截屏操作");
});
```

## 编辑器扩展
选中场景中的 `[ModuleManager]` 节点（运行时自动创建），在 Inspector 面板中找到 `CoroutineModule` 组件：
- **Count**: 当前正在运行的协程总数。
- **列表**: 展示每个协程对象的详细信息（ToString）。
