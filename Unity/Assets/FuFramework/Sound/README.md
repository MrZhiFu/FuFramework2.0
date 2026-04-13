# FuFramework Sound Module

## 概述

Sound 模块是 FuFramework 中的音频管理系统，专门用于管理游戏中的声音播放、音效控制和音频资源管理。该模块基于 Unity AudioSource 组件，提供声音组管理、3D音效、事件通知、资源池等高级功能，是游戏音频管理的核心组件。

### 核心特性

- **声音组管理**：支持分组管理不同类型的声音（背景音乐、音效、UI音效等）
- **3D音效支持**：支持基于位置的3D音效播放和实体绑定
- **异步资源加载**：基于 YooAsset 的异步音频资源加载
- **事件驱动架构**：完整的播放成功/失败事件通知机制
- **资源池管理**：声音代理对象池，减少内存分配
- **参数配置**：丰富的音效参数配置（音量、音调、淡入淡出等）

## 系统架构

### 类继承体系

```
FuModule (抽象基类)
    ↑
SoundModule (声音管理模块)
    ├── SoundGroup (声音组)
    │       ↑
    │   MonoBehaviour
    │       ↓
    │   SoundAgent (声音代理)
    │       ↑
    │   MonoBehaviour
    │       ↓
    │   AudioSource (Unity组件)
    │
    ├── PlaySoundInfo (播放信息) → IReference
    ├── SoundParams (声音参数) → IReference
    ├── SoundParams3D (3D声音参数) → IReference
    │
    └── 事件类
            ├── PlaySoundSuccessEventArgs → GameEventArgs → IReference
            └── PlaySoundFailureEventArgs → GameEventArgs → IReference
```

### 技术架构

```
┌─────────────────────────────────────────────────────────────┐
│                      SoundModule                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  声音组字典 (Dictionary<string, SoundGroup>)         │   │
│  │  加载中声音列表 (List<int>)                          │   │
│  │  待释放声音集合 (HashSet<int>)                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                      SoundGroup                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  声音代理列表 (List<SoundAgent>)                     │   │
│  │  静音状态 (bool)                                     │   │
│  │  组音量 (float)                                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                      SoundAgent                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  AudioSource (Unity组件)                             │   │
│  │  绑定实体 (EntityLogic)                              │   │
│  │  序列编号 (int)                                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 核心类详解

### SoundModule

声音管理模块，继承自 FuModule，负责整个音频系统的生命周期管理。

**主要职责：**
- 管理声音组的创建和销毁
- 提供声音播放、暂停、停止等接口
- 处理音频资源的异步加载
- 发布音频播放事件

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_SoundGroupDict | Dictionary<string, SoundGroup> | 声音组字典 |
| m_LoadingSoundList | List<int> | 正在加载的声音ID列表 |
| m_LoadingToReleaseSet | HashSet<int> | 加载中但需释放的声音ID集合 |
| m_AssetModule | AssetModule | 资源管理模块引用 |
| m_EventModule | EventModule | 事件管理模块引用 |
| m_Serial | int | 声音自增序列号 |
| m_AudioMixer | AudioMixer | 混音器 |
| m_AudioListener | AudioListener | 声音监听器 |

**核心方法：**

```csharp
// 检查声音组是否存在
public bool HasSoundGroup(string groupName)

// 获取指定声音组
public SoundGroup GetSoundGroup(string groupName)

// 获取所有声音组
public SoundGroup[] GetAllSoundGroups()

// 增加声音组
public bool AddSoundGroup(SoundGroupInfo soundGroupInfo)

// 播放声音（基础接口）
public async UniTask<int> PlaySound(string soundAssetName, string groupName, 
    string extension = ".mp3", int serialId = -1, 
    SoundParams soundParams = null, SoundParams3D soundParams3D = null, 
    object userData = null, Action onPlayEnd = null)

// 在指定3D位置播放声音
public UniTask<int> PlaySound3DPos(string soundAssetName, string groupName, 
    Vector3 worldPosition, string extension = ".mp3", int serialId = -1,
    SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)

// 播放声音并绑定到实体
public async UniTask<int> PlaySoundToEntity(string soundAssetName, string groupName, 
    Entity.Runtime.Entity bindingEntity, string extension = ".mp3", int serialId = -1,
    SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)

// 停止播放声音
public bool StopSound(int serialId)
public bool StopSound(int serialId, float fadeOutSeconds)

// 暂停/恢复播放声音
public void PauseSound(int serialId, float fadeOutSeconds = 0)
public void ResumeSound(int serialId, float fadeInSeconds = 0)

// 停止所有声音
public void StopAllLoadedSounds(float fadeOutSeconds = 0)
public void StopAllLoadingSounds()

// 检查声音是否有效
public bool IsSoundValid(int serialId)

// 获取正在加载的声音序列号
public int[] GetAllLoadingSoundSerialIds()
```

### SoundGroup

声音组，管理一组相关的声音代理，支持组级别的音量控制和静音设置。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Name | string | 声音组名称 |
| Mute | bool | 组静音状态 |
| Volume | float | 组音量（0-1） |
| SoundAgentCount | int | 声音代理数量 |
| AllowBeReplacedBySamePriority | bool | 是否允许同优先级声音替换 |

**核心方法：**

```csharp
// 初始化声音组
public void Init(SoundGroupInfo soundGroupInfo)

// 增加声音代理辅助器
public void AddSoundAgentHelper(int idx)

// 播放声音
public SoundAgent PlaySound(PlaySoundInfo playSoundInfo, out EPlaySoundErrorCode? errorCode)

// 停止播放声音
public bool StopSound(int serialId, float fadeOutSeconds)

// 暂停/恢复播放声音
public bool PauseSound(int serialId, float fadeOutSeconds)
public bool ResumeSound(int serialId, float fadeInSeconds)

// 停止所有已加载的声音
public void StopAllLoadedSounds(float fadeOutSeconds)
```

### SoundAgent

声音播放代理，封装 AudioSource 组件，提供具体的音频播放功能。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| SerialId | int | 声音序列编号 |
| Time | float | 播放位置（秒） |
| Mute | bool | 是否静音 |
| Loop | bool | 是否循环播放 |
| Priority | int | 声音优先级（0-255） |
| Volume | float | 音量大小 |
| Pitch | float | 音调 |
| PanStereo | float | 立体声声相 |
| SpatialBlend | float | 空间混合量（0=2D, 1=3D） |
| MaxDistance | float | 最大距离 |
| DopplerLevel | float | 多普勒等级 |
| IsPlaying | bool | 是否正在播放 |
| Length | float | 声音长度（秒） |

**核心方法：**

```csharp
// 初始化
public void Init(SoundGroup soundGroup)

// 设置声音资源
internal bool SetSoundAsset(object soundAsset)

// 设置绑定实体
public void SetBindingEntity(Entity.Runtime.Entity bindingEntity)

// 设置世界位置
public void SetWorldPosition(Vector3 wPos)

// 播放控制
public void Play(string assetPath, float fadeInSeconds, Action onPlayEnd = null)
public void Stop(float fadeOutSeconds)
public void Pause(float fadeOutSeconds)
public void Resume(float fadeInSeconds)
public void Reset()

// 刷新设置
internal void RefreshMute()
internal void RefreshVolume()
```

### SoundParams

声音播放参数类，用于配置音频播放的各种参数。

**属性列表：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| Time | float | 0 | 播放位置（秒） |
| IsMute | bool | false | 是否静音 |
| Loop | bool | false | 是否循环播放 |
| Priority | int | 0 | 声音优先级 |
| Volume | float | 1 | 音量大小 |
| FadeInSeconds | float | 0 | 淡入时间（秒） |
| Pitch | float | 1 | 音调 |
| PanStereo | float | 0 | 立体声声相 |
| SpatialBlend | float | 0 | 空间混合量 |
| MaxDistance | float | 100 | 最大距离 |
| DopplerLevel | float | 1 | 多普勒等级 |

**使用方法：**

```csharp
// 创建参数
var soundParams = SoundParams.Create();
soundParams.Loop = true;
soundParams.Volume = 0.8f;
soundParams.FadeInSeconds = 2f;

// 使用完毕后释放回对象池
ReferencePool.Release(soundParams);
```

### SoundParams3D

3D声音播放参数类，用于配置3D音效的实体绑定和位置信息。

**属性列表：**

| 属性 | 类型 | 说明 |
|------|------|------|
| BindingEntity | Entity | 绑定的实体 |
| WorldPosition | Vector3 | 世界空间位置 |

**使用方法：**

```csharp
// 绑定实体
var soundParams3D = SoundParams3D.Create(entity, Vector3.zero);

// 指定位置
var soundParams3D = SoundParams3D.Create(null, worldPosition);
```

### PlaySoundInfo

播放声音信息类，用于在加载声音资源时保存相关信息。实现了 IReference 接口，支持引用池管理。

**属性列表：**

| 属性 | 类型 | 说明 |
|------|------|------|
| SerialId | int | 声音序列编号 |
| SoundAssetPath | string | 声音资源全路径 |
| SoundAsset | object | 声音资源对象 |
| SoundGroup | SoundGroup | 所在声音组 |
| SoundParams | SoundParams | 声音参数 |
| SoundParams3D | SoundParams3D | 3D声音参数 |
| OnPlayEnd | Action | 播放结束回调 |
| UserData | object | 用户自定义数据 |

### 事件参数类

#### PlaySoundSuccessEventArgs

播放声音成功事件参数。

**属性：**
- SerialId: 声音序列编号
- SoundAssetName: 声音资源名称
- UserData: 用户自定义数据

#### PlaySoundFailureEventArgs

播放声音失败事件参数。

**属性：**
- SerialId: 声音序列编号
- SoundAssetName: 声音资源名称
- SoundGroupName: 声音组名称
- ErrorCode: 错误码（EPlaySoundErrorCode）

**错误码枚举：**

```csharp
public enum EPlaySoundErrorCode : byte
{
    Unknown = 0,                    // 未知错误
    SoundGroupNotExist,             // 声音组不存在
    SoundGroupHasNoAgent,           // 声音组没有声音代理
    IgnoredBecauseLowPriority,      // 因优先级低被忽略
    SetSoundAssetFailure            // 设置声音资源失败
}
```

## 使用示例

### 基本音频播放

```csharp
using FuFramework.Sound.Runtime;
using Cysharp.Threading.Tasks;

public class SoundPlayer : MonoBehaviour
{
    private async void Start()
    {
        // 获取音频管理器
        var soundModule = ModuleManager.GetModule<SoundModule>();
        
        // 播放背景音乐
        var bgmSerialId = await soundModule.PlaySound("BGM_Main", "Music");
        
        // 播放UI音效
        var uiSerialId = await soundModule.PlaySound("UI_Click", "UI");
    }
}
```

### 带参数的音频播放

```csharp
// 配置声音参数
var soundParams = SoundParams.Create();
soundParams.Loop = true;
soundParams.Volume = 0.8f;
soundParams.FadeInSeconds = 2f;
soundParams.Priority = 10;

// 播放声音
var serialId = await soundModule.PlaySound(
    "BGM_Main",           // 声音资源名称
    "Music",              // 声音组名称
    ".mp3",               // 扩展名
    -1,                   // 序列号（-1表示自动分配）
    soundParams,          // 声音参数
    null,                 // 3D参数
    null,                 // 用户数据
    () => Debug.Log("播放结束")  // 播放结束回调
);
```

### 3D音效播放

```csharp
// 在指定位置播放3D音效
var position = new Vector3(10, 0, 10);
var serialId = await soundModule.PlaySound3DPos(
    "Explosion",
    "SFX",
    position,
    ".wav",
    -1,
    soundParams
);

// 绑定到实体播放
var entity = entityModule.ShowEntity("Player");
var serialId = await soundModule.PlaySoundToEntity(
    "Engine",
    "Vehicle",
    entity,
    ".wav",
    -1,
    soundParams
);
```

### 事件监听

```csharp
using FuFramework.Sound.Runtime;
using FuFramework.Event.Runtime;

public class SoundEventListener : MonoBehaviour
{
    private void Start()
    {
        var eventModule = ModuleManager.GetModule<EventModule>();
        
        // 注册音频播放成功事件
        eventModule.Subscribe<PlaySoundSuccessEventArgs>(OnSoundPlaySuccess);
        
        // 注册音频播放失败事件
        eventModule.Subscribe<PlaySoundFailureEventArgs>(OnSoundPlayFailed);
    }
    
    private void OnSoundPlaySuccess(object sender, PlaySoundSuccessEventArgs e)
    {
        Debug.Log($"音频播放成功: 序列号={e.SerialId}, 资源={e.SoundAssetName}");
    }
    
    private void OnSoundPlayFailed(object sender, PlaySoundFailureEventArgs e)
    {
        Debug.LogError($"音频播放失败: 序列号={e.SerialId}, 错误={e.ErrorCode}");
    }
}
```

### 声音组控制

```csharp
// 获取声音组
var musicGroup = soundModule.GetSoundGroup("Music");

// 设置组音量
musicGroup.Volume = 0.5f;

// 设置组静音
musicGroup.Mute = true;

// 停止组内所有声音
musicGroup.StopAllLoadedSounds(1f);  // 1秒淡出
```

### 声音控制

```csharp
// 停止声音
soundModule.StopSound(serialId);
soundModule.StopSound(serialId, 1f);  // 1秒淡出

// 暂停/恢复声音
soundModule.PauseSound(serialId);
soundModule.ResumeSound(serialId);

// 检查声音是否有效
bool isValid = soundModule.IsSoundValid(serialId);

// 停止所有声音
soundModule.StopAllLoadedSounds();
soundModule.StopAllLoadingSounds();
```

## 目录结构

```
FuFramework/Sound/
├── Runtime/
│   ├── SoundModule.cs                    # 声音管理模块主类
│   ├── SoundModule.SoundGroup.cs         # 声音组实现
│   ├── SoundModule.SoundAgent.cs         # 声音代理实现
│   ├── SoundModule.PlaySoundInfo.cs      # 播放信息类
│   ├── Misc/
│   │   ├── SoundParams.cs                # 声音参数类
│   │   ├── SoundParams3D.cs              # 3D声音参数类
│   │   └── EPlaySoundErrorCode.cs        # 错误码枚举
│   └── Event/
│       ├── PlaySoundSuccessEventArgs.cs  # 播放成功事件
│       └── PlaySoundFailureEventArgs.cs  # 播放失败事件
├── Editor/
│   └── Inspector/
│       └── SoundModuleInspector.cs       # 编辑器Inspector
└── README.md                             # 本文档
```

## 依赖模块

- **Core**: 提供 FuModule 基类、日志、工具类
- **Asset**: 提供音频资源异步加载功能
- **Event**: 提供事件广播机制
- **ReferencePool**: 提供对象池管理
- **Entity**: 提供实体绑定功能（可选）
- **ModuleSetting**: 提供声音模块配置

## 配置说明

声音模块通过 ModuleSetting 进行配置，主要包括：

- **AudioMixer**: 混音器资源
- **SoundGroups**: 声音组配置列表
  - Name: 声音组名称
  - Volume: 默认音量
  - Mute: 默认静音状态
  - AgentCount: 声音代理数量
  - AllowBeReplacedBySamePriority: 是否允许同优先级替换

## 注意事项

1. **声音代理数量**：每个声音组的声音代理数量决定了该组可以同时播放的声音数量上限
2. **优先级机制**：当声音代理不足时，高优先级声音会替换低优先级声音
3. **资源释放**：声音播放完成后会自动释放资源，无需手动管理
4. **3D音效**：使用 SpatialBlend 属性控制2D/3D混合程度
5. **淡入淡出**：通过 FadeInSeconds 和 fadeOutSeconds 参数实现平滑过渡
6. **应用暂停**：模块会自动处理应用进入后台时的暂停逻辑
