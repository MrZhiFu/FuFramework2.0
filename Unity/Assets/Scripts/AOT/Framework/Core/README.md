# FuFramework Core Module

## 1. 简介

**FuFramework Core** 是整个框架的基石，为框架模块和上层业务逻辑提供统一的底层架构支持。它不包含具体的游戏业务逻辑，而是专注于解决以下核心问题：

- **模块化管理**：如何规范化地定义、初始化、运行和销毁一个功能模块。
- **生命周期控制**：确保各个模块按照正确的顺序和时机执行。
- **基础设施**：提供日志、单例、序列化、数据结构、属性绑定等通用服务。
- **工具库集成**：内置大量经过优化的 `Utility` 工具类，覆盖开发中的高频需求。

***

## 2. 核心架构详解

### 2.1 模块系统 (Module System)

框架采用**模块化**设计，所有核心功能（如资源管理、UI管理、网络管理）都必须继承自 `ModuleBase`。

#### ModuleBase (模块基类)

继承自 `MonoBehaviour`，但被框架接管了生命周期，继承`MonoBehaviour`只是为了更好的在模块属性面板中显示模块信息。

- **`OnInit()`**: 模块被注册时调用，仅执行一次。用于初始化变量、注册事件，由管理器统一驱动等。
- **`OnUpdate(float deltaTime, float unscaledDeltaTime)`**: 类似于 Unity 的 Update，但由管理器统一驱动。
- **`OnLateUpdate(float deltaTime, float unscaledDeltaTime)`**: 类似于 Unity 的 LateUpdate，由管理器统一驱动。
- **`OnFixedUpdate()`**: 类似于 Unity 的 FixedUpdate，由管理器统一驱动。
- **`OnDispose()`**: 游戏退出或模块卸载时调用，用于清理资源，由管理器统一驱动。

#### ModuleManager (模块管理器)

核心静态类，负责维护所有 `ModuleBase` 的实例。

- **模块注册**：调用 `RegisterModule<T>()` 时，如果模块不存在，会自动创建并挂载到 `[FrameworkModule]` 根节点下。
- **模块不销毁**：自动标记 `DontDestroyOnLoad`，确保模块在场景切换时不会丢失。
- **生命周期管理**：统一管理所有模块的 Update、LateUpdate、FixedUpdate 和 Dispose 调用。

**主要方法：**

```csharp
// 获取模块实例（仅查找已注册的模块，不会自动创建）
T GetModule<T>() where T : ModuleBase

// 注册模块（如果模块不存在，会自动创建并挂载到 `[FrameworkModule]` 根节点下）
void RegisterModule<T>() where T : ModuleBase

// 释放所有模块（逆序释放）
void Dispose()

// 重新初始化所有模块
void ReInit()
```

***

### 2.2 基础服务 (Infrastructure)

#### 日志系统 (FuLogger)

封装了 `UnityEngine.Debug`，支持多级别日志输出，通过条件编译控制日志输出。

**日志级别：**

- `LogInfo`: 信息级别日志
- `LogWarning`: 警告级别日志
- `LogError`: 错误级别日志
- `LogFatal`: 严重错误级别日志

**条件编译宏定义：**

- `ENABLE_LOG`: 启用所有日志
- `ENABLE_INFO_LOG`: 启用信息日志
- `ENABLE_DEBUG_AND_ABOVE_LOG`: 启用 Debug 及以上级别
- `ENABLE_INFO_AND_ABOVE_LOG`: 启用 Info 及以上级别
- `ENABLE_WARNING_AND_ABOVE_LOG`: 启用 Warning 及以上级别
- `ENABLE_ERROR_AND_ABOVE_LOG`: 启用 Error 及以上级别
- `ENABLE_FATAL_AND_ABOVE_LOG`: 启用 Fatal 及以上级别

#### 单例模式

- **`Singleton<T>`**: 纯 C# 类的单例基类，线程安全。
- **`MonoSingleton<T>`**: 继承自 MonoBehaviour 的单例基类，自动管理 GameObject 的创建和持久化。

#### 高性能数据结构 (DataStruct)

- **`FuLinkedList<T>`**: 优化过的双向链表，通过节点缓存池减少 GC。
- **`FuBidirectionalDictionary<TKey, TValue>`**: 双向字典，支持通过 Value 反查 Key。
- **`FuMultiDictionary<TKey, TValue>`**: 多值字典，一个 Key 可对应多个 Value。

#### 属性绑定 (BindableProperty<T>)

实现了观察者模式的属性包装器。

- 当 `Value` 发生变化时，自动触发注册的回调函数。
- 支持 `RegisterWithInitValue`，在注册时立即执行一次回调，方便 UI 初始化。

**主要方法：**

```csharp
// 注册值变化事件
BindableProperty<T> Register(Action<T> callback)

// 注册值变化事件，并立即触发一次回调
BindableProperty<T> RegisterWithInitValue(Action<T> callback)

// 移除事件
void UnRegister(Action<T> callback)

// 清除所有事件和值
void Clear()
```

#### 异常防护 (FuGuard)

提供静态判空方法，用于参数校验。

- `NotNull<T>(T value, string name)`: 确保值不为 null
- `NotNullOrEmpty(string value, string name)`: 确保字符串不为空
- `NotRange(int value, int min, int max, string name)`: 确保值在指定范围内

***

## 3. 工具库 (Utility)

位于 `Runtime/Utility` 目录，提供了极其丰富的静态工具类。

### 3.1 应用程序工具 (Utility.Application)

提供平台相关的获取和判断功能。

**平台判断属性：**

- `PlatformName`: 获取平台名称 (Android/MacOs/iOS/WebGL/Windows)
- `IsEditor`: 是否是编辑器环境
- `IsAndroid`: 是否是安卓平台
- `IsWebGL`: 是否是 WebGL 平台
- `IsWindows`: 是否是 Windows 平台
- `IsLinux`: 是否是 Linux 平台
- `IsMacOsx`: 是否是 Mac 平台
- `IsIOS`: 是否是 iOS 平台

**方法：**

- `OpenURL(string url)`: 打开 URL（支持 iOS 原生调用）

### 3.2 文件操作工具 (Utility.File)

跨平台文件读写、复制、删除、移动，支持流式操作。

**主要方法：**

```csharp
// 获取带单位的字节大小字符串 (B/KB/MB/GB/TB/PB)
string GetBytesSizeWithUnit(long size)

// 获取目录下所有文件（递归）
void GetAllFiles(List<string> files, string dir)

// 清理目录
void CleanDirectory(string dir)

// 复制目录
void CopyDirectory(string srcDir, string targetDir)

// 复制文件
void Copy(string sourceFileName, string destFileName, bool overwrite = false)

// 删除文件/目录
void Delete(string path)
void DeleteDir(string path)

// 判断文件是否存在（支持 Android StreamingAssets）
bool IsExists(string path)

// 移动文件
void Move(string sourceFileName, string destFileName)

// 读取文件
byte[] ReadAllBytes(string path)
string ReadAllText(string path)
string[] ReadAllLines(string path)
string[] ReadAllLines(string path, Encoding encoding)

// 写入文件
void WriteAllBytes(string path, byte[] buffer)
void WriteAllText(string path, string content)
void WriteAllText(string path, string content, Encoding encoding)
void WriteAllLines(string path, string[] lines)
void WriteAllLines(string path, string[] lines, Encoding encoding)
```

### 3.3 JSON 工具 (Utility.Json)

基于 Newtonsoft.Json 的 JSON 序列化/反序列化工具。

**主要方法：**

```csharp
// 序列化对象为 JSON 字符串
string ToJson(object obj)

// 反序列化 JSON 字符串为对象
T ToObject<T>(string json)
object ToObject(Type objectType, string json)
```

### 3.4 加密工具 (Utility.Encryption)

提供多种加密算法封装。

#### AES 加密 (Utility.Encryption.Aes)

对称加密算法，速度快，安全级别高。

```csharp
// 加密字符串
string AesEncrypt(string encryptStr, string encryptKey)

// 加密字节数组
byte[] AesEncrypt(byte[] encryptByte, string encryptKey)

// 解密字符串
string AesDecrypt(string decryptStr, string decryptKey)

// 解密字节数组
byte[] AesDecrypt(byte[] decryptByte, string decryptKey)
```

#### 其他加密算法

- **RSA**: 非对称加密算法
- **DSA**: 数字签名算法
- **XOR**: 异或加密（简单快速）

### 3.5 哈希工具 (Utility.Hash)

提供多种哈希算法。

- **MD5**: 文件或字符串 MD5 计算
- **SHA1**: SHA1 哈希计算
- **SHA256**: SHA256 哈希计算
- **MurmurHash3**: 高性能哈希算法
- **XxHash**: 极高速哈希算法

### 3.6 校验工具 (Utility.Verifier)

提供 CRC 校验算法。

- **CRC32**: 32位循环冗余校验
- **CRC64**: 64位循环冗余校验

### 3.7 时间工具 (Utility.Time)

获取时间戳、格式化时间字符串、定时器工具。

**主要功能：**

- 获取本地时间戳
- 获取 UTC 时间戳
- 时间格式化
- 定时器工具

#### 防作弊时间 (Utility.TimeAntiCheating)

基于网络时间校准的防作弊时间获取。

### 3.8 其他工具类

| 类名                     | 主要功能                        |
| :--------------------- | :-------------------------- |
| `Utility.Assembly`     | 反射获取类型、获取所有程序集等             |
| `Utility.Asset.Path`   | 统一的资源路径处理，支持不同平台路径转换        |
| `Utility.BitConverter` | 字节数组与基础类型互转                 |
| `Utility.Net`          | IP 地址处理、网络状态检测              |
| `Utility.Random`       | 提供比 System.Random 更高效的随机数生成 |
| `Utility.Math`         | 常用数学计算辅助                    |
| `Utility.Object`       | 对象深度拷贝、类型判断等                |
| `Utility.IdGenerator`  | 雪花算法等全局唯一 ID 生成器            |
| `Utility.Path`         | 路径规范化、获取相对路径、平台路径适配         |
| `Utility.Zip`          | 简易的 Zip 文件压缩与解压工具           |
| `Utility.Fui`          | FairyGUI 相关辅助工具             |

***

## 4. 扩展方法 (Extension)

位于 `Runtime/Extension` 目录，为 C# 和 Unity 原生类型提供链式扩展方法。

### 4.1 GameObject 扩展

```csharp
// 销毁物体下的所有子物体
go.RemoveChildren()

// 销毁游戏物体
go.DestroyObject()

// 获取或增加组件
go.GetOrAddComponent<T>()
go.GetOrAddComponent(Type type)

// 销毁组件
go.DestroyComponent<T>()

// 重置变换数据
go.ResetTransform()

// 递归设置层次
go.SetLayerRecursively(int layer, bool children = true)

// 根据名称查找子对象
go.FindChildGameObjectByName(string name)

// 设置排序层
go.SetSortingGroupLayer(string sortingLayer, bool children = true)

// 判断是否在场景中
go.InScene()

// 创建游戏对象
GameObject.Create(Transform parent, string name)
GameObject.Create(GameObject parent, string name)
```

### 4.2 Transform 扩展

```csharp
// 查找子节点（递归）
transform.FindChildName(string name)

// 设置绝对位置坐标
transform.SetPositionX(float newValue)
transform.SetPositionY(float newValue)
transform.SetPositionZ(float newValue)

// 增加绝对位置坐标
transform.AddPositionX(float deltaValue)
transform.AddPositionY(float deltaValue)
transform.AddPositionZ(float deltaValue)

// 设置相对位置坐标
transform.SetLocalPositionX(float newValue)
transform.SetLocalPositionY(float newValue)
transform.SetLocalPositionZ(float newValue)

// 增加相对位置坐标
transform.AddLocalPositionX(float deltaValue)
transform.AddLocalPositionY(float deltaValue)
transform.AddLocalPositionZ(float deltaValue)

// 设置相对尺寸
transform.SetLocalScaleX(float newValue)
transform.SetLocalScaleY(float newValue)
transform.SetLocalScaleZ(float newValue)

// 增加相对尺寸
transform.AddLocalScaleX(float deltaValue)
transform.AddLocalScaleY(float deltaValue)
transform.AddLocalScaleZ(float deltaValue)

// 二维空间下朝向目标点
transform.LookAt2D(Vector2 lookAtPoint2D)
```

### 4.3 字符串扩展

```csharp
// 快速比较字符串
str.EqualsFast(string target)
str.EndsWithFast(string target)
str.StartsWithFast(string target)

// 字符串转字节数组
str.ToByteArray()
str.ToUtf8()
str.HexToBytes()

// 判空
str.IsNullOrWhiteSpace()
str.IsNullOrEmpty()
str.IsNotNullOrWhiteSpace()
str.IsNotNullOrEmpty()

// 格式化
str.Format(params object[] args)

// 清理字符
str.TrimEmpty()      // 移除 \n、\t、\r、空格
str.TrimZhCn()       // 移除中文字符

// 命名转换
str.ConvertToSnakeCase()  // 驼峰转蛇形命名

// 分割转换
str.SplitToIntArray(char sep = '+')
str.SplitTo2IntArray(char sep1 = ';', char sep2 = '+')

// 创建目录
str.CreateAsDirectory(bool isFile = false)

// 读取一行
str.ReadLine(ref int position)
```

### 4.4 集合扩展

```csharp
// Dictionary 扩展
dict.Merge(key, value, func)           // 合并值
dict.GetOrAdd(key, valueGetter)        // 获取或添加
dict.GetOrAdd<TKey, TValue>(key)       // 获取或添加（使用默认构造函数）
dict.RemoveIf(predict)                 // 条件移除

// ICollection 扩展
collection.IsNullOrEmpty()             // 判断是否为空
source.DistinctBy(keySelector)         // 根据条件去重

// List 扩展
list.Shuffle()                         // 打乱顺序（洗牌算法）
list.RemoveIf(condition)               // 条件移除
list.ListToString(separator = ",")     // 转字符串

// HashSet 扩展
hashSet.AddRange(IEnumerable<T> e)     // 批量添加
```

***

## 5. 编辑器功能 (Editor)

Core 模块包含强大的编辑器扩展能力。

### 5.1 Inspector 增强 (FuFrameworkInspector)

为所有框架组件提供统一的 Inspector 绘制基类，支持编译状态监听。

```csharp
public abstract class FuFrameworkInspector : UnityEditor.Editor
{
    // 编译开始事件
    protected virtual void OnCompileStart() { }
    
    // 编译完成事件
    protected virtual void OnCompileComplete() { }
    
    // 判断是否是预制体
    protected bool IsPrefabInHierarchy(Object obj)
}
```

### 5.2 脚本宏定义管理 (ScriptingDefineSymbols)

通过菜单 `FuFramework/相关的脚本编译宏定义设置`，可以快速开启或关闭特定功能。

**主要方法：**

```csharp
// 检查是否存在宏定义
bool HasScriptingDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)

// 添加宏定义
void AddScriptingDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
void AddScriptingDefineSymbol(string symbol)  // 所有平台

// 移除宏定义
void RemoveScriptingDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
void RemoveScriptingDefineSymbol(string symbol)  // 所有平台

// 获取/设置宏定义
string[] GetScriptingDefineSymbols(BuildTargetGroup buildTargetGroup)
void SetScriptingDefineSymbols(BuildTargetGroup buildTargetGroup, string[] symbols)
```

### 5.3 热更新编译工具 (BuildHotfixHelper)

集成 HybridCLR 热更新代码编译和拷贝功能。

**菜单项：**

- `FuFramework/Build/Copy Hotfix Code`: 复制热更新代码 DLL 到 `Assets/Bundles/Code`
- `FuFramework/Build/Copy AOT Code`: 复制 AOT 代码 DLL 到 `Assets/Bundles/AOTCode`

### 5.4 小游戏宏定义 (MiniGameDefineSymbolHelper)

快速开启/关闭小游戏平台适配。

**菜单项：**

- `FuFramework/MiniGame/WeChat/Open`: 开启微信小游戏适配 (`ENABLE_WECHAT_MINI_GAME`)
- `FuFramework/MiniGame/WeChat/Close`: 关闭微信小游戏适配
- `FuFramework/MiniGame/DouYin/Open`: 开启抖音小游戏适配 (`ENABLE_DOUYIN_MINI_GAME`)
- `FuFramework/MiniGame/DouYin/Close`: 关闭抖音小游戏适配

***

## 6. 使用指南

### 6.1 定义一个自定义模块

```csharp
using FuFramework.Core.Runtime;
using UnityEngine;

// 1. 继承 ModuleBase
public class NetworkModule : ModuleBase
{
    // 2. 初始化
    protected internal override void OnInit()
    {
        FuLogger.LogInfo("NetworkModule 初始化...");
        // 初始化网络连接、协议注册等...
    }

    // 3. 每帧更新 (可选)
    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 处理网络消息队列...
    }

    // 4. 延迟帧更新 (可选)
    protected internal override void OnLateUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 延迟处理...
    }

    // 5. 固定帧更新 (可选)
    protected internal override void OnFixedUpdate()
    {
        // 物理相关更新...
    }

    // 6. 关闭清理
    protected internal override void OnDispose()
    {
        FuLogger.LogInfo("NetworkModule 关闭...");
        // 断开连接、清理资源...
    }
    
    // 模块功能方法
    public void Connect(string ip, int port) { ... }
}
```

### 6.2 调用模块

```csharp
// 注册模块
ModuleManager.RegisterModule<NetworkModule>();

// 获取模块实例
var netMgr = ModuleManager.GetModule<NetworkModule>();

// 调用模块方法
netMgr.Connect("127.0.0.1", 8888);
```

### 6.3 使用 BindableProperty

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

### 6.4 使用扩展方法

```csharp
// 引用命名空间
using FuFramework.Core.Runtime;

// Transform 扩展
transform.SetLocalPositionX(10f);  // 仅设置 X 坐标
transform.AddLocalScaleY(0.5f);    // 增加 Y 缩放

// GameObject 扩展
go.GetOrAddComponent<Rigidbody>();        // 获取或添加组件
go.RemoveChildren();                       // 销毁所有子物体
go.SetLayerRecursively(LayerMask.NameToLayer("UI"));  // 递归设置层

// 字符串扩展
if (str.IsNullOrEmpty()) { ... }           // 判空
var bytes = str.ToUtf8();                   // 转 UTF8 字节数组
var snake = "HelloWorld".ConvertToSnakeCase();  // hello_world

// 集合扩展
var dict = new Dictionary<string, List<int>>();
var list = dict.GetOrAdd("key", k => new List<int>());  // 获取或创建

var numbers = new List<int> { 1, 2, 3, 4, 5 };
numbers.Shuffle();                          // 打乱顺序
```

### 6.5 使用工具类

```csharp
// 文件操作
var size = Utility.File.GetBytesSizeWithUnit(1024 * 1024);  // "1MB"
Utility.File.Copy("source.txt", "dest.txt", true);
var content = Utility.File.ReadAllText("config.json");

// JSON 序列化
var json = Utility.Json.ToJson(playerData);
var data = Utility.Json.ToObject<PlayerData>(json);

// AES 加密
var encrypted = Utility.Encryption.Aes.AesEncrypt("secret data", "password");
var decrypted = Utility.Encryption.Aes.AesDecrypt(encrypted, "password");

// 平台判断
if (Utility.Application.IsAndroid) { ... }
if (Utility.Application.IsWebGL) { ... }

// 打开 URL
Utility.Application.OpenURL("https://www.example.com");
```

***

## 7. 目录结构说明

```text
Core/
├── Editor/                          # 编辑器扩展代码
│   ├── BuildHotfix/                 # 热更新编译工具
│   │   ├── BuildHotfixHelper.cs     # 热更新代码拷贝
│   │   ├── HotFixAssemblyDefinitionHelper.cs
│   │   └── HotFixEditorCompilerHelper.cs
│   ├── BuildProduct/                # 构建产物工具
│   ├── BuildWebGLTools/             # WebGL 构建工具
│   ├── Cropping/                    # 裁剪工具
│   ├── Inspector/                   # Inspector 绘制基类
│   │   └── FuFrameworkInspector.cs
│   ├── MiniGame/                    # 小游戏宏定义管理
│   │   └── MiniGameDefineSymbolHelper.cs
│   └── Misc/                        # 杂项工具
│       ├── ScriptingDefineSymbols.cs    # 宏定义管理
│       ├── Toolbar/                 # 工具栏扩展
│       └── ...
└── Runtime/                         # 运行时核心代码
    ├── Base/                        # 基础架构
    │   ├── DataStruct/              # 高性能数据结构
    │   │   ├── FuBidirectionalDictionary.cs
    │   │   ├── FuLinkedList.cs
    │   │   ├── FuLinkedListRange.cs
    │   │   ├── FuMultiDictionary.cs
    │   │   └── TypeNamePair.cs
    │   ├── Exception/               # 异常处理
    │   │   ├── FuException.cs
    │   │   └── FuGuard.cs
    │   ├── Log/                     # 日志系统
    │   │   ├── ELogLevel.cs
    │   │   └── FuLogger.cs
    │   ├── Serializer/              # 序列化
    │   │   └── FuSerializer.cs
    │   └── Singleton/               # 单例模式
    │       ├── MonoSingleton.cs
    │       └── Singleton.cs
    ├── Extension/                   # 扩展方法
    │   ├── Common/                  # C# 类型扩展
    │   │   ├── BinaryEx.cs
    │   │   ├── BufferEx.cs
    │   │   ├── CollectionEx.cs
    │   │   ├── ObjectEx.cs
    │   │   ├── StringEx.cs
    │   │   └── TypeEx.cs
    │   └── UnityEngine/             # Unity 类型扩展
    │       ├── CameraEx.cs
    │       ├── GameObjectEx.cs
    │       ├── TransformEx.cs
    │       ├── Vector2Ex.cs
    │       └── Vector3Ex.cs
    ├── Framework/                   # 框架核心
    │   ├── ModuleBase.cs              # 模块基类
    │   └── ModuleManager.cs         # 模块管理器
    ├── Property/                    # 属性绑定系统
    │   └── BindableProperty.cs
    └── Utility/                     # 通用工具库
        ├── Utility.Application.cs
        ├── Utility.Assembly.cs
        ├── Utility.Asset.Path.cs
        ├── Utility.BitConverter.cs
        ├── Utility.Encryption.*.cs
        ├── Utility.File.*.cs
        ├── Utility.Hash.*.cs
        ├── Utility.IdGenerator.cs
        ├── Utility.Json.cs
        ├── Utility.Math.cs
        ├── Utility.Net.cs
        ├── Utility.Object.cs
        ├── Utility.Path.cs
        ├── Utility.Random.cs
        ├── Utility.Time.*.cs
        ├── Utility.Verifier.*.cs
        ├── Utility.Zip.cs
        └── ...
```

***

## 8. 依赖

- **Unity**: 2021.3 LTS 或更高版本
- **Newtonsoft.Json**: JSON 序列化库
- **HybridCLR** (可选): 用于热更新功能

