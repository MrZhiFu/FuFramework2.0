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

### 核心类说明

#### 1. SoundManager
音频管理器，继承自 FuModule，负责整个音频系统的生命周期管理。

**主要职责：**
- 管理声音组的创建和销毁
- 提供声音播放、暂停、停止等接口
- 处理音频资源的异步加载
- 发布音频播放事件

#### 2. SoundGroup
声音组，管理一组相关的声音代理，支持组级别的音量控制和静音设置。

**主要属性：**
- `Name`：声音组名称
- `Mute`：组静音状态
- `Volume`：组音量
- `SoundAgentCount`：声音代理数量

#### 3. SoundAgent
声音播放代理，封装 AudioSource 组件，提供具体的音频播放功能。

**主要功能：**
- 音频播放控制（播放、暂停、停止、重置）
- 音频参数设置（音量、音调、循环等）
- 3D音效支持
- 实体绑定功能

#### 4. SoundParams / SoundParams3D
音频播放参数类，用于配置音频播放的各种参数。

### 技术架构

```
SoundManager (管理器)
    ↓
SoundGroup (声音组)
    ↓
SoundAgent (声音代理)
    ↓
AudioSource (Unity音频组件)
```

## 快速开始

### 1. 基本音频播放示例

```csharp
using FuFramework.Sound.Runtime;
using Cysharp.Threading.Tasks;

public class SoundPlayer : MonoBehaviour
{
    private async void Start()
    {
        // 获取音频管理器
        var soundManager = SoundManager.Instance;
        
        // 播放背景音乐
        var bgmSerialId = await soundManager.PlaySound("BGM_Main", "Music");
        
        // 播放UI音效
        var uiSerialId = await soundManager.PlaySound("UI_Click", "UI");
        
        Debug.Log($"背景音乐序列号: {bgmSerialId}, UI音效序列号: {uiSerialId}");
    }
}
```

### 2. 事件监听示例

```csharp
using FuFramework.Sound.Runtime;
using FuFramework.Event.Runtime;

public class SoundEventListener : MonoBehaviour
{
    private void Start()
    {
        // 注册音频播放成功事件
        EventManager.Instance.Subscribe<PlaySoundSuccessEventArgs>(OnSoundPlaySuccess);
        
        // 注册音频播放失败事件
        EventManager.Instance.Subscribe<PlaySoundFailureEventArgs>(OnSoundPlayFailed);
    }
    
    private void OnSoundPlaySuccess(object sender, PlaySoundSuccessEventArgs e)
    {
        Debug.Log($"音频播放成功: 序列号={e.SerialId}, 资源={e.SoundAssetName}, 组={e.SoundGroupName}");
    }
    
    private void OnSoundPlayFailed(object sender, PlaySoundFailureEventArgs e)
    {
        Debug.LogError($"音频播放失败: 序列号={e.SerialId}, 资源={e.SoundAssetName}, 错误代码={e.ErrorCode}");
    }
    
    private void OnDestroy()
    {
        // 注销事件监听
        EventManager.Instance.Unsubscribe<PlaySoundSuccessEventArgs>(OnSoundPlaySuccess);
        EventManager.Instance.Unsubscribe<PlaySoundFailureEventArgs>(OnSoundPlayFailed);
    }
}
```

## 详细使用指南

### 1. 音频管理流程示例

#### 完整的音频播放系统

```csharp
using FuFramework.Sound.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    // 声音组定义
    private const string MUSIC_GROUP = "Music";
    private const string SFX_GROUP = "SFX";
    private const string UI_GROUP = "UI";
    
    // 音频资源定义
    private const string BGM_MAIN = "BGM_Main";
    private const string BGM_BATTLE = "BGM_Battle";
    private const string SFX_EXPLOSION = "SFX_Explosion";
    private const string UI_CLICK = "UI_Click";
    
    private int m_CurrentBgmSerialId = -1;
    
    private async void Start()
    {
        // 初始化音频系统
        await InitializeAudioSystem();
    }
    
    // 初始化音频系统
    private async UniTask InitializeAudioSystem()
    {
        var soundManager = SoundManager.Instance;
        
        // 播放主菜单背景音乐
        await PlayBackgroundMusic(BGM_MAIN);
    }
    
    // 播放背景音乐
    public async UniTask PlayBackgroundMusic(string bgmName)
    {
        var soundManager = SoundManager.Instance;
        
        // 停止当前背景音乐
        if (m_CurrentBgmSerialId > 0)
        {
            soundManager.StopSound(m_CurrentBgmSerialId);
        }
        
        // 配置背景音乐参数
        var soundParams = SoundParams.Create();
        soundParams.Loop = true;
        soundParams.Volume = 0.8f;
        soundParams.FadeInSeconds = 2f; // 2秒淡入
        
        // 播放新的背景音乐
        m_CurrentBgmSerialId = await soundManager.PlaySound(bgmName, MUSIC_GROUP, ".mp3", -1, soundParams);
        
        Debug.Log($"播放背景音乐: {bgmName}, 序列号: {m_CurrentBgmSerialId}");
    }
    
    // 播放3D音效（在指定位置）
    public async UniTask<int> Play3DSoundAtPosition(string soundName, Vector3 position)
    {
        var soundManager = SoundManager.Instance;
        
        // 配置3D音效参数
        var soundParams = SoundParams.Create();
        soundParams.Volume = 1.0f;
        soundParams.Priority = 10; // 较高优先级
        
        // 在指定位置播放3D音效
        var serialId = await soundManager.PlaySound3DPos(soundName, SFX_GROUP, position, ".wav", -1, soundParams);
        
        Debug.Log($"播放3D音效: {soundName} 在位置 {position}, 序列号: {serialId}");
        return serialId;
    }
    
    // 播放UI音效
    public async UniTask<int> PlayUISound(string soundName)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Volume = 0.7f;
        
        var serialId = await soundManager.PlaySound(soundName, UI_GROUP, ".wav", -1, soundParams);
        
        Debug.Log($"播放UI音效: {soundName}, 序列号: {serialId}");
        return serialId;
    }
    
    // 暂停所有音频
    public void PauseAllAudio()
    {
        var soundManager = SoundManager.Instance;
        soundManager.PauseAllSounds();
        Debug.Log("暂停所有音频");
    }
    
    // 恢复所有音频
    public void ResumeAllAudio()
    {
        var soundManager = SoundManager.Instance;
        soundManager.ResumeAllSounds();
        Debug.Log("恢复所有音频");
    }
    
    // 设置声音组音量
    public void SetGroupVolume(string groupName, float volume)
    {
        var soundManager = SoundManager.Instance;
        var soundGroup = soundManager.GetSoundGroup(groupName);
        
        if (soundGroup != null)
        {
            soundGroup.Volume = volume;
            Debug.Log($"设置声音组 {groupName} 音量: {volume}");
        }
    }
    
    // 设置声音组静音
    public void SetGroupMute(string groupName, bool mute)
    {
        var soundManager = SoundManager.Instance;
        var soundGroup = soundManager.GetSoundGroup(groupName);
        
        if (soundGroup != null)
        {
            soundGroup.Mute = mute;
            Debug.Log($"设置声音组 {groupName} 静音: {mute}");
        }
    }
}
```

#### 高级音频控制示例

```csharp
using FuFramework.Sound.Runtime;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class AdvancedAudioController : MonoBehaviour
{
    private readonly Dictionary<string, int> m_PlayingSounds = new();
    
    // 播放带回调的音效
    public async UniTask<int> PlaySoundWithCallback(string soundName, string groupName, System.Action onPlayEnd = null)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Volume = 1.0f;
        
        var serialId = await soundManager.PlaySound(soundName, groupName, ".wav", -1, soundParams, null, null, onPlayEnd);
        
        // 记录正在播放的声音
        m_PlayingSounds[soundName] = serialId;
        
        return serialId;
    }
    
    // 播放循环音效并设置停止条件
    public async UniTask<int> PlayLoopSoundWithCondition(string soundName, string groupName, System.Func<bool> stopCondition)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Loop = true;
        soundParams.Volume = 0.5f;
        
        var serialId = await soundManager.PlaySound(soundName, groupName, ".wav", -1, soundParams);
        
        // 启动条件检查协程
        StartCoroutine(CheckStopCondition(serialId, stopCondition));
        
        return serialId;
    }
    
    private System.Collections.IEnumerator CheckStopCondition(int serialId, System.Func<bool> stopCondition)
    {
        var soundManager = SoundManager.Instance;
        
        while (soundManager.IsSoundValid(serialId))
        {
            if (stopCondition())
            {
                soundManager.StopSound(serialId);
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    // 渐入渐出音效切换
    public async UniTask CrossFadeBackgroundMusic(string fromBgm, string toBgm, float fadeDuration = 3f)
    {
        var soundManager = SoundManager.Instance;
        
        // 渐出当前背景音乐
        if (m_PlayingSounds.ContainsKey(fromBgm))
        {
            var fromSerialId = m_PlayingSounds[fromBgm];
            soundManager.StopSound(fromSerialId, fadeDuration);
            m_PlayingSounds.Remove(fromBgm);
        }
        
        // 渐入新背景音乐
        var soundParams = SoundParams.Create();
        soundParams.Loop = true;
        soundParams.Volume = 0f; // 初始音量为0
        soundParams.FadeInSeconds = fadeDuration;
        
        var toSerialId = await soundManager.PlaySound(toBgm, "Music", ".mp3", -1, soundParams);
        m_PlayingSounds[toBgm] = toSerialId;
        
        Debug.Log($"背景音乐切换: {fromBgm} -> {toBgm}, 淡入淡出时间: {fadeDuration}秒");
    }
    
    // 批量停止音效
    public void StopMultipleSounds(List<int> serialIds)
    {
        var soundManager = SoundManager.Instance;
        
        foreach (var serialId in serialIds)
        {
            if (soundManager.IsSoundValid(serialId))
            {
                soundManager.StopSound(serialId);
            }
        }
        
        Debug.Log($"批量停止 {serialIds.Count} 个音效");
    }
}
```

### 2. 3D音效和实体绑定

#### 3D音效管理系统

```csharp
using FuFramework.Sound.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Sound3DManager : MonoBehaviour
{
    // 播放3D音效在指定位置
    public async UniTask<int> PlayExplosionAtPosition(Vector3 explosionPosition)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Volume = 1.0f;
        soundParams.Priority = 100; // 最高优先级
        
        // 在爆炸位置播放3D音效
        var serialId = await soundManager.PlaySound3DPos("Explosion_Large", "SFX", explosionPosition, ".wav", -1, soundParams);
        
        Debug.Log($"播放爆炸音效在位置: {explosionPosition}, 序列号: {serialId}");
        return serialId;
    }
    
    // 播放跟随玩家的脚步声
    public async UniTask<int> PlayFootstepSound(Transform playerTransform)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Volume = 0.3f;
        soundParams.Pitch = Random.Range(0.9f, 1.1f); // 随机音调增加真实感
        
        // 在玩家位置播放脚步声
        var serialId = await soundManager.PlaySound3DPos("Footstep_Grass", "SFX", playerTransform.position, ".wav", -1, soundParams);
        
        return serialId;
    }
    
    // 播放环境音效（如风声、水流声）
    public async UniTask<int> PlayAmbientSound(Vector3 position, string ambientSound, float maxDistance = 50f)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Loop = true;
        soundParams.Volume = 0.4f;
        
        var soundParams3D = SoundParams3D.Create(null, position);
        // 可以设置3D音效参数，如最大距离等
        
        var serialId = await soundManager.PlaySound(ambientSound, "Ambient", ".wav", -1, soundParams, soundParams3D);
        
        Debug.Log($"播放环境音效: {ambientSound} 在位置 {position}, 最大距离: {maxDistance}");
        return serialId;
    }
    
    // 根据距离调整音量（模拟距离衰减）
    public void AdjustVolumeByDistance(int serialId, Vector3 listenerPosition, Vector3 soundPosition, float maxDistance)
    {
        var soundManager = SoundManager.Instance;
        
        if (!soundManager.IsSoundValid(serialId)) return;
        
        var distance = Vector3.Distance(listenerPosition, soundPosition);
        var volume = Mathf.Clamp01(1f - (distance / maxDistance));
        
        soundManager.SetSoundVolume(serialId, volume);
    }
}
```

#### 实体绑定音效系统

```csharp
using FuFramework.Sound.Runtime;
using FuFramework.Entity.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EntitySoundSystem : MonoBehaviour
{
    // 为实体绑定音效
    public async UniTask<int> BindSoundToEntity(EntityLogic entity, string soundName, string groupName)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Loop = true;
        soundParams.Volume = 0.6f;
        
        // 将音效绑定到实体
        var serialId = await soundManager.PlaySoundToEntity(soundName, groupName, entity, ".wav", -1, soundParams);
        
        Debug.Log($"为实体 {entity.name} 绑定音效: {soundName}, 序列号: {serialId}");
        return serialId;
    }
    
    // 车辆引擎声音系统
    public class VehicleSoundSystem : MonoBehaviour
    {
        private EntityLogic m_VehicleEntity;
        private int m_EngineSoundId = -1;
        private int m_TireSoundId = -1;
        
        private async void Start()
        {
            m_VehicleEntity = GetComponent<EntityLogic>();
            
            // 绑定引擎声音
            m_EngineSoundId = await BindEngineSound();
            
            // 绑定轮胎声音
            m_TireSoundId = await BindTireSound();
        }
        
        private async UniTask<int> BindEngineSound()
        {
            var soundManager = SoundManager.Instance;
            
            var soundParams = SoundParams.Create();
            soundParams.Loop = true;
            soundParams.Volume = 0f; // 初始音量为0
            
            return await soundManager.PlaySoundToEntity("Engine_Idle", "Vehicle", m_VehicleEntity, ".wav", -1, soundParams);
        }
        
        private async UniTask<int> BindTireSound()
        {
            var soundManager = SoundManager.Instance;
            
            var soundParams = SoundParams.Create();
            soundParams.Loop = true;
            soundParams.Volume = 0f; // 初始音量为0
            
            return await soundManager.PlaySoundToEntity("Tire_Skid", "Vehicle", m_VehicleEntity, ".wav", -1, soundParams);
        }
        
        // 根据车速调整引擎声音音调
        public void UpdateEngineSound(float speed, float maxSpeed)
        {
            var soundManager = SoundManager.Instance;
            
            if (m_EngineSoundId > 0 && soundManager.IsSoundValid(m_EngineSoundId))
            {
                // 根据速度计算音调（0.8到1.2范围）
                var pitch = 0.8f + (speed / maxSpeed) * 0.4f;
                soundManager.SetSoundPitch(m_EngineSoundId, pitch);
                
                // 根据速度调整音量
                var volume = Mathf.Clamp01(speed / maxSpeed) * 0.8f;
                soundManager.SetSoundVolume(m_EngineSoundId, volume);
            }
        }
        
        // 播放刹车音效
        public async void PlayBrakeSound()
        {
            var soundManager = SoundManager.Instance;
            
            var soundParams = SoundParams.Create();
            soundParams.Volume = 0.7f;
            soundParams.Priority = 50;
            
            await soundManager.PlaySoundToEntity("Brake_Squeal", "Vehicle", m_VehicleEntity, ".wav", -1, soundParams);
        }
    }
}
```

## 高级用法

### 1. 自定义音频播放策略

```csharp
using FuFramework.Sound.Runtime;
using Cysharp.Threading.Tasks;
using System;

public class AdvancedSoundPlayer : MonoBehaviour
{
    // 自定义音频播放配置
    [Serializable]
    public class SoundPlayConfig
    {
        public string SoundName;
        public string GroupName = "SFX";
        public float Volume = 1.0f;
        public bool Loop = false;
        public float FadeInTime = 0f;
        public int Priority = 0;
        public Action OnPlayEnd;
        public Vector3? Position3D = null;
    }
    
    // 带配置的音频播放
    public async UniTask<int> PlaySoundWithConfig(SoundPlayConfig config)
    {
        var soundManager = SoundManager.Instance;
        
        var soundParams = SoundParams.Create();
        soundParams.Volume = config.Volume;
        soundParams.Loop = config.Loop;
        soundParams.FadeInSeconds = config.FadeInTime;
        soundParams.Priority = config.Priority;
        
        if (config.Position3D.HasValue)
        {
            // 3D音效播放
            return await soundManager.PlaySound3DPos(config.SoundName, config.GroupName, 
                config.Position3D.Value, ".wav", -1, soundParams, null, config.OnPlayEnd);
        }
        else
        {
            // 2D音效播放
            return await soundManager.PlaySound(config.SoundName, config.GroupName, ".wav", 
                -1, soundParams, null, null, config.OnPlayEnd);
        }
    }
    
    // 批量音频播放
    public async UniTask PlayMultipleSounds(params SoundPlayConfig[] configs)
    {
        var playTasks = new List<UniTask>();
        
        foreach (var config in configs)
        {
            playTasks.Add(PlaySoundWithConfig(config));
        }
        
        await UniTask.WhenAll(playTasks);
        Debug.Log($"批量播放 {configs.Length} 个音效完成");
    }
}
```

### 2. 音频资源管理和优化

```csharp
using FuFramework.Sound.Runtime;
using UnityEngine;

public class SoundResourceManager : MonoBehaviour
{
    [SerializeField] private bool m_EnableResourceMonitoring = true;
    [SerializeField] private float m_MonitorInterval = 5f;
    
    private float m_LastMonitorTime;
    
    private void Update()
    {
        if (!m_EnableResourceMonitoring) return;
        
        if (Time.time - m_LastMonitorTime >= m_MonitorInterval)
        {
            MonitorSoundResources();
            m_LastMonitorTime = Time.time;
        }
    }
    
    private void MonitorSoundResources()
    {
        var soundManager = SoundManager.Instance;
        var soundGroups = soundManager.GetAllSoundGroups();
        
        int totalPlayingSounds = 0;
        int totalLoadingSounds = 0;
        
        foreach (var soundGroup in soundGroups)
        {
            Debug.Log($"声音组 '{soundGroup.Name}': {soundGroup.SoundAgentCount} 个代理");
            totalPlayingSounds += soundGroup.SoundAgentCount;
        }
        
        var loadingSounds = soundManager.GetAllLoadingSoundSerialIds();
        totalLoadingSounds = loadingSounds.Length;
        
        Debug.Log($"音频资源统计: 播放中={totalPlayingSounds}, 加载中={totalLoadingSounds}");
        
        // 内存使用监控
        var totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024;
        if (totalMemory > 200) // 200MB阈值
        {
            Debug.LogWarning("音频内存使用过高，建议优化音频资源");
            OptimizeSoundResources();
        }
    }
    
    private void OptimizeSoundResources()
    {
        var soundManager = SoundManager.Instance;
        
        // 停止长时间播放但音量很小的音效
        // 这里可以实现具体的优化逻辑
        Debug.Log("执行音频资源优化");
    }
    
    // 预加载常用音效
    public void PreloadFrequentSounds()
    {
        var frequentSounds = new[]
        {
            "UI_Click",
            "UI_Hover", 
            "UI_Confirm",
            "Footstep_Default",
            "Jump"
        };
        
        // 这里可以实现音效预加载逻辑
        Debug.Log($"预加载 {frequentSounds.Length} 个常用音效");
    }
}
```

## 性能优化建议

### 1. 音频资源优化

```csharp
using FuFramework.Sound.Runtime;
using UnityEngine;

public class SoundOptimizationManager : MonoBehaviour
{
    [SerializeField] private int m_MaxConcurrentSounds = 20;
    [SerializeField] private float m_SoundCleanupInterval = 30f;
    
    private float m_LastCleanupTime;
    
    private void Update()
    {
        // 定期清理音频资源
        if (Time.time - m_LastCleanupTime >= m_SoundCleanupInterval)
        {
            CleanupSoundResources();
            m_LastCleanupTime = Time.time;
        }
    }
    
    private void CleanupSoundResources()
    {
        var soundManager = SoundManager.Instance;
        var soundGroups = soundManager.GetAllSoundGroups();
        
        int totalStopped = 0;
        
        foreach (var soundGroup in soundGroups)
        {
            // 这里可以实现具体的清理逻辑
            // 例如：停止长时间播放但音量很小的音效
        }
        
        if (totalStopped > 0)
        {
            Debug.Log($"音频资源清理: 停止 {totalStopped} 个音效");
        }
    }
    
    // 限制同时播放的音效数量
    public bool CanPlayNewSound()
    {
        var soundManager = SoundManager.Instance;
        var loadingSounds = soundManager.GetAllLoadingSoundSerialIds();
        
        // 计算当前播放中的音效数量
        int currentPlayingCount = 0;
        var soundGroups = soundManager.GetAllSoundGroups();
        foreach (var soundGroup in soundGroups)
        {
            currentPlayingCount += soundGroup.SoundAgentCount;
        }
        
        return currentPlayingCount + loadingSounds.Length < m_MaxConcurrentSounds;
    }
}
```

### 2. 内存使用监控

```csharp
using FuFramework.Sound.Runtime;
using UnityEngine;

public class SoundMemoryMonitor : MonoBehaviour
{
    [SerializeField] private bool m_EnableMemoryMonitoring = true;
    [SerializeField] private float m_CheckInterval = 10f;
    
    private float m_LastCheckTime;
    
    private void Update()
    {
        if (!m_EnableMemoryMonitoring) return;
        
        if (Time.time - m_LastCheckTime >= m_CheckInterval)
        {
            CheckSoundMemoryUsage();
            m_LastCheckTime = Time.time;
        }
    }
    
    private void CheckSoundMemoryUsage()
    {
        var soundManager = SoundManager.Instance;
        var soundGroups = soundManager.GetAllSoundGroups();
        
        Debug.Log($"当前声音组数量: {soundGroups.Length}");
        
        // 检查内存使用情况
        var totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024;
        Debug.Log($"总内存使用: {totalMemory} MB");
        
        // 如果内存使用过高，建议优化音频资源
        if (totalMemory > 300) // 300MB阈值
        {
            Debug.LogWarning("音频内存使用过高，建议优化音频资源");
        }
    }
}
```

## 注意事项

### 1. 内存管理
- **及时停止**：不再使用的音效应及时停止释放资源
- **避免泄漏**：确保音效对象正确释放，避免内存泄漏
- **合理预加载**：根据使用频率合理预加载音频资源

### 2. 性能考虑
- **并发控制**：限制同时播放的音效数量
- **资源优化**：使用合适的音频格式和压缩设置
- **优先级管理**：合理设置音效优先级

### 3. 用户体验
- **音量平衡**：确保各音效组音量平衡
- **淡入淡出**：使用淡入淡出效果提升体验
- **错误处理**：完善的错误处理机制确保稳定性

### 4. 平台兼容性
- **格式支持**：确保音频格式在所有目标平台兼容
- **性能适配**：根据不同设备性能调整音频设置

## API 参考

### SoundManager 类

#### 静态属性

##### Instance
```csharp
public static SoundManager Instance { get; }
```
**功能**：获取音频管理器单例实例

#### 实例方法

##### PlaySound(string soundAssetName, string groupName, string extension, int serialId, SoundParams soundParams, SoundParams3D soundParams3D, object userData, Action onPlayEnd)
```csharp
public UniTask<int> PlaySound(string soundAssetName, string groupName, string extension = ".mp3", int serialId = -1, SoundParams soundParams = null, SoundParams3D soundParams3D = null, object userData = null, Action onPlayEnd = null)
```
**功能**：播放音频

**参数**：
- `soundAssetName` (string)：音频资源名称
- `groupName` (string)：声音组名称
- `extension` (string)：音频文件扩展名，默认 ".mp3"
- `serialId` (int)：序列编号，-1表示自动分配
- `soundParams` (SoundParams)：音频播放参数
- `soundParams3D` (SoundParams3D)：3D音频参数
- `userData` (object)：用户自定义数据
- `onPlayEnd` (Action)：播放结束回调

**返回值**：
- `UniTask<int>`：异步音频播放任务，返回序列编号

**示例**：
```csharp
var serialId = await SoundManager.Instance.PlaySound("BGM_Main", "Music");
```

##### PlaySound3DPos(string soundAssetName, string groupName, Vector3 worldPosition, string extension, int serialId, SoundParams soundParams, object userData, Action onPlayEnd)
```csharp
public UniTask<int> PlaySound3DPos(string soundAssetName, string groupName, Vector3 worldPosition, string extension = ".mp3", int serialId = -1, SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)
```
**功能**：在指定3D位置播放音频

**参数**：
- `worldPosition` (Vector3)：3D世界坐标位置

**示例**：
```csharp
var serialId = await SoundManager.Instance.PlaySound3DPos("Explosion", "SFX", explosionPosition);
```

##### StopSound(int serialId, float fadeOutSeconds)
```csharp
public void StopSound(int serialId, float fadeOutSeconds = 0f)
```
**功能**：停止指定音频

**参数**：
- `serialId` (int)：音频序列编号
- `fadeOutSeconds` (float)：淡出时间，默认0秒立即停止

**示例**：
```csharp
SoundManager.Instance.StopSound(serialId, 2f); // 2秒淡出停止
```

## 常见问题解答

### Q: 音频播放失败怎么办？
A: 检查音频资源路径是否正确，资源文件是否存在，声音组是否配置正确。

### Q: 如何实现音频的淡入淡出效果？
A: 使用 SoundParams 的 FadeInSeconds 参数和 StopSound 的 fadeOutSeconds 参数。

### Q: 3D音效没有声音怎么办？
A: 检查 AudioListener 组件是否存在，3D位置设置是否正确，距离衰减参数是否合理。

### Q: 如何优化音频内存使用？
A: 合理设置同时播放音效数量，及时停止不再使用的音效，使用合适的音频压缩格式。

### Q: 如何实现音频的优先级管理？
A: 使用 SoundParams 的 Priority 参数，高优先级音效可以打断低优先级音效。

## 总结

Sound 模块为 FuFramework 提供了强大的音频管理系统，支持声音组管理、3D音效、事件通知、资源池等高级功能。通过合理的音频管理策略，可以显著提升游戏的音频体验和性能表现。

该模块设计合理，功能完善，与资源管理系统深度集成，是游戏开发中音频管理的理想解决方案。