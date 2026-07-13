# Launcher 模块重构设计

## 1. 背景与目标

Launcher 模块是 AOT 侧的 MonoBehaviour 入口，负责启动引导流程和驱动 Hotfix 侧的帧更新循环。经过前几轮"框架模块下沉 Hotfix"重构后，Launcher 已大幅精简，但仍有以下问题：

1. **委托与入口混杂**：5 个 `public static Action` 委托直接挂在 Launcher 上，职责不清
2. **不必要的单例模式**：Launcher 继承 `MonoSingleton<Launcher>`，但 `Launcher.Instance` 在代码库中零引用
3. **游戏控制逻辑孤立**：`Launcher.GameControl.cs`（暂停/恢复/重启/退出）作为 partial class 存在，实际没有任何调用方，且依赖 Launcher 的静态委托

本次重构目标：
- 将帧驱动委托 + 游戏控制抽取为独立的 `GameDriven` 类
- Launcher 降级为纯 `MonoBehaviour`，消除 `MonoSingleton` 依赖
- 删除 `Launcher.GameControl.cs`，功能合并到 `GameDriven`

## 2. 架构设计

### 2.1 重构前后对比

**重构前：**

```
Launcher/Runtime/
├── Launcher.cs              ← MonoSingleton + 5 个静态委托 + 反射入口
└── Launcher.GameControl.cs  ← partial class，游戏控制
```

**重构后：**

```
Launcher/Runtime/
├── Launcher.cs              ← 纯 MonoBehaviour，仅负责启动引导
└── GameDriven.cs            ← MonoSingleton，帧驱动委托 + 游戏控制
```

### 2.2 GameObject 结构

```
GameObject [Launcher]
  ├── Launcher     : MonoBehaviour   → 启动引导流程（Start）
  └── GameDriven   : MonoSingleton  → 帧驱动 + 游戏控制
```

两者共用同一 GameObject，`DontDestroyOnLoad` 由 Launcher 在 `Awake` 中执行。

### 2.3 数据流

```
AOT                                          Hotfix
────                                         ──────

Launcher.Start()
  └── BootstrapProcess.RunAsync()
        └── 下载资源 → 加载 Hotfix.dll
              └── InvokeHotfixEntryAsync()
                    └── 反射调用 HotfixLauncher.MainAsync()
                          └── GameDriven.Instance.OnUpdate = ModuleManager.Update   ← 挂接委托
                              GameDriven.Instance.OnLateUpdate = ...
                              ...

GameDriven.Update()                          ModuleManager.Update(dt, udt)
  └── OnUpdate?.Invoke()  ──────────────────→ 遍历所有模块.OnUpdate()

GameDriven.PauseGame()  ←  Hotfix UI 通过 GameDriven.Instance 调用
GameDriven.RestartGame()
  ├── DisposeModules?.Invoke()  ────────────→ ModuleManager.Dispose
  ├── ReInitModules?.Invoke()   ────────────→ ModuleManager.ReInit
  └── Launcher.RestartBootstrap()
```

## 3. 组件详细设计

### 3.1 Launcher

纯入口 MonoBehaviour，不继承任何基类（除 `MonoBehaviour`）。

```csharp
public class Launcher : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if ENABLE_SRDEBUGGER
        SRDebug.Init();
#endif
        FuLogger.LogInfo($"游戏版本号: {Application.version}, Unity版本号: {Application.unityVersion}");
    }

    private void Start()
    {
        BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
    }

    private static async UniTask InvokeHotfixEntryAsync(IBootstrapView view)
    {
        var hotfixAssembly = GetHotfixAssembly();
        if (hotfixAssembly == null)
        {
            FuLogger.LogError("[Launcher] 未找到已加载的 Hotfix 程序集，无法进入热更入口。");
            return;
        }

        var entryType  = hotfixAssembly.GetType("Hotfix.HotfixLauncher");
        var mainMethod = entryType?.GetMethod("MainAsync", BindingFlags.Public | BindingFlags.Static);
        if (mainMethod == null)
        {
            FuLogger.LogError("[Launcher] 未找到热更入口 Hotfix.HotfixLauncher.MainAsync。");
            return;
        }

        await (UniTask)mainMethod.Invoke(null, new object[] { view });
    }

    private static Assembly GetHotfixAssembly()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == "Hotfix")
                return assembly;
        }
        return null;
    }

    /// <summary>
    /// 重启引导流程，供 GameDriven.RestartGame 调用。
    /// </summary>
    internal static void RestartBootstrap()
        => BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
}
```

**关键变化：**
- 继承从 `MonoSingleton<Launcher>` → `MonoBehaviour`
- 移除 5 个静态委托字段
- 移除 `Update`/`LateUpdate`/`FixedUpdate`（由 GameDriven 自行驱动）
- 移除 `OnInit()`，逻辑并入 `Awake()`
- 新增 `internal static RestartBootstrap()` 供 GameDriven 调用

### 3.2 GameDriven

帧驱动 + 游戏控制中枢，继承 `MonoSingleton<GameDriven>`，自驱动生命周期。

```csharp
public class GameDriven : MonoSingleton<GameDriven>
{
    // === 帧驱动委托（Hotfix 侧挂接 ModuleManager 方法）===
    public Action<float, float> OnUpdate;
    public Action<float, float> OnLateUpdate;
    public Action OnFixedUpdate;
    public Action DisposeModules;
    public Action ReInitModules;

    // === 帧驱动（MonoBehaviour 生命周期）===
    private void Update()
        => OnUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);

    private void LateUpdate()
        => OnLateUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);

    private void FixedUpdate()
        => OnFixedUpdate?.Invoke();

    // === 游戏控制 ===
    public void PauseGame()
        => ModuleSetting.Runtime.ModuleSetting.Instance.PauseGame();

    public void ResumeGame()
        => ModuleSetting.Runtime.ModuleSetting.Instance.ResumeGame();

    public void RestartGame()
    {
        DisposeModules?.Invoke();
        ReInitModules?.Invoke();
        Launcher.RestartBootstrap();
    }

    public void QuitGame()
    {
        DisposeModules?.Invoke();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
```

**设计要点：**
- 继承 `MonoSingleton<GameDriven>`，复用现有单例模式，提供 `Instance` 访问
- 帧更新由自身 `Update`/`LateUpdate`/`FixedUpdate` 驱动，无需外部喂帧
- 委托在挂接前为 null，`?.Invoke()` 天然安全
- 游戏控制方法通过 `GameDriven.Instance` 供 Hotfix 侧调用

### 3.3 HotfixLauncher 适配

```csharp
// 之前：全限定类名 + 静态字段
FuFramework.Launcher.Runtime.Launcher.OnUpdate       = ModuleManager.Update;
FuFramework.Launcher.Runtime.Launcher.OnLateUpdate   = ModuleManager.LateUpdate;
FuFramework.Launcher.Runtime.Launcher.OnFixedUpdate  = ModuleManager.FixedUpdate;
FuFramework.Launcher.Runtime.Launcher.DisposeModules = ModuleManager.Dispose;
FuFramework.Launcher.Runtime.Launcher.ReInitModules  = ModuleManager.ReInit;

// 之后：通过 GameDriven.Instance
GameDriven.Instance.OnUpdate       = ModuleManager.Update;
GameDriven.Instance.OnLateUpdate   = ModuleManager.LateUpdate;
GameDriven.Instance.OnFixedUpdate  = ModuleManager.FixedUpdate;
GameDriven.Instance.DisposeModules = ModuleManager.Dispose;
GameDriven.Instance.ReInitModules  = ModuleManager.ReInit;
```

`HotfixLauncher.cs` 中已存在 `using FuFramework.Launcher.Runtime;`（第 20 行），无需额外添加。

## 4. 文件变更清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `Runtime/Launcher.cs` | 去掉 MonoSingleton，移除委托和生命周期方法 |
| 新增 | `Runtime/GameDriven.cs` | MonoSingleton，委托 + 驱动 + 游戏控制 |
| 新增 | `Runtime/GameDriven.cs.meta` | Unity 元文件 |
| 删除 | `Runtime/Launcher.GameControl.cs` | 功能合并到 GameDriven |
| 删除 | `Runtime/Launcher.GameControl.cs.meta` | Unity 元文件 |
| 修改 | `Runtime/README.md` | 更新架构说明 |
| 修改 | `Hotfix/HotfixLauncher.cs` | 委托挂接目标从 Launcher 改为 GameDriven |

## 5. 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| HotfixLauncher 引用断裂 | 低 | 仅 5 行赋值代码需修改，编译期即可发现 |
| 场景中 Launcher 引用失效 | 低 | `Launcher.Instance` 零引用，无破坏 |
| MonoSingleton 默认创建行为差异 | 低 | 场景中手动挂载 GameDriven，MonoSingleton 优先使用场景实例 |
| RestartGame 中 `Forget()` 无异常处理 | 中 | 保持现有行为，不在此次重构中修复（单独跟进） |

## 6. 不在本次范围

- `ModuleSetting` 的 `MonoSingleton` 继承 —— 独立问题，不在 Launcher 重构范围
- `BootstrapProcess` 的流程变更
- 反射调用 `HotfixLauncher.MainAsync` 改为接口方式 —— 独立重构议题
- `RestartGame` 的异常处理加固 —— 后续跟进
