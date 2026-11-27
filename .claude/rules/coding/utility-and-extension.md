---
paths:
  - "**/*.cs"
---

# 工具类与扩展方法

本项目有三种工具形态，职责不同，**新建前先按下面的表选形态**。

| 形态 | 命名 | 形态特征 | 放哪 |
|---|---|---|---|
| 静态工具 | `Utility` / `UtilityAOT`（分部） | `public static partial class`，按功能拆文件 | AOT：`AOT/Framework/Utility/Utility.<功能>.cs`<br>热更：`Hotfix/Framework/Core/Utility/Utility.<功能>.cs` |
| 扩展方法 | `XxxEx` | `public static class`，方法首参 `this 目标类型` | `Core/Extension/Common/`（.NET 类型）<br>`Core/Extension/UnityEngine/`（Unity 类型） |
| 辅助类 | `XxxHelper` | 形态按需（见下） | 与使用它的模块同目录 |

## 扩展方法

- 命名 `XxxEx`，文件名同名：`StringEx.cs`、`TransformEx.cs`。
- **按目标类型分目录**：
  - `Common/`——纯 .NET 类型（`StringEx`、`CollectionEx`、`ObjectEx`、`BufferEx`、`BinaryEx`、`FuGuardEx`）
  - `UnityEngine/`——Unity 类型（`TransformEx`、`GameObjectEx`、`CameraEx`、`Vector2Ex`、`Vector3Ex`）
- 扩展方法必须无状态：**禁止在 `XxxEx` 里持静态字段**。
- 只加"语法糖"级的能力；有业务含义的逻辑不要塞进扩展方法。

## 辅助类（Helper）

**本项目 Helper 形态不统一是历史现状，不要强行统一**。新建时按实际需要选：

| 需要什么 | 用什么形态 | 现有例子 |
|---|---|---|
| 纯函数、无状态、可复用 | `public static class` | `SafeAreaHelper` |
| 需要实例状态（一份逻辑一份数据） | 普通实例类 | `StorageHelper` |
| 需要挂到 GameObject 上跑生命周期 | `MonoBehaviour` | `EntityHelper` |
| 需要按模块组装多个调用 | 与模块同目录的派生类 | `UnityWebRequestDownloadAgentHelper` |

**判断依据是"要不要状态/生命周期"，不是"名字带 Helper 就必须静态"。**

静态 Helper 里出现 `private static` 可变字段时，要能说清为什么必须有——它是隐藏的全局状态。

## 常量与服务

- Helper / Utility 里的常量同样收敛到对应模块的 `.Const.cs`（见 `framework/module-system.md`）。
- Helper 不承载业务流程：业务编排留在模块或业务层，Helper 只做可复用的局部能力。
