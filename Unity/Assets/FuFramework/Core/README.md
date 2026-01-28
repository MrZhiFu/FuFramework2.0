# FuFramework Core Module

## 1. 简介
**FuFramework Core** 是整个框架的基石（Kernel），为上层业务逻辑和功能模块提供统一的底层架构支持。它不包含具体的游戏业务逻辑，而是专注于解决以下核心问题：
- **模块化管理**：如何规范化地定义、初始化、运行和销毁一个功能模块。
- **生命周期控制**：确保各个模块按照正确的顺序和时机执行。
- **基础设施**：提供日志、单例、序列化、数据结构、属性绑定等通用服务。
- **工具库集成**：内置大量经过优化的 `Utility` 工具类，覆盖开发中的高频需求。

---

## 2. 核心架构详解

### 2.1 模块系统 (Module System)
框架采用**单例模块化**设计，所有核心功能（如资源管理、UI管理、网络管理）都必须继承自 `FuModule`。

#### FuModule (模块基类)
继承自 `MonoBehaviour`，但被框架接管了生命周期。
- **`Priority` (优先级)**：
  - 类型：`int`
  - 作用：决定模块的初始化 (`OnInit`)、轮询 (`OnUpdate`) 和销毁 (`OnShutdown`) 的顺序。
  - 规则：优先级**高**的模块先初始化、先轮询，但**后**销毁（栈式管理）。
  - 预定义值：`ModulePriority.System` (100) > `Core` (80) > `Game` (60) > `UI` (50) > `Default` (0)。
- **`OnInit()`**: 模块被注册时调用，仅执行一次。用于初始化变量、注册事件等。
- **`OnUpdate(float elapseSeconds, float realElapseSeconds)`**: 类似于 Unity 的 Update，但由管理器统一驱动。
- **`OnShutdown(ShutdownType shutdownType)`**: 游戏退出或模块卸载时调用，用于清理资源。

#### ModuleManager (模块管理器)
核心静态类，负责维护所有 `FuModule` 的实例。
- **自动注册**：调用 `GetModule<T>()` 时，如果模块不存在，会自动创建并挂载到 `[ModuleManager]` 根节点下。
- **持久化**：自动标记 `DontDestroyOnLoad`，确保模块在场景切换时不会丢失。
- **依赖检测**：内部维护了依赖链，防止模块初始化时的循环依赖死锁。

### 2.2 基础服务 (Infrastructure)
- **日志系统 (`FuLogger`)**：
  - 封装了 `UnityEngine.Debug`，支持 `Info`, `Warning`, `Error` 等级别。
  - 支持条件编译，可在 Release 版本中自动剥离日志代码以提升性能。
- **单例模式**：
  - `Singleton<T>`: 纯 C# 类的单例基类。
  - `MonoSingleton<T>`: 继承自 MonoBehaviour 的单例基类，自动管理 GameObject 的创建。
- **高性能数据结构 (`DataStruct`)**：
  - `FuLinkedList<T>`: 优化过的双向链表，减少 GC。
  - `FuBidirectionalDictionary<TKey, TValue>`: 双向字典，支持通过 Value 反查 Key。
  - `FuMultiDictionary<TKey, TValue>`: 多值字典，一个 Key 可对应多个 Value。
- **属性绑定 (`BindableProperty<T>`)**：
  - 实现了观察者模式的属性包装器。
  - 当 `Value` 发生变化时，自动触发注册的回调函数。
  - 支持 `RegisterWithInitValue`，在注册时立即执行一次回调，方便 UI 初始化。

---

## 3. 工具库 (Utility)
位于 `Runtime/Utility` 目录，提供了极其丰富的静态工具类，建议优先使用这些经过验证的工具而非重复造轮子。

| 分类 | 类名 | 主要功能 |
| :--- | :--- | :--- |
| **应用程序** | `Utility.Application` | 帧率设置、后台运行、系统语言获取等。 |
| **程序集** | `Utility.Assembly` | 反射获取类型、获取所有程序集等。 |
| **资源路径** | `Utility.Asset.Path` | 统一的资源路径处理，支持不同平台路径转换。 |
| **类型转换** | `Utility.Converter` | 字节数组与基础类型互转、Hex 字符串转换等。 |
| **加密安全** | `Utility.Encryption` | 提供 AES, RSA, DSA 等加密算法封装。 |
| **哈希校验** | `Utility.Hash` | 提供 MD5, SHA1, HMAC-SHA256, MurmurHash3, XxHash 等哈希计算。 |
| **数据校验** | `Utility.Verifier` | 提供 CRC32, CRC64 校验算法。 |
| **文件操作** | `Utility.File` | 跨平台文件读写、复制、删除、移动，支持流式操作。 |
| **路径处理** | `Utility.Path` | 路径规范化、获取相对路径、平台路径适配。 |
| **压缩解压** | `Utility.Zip` | 简易的 Zip 文件压缩与解压工具。 |
| **JSON** | `Utility.Json` | 统一的 JSON 序列化/反序列化接口（底层可适配不同库）。 |
| **网络辅助** | `Utility.Net` | IP 地址处理、网络状态检测。 |
| **时间管理** | `Utility.Time` | 获取时间戳、格式化时间字符串、定时器工具。 |
| **防作弊时间** | `Utility.TimeAntiCheating` | 基于网络时间校准的防作弊时间获取。 |
| **随机数** | `Utility.Random` | 提供比 System.Random 更高效、功能更丰富的随机数生成。 |
| **数学工具** | `Utility.Math` | 常用数学计算辅助。 |
| **对象工具** | `Utility.Object` | 对象深度拷贝、类型判断等。 |
| **ID生成** | `Utility.IdGenerator` | 雪花算法等全局唯一 ID 生成器。 |
| **DoTween** | `Utility.DoTween` | DoTween 常用动画封装。 |
| **FairyGUI** | `Utility.Fui` | FairyGUI 相关辅助工具。 |
| **Unity渲染** | `Utility.UnityRenderer` | 渲染相关辅助，如获取包围盒等。 |
| **Unity扩展** | `Extension` | 包含对 `GameObject`, `Transform`, `Vector3`, `Color` 等的链式扩展方法。 |

---

## 4. 使用指南

### 4.1 定义一个自定义模块
假设我们需要扩展一个“网络管理器”模块：

```csharp
using FuFramework.Core.Runtime;
using UnityEngine;

// 1. 继承 FuModule
public class NetworkManager : FuModule
{
    // 2. 设置优先级 (网络模块属于核心基础设施，优先级较高)
    protected override int Priority => ModulePriority.Core;

    // 3. 初始化
    protected override void OnInit()
    {
        FuLogger.LogInfo("NetworkManager 初始化...");
        // 初始化网络连接、协议注册等...
    }

    // 4. 每帧更新 (可选)
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 处理网络消息队列...
    }

    // 5. 关闭清理
    protected override void OnShutdown(ShutdownType shutdownType)
    {
        FuLogger.LogInfo("NetworkManager 关闭...");
        // 断开连接、清理资源...
    }
    
    // 模块功能方法
    public void Connect(string ip, int port) { ... }
}
```

### 4.2 调用模块
在任何地方（如登录逻辑中）：

```csharp
// 获取模块实例（如果未创建会自动创建）
var netMgr = ModuleManager.GetModule<NetworkManager>();

// 调用模块方法
netMgr.Connect("127.0.0.1", 8888);
```

### 4.3 使用 BindableProperty
用于实现数据驱动的 UI 更新：

```csharp
public class PlayerData
{
    // 定义可绑定属性，初始值为 100
    public BindableProperty<int> Health = new BindableProperty<int>(100);
}

// 在 UI 中使用
public class PlayerUI : MonoBehaviour
{
    private PlayerData m_Data;
    public Text healthText;

    void Start()
    {
        m_Data = new PlayerData();
        
        // 注册回调，当 Health 变化时自动更新 UI
        // RegisterWithInitValue 会立即执行一次回调，确保 UI 显示初始值
        m_Data.Health.RegisterWithInitValue(OnHealthChanged);
    }

    void OnHealthChanged(int currentHealth)
    {
        healthText.text = $"HP: {currentHealth}";
    }

    void OnDestroy()
    {
        // 记得注销事件，防止内存泄漏
        m_Data.Health.UnRegister(OnHealthChanged);
    }
}
```

### 4.4 使用扩展方法
Core 模块为 Unity 原生类型提供了大量便捷扩展，需引用命名空间 `FuFramework.Core.Runtime.Extension` (通常已包含)。

```csharp
// Transform 扩展
transform.Reset(); // 重置位置、旋转、缩放
transform.SetPosX(10f); // 仅设置 X 坐标

// GameObject 扩展
gameObject.GetOrAddComponent<Rigidbody>(); // 获取或添加组件

// 集合扩展
var list = new List<int> { 1, 2, 3 };
list.IsNullOrEmpty(); // 判断是否为空
```

---

## 5. 编辑器功能 (Editor)
Core 模块还包含强大的编辑器扩展能力：
- **Inspector 增强**：`FuFrameworkInspector` 为所有框架组件提供了统一的绘制基类，支持编译状态监听。
- **宏定义管理**：通过菜单 `FuFramework -> 脚本编译宏定义设置`，可以快速开启或关闭特定功能（如日志、热更新模式等）。
- **构建工具链**：集成了热更新代码编译 (`BuildHotfix`) 和 WebGL 构建辅助工具，简化打包流程。

## 6. 目录结构说明
```text
Core/
├── Editor/                 # 编辑器扩展代码
│   ├── BuildHotfix/        # 热更新编译工具
│   ├── Inspector/          # Inspector绘制基类
│   └── Misc/               # 杂项工具(宏定义管理等)
└── Runtime/                # 运行时核心代码
    ├── Base/               # 基础架构(FuModule, ModuleManager)
    ├── Extension/          # C#及Unity类型扩展方法
    ├── Property/           # 属性绑定系统(BindableProperty)
    └── Utility/            # 通用工具库(File, Json, Encryption...)
```
