# FuFramework 代码风格参考规范

---

## 1. 命名风格

### 1.1 类名

| 类别       | 规则                   | 示例                                                      |
| ---------- | ---------------------- | --------------------------------------------------------- |
| 静态工具类 | PascalCase +`static` | `public static class HotfixLauncher`                    |
| 模块类     | `XxxModule` 后缀     | `FsmModule`、`EventModule`、`RedDotModule`          |
| 扩展方法类 | `XxxEx` 后缀         | `StringEx`、`TransformEx`、`CollectionEx`           |
| Manager    | `XxxManager` 后缀    | `BagManager`、`PlayerManager`、`AccountManager`     |
| Binder     | `XxxBinder` 后缀     | `BagBinder`、`LoginBinder`、`CommonBinder`          |
| Handler    | `XxxHandler` 后缀    | `HotfixProtoHandler`、`DefaultPacketHeartBeatHandler` |
| Provider   | `XxxProvider` 后缀   | `LocalizationProvider`                                  |
| Impl       | `XxxImpl` 后缀       | `GuideActionImpl`                                       |

### 1.2 枚举（Enum）

- 枚举统一使用 `E` 前缀：`EQuality`、`ELanguage`、`EEntityStatus`、`EPlayMode`、`ELogLevel`
- Luban 工具生成和Proto 生成的枚举不做限制

### 1.3 接口

`I` 前缀：`IReference`、`INetworkChannel`、`IMessageHandler`、`ICustomComp`

### 1.4 方法

- **PascalCase**，无论 public/private
- 异步方法必须加 **`Async` **后缀：`RequestGetBagInfoAsync()`、`LoadConfigAsync()`、`LoginAsync()`
- 事件处理方法：`On` + 事件名：`OnBtnLoginClick`、`OnNetworkConnected`
- 渲染回调：`OnRenderList` + 控件名：`OnRenderListItemItem`、`OnRenderListTypeItem`
- 生命周期方法（override）：`OnInit`、`OnOpen`、`OnClose`、`OnDispose`
- 分部初始化步骤：`InitUIComp`、`InitUIEvent`、`InitEvent`、`InitRedDot`

### 1.5 属性

PascalCase：`public int Count => m_FsmDict.Count;`

### 1.6 常量

PascalCase，**不使用 ALL_CAPS**：`public const string Bag = "Bag";`、`private const string HotfixDllName = "Game.Hotfix";`

### 1.7 字段（私有）

* 严格使用`m_ 前缀 + PascalCase`
* 自动生成代码（.Gen.cs）不限制

### 1.8 局部变量和参数

- 局部变量：camelCase + `var`：`var winLauncher`、`var respBagInfo`
- 参数：camelCase：`itemId`、`redDotKey`、`displayMode`

---

## 2. 注释风格 —— 核心习惯

### 2.1 XML 文档注释（`///`）是绝对主导

每个类、方法、属性、字段、枚举成员都有完整的 `<summary>` 注释，包括私有方法：

```csharp
/// <summary>
/// 热更代码入口
/// </summary>
public static class HotfixLauncher
{
    /// <summary>
    /// 启动入口
    /// </summary>
    public static async UniTask MainAsync()
    {
        ...
    }

    /// <summary>
    /// 加载配置表
    /// </summary>
    private static async UniTask LoadConfigAsync()
    {
        ...
    }
}
```

参数和返回值也完整注释：

```csharp
/// <summary>
/// 请求使用道具
/// </summary>
/// <param name="itemId">道具ID</param>
/// <param name="count">道具数量</param>
public async UniTask RequestUseItemAsync(int itemId, long count = 1)
```

枚举成员也有注释：

```csharp
/// <summary>
/// 白
/// </summary>
White = 1,
/// <summary>
/// 蓝
/// </summary>
Blue = 2,
```

### 2.2 行注释（`//`）

- 仅用于方法体内的简短说明
- 占位/示例注释：`// Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);`
- 待完成标记：`// TODO：刷新逻辑`
- 格式保护：`//@formatter:off` / `//@formatter:on`
- ReSharper 抑制：`// ReSharper disable once CheckNamespace`

### 2.3 注释语言

**全部使用中文**，无一例外。

---

## 3. 缩进与括号

### 3.1 缩进

Tab，全项目统一。

### 3.2 括号风格

**K&R（Egyptian）风格**，100% 一致。左大括号与声明同行，右大括号独占一行：

```csharp
public sealed class FsmModule : FuModule
{
    protected override void OnInit() { }

    public bool HasFsm<T>() where T : class
    {
        return m_FsmDict.ContainsKey(new TypeNamePair(typeof(T)));
    }
}
```

单行语句也使用大括号，仅极简的卫语句例外：

```csharp
if (m_FsmDict.Count <= 0) return;          // 极简卫语句可省略大括号
if (view == null) return;

if (txtUsername.text.IsNullOrWhiteSpace())  // 稍复杂就用大括号
{
    txtError.text = "用户名或密码不能为空";
    return;
}
```

空方法体写在同一行：

```csharp
protected override void OnInit() { }
protected override void OnClose() { }
```

---

## 4. 代码组织

### 4.1 partial class

* 单独代码文件不得超过500行，超过500行则拆分为多个 partial 文件

### 4.2 `#region` 使用

- 大型类（Module、View）中广泛使用
- Region 名称用**中文**：`#region 获取有限状态机`、`#region 销毁有限状态机`
- 小型数据类和 Proto 类不使用 region

### 4.3 `@formatter:off` / `@formatter:on`

用于保护手动对齐的属性赋值区域：

```csharp
//@formatter:off
protected override EUILayer Layer         => EUILayer.Normal;   // 界面所属的层级。
protected override EUITweenType TweenType => EUITweenType.Fade; // 界面打开/关闭时的动画效果。
public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
//@formatter:on
```

### 4.4 命名空间与 ReSharper

每个文件 namespace 前都有：

```csharp
// ReSharper disable once CheckNamespace
```

---

## 5. 访问修饰符

- **始终显式声明**，不依赖默认值。`private`、`public`、`protected`、`internal` 全部写明。

---

## 6. 字段与属性声明

### 6.1 属性优先

```csharp
public int Level { get; set; }
public long Count { get; set; }
public string Name { get; private set; }
```

### 6.2 Expression-bodied 属性

简单只读属性广泛使用 `=>` 语法：

```csharp
public int Count => m_FsmDict.Count;
public RedDotNode GetNode(string key) => NodeDict.GetValueOrDefault(key);
```

### 6.3 readonly

可能的地方都加上 `readonly`：

```csharp
private readonly Dictionary<int, BagItem> m_ItemDic = new();
private readonly List<Fsm> m_TempFsmList = new();
```

### 6.4 Target-typed new()

使用C# 9.0 的简化 `new()` 语法：

```csharp
private readonly Dictionary<TypeNamePair, Fsm> m_FsmDict = new();
private List<ItemTypeData> m_Tabs = new();
```

---

## 7. `var` 关键字

- **局部变量**使用 `var`（类型从右侧显而易见时）。
- **字段、属性、参数、返回值**始终使用显式类型。

```csharp
// var 使用场景
var winLauncher = GlobalModule.UIModule.GetUI<WinLauncher>();
var respBagInfo = await ...;
var idx = listPlayer.GetChildIndex((GObject)ctx.data);

// 显式类型使用场景
public List<BagItem> GetItems() { ... }
private readonly Dictionary<int, BagItem> m_ItemDic = new();
```

---

## 8. 错误处理与日志

### 8.1 错误处理

**卫语句模式**（早返回）：

```csharp
if (view == null) return;
if (string.IsNullOrEmpty(key)) return string.Empty;
if (target == null) return;
```

**使用 `FuGuard` 工具类**：

```csharp
FuGuard.NotNull(value, nameof(value));
FuGuard.NotNullOrEmpty(name, nameof(name));
```

**使用 `FuException`（中文消息，带模块名前缀）**：

```csharp
if (HasFsm(ownerType))
    throw new FuException($"[FsmModule] 有限状态机 '{typeNamePair}' 已经存在，不能重复创建。");
```

### 8.2 日志

统一使用 `FuLogger`：

- `FuLogger.LogInfo(...)` — 一般信息和状态切换
- `FuLogger.LogError(...)` — 错误情况
- `FuLogger.LogWarning(...)` — 非关键警告
- `FuLogger.LogFatal(...)` — 致命错误（极少使用）

日志格式：字符串插值 + 模块名前缀

```csharp
FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {NodeDict.Count}");
FuLogger.LogError($"[RedDot] 注册红点失败 [{redDotKey}]，Common 包未加载");
FuLogger.LogWarning($"[RedDotModule] 注册监听时未找到节点: {key}");
```

**日志语言**：全项目统一使用中文。

---

## 9. 方法风格

### 9.1 长度

- 大部分方法 **1-10 行**，职责单一。
- 复杂业务方法不超过 50 行。

### 9.2 单一职责拆分

生命周期方法拆为明确的子步骤：

```csharp
protected override void OnInit()
{
    InitUIComp();
    InitUIEvent();
    InitEvent();
    InitRedDot();
}
```

### 9.3 Expression-bodied 方法

简单方法使用 `=>` 语法：

```csharp
public RedDotNode GetNode(string key) => NodeDict.GetValueOrDefault(key);
public bool HasNode(string key) => NodeDict.ContainsKey(key);
public void ResetCount(string key) => SetCount(key, 0);
```

### 9.4 异步模式

- 返回值：`UniTask`（可等待）或 `UniTaskVoid`（fire-and-forget）
- **必须加 `Async` 后缀**
- fire-and-forget 调用加上 `.Forget()`：

```csharp
private void OnBtnLoginClick(EventContext ctx)
{
    LoginAsync().Forget();
}

private async UniTaskVoid LoginAsync()
{
    // ...
}
```

---

## 10. 已知差异（可接受的设计决定）

| 差异                             | 说明                                                                                                                                                                                                |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **枚举前缀不统一**         | Luban/Proto 生成的枚举无前缀（`ItemType`）。这是工具限制，生成代码不手动修改。                                                                                                                    |
| **Gen 文件字段无前缀**     | FairyGUI`CSharpAPIGen` 自动生成的 `.Gen.cs` 文件中 UI 控件字段使用无前缀的匈牙利风格命名（`btnLogin`、`txtUsername`），与手写代码的 `m_` 前缀不同。修改生成器模板维护成本高，接受此差异。 |
| **Gen 文件缩进可能不一致** | 自动生成文件可能有格式化差异，导出后格式化即可。                                                                                                                                                    |

---

## 11. 快速参考卡片

```
命名：     PascalCase 类/方法/属性/常量, camelCase 局部/参数, m_ 前缀私有字段（全项目统一）
注释：     /// XML 文档注释为主，中文，全部公开方法都要写
日志：     全项目统一中文，FuLogger + 字符串插值 + 模块名前缀
缩进：     Tab
括号：     K&R（左括号同行）
修饰符：   全部显式声明，类尽量 sealed
属性：     优先属性，简单用 => 表达式
var：      局部变量用，声明处用显式类型
readonly： 能加就加
new()：    用简化语法
错误：     卫语句 + FuException
异步：     UniTask / UniTaskVoid + Async 后缀 + .Forget()
region：   中文名称
```
