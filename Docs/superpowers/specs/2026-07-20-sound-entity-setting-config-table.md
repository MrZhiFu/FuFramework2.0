# SoundSetting / EntitySetting 迁移配置表设计

## 概述

将 `SoundSetting.asset` 和 `EntitySetting.asset` 从 Unity ScriptableObject 迁移至 Luban 配置表系统，与红点 `TbRedDot` 保持一致的架构模式。

## 数据模型

### 新增枚举（定义于 `__enums__.xlsx`）

```csharp
// 声音组枚举
enum ESoundGroup
{
    BGM = 1,
    SFX = 2,
    UI = 3,
}

// 实体组枚举
enum EEntityGroup
{
    Player = 1,
    Enemy = 2,
    NPC = 3,
    Item = 4,
    Effect = 5,
}
```

### TbSoundGroup（声音组配置表，Excel：`S-SoundGroup-声音组配置表.xlsx`）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | ESoundGroup (enum, 主键) | 声音组标识 |
| Mute | bool | 默认静音 |
| Volume | float | 默认音量 (0-1) |
| AgentCount | int | 播放代理数 |
| AllowBeReplacedBySamePriority | bool | 是否允许被同优先级替换 |

### TbEntityGroup（实体组配置表，Excel：`E-EntityGroup-实体组配置表.xlsx`）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | EEntityGroup (enum, 主键) | 实体组标识 |
| InstanceAutoReleaseInterval | float | 对象池自动释放间隔(秒) |
| InstanceCapacity | int | 对象池容量 |
| InstanceExpireTime | float | 对象池对象过期时间(秒) |
| InstancePriority | int | 对象池优先级 |

## 代码改动

### 1. SoundModule.cs

- `OnInit()` 不再引用 `ModuleSetting.Instance.SoundSetting`
- 改为从 `ConfigModule.Instance.GetConfig<TbSoundGroup>()` 获取配置
- AudioMixer 路径硬编码在 SoundModule 中，通过 YooAsset 加载

```csharp
// 原代码：
var soundSetting = ModuleSetting.Instance.SoundSetting;
m_AudioMixer ??= soundSetting.AudioMixer;
foreach (var group in soundSetting.AllGroups) { ... }

// 新代码：
m_AudioMixer ??= LoadAudioMixer(); // 硬编码路径加载
var tbSoundGroup = ConfigModule.Instance.GetConfig<TbSoundGroup>();
foreach (var group in tbSoundGroup.All) { ... }
```

### 2. EntityModule.cs

- `OnInit()` 不再引用 `ModuleSetting.Instance.EntitySetting`
- 改为从 `ConfigModule.Instance.GetConfig<TbEntityGroup>()` 获取配置

```csharp
// 原代码：
var setting = ModuleSetting.Instance.EntitySetting;
foreach (var entityGroup in setting.AllGroups) { ... }

// 新代码：
var tbEntityGroup = ConfigModule.Instance.GetConfig<TbEntityGroup>();
foreach (var row in tbEntityGroup.All) { ... }
```

### 3. ModuleSetting.cs

- 移除 `SoundSetting m_SoundSetting` 字段和 `SoundSetting` 属性
- 移除 `EntitySetting m_EntitySetting` 字段和 `EntitySetting` 属性

### 4. 清理旧文件

**AOT 层删除：**
- `ModuleSetting/Runtime/Sound/SoundSetting.cs`
- `ModuleSetting/Runtime/Sound/SoundGroupInfo.cs`
- `ModuleSetting/Runtime/Entity/EntitySetting.cs`
- `ModuleSetting/Runtime/Entity/EntityGroupInfo.cs`
- `ModuleSetting/Editor/Sound/SoundSettingCreator.cs`
- `ModuleSetting/Editor/Sound/SoundSettingEditor.cs`
- `ModuleSetting/Editor/Entity/EntitySettingCreator.cs`
- `ModuleSetting/Editor/Entity/EntitySettingEditor.cs`
- `ModuleSetting/SettingAssets/SoundSetting.asset`
- `ModuleSetting/SettingAssets/SoundSetting.asset.meta`
- `ModuleSetting/SettingAssets/EntitySetting.asset`
- `ModuleSetting/SettingAssets/EntitySetting.asset.meta`

## 初始化顺序

SoundModule 和 EntityModule 的 `OnInit` 依赖 `ConfigModule` 已完成初始化且配置表已加载完毕。需确保初始化顺序：

```
ConfigModule.OnInit() → TableManager.LoadAsync() → SoundModule.OnInit() / EntityModule.OnInit()
```

SoundModule 和 EntityModule 在 `OnInit` 中需校验 `ConfigModule.Instance.GetConfig<TbXxx>()` 非空，配置表未就绪时记录 Fatal 日志并提前返回。

## Luban 生成流程

1. 在 `__enums__.xlsx` 新增 `ESoundGroup` 和 `EEntityGroup` 枚举
2. 在 `Config/Excels/Tables/` 下新建两个 Excel 文件
3. 更新 `__tables__.xlsx` 注册新表
4. 运行 Luban 导出脚本，自动生成：
   - `Tables/ESoundGroup.cs`、`Tables/EEntityGroup.cs`（枚举）
   - `Tables/SoundGroup.cs`、`Tables/TbSoundGroup.cs`（声音组配置表）
   - `Tables/EntityGroup.cs`、`Tables/TbEntityGroup.cs`（实体组配置表）
   - 更新 `TableManager.cs` 自动注册新表

## 注意事项

1. SoundGroup/EntityGroup 的运行时类（`Hotfix.Framework.Sound.SoundModule.SoundGroup`、`Hotfix.Framework.Entity.EntityGroup`）命名与 Luban 生成的 bean 类在**不同命名空间**，无冲突
2. 原有 `Sound.cs`（音频资源表）与新增 `SoundGroup.cs`（声音组配置表）是两张不同的表，职责不同
3. 旧的 `.asset` 文件删除后，场景中 `ModuleSetting` GameObject 上对应的 Inspector 引用会自动变为 None
