# FuFramework Core Module

## 1. 简介

FuFramework Core 模块是整个框架的核心基础设施，提供模块生命周期管理、增强型数据结构、实用工具集、扩展方法等底层能力。所有其他框架模块都依赖 Core 模块，它为框架的运行提供了统一的运行环境和管理机制。

## 2. 核心特性

- **模块生命周期管理**：`ModuleBase` + `ModuleManager` 提供完整的模块注册、获取和生命周期驱动
- **帧驱动系统**：`GameDriven` 桥接 Unity MonoBehaviour 和 Hotfix 层，驱动所有模块的 Update/LateUpdate/FixedUpdate
- **增强型数据结构**：双向字典、优化链表、多值字典等高性能数据结构
- **丰富工具集**：加密、哈希、压缩、随机数、数学计算、网络工具等
- **扩展方法库**：Unity 引擎类型和 C# 基础类型的便捷扩展
- **单例模式**：线程安全的 `Singleton<T>` 和 `MonoSingleton<T>`
- **可绑定属性**：`BindableProperty<T>` 支持值变化自动回调

## 3. 核心概念

### 3.1 模块生命周期

```
OnInit → OnUpdate(每帧) → OnLateUpdate(每帧) → OnFixedUpdate(固定帧) → OnPerSecondUpdate(每秒) → OnDispose
```

### 3.2 模块驱动架构

```
┌─────────────────────────────────────────────────────────────┐
│                      GameDriven                              │
│                  (MonoBehaviour 帧驱动入口)                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Update / LateUpdate / FixedUpdate / OnDestroy      │   │
│  │         │                                            │   │
│  │         ▼                                            │   │
│  │  ModuleManager (模块管理器)                          │   │
│  │  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐      │   │
│  │  │Event │ │Asset │ │ UI   │ │Sound │ │Network│ ... │   │
│  │  │Module│ │Module│ │Module│ │Module│ │Module │      │   │
│  │  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │  GlobalModule    │
                    │ (全局模块访问入口) │
                    └──────────────────┘
```

### ICancelAsync 可取消异步对象

实现 `ICancelAsync`（`Token` + `CancelAsync`）的对象，其所有异步操作随对象销毁而取消，且可被 await 等待清理完成。

```csharp
public interface ICancelAsync
{
    CancellationToken Token { get; }   // 对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止
    UniTask CancelAsync();             // 触发取消并等待所有在途操作完成清理后才返回（可重入、幂等）
}
```

- **实现方式**：组合 `CancellationScope`（内部 CTS + 在途计数 + 「全部完成」信号），异步操作入口 `Begin()` 登记、`finally` 归零；`Begin()` 返回 struct `BeginScope`，`using` 直接调用 Dispose，零装箱零分配（勿经 `IDisposable` 接口使用）。
- **框架集成**：重启 `RestartGameAsync` 在 Dispose 与 重新初始化 之间 `await ModuleManager.CancelAllAsync()` 逐个等待取消清理，根除旧生命周期任务写回新生命周期。
- **已接入模块**：Asset/Scene/Entity/UI/Sound/Web 六个模块及 `AssetLoadRegister`；接入要点（组合 `CancellationScope`、`OnInit` 重建、`OnDispose` Cancel）见各模块 README。

## 4. 核心类说明

### 4.1 ModuleBase

模块抽象基类，所有框架模块必须继承此类。

**生命周期方法（可重写）：**

```csharp
public abstract class ModuleBase
{
    protected internal virtual void OnInit() { }              // 模块初始化
    protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }
    protected internal virtual void OnLateUpdate(float deltaTime, float unscaledDeltaTime) { }
    protected internal virtual void OnFixedUpdate() { }
    protected internal virtual void OnPerSecondUpdate() { }
    protected internal virtual void OnDispose() { }           // 模块销毁
}
```

### 4.2 ModuleManager

模块管理器，负责所有模块的注册、查找和统一驱动。

**核心方法：**

```csharp
// 模块管理
void RegisterModule<T>(T module) where T : ModuleBase
T GetModule<T>() where T : ModuleBase
bool HasModule<T>() where T : ModuleBase
void GetAllModules(List<ModuleBase> results)

// 生命周期驱动
void OnInit()     // 按依赖拓扑排序初始化所有模块
void OnUpdate(float deltaTime, float unscaledDeltaTime)
void OnLateUpdate(float deltaTime, float unscaledDeltaTime)
void OnFixedUpdate(float deltaTime, float unscaledDeltaTime)
void OnPerSecondUpdate(float deltaTime, float unscaledDeltaTime)
void OnDispose()  // 按初始化逆序销毁所有模块
```

**模块依赖特性：**

```csharp
[ModuleDependency(typeof(FsmModule))]  // 声明依赖，确保初始化顺序
public sealed class ProcedureModule : ModuleBase { }
```

### 4.3 GlobalModule

全局模块访问入口，聚合所有常用模块的延迟加载引用，提供简洁的访问方式。

### 4.4 GameDriven

Unity 帧驱动入口，继承 `MonoBehaviour`，挂接 `ModuleManager` 的生命周期方法到 Unity 的 Update/LateUpdate/FixedUpdate。

### 4.5 数据结构

| 类 | 说明 |
|---|---|
| `FuBidirectionalDictionary<T1, T2>` | 双向字典，支持键值互查 |
| `FuLinkedList<T>` | 带缓存节点队列的优化链表，减少 GC |
| `FuLRUCache<TKey, TValue>` | 泛型 LRU 缓存，支持驱逐回调，容量满自动淘汰最少使用项 |
| `FuMultiDictionary<TKey, TValue>` | 多值字典，一键对应多个值 |
| `TypeNamePair` | 类型+名称的组合值结构体 |

### 4.6 扩展方法

**C# 通用扩展 (`Extension/Common/`)：**

| 类 | 说明 |
|---|---|
| `CollectionEx` | 集合扩展：Merge、条件移除、洗牌算法、列表转字符串 |
| `FuGuardEx` | 守卫断言扩展：NotNull、CheckNull |
| `ObjectEx` | 对象扩展：IsNull、IsNotNull |
| `StringEx` | 字符串扩展：快速比较、分割整数数组、蛇形命名 |
| `BinaryEx` | 二进制数据扩展 |
| `BufferEx` | 缓冲区扩展 |

**Unity 引擎扩展 (`Extension/UnityEngine/`)：**

| 类 | 说明 |
|---|---|
| `GameObjectEx` | GameObject 扩展：递归销毁子物体、GetOrAddComponent |
| `TransformEx` | Transform 扩展：递归查找子物体、设置位置分量 |
| `Vector2Ex` / `Vector3Ex` | 向量扩展方法 |
| `CameraEx` | Camera 扩展方法 |

### 4.7 工具集 (Utility)

Utility 是一个 partial class，按功能拆分为多个文件：

| 工具类 | 说明 |
|---|---|
| `Utility.Encryption.Aes` | AES 对称加密 |
| `Utility.Encryption.Rsa` | RSA 非对称加密 |
| `Utility.Encryption.Dsa` | DSA 数字签名 |
| `Utility.Encryption.Xor` | XOR 异或加密 |
| `Utility.Hash.Md5` | MD5 哈希 |
| `Utility.Hash.Sha1` / `Sha256` | SHA 系列哈希 |
| `Utility.Hash.XxHash` | XXHash 高速哈希 |
| `Utility.Hash.Hash3` | Hash3 哈希 |
| `Utility.Zip` | 压缩/解压缩（基于 SharpZipLib） |
| `Utility.Verifier.Crc32` | CRC32 数据校验 |
| `Utility.Random` | 可设种子的随机数生成器 |
| `Utility.Math` | 数学工具：像素/厘米转换、矩形相交检测、角度/四元数转换 |
| `Utility.Net` | 网络工具：获取可用端口、本机 IP |
| `Utility.Time` / `Time.AntiCheating` | 时间工具与反作弊时间 |
| `Utility.Fui` | FairyGUI 辅助：获取 UI 路径、搜索父节点 |
| `Utility.IdGenerator` | 线程安全的唯一 ID 生成器 |
| `Utility.BitConverter` | 位转换工具 |

### 4.8 其他

| 类 | 说明 |
|---|---|
| `Singleton<T>` | 线程安全的纯 C# 单例基类 |
| `MonoSingleton<T>` | 线程安全的 MonoBehaviour 单例基类 |
| `BindableProperty<T>` | 可绑定属性，值变化时自动触发 Action 回调 |
| `FuSerializer` | 序列化器基类，支持版本化的序列化/反序列化回调注册 |

## 5. 使用示例

### 5.1 创建自定义模块

```csharp
using Hotfix.Framework.Core;

public class MyModule : ModuleBase
{
    protected override void OnInit()
    {
        FuLogger.LogInfo("MyModule 初始化");
    }

    protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 每帧更新逻辑
    }

    protected override void OnDispose()
    {
        FuLogger.LogInfo("MyModule 销毁");
    }
}
```

### 5.2 获取模块

```csharp
// 通过 ModuleManager 获取
var eventModule = ModuleManager.GetModule<EventModule>();

// 通过 GlobalModule 获取（延迟加载）
var uiModule = GlobalModule.UIModule;
```

### 5.3 使用扩展方法

```csharp
using Hotfix.Framework.Core;

// 空值检查
gameObject.CheckNull("gameObject 不能为空");
someVar.NotNull(nameof(someVar));

// 字符串操作
string[] ids = "1,2,3,4,5".SplitToIntArray(',');

// Transform 扩展
Transform child = transform.FindDeep("TargetChild");

// GameObject 扩展
var comp = gameObject.GetOrAddComponent<Rigidbody>();
```

### 5.4 使用单例

```csharp
// 纯 C# 单例
public class GameManager : Singleton<GameManager>
{
    public void Init() { /* ... */ }
}
// 使用: GameManager.Instance.Init();

// MonoBehaviour 单例
public class AudioManager : MonoSingleton<AudioManager>
{
    protected override void Awake() { base.Awake(); }
}
```

### 5.5 使用工具方法

```csharp
// 加密
byte[] encrypted = Utility.Encryption.Aes.Encrypt(data, key);

// 哈希
string md5 = Utility.Hash.Md5.ComputeHashString(input);

// 随机数
int randomValue = Utility.Random.GetRandom(1, 100);

// 唯一 ID
long uniqueId = Utility.IdGenerator.GenerateId();

// 压缩
byte[] compressed = Utility.Zip.Compress(data);
```

## 6. 目录结构

```text
Core/
├── 
│   ├── ModuleBase.cs              # 模块抽象基类
│   ├── ModuleManager.cs           # 模块管理器
│   ├── GlobalModule.cs            # 全局模块访问入口
│   ├── GameDriven.cs              # 帧驱动 + 游戏控制中枢
│   ├── Async/                     # 可取消异步基础设施
│   │   ├── ICancelAsync.cs        # 可取消异步对象接口
│   │   └── CancellationScope.cs   # 取消范围登记助手（CTS + 在途计数 + 全部完成信号）
│   ├── DataStruct/                # 数据结构
│   │   ├── FuBidirectionalDictionary.cs
│   │   ├── FuLinkedList.cs
│   │   ├── FuLinkedListRange.cs
│   │   ├── FuLRUCache.cs
│   │   ├── FuMultiDictionary.cs
│   │   └── TypeNamePair.cs
│   ├── Extension/                 # 扩展方法
│   │   ├── Common/                # C# 通用扩展
│   │   └── UnityEngine/           # Unity 引擎扩展
│   ├── Property/
│   │   └── BindableProperty.cs
│   ├── Serializer/
│   │   └── FuSerializer.cs
│   ├── Singleton/
│   │   ├── MonoSingleton.cs
│   │   └── Singleton.cs
│   ├── Utility/                   # 工具集 (partial class)
│   │   ├── Utility.Encryption.*.cs
│   │   ├── Utility.Hash.*.cs
│   │   ├── Utility.Zip.cs
│   │   ├── Utility.Random.cs
│   │   ├── Utility.Math.cs
│   │   ├── Utility.Net.cs
│   │   ├── Utility.Time.cs
│   │   ├── Utility.Fui.cs
│   │   ├── Utility.IdGenerator.cs
│   │   └── ...
└── README.md                      # 本文档
```

## 7. 依赖

- **Unity**：2021.3 LTS 或更高版本
- **SharpZipLib**（外部）：压缩功能

## 8. 最佳实践

2. **异常处理**：使用 `FuGuardEx` 进行参数校验，抛出明确异常
3. **数据结构选择**：一对多关系使用 `FuMultiDictionary`，双向查找使用 `FuBidirectionalDictionary`
4. **工具方法**：优先使用 Utility 中已有方法，避免重复造轮子
5. **单例使用**：MonoBehaviour 单例继承 `MonoSingleton<T>`，纯 C# 单例继承 `Singleton<T>`
