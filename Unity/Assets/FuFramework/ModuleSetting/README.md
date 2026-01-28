# FuFramework ModuleSetting Module

## 简介
FuFramework ModuleSetting 模块是游戏框架的核心配置管理系统，采用单例模式统一管理所有游戏模块的配置信息。该模块提供了游戏基础设置、音频系统配置、资源系统配置、实体系统配置、数据存储配置、红点系统配置和引导系统配置等功能，是游戏框架的配置中枢。

## 核心特性

- **统一配置管理**：集中管理所有游戏模块的配置信息
- **单例模式**：确保全局唯一的配置实例
- **游戏基础设置**：帧率、游戏速度、后台运行、休眠控制
- **模块化配置**：支持音频、资源、实体、数据存储、红点、引导等模块的独立配置
- **编辑器集成**：提供可视化的配置界面和编辑器工具
- **运行时控制**：支持游戏暂停、恢复、重置等运行时操作

## 核心类说明

### ModuleSetting
模块配置管理器，继承自 `MonoSingleton<ModuleSetting>`。
- **职责**：
  1. 管理游戏基础设置（帧率、游戏速度、后台运行等）
  2. 提供各模块配置的访问接口
  3. 实现游戏暂停、恢复、重置功能
  4. 确保单例模式的正确初始化

### SoundSetting
音频系统配置，管理声音组和音频混音器设置。
- **职责**：
  1. 管理音频混音器引用
  2. 配置声音组信息（背景音乐、音效等）
  3. 提供声音组的快速查找和访问

### AssetSetting
资源系统配置，基于 YooAsset 的资源管理设置。
- **职责**：
  1. 配置资源运行模式（编辑器模拟、离线、联机等）
  2. 设置资源包名称和下载参数
  3. 管理异步系统性能参数

### EntitySetting
实体系统配置，管理实体组和实体实例设置。
- **职责**：
  1. 配置实体组信息
  2. 管理实体实例限制
  3. 提供实体组的快速访问

### DataSaveSetting
本地数据存储配置，管理数据保存和加密设置。
- **职责**：
  1. 配置自动保存参数
  2. 管理数据加密设置
  3. 设置保存间隔和加密密钥

### RedDotSetting
红点系统配置，管理红点节点数据结构。
- **职责**：
  1. 配置红点根节点
  2. 管理红点层级关系
  3. 支持复杂的红点树结构

### GuideSetting
引导系统配置，管理游戏引导流程设置。
- **职责**：
  1. 配置引导信息
  2. 管理引导步骤
  3. 支持复杂的引导流程

## 技术架构

### 依赖关系
- **FuFramework.Core**：基础框架模块（MonoSingleton）
- **Unity Engine**：基础运行环境
- **YooAsset**：资源管理系统（AssetSetting）
- **Unity.Audio**：音频系统支持

### 单例模式
ModuleSetting 采用单例模式，确保全局唯一的配置实例，通过 `MonoSingleton<ModuleSetting>` 实现。

## 使用指南

### 1. 基础配置设置
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class GameConfigController : MonoBehaviour
{
    private void Start()
    {
        // 获取模块配置管理器（单例）
        var moduleSetting = ModuleSetting.Instance;
        
        // 设置游戏帧率
        moduleSetting.FrameRate = 60;
        
        // 设置游戏速度
        moduleSetting.GameSpeed = 1.0f;
        
        // 允许后台运行
        moduleSetting.RunInBackground = true;
        
        // 禁止休眠
        moduleSetting.NeverSleep = true;
        
        // 检查游戏状态
        Debug.Log($"游戏是否暂停: {moduleSetting.IsGamePaused}");
        Debug.Log($"是否正常速度: {moduleSetting.IsNormalGameSpeed}");
        
        // 游戏控制操作
        moduleSetting.PauseGame();      // 暂停游戏
        moduleSetting.ResumeGame();     // 恢复游戏
        moduleSetting.ResetNormalGameSpeed(); // 重置为正常速度
    }
}
```

### 2. 音频系统配置使用
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class AudioConfigManager : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        var soundSetting = moduleSetting.SoundSetting;
        
        // 获取音频混音器
        var audioMixer = soundSetting.AudioMixer;
        if (audioMixer != null)
        {
            // 设置主音量
            audioMixer.SetFloat("MasterVolume", -20f); // -20dB
        }
        
        // 获取所有声音组
        var allGroups = soundSetting.AllGroups;
        foreach (var group in allGroups)
        {
            Debug.Log($"声音组: {group.Name}, 音量: {group.Volume}, 静音: {group.Mute}");
        }
        
        // 通过名称获取特定声音组
        var bgmGroup = soundSetting.GetGroup("BackgroundMusic");
        if (bgmGroup != null)
        {
            // 设置背景音乐音量
            bgmGroup.Volume = 0.8f;
            bgmGroup.Mute = false;
        }
        
        // 通过ID获取声音组
        var sfxGroup = soundSetting.GetGroupByID("sound_effects_group_id");
        if (sfxGroup != null)
        {
            // 设置音效参数
            sfxGroup.AgentCount = 10; // 最多同时播放10个音效
            sfxGroup.AllowBeReplacedBySamePriority = true;
        }
    }
}

// 自定义音频控制器
public class CustomAudioController : MonoBehaviour
{
    private SoundSetting soundSetting;
    
    private void Start()
    {
        soundSetting = ModuleSetting.Instance.SoundSetting;
        
        // 动态添加声音组
        var newGroup = new SoundGroupInfo("UI Sounds")
        {
            Volume = 0.7f,
            AgentCount = 5,
            AllowBeReplacedBySamePriority = false
        };
        
        // 注意：实际项目中需要通过编辑器添加，这里仅为示例
    }
    
    public void SetMasterVolume(float volume)
    {
        var audioMixer = soundSetting.AudioMixer;
        if (audioMixer != null)
        {
            // 将0-1的线性音量转换为dB
            float dB = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat("MasterVolume", dB);
        }
    }
}
```

### 3. 资源系统配置使用
```csharp
using FuFramework.ModuleSetting.Runtime;
using YooAsset;
using UnityEngine;

public class ResourceConfigManager : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        var assetSetting = moduleSetting.AssetSetting;
        
        // 获取资源运行模式
        var playMode = assetSetting.PlayMode;
        Debug.Log($"资源运行模式: {playMode}");
        
        // 根据运行模式执行不同的初始化逻辑
        switch (playMode)
        {
            case EPlayMode.EditorSimulateMode:
                Debug.Log("编辑器模拟模式 - 使用AssetDatabase加载资源");
                break;
            case EPlayMode.OfflinePlayMode:
                Debug.Log("离线模式 - 使用本地资源包");
                break;
            case EPlayMode.HostPlayMode:
                Debug.Log("联机模式 - 使用远程资源服务器");
                break;
            case EPlayMode.WebPlayMode:
                Debug.Log("Web模式 - 使用Web资源服务器");
                break;
        }
        
        // 获取资源包配置
        string packageName = assetSetting.DefaultPackageName;
        int maxDownloadNum = assetSetting.DownloadingMaxNum;
        int retryCount = assetSetting.FailedTryAgainNum;
        int asyncSliceTime = assetSetting.AsyncSystemMaxSlicePerFrame;
        
        Debug.Log($"资源包: {packageName}, 最大下载数: {maxDownloadNum}");
        Debug.Log($"重试次数: {retryCount}, 异步切片时间: {asyncSliceTime}ms");
        
        // 初始化YooAsset资源系统
        InitializeYooAsset(assetSetting);
    }
    
    private void InitializeYooAsset(AssetSetting setting)
    {
        // 创建资源包初始化参数
        var initParameters = new YooAssets.InitializeParameters
        {
            // 根据配置设置运行模式
            PlayMode = setting.PlayMode,
            
            // 设置异步系统参数
            AsyncSystemMaxSliceTimeMs = setting.AsyncSystemMaxSlicePerFrame,
            
            // 其他初始化参数...
        };
        
        // 初始化YooAsset
        YooAssets.Initialize(initParameters);
        
        // 创建默认资源包
        var package = YooAssets.CreatePackage(setting.DefaultPackageName);
        YooAssets.SetDefaultPackage(package);
    }
}
```

### 4. 实体系统配置使用
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class EntityConfigManager : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        var entitySetting = moduleSetting.EntitySetting;
        
        // 获取所有实体组
        var allGroups = entitySetting.AllGroups;
        Debug.Log($"实体组数量: {entitySetting.Count}");
        
        foreach (var group in allGroups)
        {
            Debug.Log($"实体组: {group.Name}, 实例限制: {group.InstanceLimit}");
            Debug.Log($"优先级: {group.Priority}, 自动释放: {group.AutoRelease}");
        }
        
        // 通过名称获取实体组
        var playerGroup = entitySetting.GetGroup("PlayerGroup");
        if (playerGroup != null)
        {
            // 配置玩家实体组
            playerGroup.InstanceLimit = 4; // 最多4个玩家实体
            playerGroup.Priority = 10;     // 高优先级
            playerGroup.AutoRelease = false; // 不自动释放
        }
        
        // 通过ID获取实体组
        var enemyGroup = entitySetting.GetGroupByID("enemy_group_001");
        if (enemyGroup != null)
        {
            // 配置敌人实体组
            enemyGroup.InstanceLimit = 50; // 最多50个敌人实体
            enemyGroup.Priority = 5;       // 中等优先级
            enemyGroup.AutoRelease = true; // 自动释放
        }
    }
}

// 实体组信息使用示例
public class EntitySpawner : MonoBehaviour
{
    [SerializeField] private string targetGroupName = "EnemyGroup";
    
    private EntitySetting entitySetting;
    
    private void Start()
    {
        entitySetting = ModuleSetting.Instance.EntitySetting;
    }
    
    public bool CanSpawnEntity()
    {
        var group = entitySetting.GetGroup(targetGroupName);
        if (group == null) return false;
        
        // 检查实体组是否达到实例限制
        // 这里需要结合实体管理器的实际实现
        return true;
    }
    
    public int GetAvailableInstanceCount()
    {
        var group = entitySetting.GetGroup(targetGroupName);
        if (group == null) return 0;
        
        // 计算可用的实体实例数量
        // 实际实现需要结合实体管理器的当前实例数
        return group.InstanceLimit - GetCurrentInstanceCount();
    }
    
    private int GetCurrentInstanceCount()
    {
        // 从实体管理器获取当前实例数
        return 0; // 示例返回值
    }
}
```

### 5. 数据存储配置使用
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class DataSaveConfigManager : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        var dataSaveSetting = moduleSetting.DataSaveSetting;
        
        // 获取数据存储配置
        bool autoSaveEnabled = dataSaveSetting.EnableAutoSave;
        float saveInterval = dataSaveSetting.AutoSaveInterval;
        bool encryptionEnabled = dataSaveSetting.EnableEncrypt;
        string encryptKey = dataSaveSetting.EncryptKey;
        
        Debug.Log($"自动保存: {autoSaveEnabled}, 间隔: {saveInterval}秒");
        Debug.Log($"数据加密: {encryptionEnabled}, 密钥: {encryptKey}");
        
        // 根据配置初始化数据保存系统
        InitializeDataSaveSystem(dataSaveSetting);
    }
    
    private void InitializeDataSaveSystem(DataSaveSetting setting)
    {
        // 获取数据保存管理器
        var dataSaveManager = GlobalModule.DataSaveModule;
        
        // 配置自动保存
        if (setting.EnableAutoSave)
        {
            // 设置自动保存定时器
            StartCoroutine(AutoSaveCoroutine(setting.AutoSaveInterval));
        }
        
        // 配置数据加密
        if (setting.EnableEncrypt)
        {
            // 设置加密密钥
            dataSaveManager.SetEncryptionKey(setting.EncryptKey);
        }
    }
    
    private System.Collections.IEnumerator AutoSaveCoroutine(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            
            // 执行自动保存
            GlobalModule.DataSaveModule.SaveAll();
            Debug.Log("自动保存完成");
        }
    }
}

// 自定义数据保存控制器
public class CustomDataSaveController : MonoBehaviour
{
    private DataSaveSetting dataSaveSetting;
    
    private void Start()
    {
        dataSaveSetting = ModuleSetting.Instance.DataSaveSetting;
        
        // 监听配置变化
        // 可以在运行时动态调整配置
    }
    
    public void ToggleAutoSave(bool enable)
    {
        // 注意：实际配置修改需要通过编辑器，这里仅为示例逻辑
        if (enable)
        {
            StartCoroutine(AutoSaveCoroutine(dataSaveSetting.AutoSaveInterval));
        }
        else
        {
            StopAllCoroutines();
        }
    }
    
    public void ChangeSaveInterval(float newInterval)
    {
        // 修改保存间隔的逻辑
        // 实际实现需要重新启动自动保存协程
    }
}
```

### 6. 红点系统配置使用
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class RedDotConfigManager : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        var redDotSetting = moduleSetting.RedDotSetting;
        
        // 获取红点根节点配置
        var rootNodes = redDotSetting.m_RootNodes;
        Debug.Log($"红点系统根节点数量: {rootNodes.Count}");
        
        // 遍历红点树结构
        TraverseRedDotTree(rootNodes, 0);
    }
    
    private void TraverseRedDotTree(List<RedDotNodeData> nodes, int depth)
    {
        foreach (var node in nodes)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}红点节点: {node.m_Key}");
            
            // 递归遍历子节点
            if (node.m_Children != null && node.m_Children.Count > 0)
            {
                TraverseRedDotTree(node.m_Children, depth + 1);
            }
        }
    }
}

// 红点系统集成示例
public class RedDotSystemIntegrator : MonoBehaviour
{
    private RedDotSetting redDotSetting;
    
    private void Start()
    {
        redDotSetting = ModuleSetting.Instance.RedDotSetting;
        
        // 根据配置初始化红点系统
        InitializeRedDotSystem();
    }
    
    private void InitializeRedDotSystem()
    {
        // 获取红点管理器
        var redDotManager = GlobalModule.RedDotModule;
        
        // 注册红点节点
        RegisterRedDotNodes(redDotSetting.m_RootNodes, null);
    }
    
    private void RegisterRedDotNodes(List<RedDotNodeData> nodes, string parentKey)
    {
        foreach (var node in nodes)
        {
            // 注册红点节点
            // redDotManager.RegisterNode(node.m_Key, parentKey);
            
            // 递归注册子节点
            if (node.m_Children != null && node.m_Children.Count > 0)
            {
                RegisterRedDotNodes(node.m_Children, node.m_Key);
            }
        }
    }
}
```

## 编辑器集成

### 1. 配置资源创建
ModuleSetting 模块提供了丰富的编辑器工具，可以通过 Unity 的 Create 菜单创建各种配置资源：

- **Create > FuFramework > Entity Settings**：创建实体系统配置
- **其他配置资源**：通过相应的 SettingCreator 类创建

### 2. 自定义编辑器界面
每个配置类都有对应的编辑器类，提供可视化的配置界面：

- **ModuleSettingInspector**：模块配置总览界面
- **SoundSettingEditor**：音频系统配置界面
- **AssetSettingEditor**：资源系统配置界面
- **EntitySettingEditor**：实体系统配置界面
- **DataSaveSettingEditor**：数据存储配置界面
- **RedDotSettingEditor**：红点系统配置界面
- **GuideSettingEditor**：引导系统配置界面

### 3. 配置验证和代码生成
编辑器工具还提供配置验证和代码生成功能：

- **配置验证**：检查配置的完整性和正确性
- **代码生成**：根据配置自动生成相关的代码文件
- **导航功能**：快速在配置树中导航

## 高级用法

### 1. 运行时配置动态调整
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class DynamicConfigAdjuster : MonoBehaviour
{
    private ModuleSetting moduleSetting;
    
    private void Start()
    {
        moduleSetting = ModuleSetting.Instance;
        
        // 根据设备性能动态调整配置
        AdjustSettingsBasedOnDevice();
    }
    
    private void AdjustSettingsBasedOnDevice()
    {
        // 根据设备性能调整帧率
        if (SystemInfo.systemMemorySize < 2000) // 低内存设备
        {
            moduleSetting.FrameRate = 30;
        }
        else if (SystemInfo.systemMemorySize < 4000) // 中等内存设备
        {
            moduleSetting.FrameRate = 45;
        }
        else // 高内存设备
        {
            moduleSetting.FrameRate = 60;
        }
        
        // 根据平台调整后台运行设置
        if (Application.platform == RuntimePlatform.Android || 
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            moduleSetting.RunInBackground = false; // 移动设备通常不允许后台运行
        }
    }
    
    // 响应系统事件调整配置
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 应用进入后台，暂停游戏
            moduleSetting.PauseGame();
        }
        else
        {
            // 应用回到前台，恢复游戏
            moduleSetting.ResumeGame();
        }
    }
}
```

### 2. 多环境配置管理
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class MultiEnvironmentConfigManager : MonoBehaviour
{
    [System.Serializable]
    public class EnvironmentConfig
    {
        public string environmentName;
        public int targetFrameRate;
        public float gameSpeed;
        public EPlayMode assetPlayMode;
    }
    
    [SerializeField] private EnvironmentConfig[] environmentConfigs;
    
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        
        // 根据当前环境应用配置
        string currentEnvironment = GetCurrentEnvironment();
        ApplyEnvironmentConfig(currentEnvironment, moduleSetting);
    }
    
    private string GetCurrentEnvironment()
    {
        // 根据编译符号或配置文件确定当前环境
        #if DEVELOPMENT_BUILD
            return "Development";
        #elif STAGING_BUILD
            return "Staging";
        #else
            return "Production";
        #endif
    }
    
    private void ApplyEnvironmentConfig(string environment, ModuleSetting setting)
    {
        var config = System.Array.Find(environmentConfigs, c => c.environmentName == environment);
        if (config != null)
        {
            setting.FrameRate = config.targetFrameRate;
            setting.GameSpeed = config.gameSpeed;
            setting.AssetSetting.PlayMode = config.assetPlayMode;
            
            Debug.Log($"已应用 {environment} 环境配置");
        }
    }
}
```

### 3. 配置热重载
```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class ConfigHotReloader : MonoBehaviour
{
    private ModuleSetting moduleSetting;
    private SoundSetting originalSoundSetting;
    
    private void Start()
    {
        moduleSetting = ModuleSetting.Instance;
        originalSoundSetting = moduleSetting.SoundSetting;
        
        // 监听配置文件变化（仅开发环境）
        #if UNITY_EDITOR
        StartCoroutine(MonitorConfigChanges());
        #endif
    }
    
    #if UNITY_EDITOR
    private System.Collections.IEnumerator MonitorConfigChanges()
    {
        var lastCheckTime = System.DateTime.Now;
        
        while (true)
        {
            yield return new WaitForSeconds(1f); // 每秒检查一次
            
            // 检查配置资源是否被修改
            if (UnityEditor.EditorUtility.IsDirty(moduleSetting.SoundSetting))
            {
                OnSoundConfigChanged();
            }
        }
    }
    
    private void OnSoundConfigChanged()
    {
        Debug.Log("音频配置已修改，应用新设置...");
        
        // 重新加载音频配置
        // 这里可以实现配置的热重载逻辑
    }
    #endif
}
```

## 性能优化建议

### 1. 配置初始化优化
- 在游戏启动时一次性加载所有配置
- 避免在运行时频繁访问配置资源
- 对常用配置进行缓存

### 2. 内存管理
- 及时释放不再使用的配置引用
- 避免配置资源的循环引用
- 使用 ScriptableObject 的引用计数管理

### 3. 运行时性能
- 减少配置验证的频率
- 对配置访问进行性能监控
- 使用字典加速配置查找

## 注意事项

### 1. 单例模式使用
- ModuleSetting 必须挂载到首个初始化场景的 GameObject 上
- 不要手动创建多个 ModuleSetting 实例
- 通过 `ModuleSetting.Instance` 访问单例实例

### 2. 配置资源管理
- 配置资源应该放在固定的目录中
- 避免在运行时修改配置资源的序列化数据
- 使用版本控制管理配置变更

### 3. 平台兼容性
- 不同平台的配置可能需要差异化处理
- 注意移动设备的性能限制
- 考虑不同分辨率和屏幕比例的适配

### 4. 安全性考虑
- 敏感配置（如加密密钥）应该进行保护
- 避免在客户端存储敏感服务器配置
- 使用适当的加密措施保护配置数据

## 依赖模块

- **FuFramework.Core**：基础框架模块（单例基类）
- **Unity Engine**：基础运行环境
- **YooAsset**：资源管理系统
- **Unity.Audio**：音频系统支持

## 技术支持

如遇到配置相关问题，请检查：
1. ModuleSetting 是否正确挂载到首个场景
2. 配置资源的路径和引用是否正确
3. 各模块的依赖关系是否满足
4. 平台特定的配置是否适配
5. 配置数据的序列化是否正确