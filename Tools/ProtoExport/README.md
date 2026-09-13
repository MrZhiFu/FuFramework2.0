# ProtoExport 协议导出工具

**自研的 `.proto` → C# 代码生成器**（.NET 8 控制台程序）。它**不依赖 `protoc`**：直接读取并
解析 `.proto` 文本，按模式生成客户端 / 服务端 C#。

- **谁调用它**：`Protobuf/Proto2CsExport_*.bat/.sh`（`dotnet ProtoExport.dll …`）。用法见 `Protobuf/README.md`。
- **本目录没有构建依赖**：它只被上面的脚本/IDE 调用，**不被 Unity 工程引用**。

---

## 1. 命令行参数

| 参数 | 必填 | 说明 |
|---|---|---|
| `--mode` | ✅ | 生成模式：`Server` / `Unity`（大小写不敏感） |
| `--inputPath` | ✅ | `.proto` 所在目录（递归扫描 `*.proto`），如 `Protobuf/Proto` |
| `--outputPath` | ✅ | 代码输出目录；**每次执行会先整个删除再重建**（见 §3） |
| `--namespaceName` | ✅ | 生成代码的命名空间 |
| `--isGenerateErrorCode` | 否（默认 `true`） | 是否生成错误码 |

示例：

```bat
dotnet ProtoExport.dll --mode unity --inputPath ./Protobuf/Proto --outputPath ./Unity/Assets/Scripts/Hotfix/Game/AutoGen/Proto --namespaceName Hotfix.Game.Proto --isGenerateErrorCode true
```

## 2. 模式与生成器

解析出 `ModeType` 后，`ProtoBufMessageHandler` 通过 `[Mode(...)]` 特性在程序集内选出对应的
`IProtoGenerateHelper` 实现：

| 模式 | 生成器 | 产出 |
|---|---|---|
| `Unity` | `ProtoBuffUnityHelper` | 客户端 C#（消息类 `… : MessageObject, IRequestMessage/IResponseMessage/INotifyMessage`，类上带 `[MessageTypeHandler(消息ID)]`） |
| `Server` | `ProtoBuffServerHelper` | 服务端 C# |

> 选实现用 `GetTypes` + `Activator.CreateInstance` 反射 —— 这是**构建期工具**，不属于运行时热更代码，**不受项目铁律 4（运行时禁反射）约束**。

每个生成器实现 `IProtoGenerateHelper`：

```csharp
void Run(MessageInfoList info, string outputPath, string namespaceName);  // 逐个 proto 生成
void Post(List<MessageInfoList> all, string outputPath);                  // 全部生成后的后处理（当前两个实现均为空钩子）
```

## 3. 消息 ID 规则

`MessageHelper` 用正则提取每个 proto 的 `option module = N;`（须在 `short` 范围内），
消息 ID 按**声明顺序**从 `10` 起递增：

```
消息ID = (module << 16) + opcode        // opcode 从 10 开始，每个消息 +1
```

例：`Bag`（`option module = 100;`）的第一个消息 → `6553600 + 10 = 6553610`。

## 4. 关键行为（使用前必读）

`ProtoBufMessageHandler.Start()` 开头会在输出目录存在时**整个删除该目录再重建**：

```csharp
if (outputDirectoryInfo.Exists) outputDirectoryInfo.Delete(true);
outputDirectoryInfo.Create();
```

因此**每次导出都会删掉输出目录里的 Unity `.meta` 文件**（工具不认识它们）。
故标准流程是：**导出 → 回 Unity 触发重导入（重建 `.meta`）→ 再提交 `.cs` 与 `.cs.meta`**。

## 5. 源码结构

| 文件 | 职责 |
|---|---|
| `Program.cs` | 入口：解析参数 → 解析模式 → 调用 `ProtoBufMessageHandler.Start` |
| `LauncherOptions.cs` | 命令行参数定义（基于 `CommandLine` 库） |
| `ModeType.cs` / `ModeAttribute.cs` | 模式枚举与用于挑选生成器的 `[Mode(...)]` 特性 |
| `ProtoBufMessageHandler.cs` | 主流程：删建输出目录 → 选生成器 → 遍历 proto 生成 → `Post` |
| `MessageHelper.cs` | `.proto` 文本解析：`package` / `option module` / 枚举 / 消息 / 注释 / 消息 ID 分配 |
| `MessageInfo.cs` | 解析结果模型（`MessageInfoList` / `MessageInfo` 等） |
| `IProtoGenerateHelper.cs` | 生成器接口（`Run` / `Post`） |
| `ProtoBuffUnityHelper.cs` / `ProtoBuffServerHelper.cs` | 两个模式的生成实现 |
| `Utility.cs` | 字符串等辅助 |

## 6. 构建与运行

```bat
:: 命令行直接构建 / 运行
dotnet build ProtoExport.csproj
dotnet run   --project ProtoExport.csproj -- --mode unity …

:: IDE 调试：Properties/launchSettings.json 预置了两个启动配置
::   ProtoServerToolsApp / ProtoUnityToolsApp（参数与 .bat 脚本一致）
```

- `ProtoExport.sln`：IDE（VS / Rider）解决方案文件。
- `Dockerfile`：容器化运行用（构建上下文 = 本目录，`docker build Tools/ProtoExport`）。
- 构建产物 `bin/`、`obj/` 不入库（见 `.gitignore`）。

## 7. 扩展：新增一种生成模式

1. 在 `ModeType` 增加一个成员；
2. 新建类实现 `IProtoGenerateHelper`，并用 `[Mode(ModeType.你新增的模式)]` 标注；
3. 在 `ProtoBufMessageHandler.Start` 中处理该模式——当前 `Server` / `Unity` 调用形态一致（均以 `--namespaceName` 作命名空间），**共用同一行 `Run(...)`**；若新模式需要不同的入参（例如以文件名作命名空间），在此处加回 `switch` 分支即可。

> 历史：曾支持 `TypeScript` 模式，因本仓库未使用而**已剥离**（生成器、枚举成员、主流程分支、IDE 启动配置均已移除）。
