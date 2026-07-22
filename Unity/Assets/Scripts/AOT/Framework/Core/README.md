# FuFramework AOT Core

## 1. 简介

AOT Core 是框架在 AOT 侧的基础设施层，提供**日志、文件路径、JSON、版本处理**等不依赖 Hotfix 的基础工具。

> **注意**：模块系统（`ModuleBase`、`ModuleManager`）、属性绑定（`BindableProperty`）、高性能数据结构、加密/哈希工具等已迁移至 `Hotfix/Framework/Core`。本目录仅保留 AOT 引导阶段必需的工具。

## 2. 目录结构

```
Core/
├── Log/                         # 日志系统
│   ├── ELogLevel.cs             # 日志级别枚举
│   └── FuLogger.cs              # 日志输出封装（条件编译控制）
├── Extension/                   # 扩展方法
│   └── TypeEx.cs                # Type 扩展方法
└── Utility/                     # 静态工具类
    ├── Utility.Application.cs   # 平台判断、打开 URL
    ├── Utility.Assembly.cs      # 反射获取类型和程序集
    ├── Utility.Asset.Path.cs    # 资源路径处理
    ├── Utility.File.cs          # 文件读写、复制、删除
    ├── Utility.File.WithBSA.cs  # BSA 文件支持
    ├── Utility.Json.cs          # JSON 序列化/反序列化（Newtonsoft.Json）
    ├── Utility.Path.cs          # 路径规范化、平台适配
    └── Utility.Version.cs       # 版本号解析与比较
```

## 3. 日志系统（FuLogger）

封装 `UnityEngine.Debug`，支持多级别日志输出和条件编译控制。

**日志级别：**`LogInfo`、`LogWarning`、`LogError`、`LogFatal`

**条件编译宏：**
- `ENABLE_LOG` — 启用所有日志
- `ENABLE_INFO_LOG` / `ENABLE_DEBUG_AND_ABOVE_LOG` / `ENABLE_INFO_AND_ABOVE_LOG` / `ENABLE_WARNING_AND_ABOVE_LOG` / `ENABLE_ERROR_AND_ABOVE_LOG` / `ENABLE_FATAL_AND_ABOVE_LOG` — 按级别控制

## 4. 工具类（Utility）

### 4.1 Utility.Application

平台判断和 URL 打开：
- `PlatformName` — 平台名称（Android/MacOs/iOS/WebGL/Windows）
- `IsEditor` / `IsAndroid` / `IsWebGL` / `IsWindows` / `IsLinux` / `IsMacOsx` / `IsIOS` — 平台布尔属性
- `OpenURL(string url)` — 打开 URL

### 4.2 Utility.File

跨平台文件操作：`ReadAllBytes`、`ReadAllText`、`ReadAllLines`、`WriteAllBytes`、`WriteAllText`、`WriteAllLines`、`Copy`、`Delete`、`Move`、`CleanDirectory`、`GetBytesSizeWithUnit` 等。

### 4.3 Utility.Json

基于 Newtonsoft.Json 的序列化工具：`ToJson`、`ToObject<T>`。

### 4.4 其他

| 类 | 功能 |
|----|------|
| `Utility.Asset.Path` | 资源路径处理，跨平台路径转换 |
| `Utility.Path` | 路径规范化、获取相对路径 |
| `Utility.Assembly` | 反射获取类型、所有程序集 |
| `Utility.Version` | 版本号解析与比较 |

## 5. 与 Hotfix Framework Core 的关系

| 功能 | AOT Core | Hotfix Framework Core |
|------|----------|----------------------|
| 日志 | ✅ `FuLogger` | — |
| 平台判断 | ✅ `Utility.Application` | — |
| 文件操作 | ✅ `Utility.File` | — |
| JSON | ✅ `Utility.Json` | — |
| 模块系统 | — | ✅ `ModuleBase`、`ModuleManager` |
| 属性绑定 | — | ✅ `BindableProperty` |
| 数据结构 | — | ✅ `FuLinkedList`、`FuMultiDictionary` 等 |
| 加密/哈希 | — | ✅ `Utility.Encryption.*`、`Utility.Hash.*` |
| 扩展方法 | ✅ `TypeEx` | ✅ 完整集合（String、Collection、GameObject、Transform 等） |
| 单例 | — | ✅ `Singleton<T>`、`MonoSingleton<T>` |
