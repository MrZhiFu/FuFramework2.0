# 1. FuFramework ModuleSetting Module

## 1. 简介

FuFramework ModuleSetting 模块是游戏框架的核心配置管理系统，采用 **单例模式** 统一管理所有游戏模块的配置信息。该模块提供了游戏基础设置、音频系统配置、资源系统配置、实体系统配置、数据存储配置、红点系统配置和引导系统配置等功能，是游戏框架的配置中枢。

## 2. 核心特性

- **统一配置管理**：集中管理所有游戏模块的配置信息
- **单例模式**：确保全局唯一的配置实例
- **游戏基础设置**：帧率、游戏速度、后台运行、休眠控制
- **模块化配置**：支持音频、资源、实体、数据存储、红点、引导等模块的独立配置
- **编辑器集成**：提供可视化的配置界面和编辑器工具
- **运行时控制**：支持游戏暂停、恢复、重置等运行时操作

## 3. 核心概念

### 3.1 配置层级结构

```
ModuleSetting (单例配置管理器)
    ├── SoundSetting (音频系统配置)
    ├── AssetSetting (资源系统配置)
    ├── EntitySetting (实体系统配置)
    ├── DataSaveSetting (数据存储配置)
    ├── RedDotSetting (红点系统配置)
    └── GuideSetting (引导系统配置)
```

### 3.2 配置数据类层级

```
ScriptableObject
    ├── SoundSetting → SoundGroupInfo[]
    ├── AssetSetting
    ├── EntitySetting → EntityGroupInfo[]
    ├── DataSaveSetting
    ├── RedDotSetting → RedDotNodeData[]
    └── GuideSetting → GuideInfo[] → StepInfo[]
```

## 4. 核心类详细说明

### 4.1 ModuleSetting

模块配置管理器，继承自 `MonoSingleton<ModuleSetting>`，是框架配置的核心入口。

**职责：**
1. 管理游戏基础设置（帧率、游戏速度、后台运行等）
2. 提供各模块配置的访问接口
3. 实现游戏暂停、恢复、重置功能
4. 确保单例模式的正确初始化

**核心属性：**

```csharp
// 游戏基础设置
public int FrameRate { get; set; }                    // 游戏帧率 (1-120)
public float GameSpeed { get; set; }                  // 游戏速度 (0-8)
public bool IsGamePaused { get; }                     // 游戏是否暂停
public bool IsNormalGameSpeed { get; }                // 是否为正常速度 (1x)
public bool RunInBackground { get; set; }             // 是否允许后台运行
public bool NeverSleep { get; set; }                  // 是否禁止休眠
public bool OpenGuide { get; set; }                   // 是否开启引导

// 模块配置访问
public SoundSetting SoundSetting { get; }             // 音频系统配置
public AssetSetting AssetSetting { get; }             // 资源系统配置
public EntitySetting EntitySetting { get; }           // 实体系统配置
public DataSaveSetting DataSaveSetting { get; }       // 数据存储配置
public RedDotSetting RedDotSetting { get; }           // 红点系统配置
public GuideSetting GuideSetting { get; }             // 引导系统配置
```

**核心方法：**

```csharp
// 游戏控制
public void PauseGame()                               // 暂停游戏 (GameSpeed = 0)
public void ResumeGame()                              // 恢复游戏 (恢复到暂停前速度)
public void ResetNormalGameSpeed()                    // 重置为正常速度 (1x)

// 生命周期
protected override void OnInit()                      // 初始化时应用所有设置
```

**实现细节：**
- 使用 `MonoSingleton` 确保全局唯一实例
- 配置通过 Unity Inspector 进行可视化编辑
- 运行时修改属性会立即生效（如修改 FrameRate 会立即设置 Application.targetFrameRate）
- 游戏暂停时会保存当前速度，恢复时还原

### 4.2 SoundSetting

音频系统配置，管理声音组和音频混音器设置。

**职责：**
1. 管理音频混音器引用
2. 配置声音组信息（背景音乐、音效等）
3. 提供声音组的快速查找和访问

**核心属性：**

```csharp
public AudioMixer AudioMixer { get; }                 // 音频混音器
public IReadOnlyList<SoundGroupInfo> AllGroups { get; } // 所有声音组
public int Count { get; }                             // 声音组数量
public SoundGroupInfo this[string groupName] { get; } // 通过名称索引
public SoundGroupInfo this[int index] { get; }        // 通过索引访问
```

**核心方法：**

```csharp
public SoundGroupInfo GetGroup(string groupName)      // 通过名称获取声音组
public SoundGroupInfo GetGroupByID(string groupID)    // 通过ID获取声音组
public void AddGroup(SoundGroupInfo groupInfo)        // 添加声音组
public void AddDefaultSoundGroups()                   // 添加默认声音组 (BGM, SFX, UI)
public SoundGroupInfo CreateNewSoundGroup(string groupName) // 创建新声音组
public void RemoveGroup(SoundGroupInfo groupInfo)     // 移除声音组
public void RemoveGroup(string groupName)             // 通过名称移除
public void RemoveGroupAt(int index)                  // 通过索引移除
public void ClearGroups()                             // 清空所有声音组
public bool ContainsGroup(string groupName)           // 检查是否包含
public List<string> GetAllGroupNames()                // 获取所有名称
```

**实现细节：**
- 使用字典缓存实现 O(1) 的声音组查找
- 支持运行时动态添加/移除声音组
- 提供默认声音组快速创建（BGM、SFX、UI）
- 自动处理名称唯一性

### 4.3 SoundGroupInfo

声音组信息，定义单个声音组的配置参数。

**核心属性：**

```csharp
public string GroupID { get; }                        // 唯一标识符 (GUID)
public string Name { get; set; }                      // 声音组名称
public bool Mute { get; set; }                        // 是否静音
public float Volume { get; set; }                     // 音量大小 (0-1)
public int AgentCount { get; set; }                   // 播放代理数量
public bool AllowBeReplacedBySamePriority { get; set; } // 是否允许被同优先级替换
```

**实现细节：**
- GroupID 在构造时自动生成 GUID
- Volume 自动限制在 0-1 范围
- AgentCount 自动限制最小值为 1

### 4.4 AssetSetting

资源系统配置，基于 YooAsset 的资源管理设置。

**职责：**
1. 配置资源运行模式（编辑器模拟、离线、联机等）
2. 设置资源包名称和下载参数
3. 管理异步系统性能参数

**核心属性：**

```csharp
public EPlayMode PlayMode { get; }                    // 资源运行模式
public string DefaultPackageName { get; }             // 默认资源包名称
public int DownloadingMaxNum { get; }                 // 最大并发下载数
public int FailedTryAgainNum { get; }                 // 失败重试次数
public int AsyncSystemMaxSlicePerFrame { get; }       // 异步系统每帧最大时间切片(ms)
```

**资源运行模式：**

```csharp
public enum EPlayMode
{
    EditorSimulateMode,   // 编辑器模拟模式 - 使用 AssetDatabase
    OfflinePlayMode,      // 离线模式 - 使用本地资源包
    HostPlayMode,         // 联机模式 - 从远程服务器下载
    WebPlayMode           // Web模式 - 使用 WebGL 资源服务器
}
```

**核心方法：**

```csharp
public void Reset()                                   // 重置为默认配置
```

### 4.5 EntitySetting

实体系统配置，管理实体组和实体实例设置。

**职责：**
1. 配置实体组信息
2. 管理实体实例限制
3. 提供实体组的快速访问

**核心属性：**

```csharp
public IReadOnlyList<EntityGroupInfo> AllGroups { get; } // 所有实体组
public int Count { get; }                             // 实体组数量
public EntityGroupInfo this[string groupName] { get; } // 通过名称索引
public EntityGroupInfo this[int index] { get; }       // 通过索引访问
```

**核心方法：**

```csharp
public EntityGroupInfo GetGroup(string groupName)     // 通过名称获取
public EntityGroupInfo GetGroupByID(string groupID)   // 通过ID获取
public void AddGroup(EntityGroupInfo groupInfo)       // 添加实体组
public void AddDefaultEntityGroups()                  // 添加默认组 (Player, Enemy, NPC, Prop, Effect)
public EntityGroupInfo CreateNewEntityGroup(string groupName) // 创建新组
public void RemoveGroup(EntityGroupInfo groupInfo)    // 移除实体组
public void RemoveGroup(string groupName)             // 通过名称移除
public void RemoveGroupAt(int index)                  // 通过索引移除
public void ClearGroups()                             // 清空所有组
public bool ContainsGroup(string groupName)           // 检查是否包含
public List<string> GetAllGroupNames()                // 获取所有名称
public void SetAllGroupsInstanceCapacity(int capacity) // 设置所有组容量
public void SetAllGroupsInstanceExpireTime(float expireTime) // 设置过期时间
public void SetAllGroupsAutoReleaseInterval(float interval) // 设置自动释放间隔
```

### 4.6 EntityGroupInfo

实体组信息，定义单个实体组的配置参数。

**核心属性：**

```csharp
public string GroupID { get; }                        // 唯一标识符 (GUID)
public string Name { get; set; }                      // 实体组名称
public float InstanceAutoReleaseInterval { get; set; } // 自动释放间隔 (秒)
public int InstanceCapacity { get; set; }             // 对象池容量
public float InstanceExpireTime { get; set; }         // 实例过期时间 (秒)
public int InstancePriority { get; set; }             // 实例优先级
```

### 4.7 DataSaveSetting

本地数据存储配置，管理数据保存和加密设置。

**职责：**
1. 配置自动保存参数
2. 管理数据加密设置
3. 设置保存间隔和加密密钥

**核心属性：**

```csharp
public bool EnableAutoSave { get; }                   // 是否启用自动保存
public float AutoSaveInterval { get; }                // 自动保存间隔 (秒, 默认5分钟)
public bool EnableEncrypt { get; }                    // 是否启用加密
public string EncryptKey { get; }                     // 加密密钥
```

**核心方法：**

```csharp
public void Reset()                                   // 重置为默认配置
```

### 4.8 RedDotSetting

红点系统配置，管理红点节点数据结构。

**职责：**
1. 配置红点根节点
2. 管理红点层级关系
3. 支持复杂的红点树结构

**核心属性：**

```csharp
public List<RedDotNodeData> m_RootNodes { get; set; } // 根节点列表
```

### 4.9 RedDotNodeData

红点节点数据，定义红点树的节点结构。

**核心属性：**

```csharp
public string m_Key { get; set; }                     // 红点Key
public List<RedDotNodeData> m_Children { get; set; }  // 子节点列表
```

**实现细节：**
- 使用 `[SerializeReference]` 支持多态序列化
- 支持无限层级的树形结构
- 每个节点可以有多个子节点

### 4.10 GuideSetting

引导系统配置，管理游戏引导流程设置。

**职责：**
1. 配置引导信息
2. 管理引导步骤
3. 支持复杂的引导流程

**核心属性：**

```csharp
public IReadOnlyList<GuideInfo> AllGuides { get; }    // 所有引导
public int GuideCount { get; }                        // 引导数量
public int TotalStepCount { get; }                    // 总步骤数量
public GuideInfo this[string guideId] { get; }        // 通过ID索引
public GuideInfo this[int index] { get; }             // 通过索引访问
```

**核心方法：**

```csharp
// 获取方法
public GuideInfo GetGuide(string guideId)             // 通过ID获取引导
public GuideInfo GetGuideByName(string guideName)     // 通过名称获取引导
public StepInfo GetStep(string stepId)                // 通过ID获取步骤
public List<StepInfo> GetStepsInGuide(string guideId) // 获取指定引导的所有步骤
public List<StepInfo> GetAllSteps()                   // 获取所有步骤
public bool ContainsGuide(string guideId)             // 检查是否包含引导
public bool ContainsStep(string stepId)               // 检查是否包含步骤

// 设置方法
public void AddGuide(GuideInfo guideInfo)             // 添加引导
public GuideInfo CreateGuide(string guideName)        // 创建新引导
public void RemoveGuide(string guideId)               // 移除引导
public StepInfo CreateStep(string guideId, string stepName, StepType stepType) // 创建步骤
public void RemoveStep(string stepId)                 // 移除步骤
public StepInfo AddStepToGuide(string guideId, StepInfo stepInfo) // 添加步骤到引导
public void ClearAll()                                // 清空所有引导

// 验证方法
public bool Validate(out List<string> errors)         // 验证配置有效性
```

### 4.11 GuideInfo

引导信息，定义单个引导的数据结构。

**核心属性：**

```csharp
public string m_GuideId { get; set; }                 // 引导ID
public string m_GuideName { get; set; }               // 引导名称
public string m_StartStepId { get; set; }             // 开始步骤ID
public List<StepInfo> m_Steps { get; set; }           // 步骤列表
```

### 4.12 StepInfo

步骤信息，定义单个引导步骤的数据结构。

**核心属性：**

```csharp
public string m_StepId { get; set; }                  // 步骤ID
public StepType m_StepType { get; set; }              // 步骤类型
public string m_NextStepId { get; set; }              // 下一个步骤ID
public bool m_IsCanJump { get; set; }                 // 是否可以跳过
public string m_TargetWindow { get; set; }            // 目标窗口 (ClickUI类型)
public string m_TargetUI { get; set; }                // 目标UI (ClickUI类型)
public string m_DialogContent { get; set; }           // 对话内容 (Dialog类型)
public float m_WaitTime { get; set; }                 // 等待时间 (Wait类型)
```

**步骤类型：**

```csharp
public enum StepType
{
    None,       // 无类型
    ClickUI,    // 点击UI引导
    Dialog,     // 对话引导
    Wait        // 等待步骤
}
```

## 5. 使用示例

### 5.1 基础配置设置

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
        moduleSetting.PauseGame();              // 暂停游戏
        moduleSetting.ResumeGame();             // 恢复游戏
        moduleSetting.ResetNormalGameSpeed();   // 重置为正常速度
    }
}
```

### 5.2 音频系统配置使用

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
            audioMixer.SetFloat("MasterVolume", -20f); // -20dB
        }
        
        // 获取所有声音组
        var allGroups = soundSetting.AllGroups;
        foreach (var group in allGroups)
        {
            Debug.Log($"声音组: {group.Name}, 音量: {group.Volume}, 静音: {group.Mute}");
        }
        
        // 通过名称获取特定声音组
        var bgmGroup = soundSetting.GetGroup("BGM");
        if (bgmGroup != null)
        {
            bgmGroup.Volume = 0.8f;
            bgmGroup.Mute = false;
        }
    }
}
```

### 5.3 资源系统配置使用

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
        
        Debug.Log($"资源包: {packageName}, 最大下载数: {maxDownloadNum}");
        Debug.Log($"重试次数: {retryCount}");
    }
}
```

### 5.4 实体系统配置使用

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
            Debug.Log($"实体组: {group.Name}, 容量: {group.InstanceCapacity}");
            Debug.Log($"优先级: {group.InstancePriority}, 过期时间: {group.InstanceExpireTime}s");
        }
        
        // 通过名称获取实体组
        var playerGroup = entitySetting.GetGroup("Player");
        if (playerGroup != null)
        {
            playerGroup.InstanceCapacity = 4;     // 最多4个玩家实体
            playerGroup.InstancePriority = 10;    // 高优先级
        }
    }
}
```

### 5.5 数据存储配置使用

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
        Debug.Log($"数据加密: {encryptionEnabled}");
    }
}
```

### 5.6 红点系统配置使用

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
            
            if (node.m_Children != null && node.m_Children.Count > 0)
            {
                TraverseRedDotTree(node.m_Children, depth + 1);
            }
        }
    }
}
```

### 5.7 引导系统配置使用

```csharp
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

public class GuideConfigManager : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        var guideSetting = moduleSetting.GuideSetting;
        
        // 获取所有引导
        var allGuides = guideSetting.AllGuides;
        Debug.Log($"引导数量: {guideSetting.GuideCount}");
        Debug.Log($"总步骤数量: {guideSetting.TotalStepCount}");
        
        // 遍历所有引导
        foreach (var guide in allGuides)
        {
            Debug.Log($"引导: {guide.m_GuideName}, ID: {guide.m_GuideId}");
            Debug.Log($"开始步骤: {guide.m_StartStepId}");
            
            // 获取该引导的所有步骤
            var steps = guideSetting.GetStepsInGuide(guide.m_GuideId);
            foreach (var step in steps)
            {
                Debug.Log($"  步骤: {step.m_StepId}, 类型: {step.m_StepType}");
            }
        }
        
        // 验证配置
        if (guideSetting.Validate(out var errors))
        {
            Debug.Log("引导配置验证通过");
        }
        else
        {
            foreach (var error in errors)
            {
                Debug.LogError($"配置错误: {error}");
            }
        }
    }
}
```

## 6. 编辑器功能

### 6.1 ModuleSettingInspector

模块配置可视化编辑器，继承自 `FuFrameworkInspector`。

**功能：**
- 帧率设置滑块 (1-120)
- 游戏速度设置滑块 + 快捷选择按钮 (0x, 0.01x, 0.1x, 0.25x, 0.5x, 1x, 1.5x, 2x, 4x, 8x)
- 后台运行开关
- 禁止休眠开关
- 引导开关
- 各模块配置字段显示

**运行时编辑支持：**
- 在 Play 模式下修改配置会立即生效
- 非 Play 模式下修改会保存到序列化属性

### 6.2 各模块编辑器

每个配置类都有对应的编辑器类：

- **SoundSettingEditor**：音频系统配置编辑器
- **AssetSettingEditor**：资源系统配置编辑器
- **EntitySettingEditor**：实体系统配置编辑器
- **DataSaveSettingEditor**：数据存储配置编辑器
- **RedDotSettingEditor**：红点系统配置编辑器（含代码生成）
- **GuideSettingEditor**：引导系统配置编辑器

### 6.3 配置创建器

通过编辑器菜单创建配置资源：

```csharp
// Create > FuFramework > Entity Settings
[CreateAssetMenu(fileName = "EntitySettings", menuName = "FuFramework/Entity Settings")]

// Create > FuFramework > Guide Settings
[CreateAssetMenu(fileName = "GuideSettings", menuName = "FuFramework/Guide Settings")]
```

## 7. 目录结构

```
FuFramework/
└── ModuleSetting/
    ├── README.md
    ├── Runtime/
    │   ├── ModuleSetting.cs              # 核心配置管理器
    │   ├── Sound/
    │   │   ├── SoundSetting.cs           # 音频系统配置
    │   │   └── SoundGroupInfo.cs         # 声音组信息
    │   ├── Asset/
    │   │   └── AssetSetting.cs           # 资源系统配置
    │   ├── Entity/
    │   │   ├── EntitySetting.cs          # 实体系统配置
    │   │   └── EntityGroupInfo.cs        # 实体组信息
    │   ├── DataSave/
    │   │   └── DataSaveSetting.cs        # 数据存储配置
    │   ├── RedDot/
    │   │   ├── RedDotSetting.cs          # 红点系统配置
    │   │   └── RedDotNodeData.cs         # 红点节点数据
    │   ├── Guide/
    │   │   ├── GuideSetting.cs           # 引导系统配置
    │   │   ├── GuideInfo.cs              # 引导信息
    │   │   └── StepInfo.cs               # 步骤信息
    │   └── FuFramework.ModuleSetting.Runtime.asmdef
    └── Editor/
        ├── ModuleSettingInspector.cs       # 模块配置Inspector
        ├── Sound/
        │   ├── SoundSettingCreator.cs
        │   └── SoundSettingEditor.cs
        ├── Asset/
        │   ├── AssetSettingCreator.cs
        │   └── AssetSettingEditor.cs
        ├── Entity/
        │   ├── EntitySettingCreator.cs
        │   └── EntitySettingEditor.cs
        ├── DataSave/
        │   ├── DataSaveSettingCreator.cs
        │   └── DataSaveSettingEditor.cs
        ├── RedDot/
        │   ├── RedDotSettingCreator.cs
        │   └── RedDotSettingEditor.cs (多文件)
        ├── Guide/
        │   ├── GuideSettingCreator.cs
        │   └── GuideSettingEditor.cs
        └── FuFramework.ModuleSetting.Editor.asmdef
```

## 8. 依赖

- **FuFramework.Core**：框架核心模块（MonoSingleton）
- **Unity Engine**：基础运行环境
- **YooAsset**：资源管理系统（AssetSetting）
- **Unity.Audio**：音频系统支持（AudioMixer）
- **Newtonsoft.Json**：JSON 序列化（配置数据）

## 9. 最佳实践

### 9.1 配置管理原则

1. **单一职责**：每个配置类只负责一个模块的配置
2. **数据驱动**：通过配置数据驱动游戏行为，避免硬编码
3. **默认值保护**：始终提供合理的默认值，确保配置缺失时游戏仍能运行
4. **版本控制**：配置资源应纳入版本控制，便于团队协作

### 9.2 运行时配置修改

```csharp
// 根据设备性能动态调整配置
public class DynamicConfigAdjuster : MonoBehaviour
{
    private void Start()
    {
        var moduleSetting = ModuleSetting.Instance;
        
        // 根据设备性能调整帧率
        if (SystemInfo.systemMemorySize < 2000)
        {
            moduleSetting.FrameRate = 30;
        }
        else if (SystemInfo.systemMemorySize < 4000)
        {
            moduleSetting.FrameRate = 45;
        }
        else
        {
            moduleSetting.FrameRate = 60;
        }
    }
}
```

### 9.3 多环境配置管理

```csharp
// 使用编译符号区分环境
#if DEVELOPMENT_BUILD
    moduleSetting.AssetSetting.PlayMode = EPlayMode.EditorSimulateMode;
#elif STAGING_BUILD
    moduleSetting.AssetSetting.PlayMode = EPlayMode.HostPlayMode;
#else
    moduleSetting.AssetSetting.PlayMode = EPlayMode.OfflinePlayMode;
#endif
```

## 10. 注意事项

1. **单例模式限制**
   - ModuleSetting 是单例类，不要创建多个实例
   - 必须挂载到首个初始化场景的 GameObject 上
   - 其他模块依赖 ModuleSetting，确保它最先初始化

2. **配置修改时机**
   - 部分配置（如 FrameRate）在运行时修改会立即生效
   - 部分配置（如资源运行模式）只在初始化时读取
   - 建议在游戏启动时完成所有配置设置

3. **序列化限制**
   - 配置类继承自 ScriptableObject，支持 Unity 序列化
   - 复杂类型需要添加 `[Serializable]` 特性
   - 多态类型使用 `[SerializeReference]` 特性

4. **线程安全**
   - ModuleSetting 的访问是线程安全的（单例模式）
   - 但配置修改应在主线程执行
   - 避免在多线程中同时修改同一配置

5. **内存管理**
   - 配置资源在场景切换时不会被销毁
   - 大量配置数据可能占用较多内存
   - 考虑按需加载大型配置

6. **编辑器与运行时差异**
   - 编辑器模式下可以实时修改配置
   - 运行时修改的配置不会自动保存到资源文件
   - 使用 PlayerPrefs 或 DataSaveModule 保存运行时配置变更
