# FuFramework Scene Module

## 1. 简介

FuFramework Scene 模块是游戏框架的场景管理系统，基于 YooAsset 的 `SceneHandle` 实现场景资源的异步加载和卸载。该模块提供完整的场景加载进度回调和丰富的事件通知机制。

## 2. 核心特性

- **异步加载**：基于 YooAsset + UniTask 的异步场景加载，避免卡顿
- **进度回调**：加载过程中提供实时进度反馈
- **场景卸载**：支持异步场景卸载
- **事件通知**：覆盖加载/卸载成功、失败、进度更新和激活场景切换事件
- **YooAsset 集成**：场景作为资源通过 YooAsset 管理，支持 AssetBundle 模式

## 3. 核心概念

### 3.1 场景架构

```
┌─────────────────────────────────────────────────────────────┐
│                     SceneModule                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  LoadSceneAsync() → SceneHandle                     │   │
│  │  UnloadSceneAsync() → UniTask                       │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  事件通知:                                          │   │
│  │  - LoadSceneUpdateEventArgs   (进度更新)            │   │
│  │  - LoadSceneSuccessEventArgs  (加载成功)            │   │
│  │  - LoadSceneFailureEventArgs  (加载失败)            │   │
│  │  - UnloadSceneSuccessEventArgs(卸载成功)            │   │
│  │  - UnloadSceneFailureEventArgs(卸载失败)            │   │
│  │  - ActiveSceneChangedEventArgs (激活场景切换)       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │    YooAsset      │
                    │  SceneHandle     │
                    └──────────────────┘
```

## 4. 核心类说明

### 4.1 SceneModule

场景管理模块，继承自 `ModuleBase`，实现 `ICancelAsync`（可取消异步对象）。

> **可取消异步**：`SceneModule` 实现 `ICancelAsync`（`Token` + `CancelAsync`）。
> 模块销毁（`OnDispose`）时触发 `Token` 取消，在途场景加载随之中止（释放句柄、不登记加载字典，
> 抛 `OperationCanceledException`），加载/卸载完成回调也不再广播事件与登记，杜绝旧生命周期残留。
> 框架重启 `RestartGame` 会在重启前 `await` 各模块 `CancelAsync` 等待清理，保证重新初始化前旧生命周期零在途残留；
> `OnInit` 重建 `CancellationScope`（新 Token = 新生命周期），重启后可正常使用。
> `LoadScene`/`LoadSceneByName` 的 `CancellationToken` 参数**必传**（调用方生命周期令牌，窗口传 `WinBase.Token`），与模块自身 Token 竞速。

**核心方法：**

```csharp
// 加载场景（token 必传：调用方生命周期取消令牌）
UniTask<SceneHandle> LoadScene(string sceneAssetPath, CancellationToken token, LoadSceneMode sceneMode = LoadSceneMode.Additive,
    object userData = null)
UniTask<SceneHandle> LoadSceneByName(string sceneAssetName, CancellationToken token, LoadSceneMode sceneMode = LoadSceneMode.Additive,
    object userData = null)

// 卸载场景
void UnloadScene(string sceneAssetPath, object userData = null)

// 查询场景状态
bool IsLoaded(string sceneAssetPath)
bool IsLoading(string sceneAssetPath)
bool IsUnloading(string sceneAssetPath)
bool HasScene(string sceneAssetPath)
string GetSceneName(string sceneAssetPath)

// 获取场景路径列表
string[] GetAllLoadedSceneAssetPaths()
void GetAllLoadedSceneAssetPaths(List<string> results)
string[] GetAllLoadingSceneAssetPaths()
void GetAllLoadingSceneAssetPaths(List<string> results)
string[] GetAllUnloadingSceneAssetPaths()
void GetAllUnloadingSceneAssetPaths(List<string> results)
```

### 4.2 场景事件

| 事件类 | 说明 |
|------|------|
| `LoadSceneUpdateEventArgs` | 场景加载进度更新（含 Progress 0-1） |
| `LoadSceneSuccessEventArgs` | 场景加载成功 |
| `LoadSceneFailureEventArgs` | 场景加载失败（含错误信息） |
| `UnloadSceneSuccessEventArgs` | 场景卸载成功 |
| `UnloadSceneFailureEventArgs` | 场景卸载失败 |
| `ActiveSceneChangedEventArgs` | Unity 激活场景切换通知 |

## 5. 使用示例

### 5.1 加载场景

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Scene;
using UnityEngine.SceneManagement;

public class SceneExample
{
    private SceneModule m_SceneModule;

    public void Init()
    {
        m_SceneModule = ModuleManager.GetModule<SceneModule>();
    }

    public async UniTask LoadGameSceneAsync()
    {
        var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）；窗口传 WinBase.Token
        // 异步加载场景
        SceneHandle handle = await m_SceneModule.LoadScene(
            "Assets/Game/Scenes/GameScene.unity",
            token,
            LoadSceneMode.Single
        );

        Debug.Log("场景加载完成");
    }

    public async UniTask LoadAdditiveSceneAsync()
    {
        // 叠加加载场景（不卸载当前场景，Additive 是默认值）
        SceneHandle handle = await m_SceneModule.LoadScene(
            sceneAssetPath: "Assets/Game/Scenes/Dungeon_01.unity"
        );
    }
}
```

### 5.2 卸载场景

```csharp
// 卸载叠加场景
m_SceneModule.UnloadScene("Assets/Game/Scenes/Dungeon_01.unity");
```

### 5.3 监听场景事件

```csharp
var eventModule = ModuleManager.GetModule<EventModule>();

// 监听加载进度
eventModule.Subscribe(LoadSceneUpdateEventArgs.EventId, (sender, e) =>
{
    var args = e as LoadSceneUpdateEventArgs;
    Debug.Log($"加载进度: {args.Progress:P1}");
});

// 监听加载成功
eventModule.Subscribe(LoadSceneSuccessEventArgs.EventId, (sender, e) =>
{
    Debug.Log("场景加载成功");
});

// 监听激活场景切换
eventModule.Subscribe(ActiveSceneChangedEventArgs.EventId, (sender, e) =>
{
    var args = e as ActiveSceneChangedEventArgs;
    Debug.Log($"激活场景切换: {args.ActiveScene.name}");
});
```

## 6. 目录结构

```text
Scene/
├── Runtime/
│   ├── SceneModule.cs                # 场景管理模块
│   ├── Event/
│   │   ├── ActiveSceneChangedEventArgs.cs
│   │   ├── LoadSceneFailureEventArgs.cs
│   │   ├── LoadSceneSuccessEventArgs.cs
│   │   ├── LoadSceneUpdateEventArgs.cs
│   │   ├── UnloadSceneFailureEventArgs.cs
│   │   └── UnloadSceneSuccessEventArgs.cs
└── README.md                         # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类
- **Hotfix.Framework.Event**：事件系统
- **YooAsset**：场景资源管理
- **UniTask**：异步支持

## 8. 最佳实践

1. **Loading 界面**：加载大场景时监听 `LoadSceneUpdateEventArgs` 事件显示 Loading 进度条
2. **Single 模式**：主场景切换使用 `LoadSceneMode.Single`，自动卸载旧场景
3. **Additive 模式**：子关卡、副本地图使用 `LoadSceneMode.Additive` 叠加加载
4. **资源预热**：场景加载前确保场景依赖的 AssetBundle 已就绪
5. **空值检查**：卸载场景前检查场景是否已加载

## 9. 注意事项

1. 场景资源路径必须与 YooAsset 中配置的资源路径一致
2. 使用 `Additive` 模式加载的场景需要手动卸载
3. 场景加载是异步操作，不要在加载完成前访问场景内容
4. 激活场景切换会触发 `ActiveSceneChangedEventArgs` 事件
5. **取消与重启**：模块销毁（`OnDispose`）后 `Token` 取消，在途场景加载被中止（句柄释放、抛 `OperationCanceledException`）；`OnInit` 重建 `CancellationScope`（新 Token），重启后可正常加载
