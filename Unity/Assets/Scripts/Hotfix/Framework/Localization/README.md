# FuFramework Localization Module

## 1. 简介

FuFramework Localization 模块是游戏框架的本地化/多语言管理系统，支持 40+ 种语言的文本本地化。该模块通过 Luban 配置表管理多语言文本，支持运行时语言切换并自动广播事件通知所有监听者刷新文本。

## 2. 核心特性

- **多语言支持**：覆盖 40+ 种语言（`ELanguage` 枚举）
- **配置表驱动**：本地化文本存储在 Luban 配置表 `TbLocalization` 中，策划可独立维护
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

### 3.2 语言类型（ELanguage 枚举）

命名空间：`Hotfix.Framework.Localization`

部分支持的语言：
`ChineseSimplified`, `ChineseTraditional`, `English`, `Japanese`, `Korean`, `French`, `German`, `Spanish`, `PortugueseBrazil`, `PortuguesePortugal`, `Russian`, `Arabic`, `Thai`, `Vietnamese`, `Indonesian`, `Turkish`, `Italian`, `Polish`, `Dutch`, ...

## 4. 核心类说明

### 4.1 LocalizationModule

本地化管理模块，继承自 `ModuleBase`。通过 `ModuleManager.GetModule<LocalizationModule>()` 获取实例。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `LocalizationModule` | 模块静态单例 |
| `Language` | `ELanguage` | 获取或设置当前语言（设置时会持久化并广播事件） |
| `SystemLanguage` | `ELanguage`（静态） | 获取当前系统语言（只读） |
| `LocalizationProvider` | `ILocalizationProvider` | 获取或设置本地化多语言提供者 |

**核心方法：**

```csharp
// 获取本地化文本（使用当前语言）
string GetLanguageText(string key, params object[] args)
```

`Language` 属性的 setter 内部会：
1. 通过 `StorageModule` 持久化语言设置
2. 通过 `EventModule` 广播 `LanguageChangeEventArgs` 事件

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

`ILocalizationProvider` 的默认实现，从 Luban 配置表 `TbLocalization` 中根据当前语言获取对应文本。通过 `ConfigModule.Instance.GetConfig<TbLocalization>()` 获取配置表并缓存。

文本查找逻辑：
1. 根据 `LocalizationModule.Instance.Language` 确定当前语言
2. 从 `TbLocalization` 表中按 key 查找配置行
3. 取对应语言字段的值，若为空则回退到 English
4. 若有 `args` 参数，使用 `string.Format` 格式化

### 4.4 LanguageChangeEventArgs

语言变更事件参数，语言切换时通过 `EventModule.Broadcast` 广播。

命名空间：`Hotfix.Framework.Localization`

**核心成员：**

| 成员 | 类型 | 说明 |
|------|------|------|
| `EventId` | `string`（常量） | 事件编号：`"Event.Localization.LanguageChange"` |
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

        // 获取当前语言下的本地化文本（无参数）
        return localizationModule.GetLanguageText("UI_StartGame_Btn");

        // 获取当前语言下的本地化文本（带参数格式化）
        // return localizationModule.GetLanguageText("UI_Gold_Count", 1000);
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
    private EventModule m_EventModule;

    public void Init()
    {
        m_EventModule = ModuleManager.GetModule<EventModule>();

        m_EventModule.Subscribe(LanguageChangeEventArgs.EventId, OnLanguageChanged);
    }

    private void OnLanguageChanged(object sender, GameEventArgs e)
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
├── ELanguage.cs                  # 语言类型枚举
├── ILocalizationProvider.cs      # 本地化提供器接口
├── LanguageChangeEventArgs.cs    # 语言变更事件
├── LocalizationModule.cs         # 本地化管理模块
├── LocalizationProvider.cs       # 本地化提供器实现
└── README.md                     # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 `ModuleBase` 基类、`ModuleManager`
- **Hotfix.Framework.Config**：配置表系统（`ConfigModule`、Luban `TbLocalization`）
- **Hotfix.Framework.Event**：事件系统（`EventModule`、`GameEventArgs`）
- **Hotfix.Framework.Storage**：本地存储（`StorageModule`，持久化语言设置）
- **AOT.Framework.Core.Log**：日志（`FuLogger`）

## 8. 最佳实践

1. **Key 命名规范**：使用 `模块_功能_元素` 格式，如 `UI_Shop_BuyBtn`
2. **避免硬编码**：所有用户可见文本都应通过 `GetLanguageText` 获取
3. **缓存文本**：频繁使用的文本可在初始化时缓存，避免重复查询
4. **语言变更时刷新**：UI 界面应监听 `LanguageChangeEventArgs` 事件，及时刷新文本
5. **兜底语言**：配置未覆盖的语言字段会自动回退到 English
6. **参数格式化**：需要动态内容的文本使用 `params object[] args` 参数，配置表中使用 `{0}`、`{1}` 等占位符

## 9. 注意事项

1. 语言切换后，所有已显示的 UI 需要手动刷新文本（监听 `LanguageChangeEventArgs.EventId` 事件）
2. 本地化文本 Key 在配置表中必须唯一
3. 若 `LocalizationProvider` 未设置，`GetLanguageText` 会返回 `[key]` 格式的占位符文本并输出警告日志
4. 语言设置存储在本地，卸载游戏后不会丢失
5. 设置语言为 `ELanguage.Unspecified` 会抛出 `InvalidOperationException`
