# FuFramework Sound Module

## 1. 简介

FuFramework Sound 模块是游戏框架的声音管理系统，基于 Unity AudioSource 提供完整的音频播放控制。该模块按声音组管理音频播放器，支持 2D（背景音乐/UI音效）和 3D（空间音效）两种播放模式，提供音量、循环、淡入淡出、音调、立体声声相等丰富的音频控制参数。

## 2. 核心特性

- **分组管理**：声音按 `SoundGroup` 分组，每组独立管理多个 `SoundAgent`
- **2D/3D 模式**：`SoundParams`（2D）和 `SoundParams3D`（3D，可绑定实体位置）
- **完整音频控制**：音量、循环、优先级、淡入淡出、音调、立体声声相、空间混合
- **对象池复用**：`SoundAgent` 通过对象池管理，减少 AudioSource 的创建销毁开销
- **异步加载**：音频资源通过 YooAsset 异步加载
- **事件通知**：播放成功/失败事件

## 3. 核心概念

### 3.1 声音架构

```
┌─────────────────────────────────────────────────────────────┐
│                     SoundModule                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_SoundGroupDict (Dictionary<string, SoundGroup>)  │   │
│  │  - 按名称管理所有声音组                              │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  事件通知:                                          │   │
│  │  - PlaySoundSuccessEventArgs  (播放成功)            │   │
│  │  - PlaySoundFailureEventArgs  (播放失败)            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   SoundGroup     │
                    │  ┌────────────┐  │
                    │  │ SoundAgent │  │
                    │  │ (AudioSource)│ │
                    │  │ SoundAgent │  │
                    │  │ SoundAgent │  │
                    │  └────────────┘  │
                    └──────────────────┘
```

### 3.2 2D vs 3D 声音

| 模式 | 参数类 | 适用场景 |
|------|--------|---------|
| 2D | `SoundParams` | BGM、UI 音效、旁白 |
| 3D | `SoundParams3D` | 脚步声、枪声、环境音效（可绑定 Entity 跟随） |

## 4. 核心类说明

### 4.1 SoundModule

声音管理模块，继承自 `ModuleBase`，实现 `ICancelAsync`（可取消异步对象）。

> **可取消异步**：`SoundModule` 实现 `ICancelAsync`（`Token` + `CancelAsync`）。
> 模块销毁（`OnDispose`）时触发 `Token` 取消，在途音频/AudioMixer 加载随之中止（释放句柄、清理 loading 状态，
> 抛 `OperationCanceledException`），不再写回模块字段。框架重启 `RestartGame` 会在重启前 `await` 各模块
> `CancelAsync` 等待清理，保证重新初始化前旧生命周期零在途残留；`OnInit` 重建 `CancellationScope`
> （新 Token = 新生命周期），重启后可正常使用。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `SoundModule` | 模块单例 |
| `SoundGroupCount` | `int` | 声音组数量 |
| `AudioMixer` | `AudioMixer` | 声音混响器 |

**核心方法：**

```csharp
// 播放声音
UniTask<int> PlaySound(string soundAssetName, string groupName, string extension = ".mp3", int serialId = -1,
    SoundParams soundParams = null, SoundParams3D soundParams3D = null, object userData = null, Action onPlayEnd = null)
UniTask<int> PlaySound3DPos(string soundAssetName, string groupName, Vector3 worldPosition, string extension = ".mp3",
    int serialId = -1, SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)
UniTask<int> PlaySoundToEntity(string soundAssetName, string groupName, Entity.Entity bindingEntity,
    string extension = ".mp3", int serialId = -1, SoundParams soundParams = null, object userData = null, Action onPlayEnd = null)

// 停止声音
bool StopSound(int serialId)
bool StopSound(int serialId, float fadeOutSeconds)
void StopAllLoadedSounds()
void StopAllLoadedSounds(float fadeOutSeconds)
void StopAllLoadingSounds()

// 暂停/恢复
void PauseSound(int serialId)
void PauseSound(int serialId, float fadeOutSeconds)
void ResumeSound(int serialId)
void ResumeSound(int serialId, float fadeInSeconds)

// 声音组管理
bool HasSoundGroup(string groupName)
SoundGroup GetSoundGroup(string groupName)
SoundGroup[] GetAllSoundGroups()
void GetAllSoundGroups(List<SoundGroup> results)
bool AddSoundGroup(SoundGroupCfg row)
```

### 4.2 SoundAgent

声音播放代理（MonoBehaviour），封装 `AudioSource` 的完整控制。

**核心控制：**

- 播放、暂停、停止、恢复
- 音量控制（支持独立音量和全局音量叠加）
- 循环播放
- 静音控制
- 音调（Pitch）调整
- 立体声声相（PanStereo）
- 空间混合（Spatial Blend，控制 2D/3D 程度）
- 3D 空间设置（跟随实体位置、衰减模式）
- 渐入渐出（FadeIn/FadeOut）

### 4.3 SoundGroup

声音组，管理一组 `SoundAgent` 实例。

**核心属性：**

| 属性 | 说明 |
|------|------|
| `Name` | 组名称 |
| `SoundAgentCount` | 代理数量 |
| `Mute` | 组静音 |
| `Volume` | 组音量 |

### 4.4 播放参数

**SoundParams（2D 声音参数）：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `Time` | `float` | 播放位置（秒，默认 0） |
| `IsMute` | `bool` | 在声音组内是否静音 |
| `Loop` | `bool` | 是否循环 |
| `Priority` | `int` | 优先级（数值越大越优先，默认 0） |
| `Volume` | `float` | 在声音组内音量（默认 1） |
| `FadeInSeconds` | `float` | 淡入时间（秒，默认 0） |
| `Pitch` | `float` | 音调（默认 1） |
| `PanStereo` | `float` | 立体声声相（-1 左, 0 中, 1 右） |
| `SpatialBlend` | `float` | 空间混合量（0 为 2D，1 为 3D，默认 0） |
| `MaxDistance` | `float` | 声音最大距离（默认 100） |
| `DopplerLevel` | `float` | 多普勒等级（默认 1） |

**SoundParams3D（3D 声音参数）：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `BindingEntity` | `Entity` | 绑定的实体（声音跟随实体移动） |
| `WorldPosition` | `Vector3` | 世界坐标位置 |

### 4.5 错误码

`EPlaySoundErrorCode` 枚举定义了播放失败的原因：
- `SoundGroupNotExist`：声音组不存在
- `SoundGroupMute`：声音组已静音
- `NoFreeAgent`：无可用播放代理
- `AssetLoadFailed`：音频资源加载失败
- ...

## 5. 使用示例

### 5.1 初始化声音系统

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Sound;
using Hotfix.Framework.Config;

public class SoundExample
{
    private SoundModule m_SoundModule;

    public void Init()
    {
        // SoundModule 在 OnInit 中自动从配置表加载声音组，无需手动添加
        m_SoundModule = ModuleManager.GetModule<SoundModule>();
    }
}
```

### 5.2 播放 2D 声音

```csharp
// 播放背景音乐
var soundParams = SoundParams.Create();
soundParams.Volume = 0.8f;
soundParams.Loop = true;
soundParams.FadeInSeconds = 2f;
int bgmSerialId = await m_SoundModule.PlaySound(
    soundAssetName: "MainTheme.ogg",
    groupName: "BGM",
    soundParams: soundParams
);

// 播放 UI 按钮音效
var sfxParams = SoundParams.Create();
sfxParams.Volume = 1f;
int sfxSerialId = await m_SoundModule.PlaySound(
    soundAssetName: "UI_Click.wav",
    groupName: "SFX",
    soundParams: sfxParams
);
```

### 5.3 播放 3D 声音

```csharp
// 播放 3D 脚步声（使用 PlaySoundToEntity 绑定实体）
var footstepParams = SoundParams.Create();
footstepParams.Volume = 1f;
footstepParams.SpatialBlend = 1f;
footstepParams.MaxDistance = 50f;
int footstepSerialId = await m_SoundModule.PlaySoundToEntity(
    soundAssetName: "Footstep.ogg",
    groupName: "3D",
    bindingEntity: playerEntity,
    soundParams: footstepParams
);

// 在指定世界位置播放 3D 声音（使用 PlaySound3DPos）
var explosionParams = SoundParams.Create();
explosionParams.Volume = 1f;
explosionParams.SpatialBlend = 1f;
explosionParams.MaxDistance = 100f;
int explosionSerialId = await m_SoundModule.PlaySound3DPos(
    soundAssetName: "Explosion.ogg",
    groupName: "3D",
    worldPosition: new Vector3(10, 0, 5),
    soundParams: explosionParams
);
```

### 5.4 声音控制

```csharp
// 暂停指定声音
m_SoundModule.PauseSound(serialId);
m_SoundModule.PauseSound(serialId, fadeOutSeconds: 0.5f);

// 恢复指定声音
m_SoundModule.ResumeSound(serialId);
m_SoundModule.ResumeSound(serialId, fadeInSeconds: 0.5f);

// 停止指定声音
m_SoundModule.StopSound(serialId);
m_SoundModule.StopSound(serialId, fadeOutSeconds: 0.5f);

// 停止所有已加载的声音
m_SoundModule.StopAllLoadedSounds();
m_SoundModule.StopAllLoadedSounds(fadeOutSeconds: 1f);
```

### 5.5 监听播放事件

```csharp
var eventModule = ModuleManager.GetModule<EventModule>();

eventModule.Subscribe(PlaySoundSuccessEventArgs.EventId, (sender, e) =>
{
    var args = e as PlaySoundSuccessEventArgs;
    Debug.Log($"声音播放成功: SerialId={args.SerialId}");
});

eventModule.Subscribe(PlaySoundFailureEventArgs.EventId, (sender, e) =>
{
    var args = e as PlaySoundFailureEventArgs;
    Debug.LogError($"声音播放失败: ErrorCode={args.ErrorCode}");
});
```

## 6. 目录结构

```text
Sound/
├── Runtime/
│   ├── SoundModule.cs                    # 声音管理模块
│   ├── SoundModule.SoundAgent.cs         # 声音播放代理 (AudioSource 封装)
│   ├── SoundModule.SoundGroup.cs         # 声音组
│   ├── SoundModule.PlaySoundInfo.cs      # 播放声音信息 (引用池)
│   ├── Misc/
│   │   ├── EPlaySoundErrorCode.cs        # 播放错误码枚举
│   │   ├── SoundParams.cs                # 2D 播放参数
│   │   └── SoundParams3D.cs              # 3D 播放参数
│   ├── Event/
│   │   ├── PlaySoundSuccessEventArgs.cs
│   │   └── PlaySoundFailureEventArgs.cs
└── README.md                             # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类
- **Hotfix.Framework.Event**：事件系统
- **Hotfix.Framework.Asset**：音频资源加载
- **Hotfix.Framework.ObjectPool**：SoundAgent 对象池
- **Hotfix.Framework.ReferencePool**：引用池

## 8. 最佳实践

1. **分组规划**：按 BGM、SFX、Voice、3D 等类别建立声音组，分别控制音量和代理数量
2. **代理数量**：根据预计同时播放的最大声音数配置代理数量，避免播放失败
3. **循环音效**：BGM 设置 `Loop = true`，使用 `FadeInSeconds` 平滑切入
4. **3D 衰减**：合理设置 `MaxDistance` 和 `SpatialBlend`，优化听觉体验
5. **音效优先级**：重要音效（如技能提示）设置较高 Priority，确保不被低优先级音效挤占

## 9. 注意事项

1. 播放失败时检查错误码，常见原因：声音组不存在、无可用代理
2. `SoundAgent` 基于 MonoBehaviour，需要在 Unity 场景中使用
3. 3D 声音绑定实体后，声音会跟随实体位置移动
4. 声音组音量变化会自动应用到该组内所有正在播放的声音
5. 音频资源需提前通过 YooAsset 打包为 AssetBundle
6. **取消与重启**：模块销毁（`OnDispose`）后 `Token` 取消，在途音频/AudioMixer 加载被中止（句柄释放、抛 `OperationCanceledException`）；`OnInit` 重建 `CancellationScope`（新 Token），重启后可正常使用
