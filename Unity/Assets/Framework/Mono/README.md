## HOMEPAGE

GameFrameX 的 Mono 生命周期组件

**Mono 生命周期组件 (Mono Component)** - 用于管理游戏中 MonoBehaviour 的事件和更新周期，例如 FixedUpdate、LateUpdate、OnDestroy
等，并提供了一种简便的方式来添加和移除这些事件的监听。

# 使用文档(文档编写于GPT4)

关于 `MonoComponent` 类的说明文档如下：

## 概述

`MonoComponent` 类是基于 Unity 引擎的一个自定义 C# 脚本，它扩展自 `GameFrameworkComponent` 类。它用于管理游戏中 MonoBehaviour 的事件和更新周期，例如 FixedUpdate、LateUpdate、OnDestroy
等，并提供了一种简便的方式来添加和移除这些事件的监听。

## 功能特点

- **MonoManager 集成**: 该类与 `MonoManager` 协作，MonoManager 是用来管理 MonoBehaviour 生命周期相关事件的。
- **事件管理器**: 利用 `IEventManager` 实例来发布和订阅游戏事件。
- **生命周期监听**: 通过公共方法，允许添加和移除不同生命周期事件的监听，例如更新（Update）、固定更新（FixedUpdate）等。

## 使用方法

1. **初始设置**: 在类初始化时（`Awake` 方法内），会尝试获取 `IMonoManager` 和 `IEventManager` 模块。（如果获取失败，会记录一个致命错误并停止进一步执行。）

2. **事件注册和注销**:
    - **FixedUpdate**: 使用 `AddFixedUpdateListener(Action fun)` 来添加 FixedUpdate 事件的监听，使用 `RemoveFixedUpdateListener(Action fun)` 来移除监听。
    - **LateUpdate**: 使用 `AddLateUpdateListener(Action fun)` 来添加 LateUpdate 事件的监听，使用 `RemoveLateUpdateListener(Action fun)` 来移除监听。
    - **OnDestroy**: 使用 `AddDestroyListener(Action fun)` 来添加 OnDestroy 事件的监听，使用 `RemoveDestroyListener(Action fun)` 来移除监听。
    - **OnApplicationFocus**: 使用 `AddOnApplicationFocusListener(Action<bool> fun)` 来添加 OnApplicationFocus 事件的监听，使用 `RemoveOnApplicationFocusListener(Action<bool> fun)` 来移除监听。
    - **OnApplicationPause**: 使用 `AddOnApplicationPauseListener(Action<bool> fun)` 来添加 OnApplicationPause 事件的监听，使用 `RemoveOnApplicationPauseListener(Action<bool> fun)` 来移除监听。

3. **监听器方法**: 以上添加和移除监听的方法都会进行非空检查，如果传入的回调函数是 `null`，会记录一个致命错误。这是为了维护程序的健壮性。

## 开发者提示

- 调用公共方法之前请确保 `MonoComponent` 实例已处于激活状态。
- 当编写监听器回调函数时，请注意不要在这些函数中执行耗时操作，以免影响游戏性能。
- 正确管理事件监听器的注册和注销可以帮助避免内存泄露等问题。

注意：此组件依赖于Event 组件：https://github.com/AlianBlank/com.alianblank.gameframex.unity.event

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.mono": "https://github.com/AlianBlank/com.gameframex.unity.mono.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.mono.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别