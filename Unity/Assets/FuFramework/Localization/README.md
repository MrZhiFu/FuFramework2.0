# FuFramework Localization Module

## 简介
FuFramework Localization 模块是一个功能完善的游戏本地化系统。它提供了多语言支持、语言切换、自动系统语言检测等功能，能够满足全球化游戏的多语言需求。

## 核心特性

- **多语言支持**：支持超过40种语言，覆盖全球主要语言
- **自动语言检测**：自动检测系统语言并设置默认语言
- **持久化存储**：语言设置自动保存到本地存储
- **事件驱动**：提供语言改变事件通知机制
- **编辑器集成**：提供可视化的编辑器界面
- **模块化设计**：依赖事件和数据保存模块，架构清晰

## 核心类说明

### LocalizationManager
本地化管理器，继承自 `FuModule`。
- **职责**：
  1. 管理当前语言设置和切换
  2. 提供系统语言检测功能
  3. 处理语言设置的持久化存储
  4. 发送语言改变事件通知

### ELanguage
语言枚举类型，定义支持的所有语言。
- **职责**：
  1. 定义完整的语言类型枚举
  2. 提供语言名称和标识
  3. 支持与Unity SystemLanguage的映射

### LocalizationLanguageChangeEventArgs
语言改变事件参数类，继承自 `GameEventArgs`。
- **职责**：
  1. 封装语言改变事件的数据
  2. 提供旧语言和新语言信息
  3. 支持对象池管理

## 支持的语言列表

该模块支持以下语言类型：

| 语言代码 | 语言名称 | 备注 |
|---------|---------|------|
| Unspecified | 未指定 | 默认值 |
| Afrikaans | 南非荷兰语 | |
| Albanian | 阿尔巴尼亚语 | |
| Arabic | 阿拉伯语 | |
| Basque | 巴斯克语 | |
| Belarusian | 白俄罗斯语 | |
| Bulgarian | 保加利亚语 | |
| Catalan | 加泰罗尼亚语 | |
| ChineseSimplified | 简体中文 | |
| ChineseTraditional | 繁体中文 | |
| Croatian | 克罗地亚语 | |
| Czech | 捷克语 | |
| Danish | 丹麦语 | |
| Dutch | 荷兰语 | |
| English | 英语 | |
| Estonian | 爱沙尼亚语 | |
| Faroese | 法罗语 | |
| Finnish | 芬兰语 | |
| French | 法语 | |
| Georgian | 格鲁吉亚语 | |
| German | 德语 | |
| Greek | 希腊语 | |
| Hebrew | 希伯来语 | |
| Hungarian | 匈牙利语 | |
| Icelandic | 冰岛语 | |
| Indonesian | 印尼语 | |
| Italian | 意大利语 | |
| Japanese | 日语 | |
| Korean | 韩语 | |
| Latvian | 拉脱维亚语 | |
| Lithuanian | 立陶宛语 | |
| Macedonian | 马其顿语 | |
| Malayalam | 马拉雅拉姆语 | |
| Norwegian | 挪威语 | |
| Persian | 波斯语 | |
| Polish | 波兰语 | |
| PortugueseBrazil | 巴西葡萄牙语 | |
| PortuguesePortugal | 葡萄牙语 | |
| Romanian | 罗马尼亚语 | |
| Russian | 俄语 | |
| SerboCroatian | 塞尔维亚克罗地亚语 | |
| SerbianCyrillic | 塞尔维亚西里尔语 | |
| SerbianLatin | 塞尔维亚拉丁语 | |
| Slovak | 斯洛伐克语 | |
| Slovenian | 斯洛文尼亚语 | |
| Spanish | 西班牙语 | |
| Swedish | 瑞典语 | |
| Thai | 泰语 | |
| Turkish | 土耳其语 | |
| Ukrainian | 乌克兰语 | |
| Vietnamese | 越南语 | |

## 使用指南

### 1. 基本语言设置
```csharp
using FuFramework.Localization.Runtime;

public class GameLanguageController : MonoBehaviour
{
    private void Start()
    {
        // 获取本地化管理器
        var localizationManager = GlobalModule.LocalizationModule;
        
        // 获取当前语言
        ELanguage currentLanguage = localizationManager.Language;
        Debug.Log($"当前语言：{currentLanguage}");
        
        // 获取系统语言
        ELanguage systemLanguage = localizationManager.SystemELanguage;
        Debug.Log($"系统语言：{systemLanguage}");
        
        // 设置语言
        localizationManager.Language = ELanguage.ChineseSimplified;
    }
}
```

### 2. 语言切换事件监听
```csharp
using FuFramework.Localization.Runtime;
using FuFramework.Event.Runtime;

public class LanguageChangeHandler : MonoBehaviour
{
    private EventManager m_EventManager;
    
    private void Start()
    {
        m_EventManager = GlobalModule.EventModule;
        
        // 订阅语言改变事件
        m_EventManager.Subscribe<LocalizationLanguageChangeEventArgs>(
            LocalizationLanguageChangeEventArgs.EventId, 
            OnLanguageChanged);
    }
    
    private void OnLanguageChanged(object sender, GameEventArgs e)
    {
        if (e is LocalizationLanguageChangeEventArgs args)
        {
            Debug.Log($"语言从 {args.OldELanguage} 改变为 {args.ELanguage}");
            
            // 更新UI显示
            UpdateUIForLanguage(args.ELanguage);
        }
    }
    
    private void UpdateUIForLanguage(ELanguage language)
    {
        // 根据语言更新UI文本
        switch (language)
        {
            case ELanguage.ChineseSimplified:
                // 更新为简体中文
                break;
            case ELanguage.English:
                // 更新为英文
                break;
            case ELanguage.Japanese:
                // 更新为日文
                break;
            // 其他语言处理...
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (m_EventManager != null)
        {
            m_EventManager.Unsubscribe<LocalizationLanguageChangeEventArgs>(
                LocalizationLanguageChangeEventArgs.EventId, 
                OnLanguageChanged);
        }
    }
}
```

### 3. 多语言文本管理
```csharp
using System.Collections.Generic;
using UnityEngine;

public class LocalizedTextManager : MonoBehaviour
{
    [System.Serializable]
    public class LocalizedText
    {
        public ELanguage language;
        public string text;
    }
    
    [SerializeField] private List<LocalizedText> localizedTexts = new();
    
    public string GetLocalizedText(ELanguage language, string defaultText = "")
    {
        var textEntry = localizedTexts.Find(t => t.language == language);
        return textEntry != null ? textEntry.text : defaultText;
    }
    
    public void AddLocalizedText(ELanguage language, string text)
    {
        var existingEntry = localizedTexts.Find(t => t.language == language);
        if (existingEntry != null)
        {
            existingEntry.text = text;
        }
        else
        {
            localizedTexts.Add(new LocalizedText { language = language, text = text });
        }
    }
}

// 使用示例
public class GameTextController : MonoBehaviour
{
    [SerializeField] private LocalizedTextManager textManager;
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    
    private void Start()
    {
        var localizationManager = GlobalModule.LocalizationModule;
        UpdateTextForCurrentLanguage(localizationManager.Language);
    }
    
    private void UpdateTextForCurrentLanguage(ELanguage language)
    {
        string title = textManager.GetLocalizedText(language, "Default Title");
        titleText.text = title;
    }
}
```

### 4. 通过 GlobalModule 访问本地化模块
```csharp
// 获取当前语言
ELanguage currentLang = GlobalModule.LocalizationModule.Language;

// 设置语言
GlobalModule.LocalizationModule.Language = ELanguage.English;

// 获取系统语言
ELanguage systemLang = GlobalModule.LocalizationModule.SystemELanguage;

// 检查是否为特定语言
bool isChinese = GlobalModule.LocalizationModule.Language == ELanguage.ChineseSimplified;
```

## 高级用法

### 1. 自动语言检测和设置
```csharp
public class AutoLanguageDetector : MonoBehaviour
{
    private void Start()
    {
        var localizationManager = GlobalModule.LocalizationModule;
        
        // 如果用户没有手动设置过语言，则使用系统语言
        if (localizationManager.Language == ELanguage.Unspecified)
        {
            // 根据系统语言设置默认语言
            ELanguage systemLang = localizationManager.SystemELanguage;
            
            // 如果系统语言不在支持列表中，使用英语作为默认
            if (systemLang == ELanguage.Unspecified)
            {
                localizationManager.Language = ELanguage.English;
            }
            else
            {
                localizationManager.Language = systemLang;
            }
        }
    }
}
```

### 2. 语言切换界面
```csharp
using System.Collections.Generic;
using UnityEngine.UI;

public class LanguageSelectionUI : MonoBehaviour
{
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private List<ELanguage> availableLanguages = new()
    {
        ELanguage.ChineseSimplified,
        ELanguage.English,
        ELanguage.Japanese,
        ELanguage.Korean
    };
    
    private void Start()
    {
        // 初始化下拉菜单选项
        InitializeDropdown();
        
        // 设置当前选中项
        var currentLang = GlobalModule.LocalizationModule.Language;
        int currentIndex = availableLanguages.IndexOf(currentLang);
        if (currentIndex >= 0)
        {
            languageDropdown.value = currentIndex;
        }
        
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
    
    private string GetLanguageDisplayName(ELanguage language)
    {
        return language switch
        {
            ELanguage.ChineseSimplified => "简体中文",
            ELanguage.English => "English",
            ELanguage.Japanese => "日本語",
            ELanguage.Korean => "한국어",
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

### 3. 多语言资源管理
```csharp
using UnityEngine;

public class LocalizedAssetManager : MonoBehaviour
{
    [System.Serializable]
    public class LocalizedAsset
    {
        public ELanguage language;
        public Sprite sprite;
        public AudioClip audioClip;
        public GameObject prefab;
    }
    
    [SerializeField] private List<LocalizedAsset> localizedAssets = new();
    
    public Sprite GetLocalizedSprite(ELanguage language)
    {
        return localizedAssets.Find(a => a.language == language)?.sprite;
    }
    
    public AudioClip GetLocalizedAudio(ELanguage language)
    {
        return localizedAssets.Find(a => a.language == language)?.audioClip;
    }
    
    public void UpdateUIForLanguage(ELanguage language)
    {
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            var sprite = GetLocalizedSprite(language);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }
        
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            var audioClip = GetLocalizedAudio(language);
            if (audioClip != null)
            {
                audioSource.clip = audioClip;
            }
        }
    }
}
```

### 4. 条件编译和语言特定逻辑
```csharp
public class LanguageSpecificLogic : MonoBehaviour
{
    private void Start()
    {
        var localizationManager = GlobalModule.LocalizationModule;
        
        // 根据语言执行特定逻辑
        switch (localizationManager.Language)
        {
            case ELanguage.ChineseSimplified:
                // 中文特定逻辑
                AdjustUIForChinese();
                break;
                
            case ELanguage.Arabic:
                // 阿拉伯语特定逻辑（RTL支持）
                EnableRTLSupport();
                break;
                
            case ELanguage.Japanese:
                // 日语特定逻辑
                AdjustFontForJapanese();
                break;
                
            default:
                // 默认逻辑
                break;
        }
    }
    
    private void AdjustUIForChinese()
    {
        // 调整UI布局以适应中文文本长度
    }
    
    private void EnableRTLSupport()
    {
        // 启用从右到左的文本支持
    }
    
    private void AdjustFontForJapanese()
    {
        // 调整字体以更好地显示日文字符
    }
}
```

## 编辑器集成

### LocalizationManager Inspector
在Unity编辑器中，LocalizationManager组件提供了自定义的Inspector界面：

1. **显示当前语言**：在Inspector中显示当前设置的语言
2. **实时预览**：可以在编辑模式下预览语言设置效果

### 使用方法
1. 在场景中添加LocalizationManager组件
2. 在Inspector中查看当前语言状态
3. 通过代码设置语言，Inspector会实时更新显示

## 性能优化建议

1. **事件管理**：及时取消订阅语言改变事件，避免内存泄漏
2. **资源管理**：按需加载语言特定的资源，避免一次性加载所有语言资源
3. **缓存机制**：对频繁访问的本地化文本进行缓存
4. **异步加载**：对于大量本地化资源，使用异步加载避免卡顿
5. **内存优化**：及时释放不再使用的语言资源

## 注意事项

- **语言映射**：确保系统语言到ELanguage的映射正确
- **事件处理**：语言改变事件的处理要确保线程安全
- **资源管理**：不同语言的资源要正确管理和释放
- **文本长度**：不同语言的文本长度差异要考虑UI布局
- **字体支持**：确保使用的字体支持目标语言的字符集
- **RTL支持**：对于阿拉伯语等从右到左的语言，需要特殊处理

## 依赖模块

- **FuFramework.Core**：基础框架模块
- **FuFramework.Event**：事件管理模块
- **FuFramework.SaveData**：数据保存模块
- **Unity引擎**：基础运行环境

## 技术支持

如遇到本地化问题，请检查：
1. 语言设置是否正确保存和加载
2. 事件订阅是否正确设置和清理
3. 系统语言检测是否正常工作
4. 目标语言是否在支持列表中
5. 资源路径和命名是否正确