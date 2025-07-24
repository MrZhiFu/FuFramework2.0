## HOMEPAGE

GameFrameX 的 Coroutine 协程组件

**Coroutine 协程组件 (Coroutine Component)** - 提供扩展了Unity的内建协程管理功能的接口。

# 使用文档(文档编写于GPT4)

## 说明

`CoroutineComponent` 类是游戏开发框架的一部分，用于管理和执行Unity协程。该类扩展了Unity的内建协程管理功能，允许开发者更容易地控制协程的生命周期。通过使用并发字典来跟踪执行的协程，它为停止和清理协程提供了更好的控制。

## 主要功能

1. **启动协程**
   使用`StartCoroutine(IEnumerator enumerator)`方法来启动一个协程。这将迭代器和Unity的协程对象存储在一个并发字典中，确保可以随时访问和管理它们。

2. **停止协程**
   可以通过`StopCoroutine(IEnumerator enumerator)`或`StopCoroutine(UnityEngine.Coroutine coroutine)`方法停止单个协程。这些方法确保同时从Unity和内部字典中移除协程，防止内存泄漏。

3. **停止所有协程**
   通过`StopAllCoroutines()`方法停止所有正在运行的协程。该方法确保所有协程的干净停止，并清空内部跟踪字典。

4. **帧结束时回调**
   `WaitForEndOfFrameFinish(System.Action callback)`方法允许在当前帧渲染结束后执行一个回调。这对于在帧的最后进行计算或更新状态非常有用。

## 如何使用

### 启动一个协程

```csharp
IEnumerator YourCoroutine()
{
    // 协程执行的内容
    yield return null;
}

// ...

CoroutineComponent coroutineComponent = gameObject.AddComponent<CoroutineComponent>();
coroutineComponent.StartCoroutine(YourCoroutine());
```

### 停止一个协程

```csharp
// 假设您已经启动了一个协程，并且您有它的引用
IEnumerator yourCoroutine = YourCoroutine();
coroutineComponent.StopCoroutine(yourCoroutine);
```

### 停止所有协程

```csharp
// 停止该组件上所有正在运行的协程
coroutineComponent.StopAllCoroutines();
```

### 帧结束时执行回调

```csharp
void YourCallback()
{
    // 回调执行的内容
}

// ...

coroutineComponent.WaitForEndOfFrameFinish(YourCallback);
```

请注意，在添加`CoroutineComponent`到您的游戏对象时，确保您的场景中没有其他相同类型的组件，因为该类使用了`[DisallowMultipleComponent]`属性。

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.coroutine": "https://github.com/AlianBlank/com.gameframex.unity.coroutine.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.coroutine.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别