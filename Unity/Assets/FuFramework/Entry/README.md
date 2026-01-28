# FuFramework Entry Module

## 简介
FuFramework Entry 模块是整个游戏框架的启动入口。它负责初始化框架核心（`ModuleManager`）、加载并启动游戏流程（`Procedure`），并提供了一个全局静态访问点（`GlobalModule`）以便于访问各个功能模块。

## 核心类说明

### Launcher
游戏启动器，继承自 `MonoBehaviour`。通常挂载在初始场景的某个 GameObject 上。
- **职责**：
  1. 初始化 `ModuleManager`。
  2. 扫描并实例化所有选定的流程（`ProcedureBase`）。
  3. 启动指定的入口流程。
  4. 在 `Update` 中驱动框架的轮询。

### GlobalModule
全局模块访问入口，静态类。
- **职责**：提供对框架所有核心模块（如 `AssetManager`, `UIManager`, `EventManager` 等）的静态访问属性，简化代码编写。
- **示例**：
  ```csharp
  // 以前可能需要这样写：
  // ModuleManager.GetModule<AssetManager>().LoadAssetAsync(...)
  
  // 现在可以直接这样写：
  GlobalModule.AssetModule.LoadAssetAsync(...)
  ```

## 使用指南

### 1. 设置启动场景
1. 创建一个空场景（如 `LaunchScene`）。
2. 创建一个 GameObject（命名为 `Launcher`）。
3. 挂载 `Launcher` 组件。

### 2. 配置流程
在 `Launcher` 组件的 Inspector 面板中：
- **所有可用的流程类型**：列出了项目中所有继承自 `ProcedureBase` 的类。勾选你需要使用的流程。
- **入口流程**：从已勾选的流程中选择一个作为游戏的起始流程（如 `ProcedureLaunch`）。

### 3. 编写流程
自定义流程需继承 `ProcedureBase`。
```csharp
public class ProcedureLaunch : ProcedureBase
{
    protected override void OnEnter(IFsm<ProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        FuLogger.LogInfo("进入启动流程");
        
        // 切换到下一个流程
        ChangeState<ProcedureMenu>(procedureOwner);
    }
}
```

### 4. 访问模块
在代码的任何地方，都可以通过 `GlobalModule` 快速访问框架模块：
```csharp
// 播放音效
GlobalModule.SoundModule.PlaySound("Click");

// 打开UI
GlobalModule.UIModule.Open<WinLogin>();

// 发送事件
GlobalModule.EventModule.Fire(this, new LoginSuccessEventArgs());
```

## 编辑器扩展
`LauncherInspector` 提供了强大的可视化配置功能：
- **自动扫描**：自动扫描工程中所有的 `ProcedureBase` 子类。
- **优先级排序**：根据流程类的 `Priority` 属性自动排序显示。
- **运行时调试**：在游戏运行时，实时显示当前正在激活的流程名称。
