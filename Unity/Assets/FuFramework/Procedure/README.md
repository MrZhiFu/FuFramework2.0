## HOMEPAGE

GameFrameX 的 Procedure 流程管理组件

**Procedure 流程管理组件 (Procedure Component)** - 提供流程管理组件相关的接口。

**该库主要服务于 `https://github.com/AlianBlank/GameFrameX.Unity` 作为子库使用。**

# 依赖组件

有限状态机

https://github.com/AlianBlank/com.alianblank.gameframex.unity.fsm

# 使用文档(文档编写于GPT4)

# ProcedureComponent 类组件说明文档

## 简介

`ProcedureComponent` 类是一款用于在基于 Unity 和 Game Framework 框架开发的游戏中管理游戏流程的组件。它依赖于 `FSMComponent` (有限状态机组件) 来管理游戏中的不同阶段或状态，例如启动、菜单、游戏、暂停和结束等。

## 功能

此组件的主要功能包括：

- 初始化流程管理器 (`IProcedureManager`)，通过 `GameFrameworkEntry` 注册并获取相关模块。
- 在游戏启动时创建并初始化所有可用的流程 (`ProcedureBase` 类型数组)。
- 启动游戏时，自动切换至入口流程 (`m_EntranceProcedure`)。
- 提供方法查询、获取当前流程及其持续时间。

## 依赖关系

`ProcedureComponent` 需要以下组件或模块来正常工作：

- `FSMComponent`：用于实现流程的有限状态机的逻辑。
- `IProcedureManager`：一个接口，由 Game Framework 提供，管理游戏的流程状态。
- `ProcedureBase`：用于扩展自定义具体流程的基类。

请确保在使用 `ProcedureComponent` 之前，游戏项目中已经包含了这些必要的组件和模块。

## 使用方法

在 Unity Inspector 中，您可以设置以下属性：

- `m_AvailableProcedureTypeNames`：可用流程的类型名称数组，用于在启动游戏时创建和初始化流程。
- `m_EntranceProcedureTypeName`：启动游戏时首先进入的流程的类型名称。

`ProcedureComponent` 实现了以下公共方法供外部调用：

- `HasProcedure<T>()`：检查是否存在指定类型的流程。
- `GetProcedure<T>()`：获取指定类型的流程。

当您创建了自定义流程类并且想要将其注册到流程管理器时，可以添加类名到 `m_AvailableProcedureTypeNames` 数组，并将入口流程的类名设置为 `m_EntranceProcedureTypeName`。

在游戏启动时，`ProcedureComponent` 会循环遍历 `m_AvailableProcedureTypeNames`，使用反射创建这些流程实例，并使用入口流程开始流程管理。

## 注意事项

请注意，每个流程都是继承自 `ProcedureBase` 的一个自定义类。您需要确保您的流程类正确实现了 `ProcedureBase` 定义的所有虚方法，以符合流程管理器的运行要求。

此外，`ProcedureComponent` 类使用 `[DisallowMultipleComponent]` 属性标记，证明该组件不可在同一个 GameObject 上添加多次。

在编写自定义流程时，您应确保它们的状态能够正确的通过 `FSMComponent` 管理，并且能够与其他流程适当切换。

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.procedure": "https://github.com/AlianBlank/com.gameframex.unity.procedure.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.procedure.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别
