# Launcher 模块重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Launcher 模块拆分为纯入口 MonoBehaviour (Launcher) 和 MonoSingleton 驱动控制中枢 (GameDriven)，删除不再需要的 Launcher.GameControl.cs，适配 HotfixLauncher 的委托挂接目标。

**Architecture:** GameDriven 继承 MonoSingleton 自驱动帧循环并持有 5 个 Action 委托 + 4 个游戏控制方法。Launcher 降级为普通 MonoBehaviour，仅负责 AOT 启动引导和 DontDestroyOnLoad。两者挂同一 GameObject，通过 GameDriven.Instance 访问。

**Tech Stack:** C#, Unity 2022.3, HybridCLR (AOT/Hotfix 分离), UniTask, MonoSingleton

## Global Constraints

- 使用中文注释（遵循 CLAUDE.md 代码风格规范）
- 提交遵循 Conventional Commits 中文版（`Docs/Git提交规范.md`）
- AOT 不直接引用 Hotfix 程序集，跨层通过反射或委托桥接
- 委托挂接前为 null，`?.Invoke()` 天然空安全，无需额外空检查
- 按 `Docs/代码风格规范.md` 规范命名

---

### Task 1: 创建 GameDriven.cs

**Files:**
- Create: `Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/GameDriven.cs`
- Create: `Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/GameDriven.cs.meta` (Unity 自动生成，检查即可)

**Interfaces:**
- Produces: `GameDriven : MonoSingleton<GameDriven>` 类，包含 `OnUpdate`, `OnLateUpdate`, `OnFixedUpdate`, `DisposeModules`, `ReInitModules` (public Action 字段), `PauseGame()`, `ResumeGame()`, `RestartGame()`, `QuitGame()` (public 方法), `Update()`, `LateUpdate()`, `FixedUpdate()` (private 生命周期)

- [ ] **Step 1: 创建 GameDriven.cs**

```csharp
using System;
using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// 框架帧驱动 + 游戏控制中枢。
    /// 功能：
    ///     1. 持有帧驱动委托，供 Hotfix 侧挂接 ModuleManager 生命周期方法
    ///     2. 自驱动 MonoBehaviour Update/LateUpdate/FixedUpdate，调用挂接的委托
    ///     3. 提供游戏级别控制：暂停、恢复、重启、退出
    /// </summary>
    public class GameDriven : MonoSingleton<GameDriven>
    {
        /// <summary>
        /// 框架模块帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.Update。
        /// </summary>
        public Action<float, float> OnUpdate;

        /// <summary>
        /// 框架模块延迟帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.LateUpdate。
        /// </summary>
        public Action<float, float> OnLateUpdate;

        /// <summary>
        /// 框架模块固定帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.FixedUpdate。
        /// </summary>
        public Action OnFixedUpdate;

        /// <summary>
        /// 释放全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.Dispose。
        /// </summary>
        public Action DisposeModules;

        /// <summary>
        /// 重新初始化全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.ReInit。
        /// </summary>
        public Action ReInitModules;

        /// <summary>
        /// 驱动框架模块帧更新
        /// </summary>
        private void Update()
        {
            OnUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            ModuleSetting.Runtime.ModuleSetting.Instance.PauseGame();
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            ModuleSetting.Runtime.ModuleSetting.Instance.ResumeGame();
        }

        /// <summary>
        /// 重启游戏（如设置界面重启）。
        /// 依次释放所有模块、重新初始化模块、重新运行 AOT 引导流程。
        /// </summary>
        public void RestartGame()
        {
            DisposeModules?.Invoke();
            ReInitModules?.Invoke();

            // 重新运行 AOT 引导流程（重新显示加载界面并重进热更入口）
            Launcher.RestartBootstrap();
        }

        /// <summary>
        /// 退出游戏。
        /// </summary>
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
}
```

- [ ] **Step 2: 确认 .meta 文件生成**

Unity 编辑器打开项目时会自动为 `GameDriven.cs` 生成 `.meta` 文件，无需手动创建。若通过命令行操作，确保该 `.meta` 文件存在后再提交。

- [ ] **Step 3: 提交**

```bash
git add "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/GameDriven.cs" "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/GameDriven.cs.meta"
git commit -m "feat: 新增 GameDriven 类，承载帧驱动委托与游戏控制"
```

---

### Task 2: 修改 Launcher.cs

**Files:**
- Modify: `Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.cs`

**Interfaces:**
- Consumes: `GameDriven` (仅作为被引用的邻类，无直接调用依赖——GameDriven 自驱动)
- Produces: `Launcher : MonoBehaviour` 类，包含 `RestartBootstrap()` (internal static)、`InvokeHotfixEntryAsync(IBootstrapView)` (private static)、`GetHotfixAssembly()` (private static)

- [ ] **Step 1: 重写 Launcher.cs**

将文件内容完整替换为：

```csharp
using System;
using UnityEngine;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// AOT 入口类。
    /// 功能：
    ///     1. 启动 AOT 极简引导流程（下载资源、加载热更程序集）
    ///     2. 引导完成后反射调用 HotfixLauncher.MainAsync() 进入热更逻辑
    /// </summary>
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
            // 启动 AOT 极简引导流程，引导完成后回调 InvokeHotfixEntryAsync 进入热更入口
            global::Launcher.BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
        }

        /// <summary>
        /// 热更入口回调。
        /// 由 AOT 引导流程在加载完 Hotfix 程序集后调用：随后反射调用热更入口 Hotfix.HotfixLauncher.MainAsync。
        /// </summary>
        /// <param name="view">AOT 加载界面句柄，透传给热更入口用于收尾关闭。</param>
        private static async UniTask InvokeHotfixEntryAsync(IBootstrapView view)
        {
            // 反射进入热更入口
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

        /// <summary>
        /// 获取已加载到当前应用域的 Hotfix 程序集。
        /// </summary>
        /// <returns>Hotfix 程序集，未找到返回 null。</returns>
        private static Assembly GetHotfixAssembly()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Hotfix")
                {
                    return assembly;
                }
            }

            return null;
        }

        /// <summary>
        /// 重启引导流程，供 GameDriven.RestartGame 调用。
        /// </summary>
        internal static void RestartBootstrap()
        {
            global::Launcher.BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
        }
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.cs"
git commit -m "refactor: Launcher 降级为纯 MonoBehaviour，移除委托与帧生命周期"
```

---

### Task 3: 删除 Launcher.GameControl.cs

**Files:**
- Delete: `Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.GameControl.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.GameControl.cs.meta`

**Interfaces:**
- Consumes: Task 2 (Launcher 不再是 partial class，GameControl 文件必须删除才能编译通过)
- Produces: 无

- [ ] **Step 1: 删除文件**

```bash
rm "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.GameControl.cs"
rm "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.GameControl.cs.meta"
```

- [ ] **Step 2: 提交**

```bash
git add -u "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.GameControl.cs" "Unity/Assets/Scripts/AOT/Framework/Launcher/Runtime/Launcher.GameControl.cs.meta"
git commit -m "refactor: 删除 Launcher.GameControl.cs，功能已合并到 GameDriven"
```

---

### Task 4: 适配 HotfixLauncher.cs

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs:83-87`

**Interfaces:**
- Consumes: `GameDriven.Instance` (MonoSingleton 静态属性), `GameDriven.OnUpdate`, `GameDriven.OnLateUpdate`, `GameDriven.OnFixedUpdate`, `GameDriven.DisposeModules`, `GameDriven.ReInitModules`

- [ ] **Step 1: 替换委托挂接代码**

将 `HotfixLauncher.cs` 第 82-87 行的 6 行代码替换为：

```csharp
            // 将 ModuleManager 的生命周期方法挂接到 GameDriven 委托
            GameDriven.Instance.OnUpdate       = ModuleManager.Update;
            GameDriven.Instance.OnLateUpdate   = ModuleManager.LateUpdate;
            GameDriven.Instance.OnFixedUpdate  = ModuleManager.FixedUpdate;
            GameDriven.Instance.DisposeModules = ModuleManager.Dispose;
            GameDriven.Instance.ReInitModules  = ModuleManager.ReInit;
```

> 注意：`using FuFramework.Launcher.Runtime;` 已在 HotfixLauncher.cs 第 20 行存在，无需新增。`GameDriven` 可直接通过短名称引用。

- [ ] **Step 2: 提交**

```bash
git add "Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs"
git commit -m "refactor: HotfixLauncher 委托挂接目标从 Launcher 迁移到 GameDriven.Instance"
```

---

### Task 5: 更新 README.md

**Files:**
- Modify: `Unity/Assets/Scripts/AOT/Framework/Launcher/README.md`

- [ ] **Step 1: 重写 README.md**

用以下完整内容替换 README.md：

```markdown
# FuFramework Launcher Module

## 1. 简介

**FuFramework Launcher** 模块是 AOT 侧的 MonoBehaviour 入口和核心驱动器。它由两个组件组成，挂载在启动场景的同一 GameObject 上：

1. **Launcher** — 纯 MonoBehaviour 入口，启动 AOT 极简引导流程（`BootstrapProcess.RunAsync()`），引导完成后反射调用 HotfixLauncher.MainAsync() 进入热更逻辑
2. **GameDriven** — MonoSingleton，提供帧驱动委托 + 游戏级别控制（暂停、恢复、重启、退出）

所有框架模块的注册和生命周期已移交 Hotfix 侧（`HotfixLauncher.MainAsync()`）。Launcher 模块仅保留 AOT 入口 + 帧驱动桥接，不再直接引用任何框架模块。

## 2. 特性

- **极简入口**：Launcher 仅负责引导启动，零模块注册代码
- **委托驱动**：GameDriven 持有帧更新委托，HotfixLauncher 挂接 ModuleManager 生命周期方法
- **游戏控制**：GameDriven 提供暂停、恢复、重启、退出游戏
- **自驱动**：GameDriven 自行处理 MonoBehaviour Update/LateUpdate/FixedUpdate，无需外部喂帧
- **反射移交**：引导完成后反射调用 `HotfixLauncher.MainAsync()` 进入热更逻辑

## 3. 核心类详解

### 3.1 Launcher

游戏启动器，普通 MonoBehaviour，是整个框架的 AOT 入口点。

#### 职责

1. **启动引导流程**：在 `Start()` 中调用 `BootstrapProcess.RunAsync()`
2. **DontDestroyOnLoad**：在 `Awake()` 中确保跨场景存活
3. **暴露重启接口**：`RestartBootstrap()` (internal static) 供 GameDriven 调用

#### 生命周期流程

```
Awake() -> Start()
              ↓
         DontDestroyOnLoad -> 启动引导流程 -> 反射进入热更
```

#### 委托挂接（Hotfix 侧）

```csharp
// HotfixLauncher.MainAsync() 中：
GameDriven.Instance.OnUpdate       = ModuleManager.Update;
GameDriven.Instance.OnLateUpdate   = ModuleManager.LateUpdate;
GameDriven.Instance.OnFixedUpdate  = ModuleManager.FixedUpdate;
GameDriven.Instance.DisposeModules = ModuleManager.Dispose;
GameDriven.Instance.ReInitModules  = ModuleManager.ReInit;
```

### 3.2 GameDriven

帧驱动 + 游戏控制中枢，继承自 `MonoSingleton<GameDriven>`。

#### 职责

1. **帧驱动委托**：持有 5 个 Action 委托供 Hotfix 侧挂接
2. **自驱动帧循环**：Update/LateUpdate/FixedUpdate 调用已挂接的委托
3. **游戏控制**：暂停、恢复、重启、退出

#### 委托桥接数据流

```
AOT                                         Hotfix
────                                        ──────

GameDriven.Update()                         ModuleManager.Update(dt, udt)
  └── OnUpdate?.Invoke()  ────────────────→ 遍历所有模块.OnUpdate()

GameDriven.PauseGame()    ← Hotfix UI 调用
GameDriven.RestartGame()
  ├── DisposeModules?.Invoke()  ───────────→ ModuleManager.Dispose
  ├── ReInitModules?.Invoke()   ───────────→ ModuleManager.ReInit
  └── Launcher.RestartBootstrap()
```

## 4. 目录结构

```
FuFramework/Launcher/
├── README.md                                 # 模块说明文档
├── Runtime/                                  # 运行时代码
│   ├── FuFramework.Launcher.Runtime.asmdef   # 程序集定义
│   ├── Launcher.cs                           # AOT 入口 MonoBehaviour
│   └── GameDriven.cs                         # 帧驱动 + 游戏控制 MonoSingleton
```

> **注意**：`Launcher.GameControl.cs` 已删除。游戏控制功能已合并到 `GameDriven.cs`。

## 5. 依赖

AOT 侧最小依赖：

- **FuFramework.Core.Runtime**：基础工具（FuLogger/Utility/FuException/MonoSingleton）
- **FuFramework.ModuleSetting.Runtime**：模块设置
- **HybridCLR.Runtime**：AOT 元数据补充
- **YooAsset**：资源管理
- **UniTask**：异步操作
- **FairyGUI**：UI 框架（BootstrapView 自包含包）

## 6. 注意事项

1. **GameObject 结构**：Launcher 和 GameDriven 挂载在同一 GameObject 上，`DontDestroyOnLoad` 由 Launcher 在 Awake 中执行
2. **委托安全**：委托默认为 null，HotfixLauncher 挂接前不会执行任何帧更新，`?.Invoke()` 天然安全
3. **Hotfix 侧引用**：挂接委托时使用 `GameDriven.Instance.OnUpdate = ...`，已存在 `using FuFramework.Launcher.Runtime;` 无需额外添加
4. **反射入口**：HotfixLauncher.MainAsync 通过反射调用，AOT 不直接引用 Hotfix 程序集
5. **重启游戏**：`RestartGame()` 通过委托调用 `DisposeModules` + `ReInitModules`，随后调用 `Launcher.RestartBootstrap()` 重新运行引导流程
```

- [ ] **Step 2: 提交**

```bash
git add "Unity/Assets/Scripts/AOT/Framework/Launcher/README.md"
git commit -m "docs: 更新 Launcher README，反映 GameDriven 拆分后的新架构"
```

---

## 实施顺序

```
Task 1 → Task 2 → Task 3 → Task 4 → Task 5
  │         │        │        │        │
GameDriven  Launcher  删除     Hotfix    README
  新建      重写    旧文件    适配委托   更新文档
```

Task 1-3 为 AOT 侧变更，可在同一编译周期内完成；Task 4 为 Hotfix 侧适配，依赖 AOT 侧接口就绪；Task 5 为文档收尾。

## 验证方式

1. Unity 编辑器打开项目，确认 AOT 侧编译无报错
2. 确认 Hotfix 侧编译无报错
3. 运行启动场景，确认引导流程正常完成、登录界面正常打开
4. 在 Unity Editor 中调用 `GameDriven.Instance.RestartGame()` 确认重启流程正常
