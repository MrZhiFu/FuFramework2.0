# 框架基座与通用机制

根路径：`Unity/Assets/Scripts/Hotfix/Framework/<模块>/`

| 功能 | 文件 |
|---|---|
| 模块基类 | `Core/ModuleBase.cs` |
| 模块管理器（注册/驱动/销毁） | `Core/ModuleManager.cs` |
| 帧驱动（MonoSingleton） | `Core/GameDriven.cs` |
| 全局模块访问入口 | `Core/GlobalModule.cs` |
| 单例基类 | `Core/Singleton/` |
| 异步工具 | `Core/Async/` |
| 数据结构（池化集合等） | `Core/DataStruct/` |
| 扩展方法 | `Core/Extension/` |
| 序列化 | `Core/Serializer/` |
| 属性系统 | `Core/Property/` |
| 引用池 | `Core/ReferencePool/` |
| 任务池 | `Core/TaskPool/` |
| 热更代码入口（模块注册顺序） | `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs` |
| 事件模块 | `Event/EventModule*.cs`、`Event/EventRegister.cs` |
| 定时器模块 | `Timer/TimerModule.cs`、`Timer/TimerRegister.cs`、`Timer/Base/`、`Timer/Sub/` |
| 对象池模块 | `ObjectPool/ObjectPoolModule*.cs`、`ObjectPool/Pool/`、`ObjectPool/Base/` |
| 有限状态机 | `FSM/FsmModule.cs`、`FSM/Fsm.cs`、`FSM/FsmStateBase.cs` |
| 游戏流程 | `Procedure/ProcedureModule.cs`、`Procedure/ProcedureBase.cs` |
| 持久化存储 | `Storage/StorageModule.cs`、`Storage/StorageHelper.cs`、`Storage/DataSerializer.cs` |
| 音效 | `Sound/SoundModule*.cs` |
| 场景 | `Scene/SceneModule.cs` |
| 下载 | `Download/DownloadModule*.cs`、`Download/Agent/`、`Download/Task/` |
| 实体 | `Entity/EntityModule*.cs`、`Entity/Info/` |
| 引导 | `Guide/GuideModule*.cs`、`Guide/Steps/` |
| 红点 | `RedDot/RedDotModule*.cs`、`RedDot/RedDotNode.cs`、`RedDot/RedDotKey.cs` |
| 数据模型基类 | `Model/ModelModule.cs`、`Model/BaseModel.cs` |
| Mono 挂载 | `Mono/MonoModule.cs` |
| AOT 工具（日志/扩展/工具类） | `Unity/Assets/Scripts/AOT/Framework/{Log,Extension,Utility}/` |
| 游戏层模型 | `Unity/Assets/Scripts/Hotfix/Game/Model/` |
| 游戏层事件 | `Unity/Assets/Scripts/Hotfix/Game/Events/` |
