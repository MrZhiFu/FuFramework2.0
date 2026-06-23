# FuFramework Localization Module

## 1. 简介

**FuFramework Localization** 模块是一个功能完善的游戏本地化系统，专为 Unity 游戏开发设计。它提供了多语言支持、语言切换、自动系统语言检测等功能，能够满足全球化游戏的多语言需求。

本模块采用**提供者模式（Provider Pattern）**设计，通过 `ILocalizationProvider` 接口解耦多语言数据的获取逻辑，由业务层（热更代码）实现具体的本地化数据提供。

***

## 2. 特性

- **多语言支持**：支持 50+ 种语言，覆盖全球主要语言
- **自动语言检测**：自动检测系统语言并设置默认语言
- **持久化存储**：语言设置自动保存到本地存储（DataSaveModule）
- **事件驱动**：提供语言改变事件通知机制
- **提供者模式**：通过接口解耦，支持自定义多语言数据源
- **参数化文本**：支持带参数的本地化文本（如 `"Welcome, {0}!"`）
- **编辑器集成**：提供可视化的编辑器界面
- **线程安全**：语言切换和事件广播线程安全

***

## 3. 核心概念

### 3.1 本地化提供者 (ILocalizationProvider)

定义获取本地化字符串的接口，由业务层实现：

```csharp
public interface ILocalizationProvider
{
    /// <summary>
    /// 获取本地化多语言接口
    /// </summary>
    /// <param name="key">多语言key</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本</returns>
    string GetLanguage(string key, params object[] args);
}
```

### 3.2 语言枚举 (ELanguage)

定义支持的所有语言类型，与 Unity 的 `SystemLanguage` 映射：

```csharp
public enum ELanguage : byte
{
    Unspecified = 0,        // 未指定
    ChineseSimplified,      // 简体中文
    ChineseTraditional,     // 繁体中文
    English,                // 英语
    Japanese,               // 日语
    Korean,                 // 韩语
    // ... 50+ 种语言
}
```

### 3.3 语言改变事件

当语言切换时，模块会广播 `LanguageChangeEventArgs` 事件：

```csharp
public class LanguageChangeEventArgs : GameEventArgs
{
    public ELanguage OldELanguage { get; set; }  // 旧语言
    public ELanguage ELanguage { get; set; }     // 新语言
}
```

***

## 4. 核心类详解

### 4.1 LocalizationModule

本地化管理器，继承自 `FuModule`，负责语言设置管理和本地化文本获取。

#### 职责

1. **管理当前语言**：获取和设置当前使用的语言
2. **系统语言检测**：自动检测并映射 Unity 系统语言
3. **持久化存储**：语言设置自动保存到本地
4. **事件通知**：语言切换时广播事件
5. **本地化文本获取**：通过提供者获取本地化字符串

#### 公开属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Language` | `ELanguage` | 获取或设置当前语言（自动持久化） |
| `SystemLanguage` | `static ELanguage` | 获取系统语言（静态属性） |
| `LocalizationProvider` | `ILocalizationProvider` | 本地化数据提供者 |

#### 主要方法

```csharp
/// <summary>
/// 获取当前语言下的多语言文本
/// </summary>
/// <param name="key">多语言key</param>
/// <param name="args">格式化参数</param>
/// <returns>本地化文本，如果未找到返回 [key]</returns>
public string GetLanguageText(string key, params object[] args)
```

#### 语言设置流程

```csharp
public ELanguage Language
{
    get => m_Language;
    set
    {
        if (value == ELanguage.Unspecified) throw new FuException("...");
        if (value == m_Language) return;
        
        var oldLanguage = m_Language;
        m_Language = value;

        // 1. 保存设置到本地存储
        m_DataSaveModule.SetString("Language", value.ToString());
        m_DataSaveModule.Save();

        // 2. 发送语言改变事件
        var args = LanguageChangeEventArgs.Create(oldLanguage, value);
        m_EventModule.Broadcast(this, args);
    }
}
```

#### 系统语言映射

```csharp
public static ELanguage SystemLanguage
{
    get
    {
        return Application.systemLanguage switch
        {
            UnityEngine.SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
            UnityEngine.SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
            UnityEngine.SystemLanguage.English            => ELanguage.English,
            UnityEngine.SystemLanguage.Japanese           => ELanguage.Japanese,
            UnityEngine.SystemLanguage.Korean             => ELanguage.Korean,
            // ... 40+ 种语言映射
            _                                             => ELanguage.Unspecified
        };
    }
}
```

### 4.2 ELanguage 枚举

完整的语言类型定义，共支持 50+ 种语言：

#### 主要语言

| 枚举值 | 语言 | 说明 |
|--------|------|------|
| `ChineseSimplified` | 简体中文 | 中国大陆 |
| `ChineseTraditional` | 繁体中文 | 中国台湾/香港 |
| `English` | 英语 | 国际通用 |
| `Japanese` | 日语 | 日本 |
| `Korean` | 韩语 | 韩国 |
| `French` | 法语 | 法国/加拿大 |
| `German` | 德语 | 德国 |
| `Spanish` | 西班牙语 | 西班牙/拉美 |
| `Russian` | 俄语 | 俄罗斯 |
| `PortugueseBrazil` | 巴西葡萄牙语 | 巴西 |
| `PortuguesePortugal` | 葡萄牙语 | 葡萄牙 |
| `Arabic` | 阿拉伯语 | 中东地区（RTL） |
| `Thai` | 泰语 | 泰国 |
| `Vietnamese` | 越南语 | 越南 |
| `Indonesian` | 印尼语 | 印尼 |

#### 欧洲语言

| 枚举值 | 语言 |
|--------|------|
| `Danish` | 丹麦语 |
| `Dutch` | 荷兰语 |
| `Finnish` | 芬兰语 |
| `Greek` | 希腊语 |
| `Hungarian` | 匈牙利语 |
| `Italian` | 意大利语 |
| `Norwegian` | 挪威语 |
| `Polish` | 波兰语 |
| `Romanian` | 罗马尼亚语 |
| `Swedish` | 瑞典语 |
| `Turkish` | 土耳其语 |
| `Ukrainian` | 乌克兰语 |
| `Czech` | 捷克语 |

### 4.3 ILocalizationProvider

本地化数据提供者接口，定义获取本地化字符串的契约：

```csharp
public interface ILocalizationProvider
{
    /// <summary>
    /// 获取本地化多语言接口
    /// </summary>
    /// <param name="key">多语言key</param>
    /// <param name="args">格式化参数（支持 string.Format）</param>
    /// <returns>本地化文本</returns>
    string GetLanguage(string key, params object[] args);
}
```

### 4.4 LanguageChangeEventArgs

语言改变事件参数类，继承自 `GameEventArgs`。

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 事件 ID（类全名） |
| `OldELanguage` | `ELanguage` | 切换前的语言 |
| `ELanguage` | `ELanguage` | 切换后的语言 |

#### 创建和释放

```csharp
// 创建事件参数（从引用池获取）
public static LanguageChangeEventArgs Create(ELanguage oldELanguage, ELanguage eLanguage)
{
    var args = ReferencePool.Acquire<LanguageChangeEventArgs>();
    args.OldELanguage = oldELanguage;
    args.ELanguage = eLanguage;
    return args;
}

// 清除事件参数（回收到引用池）
public override void Clear()
{
    OldELanguage = ELanguage.Unspecified;
    ELanguage = ELanguage.Unspecified;
}
```

### 4.5 LocalizationModuleInspector

LocalizationModule 的自定义 Inspector 编辑器。

#### 功能

- **显示当前语言**：在 Inspector 面板中实时显示当前设置的语言

```csharp
[CustomEditor(typeof(LocalizationModule))]
internal sealed class LocalizationModuleInspector : FuFrameworkInspector
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (target is not LocalizationModule module) return;
        
        // 显示当前语言
        EditorGUILayout.LabelField("当前语言：", module.Language.ToString());
    }
}
```

***

## 5. 使用示例

### 5.1 实现本地化提供者

```csharp
using System.Collections.Generic;

/// <summary>
/// 自定义本地化提供者实现
/// </summary>
public class CustomLocalizationProvider : ILocalizationProvider
{
    // 多语言数据字典
    private readonly Dictionary<ELanguage, Dictionary<string, string>> m_LanguageData;
    
    public CustomLocalizationProvider()
    {
        m_LanguageData = new Dictionary<ELanguage, Dictionary<string, string>>();
        LoadLanguageData();
    }
    
    private void LoadLanguageData()
    {
        // 加载简体中文
        m_LanguageData[ELanguage.ChineseSimplified] = new Dictionary<string, string>
        {
            ["welcome"] = "欢迎来到游戏世界！",
            ["player_name"] = "玩家：{0}",
            ["level_up"] = "恭喜！你升到了 {0} 级！",
            ["gold_count"] = "金币：{0:N0}",
        };
        
        // 加载英文
        m_LanguageData[ELanguage.English] = new Dictionary<string, string>
        {
            ["welcome"] = "Welcome to the game world!",
            ["player_name"] = "Player: {0}",
            ["level_up"] = "Congratulations! You reached level {0}!",
            ["gold_count"] = "Gold: {0:N0}",
        };
        
        // 加载日文
        m_LanguageData[ELanguage.Japanese] = new Dictionary<string, string>
        {
            ["welcome"] = "ゲーム世界へようこそ！",
            ["player_name"] = "プレイヤー：{0}",
            ["level_up"] = "おめでとう！レベル {0} に到達しました！",
            ["gold_count"] = "ゴールド：{0:N0}",
        };
    }
    
    public string GetLanguage(string key, params object[] args)
    {
        var currentLang = GlobalModule.LocalizationModule.Language;
        
        // 获取当前语言的数据字典
        if (!m_LanguageData.TryGetValue(currentLang, out var dict))
        {
            // 如果当前语言没有数据， fallback 到英文
            if (!m_LanguageData.TryGetValue(ELanguage.English, out dict))
            {
                return null;
            }
        }
        
        // 获取文本
        if (!dict.TryGetValue(key, out var text))
        {
            return null;
        }
        
        // 格式化参数
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}
```

### 5.2 初始化本地化模块

```csharp
using FuFramework.Localization.Runtime;

public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        var localizationModule = ModuleManager.GetModule<LocalizationModule>();
        
        // 设置本地化提供者
        localizationModule.LocalizationProvider = new CustomLocalizationProvider();
        
        // 如果没有设置过语言，使用系统语言
        if (localizationModule.Language == ELanguage.Unspecified)
        {
            var systemLang = LocalizationModule.SystemLanguage;
            localizationModule.Language = systemLang != ELanguage.Unspecified 
                ? systemLang 
                : ELanguage.English;
        }
    }
}
```

### 5.3 基本语言操作

```csharp
using FuFramework.Localization.Runtime;

public class LanguageController : MonoBehaviour
{
    private LocalizationModule m_LocalizationModule;
    
    private void Start()
    {
        m_LocalizationModule = ModuleManager.GetModule<LocalizationModule>();
        
        // 获取当前语言
        ELanguage currentLanguage = m_LocalizationModule.Language;
        Debug.Log($"当前语言：{currentLanguage}");
        
        // 获取系统语言
        ELanguage systemLanguage = LocalizationModule.SystemLanguage;
        Debug.Log($"系统语言：{systemLanguage}");
        
        // 设置语言（会自动保存并触发事件）
        m_LocalizationModule.Language = ELanguage.ChineseSimplified;
    }
}
```

### 5.4 获取本地化文本

```csharp
public class UIManager : MonoBehaviour
{
    [SerializeField] private Text welcomeText;
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text levelUpText;
    [SerializeField] private Text goldText;
    
    private LocalizationModule m_Localization;
    
    private void Start()
    {
        m_Localization = ModuleManager.GetModule<LocalizationModule>();
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // 获取无参数的本地化文本
        welcomeText.text = m_Localization.GetLanguageText("welcome");
        
        // 获取带参数的本地化文本
        playerNameText.text = m_Localization.GetLanguageText("player_name", "Player001");
        levelUpText.text = m_Localization.GetLanguageText("level_up", 10);
        goldText.text = m_Localization.GetLanguageText("gold_count", 1500000);
    }
}
```

### 5.5 语言切换事件监听

```csharp
using FuFramework.Localization.Runtime;
using FuFramework.Event.Runtime;

public class LanguageChangeHandler : MonoBehaviour
{
    private void Start()
    {
        // 订阅语言改变事件
        GlobalModule.EventModule.Subscribe<LanguageChangeEventArgs>(OnLanguageChanged);
    }
    
    private void OnLanguageChanged(object sender, GameEventArgs e)
    {
        if (e is LanguageChangeEventArgs args)
        {
            Debug.Log($"语言从 {args.OldELanguage} 改变为 {args.ELanguage}");
            
            // 更新所有UI文本
            UpdateAllUI();
            
            // 重新加载资源（如不同语言的图片、音频）
            ReloadLanguageAssets(args.ELanguage);
        }
    }
    
    private void UpdateAllUI()
    {
        // 获取所有需要更新的UI组件并刷新
        var allTextComponents = FindObjectsOfType<LocalizedText>();
        foreach (var text in allTextComponents)
        {
            text.UpdateText();
        }
    }
    
    private void ReloadLanguageAssets(ELanguage newLanguage)
    {
        // 根据新语言加载对应的资源
        var assetModule = GlobalModule.AssetModule;
        // ...
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        GlobalModule.EventModule.Unsubscribe<LanguageChangeEventArgs>(OnLanguageChanged);
    }
}
```

### 5.6 语言选择界面

```csharp
using System.Collections.Generic;
using UnityEngine.UI;
using FuFramework.Localization.Runtime;

public class LanguageSelectionUI : MonoBehaviour
{
    [SerializeField] private Dropdown languageDropdown;
    
    // 支持的语言列表
    [SerializeField] private List<ELanguage> availableLanguages = new()
    {
        ELanguage.ChineseSimplified,
        ELanguage.English,
        ELanguage.Japanese,
        ELanguage.Korean
    };
    
    private void Start()
    {
        InitializeDropdown();
        SetCurrentLanguageSelection();
        
        // 添加选择监听
        languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
    }
    
    private void InitializeDropdown()
    {
        languageDropdown.ClearOptions();
        
        var options = new List<Dropdown.OptionData>();
        foreach (var lang in availableLanguages)
        {
            options.Add(new Dropdown.OptionData(GetLanguageDisplayName(lang)));
        }
        
        languageDropdown.AddOptions(options);
    }
    
    private void SetCurrentLanguageSelection()
    {
        var currentLang = GlobalModule.LocalizationModule.Language;
        int currentIndex = availableLanguages.IndexOf(currentLang);
        if (currentIndex >= 0)
        {
            languageDropdown.value = currentIndex;
        }
    }
    
    private string GetLanguageDisplayName(ELanguage language)
    {
        return language switch
        {
            ELanguage.ChineseSimplified => "简体中文",
            ELanguage.ChineseTraditional => "繁體中文",
            ELanguage.English => "English",
            ELanguage.Japanese => "日本語",
            ELanguage.Korean => "한국어",
            ELanguage.French => "Français",
            ELanguage.German => "Deutsch",
            ELanguage.Spanish => "Español",
            ELanguage.Russian => "Русский",
            _ => language.ToString()
        };
    }
    
    private void OnLanguageSelected(int index)
    {
        if (index >= 0 && index < availableLanguages.Count)
        {
            GlobalModule.LocalizationModule.Language = availableLanguages[index];
        }
    }
}
```

### 5.7 自动语言检测

```csharp
public class AutoLanguageDetector : MonoBehaviour
{
    private void Start()
    {
        var localizationModule = ModuleManager.GetModule<LocalizationModule>();
        
        // 如果用户没有手动设置过语言
        if (localizationModule.Language == ELanguage.Unspecified)
        {
            // 获取系统语言
            ELanguage systemLang = LocalizationModule.SystemLanguage;
            
            // 如果系统语言在支持列表中
            if (systemLang != ELanguage.Unspecified)
            {
                localizationModule.Language = systemLang;
                Debug.Log($"自动设置语言为：{systemLang}");
            }
            else
            {
                // 默认使用英语
                localizationModule.Language = ELanguage.English;
                Debug.Log("系统语言不支持，默认使用英语");
            }
        }
    }
}
```

### 5.8 本地化文本组件

```csharp
using UnityEngine.UI;
using FuFramework.Localization.Runtime;

/// <summary>
/// 自动本地化的文本组件
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [SerializeField] private object[] formatArgs;
    
    private Text m_Text;
    private LocalizationModule m_Localization;
    
    private void Awake()
    {
        m_Text = GetComponent<Text>();
        m_Localization = ModuleManager.GetModule<LocalizationModule>();
    }
    
    private void Start()
    {
        UpdateText();
        
        // 订阅语言改变事件
        GlobalModule.EventModule.Subscribe<LanguageChangeEventArgs>(OnLanguageChanged);
    }
    
    private void OnLanguageChanged(object sender, GameEventArgs e)
    {
        UpdateText();
    }
    
    public void UpdateText()
    {
        if (m_Localization == null || string.IsNullOrEmpty(localizationKey))
            return;
            
        m_Text.text = m_Localization.GetLanguageText(localizationKey, formatArgs);
    }
    
    public void SetKey(string key, params object[] args)
    {
        localizationKey = key;
        formatArgs = args;
        UpdateText();
    }
    
    private void OnDestroy()
    {
        GlobalModule.EventModule.Unsubscribe<LanguageChangeEventArgs>(OnLanguageChanged);
    }
}
```

***

## 6. 目录结构

```
FuFramework/Localization/
├── README.md                              # 模块说明文档
├── Runtime/                               # 运行时代码
│   ├── FuFramework.Localization.Runtime.asmdef   # 程序集定义
│   ├── LocalizationModule.cs              # 本地化管理模块
│   ├── ELanguage.cs                       # 语言枚举定义
│   ├── ILocalizationProvider.cs           # 本地化提供者接口
│   └── Event/                             # 事件定义
│       └── LanguageChangeEventArgs.cs     # 语言改变事件参数
└── Editor/                                # 编辑器代码
    ├── FuFramework.Localization.Editor.asmdef    # 编辑器程序集定义
    └── Inspector/
        └── LocalizationModuleInspector.cs   # LocalizationModule Inspector
```

***

## 7. 依赖

- **FuFramework.Core**：基础框架模块
- **FuFramework.Event**：事件管理模块
- **FuFramework.DataSave**：数据保存模块
- **FuFramework.ReferencePool**：引用池模块
- **UnityEngine**：Unity 引擎

***

## 8. 最佳实践

### 8.1 本地化键管理

```csharp
// 推荐：使用常量类管理本地化键
public static class LocalizationKeys
{
    public const string WELCOME = "welcome";
    public const string PLAYER_NAME = "player_name";
    public const string LEVEL_UP = "level_up";
    public const string GOLD_COUNT = "gold_count";
    public const string SETTINGS_TITLE = "settings_title";
    public const string BUTTON_CONFIRM = "button_confirm";
    public const string BUTTON_CANCEL = "button_cancel";
}

// 使用
var text = localization.GetLanguageText(LocalizationKeys.WELCOME);
```

### 8.2 语言特定逻辑

```csharp
public class LanguageSpecificHandler : MonoBehaviour
{
    private void Start()
    {
        var lang = GlobalModule.LocalizationModule.Language;
        
        switch (lang)
        {
            case ELanguage.Arabic:
                // 阿拉伯语需要 RTL 支持
                EnableRTL();
                break;
                
            case ELanguage.ChineseSimplified:
            case ELanguage.ChineseTraditional:
                // 中文可能需要调整字体大小
                AdjustFontSizeForChinese();
                break;
                
            case ELanguage.Japanese:
            case ELanguage.Korean:
                // 日韩可能需要特殊字体
                UseCJKFont();
                break;
        }
    }
    
    private void EnableRTL()
    {
        // 启用从右到左布局
    }
}
```

### 8.3 多语言资源管理

```csharp
public class LanguageAssetManager
{
    /// <summary>
    /// 获取语言特定的资源路径
    /// </summary>
    public string GetLocalizedAssetPath(string basePath, ELanguage language)
    {
        var langSuffix = language switch
        {
            ELanguage.ChineseSimplified => "_CN",
            ELanguage.ChineseTraditional => "_TW",
            ELanguage.English => "_EN",
            ELanguage.Japanese => "_JP",
            ELanguage.Korean => "_KR",
            _ => "_EN"
        };
        
        return $"{basePath}{langSuffix}";
    }
}
```

***

## 9. 注意事项

1. **提供者设置**：使用 `GetLanguageText` 前必须先设置 `LocalizationProvider`
2. **语言回退**：建议实现语言回退机制（如当前语言无数据时 fallback 到英语）
3. **事件订阅**：语言改变事件处理函数中避免修改语言（防止循环）
4. **参数安全**：使用 `string.Format` 时注意参数数量和类型的匹配
5. **字体支持**：确保使用的字体支持目标语言的字符集（特别是中文、日文、阿拉伯文）
6. **RTL 支持**：阿拉伯语等从右到左语言需要特殊处理 UI 布局
7. **文本长度**：不同语言的文本长度差异很大，UI 设计要考虑弹性布局
8. **资源管理**：不同语言的图片、音频等资源要正确加载和释放
9. **持久化时机**：语言设置会自动保存，但游戏退出前确保调用 `DataSaveModule.Save()`
