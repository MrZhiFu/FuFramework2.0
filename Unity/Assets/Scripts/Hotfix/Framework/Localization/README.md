# FuFramework Localization Module

## 1. 简介

FuFramework Localization 模块是游戏框架的本地化/多语言管理系统，语种清单配置化（当前 6 语种）。该模块通过 Luban 配置表管理多语言文本，支持运行时语言切换并自动广播事件通知所有监听者刷新文本。

## 2. 核心特性

- **多语言支持**：语种由 `ELanguage` 枚举配置生成（`__enums__.xlsx` 语言 sheet）；扩/删语种只改数据（枚举成员 + LanguageDef 行 + 本地化表列），运行时零代码改动
- **配置表驱动**：本地化文本存储在 Luban 配置表 `TbLocalization` 中，策划可独立维护
- **按语种拆分**：产物拆成每语种一个文件（`Bundles/Config/Localization/tblocalization_<语言>.bytes`，文件数随语种清单自动增减），切换语言只加载目标语种，避免全部语种同时驻留内存
- **英语兜底懒加载**：目标语种译文为空时二段查询英语表（首次 miss 触发异步加载，加载完成后后续查询生效，不阻塞当前调用）
- **运行时切换**：通过 `Language` 属性动态切换语言，自动广播 `LanguageChangeEventArgs` 事件
- **持久化存储**：语言设置通过 `StorageModule` 持久化保存，下次启动自动恢复
- **Provider 模式**：通过 `ILocalizationProvider` 接口解耦，方便扩展自定义本地化数据源
- **系统语言检测**：`SystemLanguage` 静态属性可获取当前系统语言

## 3. 核心概念

### 3.1 本地化架构

```
┌─────────────────────────────────────────────────────────────┐
│                  LocalizationModule                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Language (ELanguage)                               │   │
│  │  - 当前语言设置（读/写属性）                          │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  LocalizationProvider (ILocalizationProvider)       │   │
│  │  - 负责从配置表获取本地化文本                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
            语言切换时广播 LanguageChangeEventArgs
                              │
                              ▼
              ┌───────────────────────────┐
              │  所有监听者刷新 UI 文本    │
              └───────────────────────────┘
```

### 3.2 语言类型（ELanguage 枚举，配置生成）

命名空间：`Hotfix.Game.Config`（**由 Luban 配置生成，禁止手写**）

定义源：`Config/Excels/__enums__.xlsx` 语言 sheet。当前 7 成员：`Unspecified=0` + 6 语言成员（`ChineseSimplified`、`ChineseTraditional`、`English`、`Japanese`、`Korean`、`Spanish`，value 连续编号）。语种清单的运行时事实源是 AOT 程序集的配置生成类 `LaunchLanguageRegistry`（语种条目/系统语言映射/兜底常量），本枚举与它由同一份数据生成。

**扩语言/删语种（纯数据操作，零代码改动）**：枚举 sheet 追加/删除成员（value 顺延）→ `TbLocalization` 加/删语言列 → `TbLanguageDef`（`Config/Excels/Tables/L-LanguageDef-c-语言定义.xlsx`）加/删行（`system_language` 填 Unity 系统语言映射）→ 重跑 `gen-client-*`。取词为 key+value 直读（拆分产物统一两字段，语言维度消失）；系统语言映射由 `LaunchLanguageRegistry.FromSystemLanguage` 数据驱动；**English 兜底语种不可删除**。语言元数据（显示名/旗帜 icon/分隔符）由语言定义表管理。

## 4. 核心类说明

### 4.1 LocalizationModule

本地化管理模块，继承自 `ModuleBase`。通过 `ModuleManager.GetModule<LocalizationModule>()` 获取实例。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `LocalizationModule` | 模块静态单例 |
| `Language` | `ELanguage` | 获取或设置当前语言（设置时会持久化并广播事件） |
| `LocalizationProvider` | `ILocalizationProvider` | 获取或设置本地化多语言提供者 |

系统语言映射私有方法 `FromSystemLanguage()`（初始化时无持久化记录时使用）内部委托配置生成的 `LaunchLanguageRegistry.FromSystemLanguage`（AOT 程序集），与 AOT 侧同一事实源；跨程序集返回值按枚举名转换（两份 `ELanguage` 数值一致但类型不同）。

**核心方法：**

```csharp
// 获取本地化文本（使用当前语言）
string GetLanguageText(string key, params object[] args)
```

`Language` 属性的 setter 内部会：
1. 通过 `StorageModule` 持久化语言设置
2. **同步语言偏好到 PlayerPrefs**（`LaunchLocalization.LANGUAGE_PREF_KEY`，字符串名）——AOT 启动阶段从 PlayerPrefs 读偏好，两源不同步会导致下次启动 AOT 界面语言与本侧不一致（见第 12 节）
3. 通过 `EventModule` 广播 `LanguageChangeEventArgs` 事件

`OnInit` 读取存档确定语言后，会把最终语言**回写 PlayerPrefs**（启动收敛，修正旧版本仅存档无偏好的漂移）。

### 4.2 ILocalizationProvider

本地化提供器接口，定义从数据源获取本地化文本的方法。

命名空间：`Hotfix.Framework.Localization`

```csharp
public interface ILocalizationProvider
{
    /// <summary>
    /// 获取本地化多语言文本
    /// </summary>
    /// <param name="key">多语言 key</param>
    /// <param name="args">格式化参数</param>
    /// <returns>对应语言下的文本</returns>
    string GetLanguage(string key, params object[] args);
}
```

### 4.3 LocalizationProvider

`ILocalizationProvider` 的默认实现。按语种拆分后，`TbLocalization` 由 `TableManager` 随语言切换加载**目标语种文件**（文件内只含 `key` + 该语种列），Provider 通过 `ConfigModule.Instance.GetConfig<TbLocalization>()` 获取并缓存当前语言表。

文本查找逻辑：
1. 根据 `LocalizationModule.Instance.Language` 确定当前语言，从当前语种表按 key 查找配置行
2. 取该语种列的值；为空（未翻译或拆分产物中该键缺失）时进入**二段查询**：查英语兜底表
3. 英语兜底表为**懒加载**：首次 miss 触发异步加载（`HotfixLauncher` 注入的 `EnglishTableLoader` 直取英语文件，不走语言路由）；加载未就绪时本次同步返回空串，加载完成后后续查询生效。并发 miss 通过 `UniTaskCompletionSource` 共享同一次加载，失败/取消后允许重试
4. 若有 `args` 参数，使用 `string.Format` 格式化
5. 当前语种与英语兜底都缺失时返回空串，`LocalizationModule.GetLanguageText` 外层再包装为 `[key]` 占位符

> Provider 无独立生命周期，英语兜底加载的取消令牌挂在 `AssetModule` 模块生命周期上。

### 4.4 LanguageChangeEventArgs

语言变更事件参数，语言切换时通过 `EventModule.Broadcast` 广播。

命名空间：`Hotfix.Framework.Localization`

**核心成员：**

| 成员 | 类型 | 说明 |
|------|------|------|
| `EventId` | `string`（静态只读） | 事件编号：`typeof(LanguageChangeEventArgs).FullName` |
| `ELanguage` | `ELanguage` | 切换后的当前语言 |
| `OldELanguage` | `ELanguage` | 切换前的旧语言 |

```csharp
// 创建事件参数（通过引用池）
public static LanguageChangeEventArgs Create(ELanguage oldELanguage, ELanguage eLanguage)

// 清理（归还引用池）
public override void Clear()
```

## 5. 使用示例

### 5.1 获取本地化文本

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Localization;

public class LocalizationExample
{
    public string GetUIText()
    {
        var localizationModule = LocalizationModule.Instance;

        // 获取当前语言下的本地化文本（无参数；key 建议使用 L10nKey 常量类字段，避免硬编码）
        return localizationModule.GetLanguageText(L10nKey.common_continue_game);

        // 获取当前语言下的本地化文本（带参数格式化）
        // return localizationModule.GetLanguageText(L10nKey.xxx_gold_count, 1000);
        // 对应配置表中的文本如: "金币: {0}" → 输出 "金币: 1000"
    }
}
```

### 5.2 切换语言

```csharp
using Hotfix.Framework.Localization;

// 切换到英语
LocalizationModule.Instance.Language = ELanguage.English;

// 切换到简体中文
LocalizationModule.Instance.Language = ELanguage.ChineseSimplified;

// 语言设置会自动持久化，下次启动时恢复
// 设置时会自动广播 LanguageChangeEventArgs 事件
```

### 5.3 监听语言变更

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.Localization;

public class UITextUpdater
{
    private EventModule m_eventModule;

    public void Init()
    {
        m_eventModule = ModuleManager.GetModule<EventModule>();

        m_eventModule.Subscribe<LanguageChangeEventArgs>(OnLanguageChanged);
    }

    private void OnLanguageChanged(GameEventArgs e)
    {
        var args = e as LanguageChangeEventArgs;
        UnityEngine.Debug.Log($"语言已切换: {args.OldELanguage} → {args.ELanguage}");

        // 刷新所有 UI 文本
        RefreshAllUIText();
    }

    private void RefreshAllUIText() { /* ... */ }
}
```

### 5.4 设置自定义数据源

```csharp
using Hotfix.Framework.Localization;

// 注入自定义的本地化数据源
LocalizationModule.Instance.LocalizationProvider = new MyCustomLocalizationProvider();
```

## 6. 目录结构

```text
Localization/
├── ILocalizationProvider.cs      # 本地化提供器接口
├── LanguageChangeEventArgs.cs    # 语言变更事件
├── LocalizationModule.cs         # 本地化管理模块
├── LocalizationProvider.cs       # 本地化提供器实现
└── README.md                     # 本文档
```

> 语言枚举 `ELanguage` 为配置生成（`Generate/ELanguage.cs`），多语言 key 常量类 `L10nKey` 在 `Game/AutoGen/Tables/Extension/` 下（`is_code=true` 的 key）。

## 7. 依赖

- **Hotfix.Framework.Core**：提供 `ModuleBase` 基类、`ModuleManager`
- **Hotfix.Framework.Config**：配置表系统（`ConfigModule`、Luban `TbLocalization`、语言枚举 `ELanguage`/语言定义表 `TbLanguageDef` 为配置生成）
- **Hotfix.Framework.Event**：事件系统（`EventModule`、`GameEventArgs`）
- **Hotfix.Framework.Storage**：本地存储（`StorageModule`，持久化语言设置）
- **AOT.Framework.Core.Log**：日志（`FuLogger`）

## 8. 最佳实践

1. **Key 命名规范**：使用 `模块_功能_元素` 格式，如 `UI_Shop_BuyBtn`
2. **避免硬编码**：所有用户可见文本都应通过 `GetLanguageText` 获取
3. **缓存文本**：频繁使用的文本可在初始化时缓存，避免重复查询
4. **语言变更时刷新**：UI 界面应监听 `LanguageChangeEventArgs` 事件，及时刷新文本
5. **兜底语言**：目标语种译文为空时二段查询英语兜底表（懒加载：首次 miss 触发加载，加载完成后后续查询生效；首次 miss 可能短暂返回空串）
6. **参数格式化**：需要动态内容的文本使用 `params object[] args` 参数，配置表中使用 `{0}`、`{1}` 等占位符

## 9. 注意事项

1. 语言切换后的文本刷新：FGUI 声明式绑定（第 11 节）与业务数据绑定（`WinBase._OnLanguageChanged` 的 `OnOpen`）自动执行；仅自管文本的界面需要自行订阅 `Subscribe<LanguageChangeEventArgs>(...)` 手动刷新
2. 本地化文本 Key 在配置表中必须唯一
3. 若 `LocalizationProvider` 未设置，`GetLanguageText` 会返回 `[key]` 格式的占位符文本并输出警告日志
4. 语言设置存储在本地，卸载游戏后不会丢失
5. 设置语言为 `ELanguage.Unspecified` 会抛出 `InvalidOperationException`

## 10. 扩展：新增多语言资源类型

体系当前仅覆盖**文本**。未来要按语言区分其他资源（图片 / 音频 / 字体等）时，复用本模块的 Provider 模式：

1. **数据侧**：在 `Config/Excels/Tables/` 新建资源表（如 `TbLocalizationImage`：`key` + `is_code` + 各语言列存资源路径）。**不能放 `Excels/Local/`**（该目录是文本校验专用通道）；表名加入 `CsharpL10NKeyCodeTarget.CollectKeys` 筛选后 key 常量随 `L10nKey` 生成。完整数据侧步骤见 `Config/README.md` 的「扩展：新增多语言资源类型」。
2. **运行时侧**：新建提供器实现 `ILocalizationProvider`（或独立接口），取值模式与 `LocalizationProvider` 一致：查表 → switch 当前语言选列 → 空值回退 English 列 → 参数格式化；返回的资源路径交由 `AssetModule` 加载。
3. **联动**：新增类型表后，未来"扩语言"时须同步加语言列（与 `TbLocalization` / `TbLanguageDef` / 枚举 sheet 联动）。

## 11. FGUI 声明式多语言（L10n 插件）

除代码取文本（`GetLanguageText`）外，FGUI 界面还支持**声明式**多语言：编辑器插件把 key 写进组件 `customData`，运行时自动应用，业务代码零参与。

### 11.1 链路

```
编辑器（L10nKeyBind 插件）选中组件绑定 key → customData 的 "L10n:<key>"（或控制器模式 "L10n:<ctrl>,0=k1,1=k2"）段 → 随包发布
  → FairyGUI 包构造时（Setup_AfterAdd 锚点）只做解析 + 控制器订阅（不应用文本）
  → UIPackage.CreateObject 出口 GObject.FlushL10n(g)：整棵树构建完成后统一解析取文本写入 text/title
    （叶子类与祖先组件的 Setup 会回填包默认文本，统一应用必须发生在其后）
  → 语言切换时 WinBase._OnLanguageChanged：OnOpen() + FairyGUI.GObject.RefreshAllL10n(WinUI) 遍历重应用
  → 窗口池化复用/暂停恢复/遮挡恢复（_OnOpen/_OnResume/_OnReveal）：补一次 RefreshAllL10n（隐藏期间语言可能已切换）
```

### 11.2 委托两阶段注入

| 阶段 | 注入 | 文本源 |
|---|---|---|
| AOT（启动期） | `LaunchLocalization.InitializeAsync`（**必须先于加载界面创建**，声明式绑定构建时即取文本） | AOT 表（`LaunchLocalizationText/tblocalizationaot`） |
| 热更后 | `HotfixLauncher.InitDependenciesAsync`（覆盖式） | 热更表 `TbLocalization` |

### 11.3 约定与坑

- **可绑组件**：GTextField（text）/ GButton（title）/ GLabel（title）；`INPUTTEXT` 等动态输入禁止绑定
- **动态文本禁止绑 L10n**：代码每次 `SetText`/`Refresh` 的组件与 L10n 声明是两套来源，会互相覆盖——单一文本来源；业务 `OnOpen` 在 L10n 刷新**之后**执行，动态赋值可覆盖
- **应用时机在 FlushL10n**：Setup 锚点只解析 + 订阅控制器；叶子类/祖先组件的 Setup 会回填包默认文本，统一应用在 `UIPackage.CreateObject` 出口——**新增"Setup 后写 text/title 的组件类型"无需再打补丁**
- **隐藏窗口刷新**：`_OnLanguageChanged` 只刷可见窗口；池化复用/暂停/遮挡的窗口由 `_OnOpen/_OnResume/_OnReveal` 补 `RefreshAllL10n`
- **控制器模式**：绑定格式 `L10n:<ctrl>,<pageId>=<key>,...`；控制器切页（onChanged）自动重应用；插件面板可视化绑定（下拉选控制器 + 逐页 key + None 首项）
- **key 查不到**：热更侧显示 `[key]`；AOT 侧（表缺 key 返回空串）回退显示 key 原文（`ResolveL10nText` 空串回退，避免空白）

### 11.4 相关文件

| 文件 | 说明 |
|---|---|
| `3rdPlugins/FairyGUI/Scripts/UI/CustomExt/*.L10n.cs` | FairyGUI 运行时钩子（解析/订阅/应用/遍历，partial 分部；`FlushL10n` 统一应用） |
| `3rdPlugins/FairyGUI/Scripts/UI/UIPackage.cs` | `CreateObject` 出口调用 `FlushL10n`（L10n：统一应用锚点） |
| `FairyGUIProject/plugins/L10nKeyBind/` | 编辑器插件（Lua），见其 README |
| 设计文档 | `Docs/superpowers/specs/2026-09-19-fgui-l10n-design.md` |

## 12. 语言偏好持久化（AOT/Hotfix 双源同步）

语言偏好存在两处，语义为"同一次安装内保持一致"：

| 阶段 | 存储 | 读取方 |
|---|---|---|
| AOT（启动期） | PlayerPrefs 键 `AOT_Localization_Language`（**字符串名**，常量 `LaunchLocalization.LANGUAGE_PREF_KEY`） | `LaunchLocalization.LoadLanguagePreference`（字符串名 → 注册表校验；旧 int 值一次性迁移；无偏好回落系统语言） |
| 热更期 | StorageModule 文件（键 `Language`，枚举名字符串） | `LocalizationModule.OnInit`（读不到时回落系统语言） |

**同步契约**（违反会导致下次启动 AOT 界面语言与本侧不一致）：
1. 热更侧切换语言（`LocalizationModule.Language` setter）：写 StorageModule 后**必须同步写 PlayerPrefs**
2. `OnInit` 启动收敛：把最终语言回写 PlayerPrefs，修正旧版本漂移
3. Hotfix 引用 `LaunchLocalization` 用别名 `using LaunchLocalization = AOT.Launch.Localization.LaunchLocalization;`（两份生成的 `ELanguage` 会 CS0104）

## 13. 多语言表健康检查工具（CleanL10nKeys）

`Tools/CleanL10nKeys/`：五项检查一次运行，完整报告写 `Tools/CleanL10nKeys/多语言配置报告.txt`（gitignore），控制台/Unity 对话框只输出带色摘要。

| # | 检查 | 说明 |
|---|---|---|
| 1 | 未引用 key（可清理） | 代码/配置数据/FGUI 三类引用源都不含的 key；`--apply` 删除 Excel 行（**不导表**，导表手动） |
| 2 | 缺失 key | 已被引用（代码/FGUI）但表中不存在 → 运行时会显示 `[key]` |
| 3 | 翻译覆盖率 | 逐表逐语言空列统计 |
| 4 | FGUI 硬编码文本 | 未绑定 L10n 的静态中文（推动声明式化） |
| 5 | key 命名规范 | `^[a-z][a-z0-9_]*$` |

**Unity 菜单**（`FuFramework/多语言检查/`）：`生成现存问题报告`(1002) → `打开现存问题报告`(1003) → `清理多语言配置表`(1004，确认对话框后删行)。

**自引用排除**（检查正确性的关键）：生成的 `L10nKey.cs`/`LaunchL10nKey.cs`（含全部 is_code key 字面量）不作为引用源；配置数据扫描范围为 `Bundles/Config/Data/*.json`，本地化拆分产物在 `Bundles/Config/Localization/tblocalization_*.bytes`，天然不在扫描范围内（否则所有 key 永远"被引用"）。

**盲区**：动态拼接 key（`"common_" + x`）双向静态分析均不可检测，清理/缺失清单须人工核对。

## 14. 改造历程索引（2026-09）

| 提交 | 内容 |
|---|---|
| `f700f494` `1b16b4a9` `4c7a21a2` `da87d6de` `fec3d904` | 体系主体：gen 脚本英文化 / cs-bean / AOT 前置本地化 / FGUI 声明式 / EventPool 多 handler 修复 |
| `743f6d7f` `e7b9a2ed` `8da575f2` | 二次解析修复 / AOT 启动时序 + 偏好双源 / 插件文档 |
| `c6e0cb5a` `6f1a7060` | CleanL10nKeys 初版 / 删除一次性迁移脚本 |
| `d5e0c1ec` | 健康检查五项升级（批1） |
| `9ab48d06` | FlushL10n 统一应用 + 隐藏窗口刷新（批2，演进替代 `743f6d7f` 的逐叶子补丁） |
| `361ac543` `2fc26efa` `d6761b7b` | 插件控制器模式交互 / 面板自适应 / None 首项（批4） |
| `fee02235` | 产物按语种拆分（Luban PostProcess）+ is_code 三端剥离 + 英语兜底二段查询 |
| `71c8978c` | 服务端配置生成修复 + 表/枚举分组接线 + 客户端专属表排除（多语言表重命名 `-c-`） |

详细设计：`Docs/superpowers/specs/2026-09-17-aot-localization-design.md`、`Docs/superpowers/specs/2026-09-19-fgui-l10n-design.md`。
