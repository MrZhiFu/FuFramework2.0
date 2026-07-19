# SoundSetting / EntitySetting 迁移配置表 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 SoundSetting.asset 和 EntitySetting.asset 从 ScriptableObject 迁移至 Luban 配置表，与 TbRedDot 一致的模式

**Architecture:** 新增两张 Luban 配置表（TbSoundGroup / TbEntityGroup），通过枚举做主键。SoundModule 和 EntityModule 的 OnInit 从 ConfigModule 读取配置。删除旧的 ScriptableObject 类和 .asset 文件。AudioMixer 路径硬编码。

**Tech Stack:** Luban (Excel→JSON→C#), YooAsset, UniTask

## Global Constraints

- 遵守 `Docs/代码风格规范.md`
- 遵守 `Docs/Git提交规范.md`
- 枚举 ID 模式与 ERedDotKey 一致
- ConfigModule 必须先于 SoundModule/EntityModule 初始化

---

### Task 1: Luban 配置表定义 — 枚举与表结构

**Files:**
- Create: `Config/Excels/Tables/S-SoundGroup-声音组配置表.xlsx`
- Create: `Config/Excels/Tables/E-EntityGroup-实体组配置表.xlsx`
- Modify: `Config/Excels/__enums__.xlsx` (新增 ESoundGroup 和 EEntityGroup 枚举 sheet)
- Modify: `Config/Excels/__tables__.xlsx` (注册新表)

**Interfaces:**
- Produces: `Hotfix.Game.Tables.ESoundGroup` enum, `Hotfix.Game.Tables.EEntityGroup` enum
- Produces: `Hotfix.Game.Tables.SoundGroup` bean, `Hotfix.Game.Tables.TbSoundGroup` table
- Produces: `Hotfix.Game.Tables.EntityGroup` bean, `Hotfix.Game.Tables.TbEntityGroup` table
- Produces: Updated `Hotfix.Game.Tables.TableManager` with new table properties

- [ ] **Step 1: 在 `__enums__.xlsx` 新增两个枚举 sheet**

在 `Config/Excels/__enums__.xlsx` 中新增两个 sheet：

**Sheet 名: `ESoundGroup`**

| name | value | alias | comment |
|------|-------|-------|---------|
| BGM | 1 | BGM | 背景音乐 |
| SFX | 2 | SFX | 音效 |
| UI | 3 | UI | UI音效 |

**Sheet 名: `EEntityGroup`**

| name | value | alias | comment |
|------|-------|-------|---------|
| Player | 1 | Player | 玩家 |
| Enemy | 2 | Enemy | 敌人 |
| NPC | 3 | NPC | NPC |
| Item | 4 | Item | 道具 |
| Effect | 5 | Effect | 特效 |

- [ ] **Step 2: 创建 `S-SoundGroup-声音组配置表.xlsx`**

创建 Excel 文件 `Config/Excels/Tables/S-SoundGroup-声音组配置表.xlsx`：

**Sheet 名: `SoundGroup`**

| Id | Mute | Volume | AgentCount | AllowBeReplacedBySamePriority |
|----|------|--------|------------|-------------------------------|
| BGM | true | 1.0 | 1 | true |
| SFX | true | 1.0 | 10 | true |
| UI | false | 1.0 | 5 | true |

- [ ] **Step 3: 创建 `E-EntityGroup-实体组配置表.xlsx`**

创建 Excel 文件 `Config/Excels/Tables/E-EntityGroup-实体组配置表.xlsx`：

**Sheet 名: `EntityGroup`**

| Id | InstanceAutoReleaseInterval | InstanceCapacity | InstanceExpireTime | InstancePriority |
|----|----------------------------|-----------------|--------------------| -----------------|
| Player | 60 | 16 | 60 | 0 |
| Enemy | 60 | 16 | 60 | 0 |
| NPC | 60 | 16 | 60 | 0 |
| Item | 60 | 16 | 60 | 0 |
| Effect | 60 | 16 | 60 | 0 |

- [ ] **Step 4: 在 `__tables__.xlsx` 注册新表**

在 `Config/Excels/__tables__.xlsx` 的 tables sheet 末尾新增两行：

| name | mode | index | key | value | group | comment |
|------|------|-------|-----|-------|-------|---------|
| TbSoundGroup | map | Id | | SoundGroup | c | 声音组配置表 |
| TbEntityGroup | map | Id | | EntityGroup | c | 实体组配置表 |

- [ ] **Step 5: 运行 Luban 导出脚本**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0\Config"
.\gen-client-json.bat
```

预期：无错误，生成以下文件：
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/ESoundGroup.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/EEntityGroup.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/SoundGroup.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbSoundGroup.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/EntityGroup.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbEntityGroup.cs`
- `TableManager.cs` 已更新，包含 TbSoundGroup 和 TbEntityGroup 属性

- [ ] **Step 6: 验证生成的代码**

检查生成的枚举 `ESoundGroup.cs` 包含 BGM=1, SFX=2, UI=3；`EEntityGroup.cs` 包含 Player=1 到 Effect=5。

检查 `TbSoundGroup` 继承 `BaseDataTable<Tables.SoundGroup>`。

- [ ] **Step 7: Commit**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
git add Config/Excels/Tables/S-SoundGroup-声音组配置表.xlsx
git add Config/Excels/Tables/E-EntityGroup-实体组配置表.xlsx
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/ESoundGroup.cs*
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/EEntityGroup.cs*
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/SoundGroup.cs*
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbSoundGroup.cs*
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/EntityGroup.cs*
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbEntityGroup.cs*
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/TableManager.cs
git add "Unity/Assets/Bundles/Config/tables_tbsoundgroup*"
git add "Unity/Assets/Bundles/Config/tables_tbentitygroup*"
git commit -m "feat: 新增 SoundGroup 和 EntityGroup Luban 配置表定义"
```

---

### Task 2: SoundModule 适配配置表

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.SoundGroup.cs`

**Interfaces:**
- Consumes: `ConfigModule.Instance.GetConfig<TbSoundGroup>()` (Task 1 产出)
- Produces: `SoundGroup.Init(Tables.SoundGroup row)` — 新签名

- [ ] **Step 1: 改造 SoundGroup.Init — 接受 Luban bean 参数**

在 `SoundModule.SoundGroup.cs` 中，修改 `Init` 方法签名和实现：

```csharp
// 旧代码（删除）：
public void Init(SoundGroupInfo soundGroupInfo)
{
    soundGroupInfo.NotNull(nameof(soundGroupInfo));
    Name                          = soundGroupInfo.Name;
    AllowBeReplacedBySamePriority = soundGroupInfo.AllowBeReplacedBySamePriority;
    Volume = soundGroupInfo.Volume;
    Mute   = soundGroupInfo.Mute;
    for (var i = 0; i < soundGroupInfo.AgentCount; i++)
    {
        AddSoundAgentHelper(i);
    }
}

// 新代码：
public void Init(Hotfix.Game.Tables.SoundGroup row)
{
    row.NotNull(nameof(row));
    Name                          = row.Id.ToString();
    AllowBeReplacedBySamePriority = row.AllowBeReplacedBySamePriority;
    Volume = row.Volume;
    Mute   = row.Mute;
    for (var i = 0; i < row.AgentCount; i++)
    {
        AddSoundAgentHelper(i);
    }
}
```

同时移除 using 中不再需要的 `AOT.Framework.ModuleSetting.Runtime.Sound`。

- [ ] **Step 2: 改造 SoundModule.OnInit / AddSoundGroup — 从 ConfigModule 读取配置**

在 `SoundModule.cs` 中，替换配置读取逻辑，硬编码 AudioMixer 路径：

```csharp
// 删除这些行：
using AOT.Framework.ModuleSetting.Runtime.Sound;
using AOT.Framework.ModuleSetting.Runtime;

// 新增 using：
using Hotfix.Game.Tables;
using Hotfix.Framework.Config;

// OnInit 中，删除以下代码：
var soundSetting = ModuleSetting.Instance.SoundSetting;
m_AudioMixer ??= soundSetting.AudioMixer;
foreach (var group in soundSetting.AllGroups)
{
    if (AddSoundGroup(group)) continue;
    FuLogger.LogWarning($"[SoundModule] 添加声音组 '{group.Name}' 失败!");
}

// 替换为：
var tbSoundGroup = ConfigModule.Instance.GetConfig<TbSoundGroup>();
if (tbSoundGroup == null || tbSoundGroup.Count == 0)
{
    FuLogger.LogFatal("[SoundModule] 声音组配置表未加载，SoundModule 初始化失败!");
    return;
}

foreach (var row in tbSoundGroup.All)
{
    if (AddSoundGroup(row)) continue;
    FuLogger.LogWarning($"[SoundModule] 添加声音组 '{row.Id}' 失败!");
}
```

- [ ] **Step 2b: 更新 AddSoundGroup 方法签名**

将 `AddSoundGroup(SoundGroupInfo)` 改为接受 Luban bean：

```csharp
// 旧代码（删除）：
public bool AddSoundGroup(SoundGroupInfo soundGroupInfo)
{
    soundGroupInfo.NotNull(nameof(soundGroupInfo));
    if (HasSoundGroup(soundGroupInfo.Name))
    {
        FuLogger.LogInfo($"[SoundModule]声音组 '{soundGroupInfo.Name}' 已存在，不可重复添加!");
        return false;
    }

    var soundGroupGo = new GameObject($"Sound Group - {soundGroupInfo.Name}");
    soundGroupGo.transform.localScale = Vector3.one;
    var soundGroup = soundGroupGo.GetOrAddComponent<SoundGroup>();
    soundGroup.Init(soundGroupInfo);
    m_SoundGroupDict.Add(soundGroupInfo.Name, soundGroup);
    return true;
}

// 新代码：
public bool AddSoundGroup(Hotfix.Game.Tables.SoundGroup row)
{
    row.NotNull(nameof(row));
    var groupName = row.Id.ToString();
    if (HasSoundGroup(groupName))
    {
        FuLogger.LogInfo($"[SoundModule]声音组 '{groupName}' 已存在，不可重复添加!");
        return false;
    }

    var soundGroupGo = new GameObject($"Sound Group - {groupName}");
    soundGroupGo.transform.localScale = Vector3.one;
    var soundGroup = soundGroupGo.GetOrAddComponent<SoundGroup>();
    soundGroup.Init(row);
    m_SoundGroupDict.Add(groupName, soundGroup);
    return true;
}
```

- [ ] **Step 3: 硬编码 AudioMixer 加载**

在 `SoundModule.OnInit()` 中，将原来的 `m_AudioMixer ??= soundSetting.AudioMixer;` 替换为硬编码路径的异步加载：

```csharp
// TODO: 确认 AudioMixer 实际资源路径，当前使用 GUID 48f2ad666d69da749ae823e0526b2249 对应的路径
private const string AudioMixerAssetPath = "Assets/.../AudioMixer.mixer"; // 替换为实际路径

// 在 OnInit 中：
LoadAudioMixerAsync().Forget();

private async UniTaskVoid LoadAudioMixerAsync()
{
    var handle = await m_AssetModule.LoadAssetAsync<AudioMixer>(AudioMixerAssetPath);
    if (handle.IsDone)
        m_AudioMixer = handle.GetAssetObject<AudioMixer>();
    else
        FuLogger.LogFatal($"[SoundModule] AudioMixer 加载失败: {AudioMixerAssetPath}");
}
```

> **注意:** `AudioMixerAssetPath` 需要在 Unity Editor 中确认实际路径后填入。

- [ ] **Step 4: 确认 HasSoundGroup/GetSoundGroup 等查询方法正常工作**

这些查询方法以 `groupName` (string) 为 key，在迁移后名称由 `ESoundGroup.ToString()` 产生（"BGM", "SFX", "UI"），与原有配置中的名称一致，无需改动。

- [ ] **Step 5: Commit**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.cs
git add Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.SoundGroup.cs
git commit -m "refactor: SoundModule 适配 Luban TbSoundGroup 配置表"
```

---

### Task 3: EntityModule 适配配置表

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Entity/EntityModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Entity/Info/EntityGroup.cs`

**Interfaces:**
- Consumes: `ConfigModule.Instance.GetConfig<TbEntityGroup>()` (Task 1 产出)
- Produces: `EntityGroup(Tables.EntityGroup row, GameObject, ObjectPoolModule)` — 新构造函数签名

- [ ] **Step 1: 改造 EntityGroup 构造函数 — 接受 Luban bean 参数**

在 `Entity/Info/EntityGroup.cs` 中，修改构造函数签名和实现：

```csharp
// 旧代码（删除）：
public EntityGroup(EntityGroupInfo groupSetting, GameObject groupGo, ObjectPoolModule objectPoolModule)
{
    if (groupSetting is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组设置信息为空.");
    if (groupGo is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组GameObject为空.");

    Name    = groupSetting.Name;
    GroupGo = groupGo;

    var poolName = $"Entity Instance Pool ({Name})";
    m_InstancePool = objectPoolModule.CreateObjectPool<EntityInstanceObject>(poolName, groupSetting.InstanceCapacity, groupSetting.InstanceExpireTime, groupSetting.InstancePriority);
    m_InstancePool.AutoReleaseInterval = groupSetting.InstanceAutoReleaseInterval;
    // ...
}

// 新代码：
public EntityGroup(Hotfix.Game.Tables.EntityGroup row, GameObject groupGo, ObjectPoolModule objectPoolModule)
{
    if (row is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组设置信息为空.");
    if (groupGo is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组GameObject为空.");

    Name    = row.Id.ToString();
    GroupGo = groupGo;

    var poolName = $"Entity Instance Pool ({Name})";
    m_InstancePool = objectPoolModule.CreateObjectPool<EntityInstanceObject>(poolName, row.InstanceCapacity, row.InstanceExpireTime, row.InstancePriority);
    m_InstancePool.AutoReleaseInterval = row.InstanceAutoReleaseInterval;
    // ...
}
```

同时移除 using 中不再需要的 `AOT.Framework.ModuleSetting.Runtime.Entity`。

- [ ] **Step 2: 改造 EntityModule.OnInit — 从 ConfigModule 读取配置**

在 `EntityModule.cs` 中，替换配置读取逻辑：

```csharp
// 删除这些行：
using AOT.Framework.ModuleSetting.Runtime.Entity;
using AOT.Framework.ModuleSetting.Runtime;

// 新增 using：
using Hotfix.Game.Tables;
using Hotfix.Framework.Config;

// OnInit 中，删除以下代码：
var setting = ModuleSetting.Instance.EntitySetting;
foreach (var entityGroup in setting.AllGroups)
{
    if (AddEntityGroup(entityGroup)) continue;
    FuLogger.LogWarning($"[EntityModule] 添加实体组 '{entityGroup.Name}' 失败.");
}

// 替换为：
var tbEntityGroup = ConfigModule.Instance.GetConfig<TbEntityGroup>();
if (tbEntityGroup == null || tbEntityGroup.Count == 0)
{
    FuLogger.LogFatal("[EntityModule] 实体组配置表未加载，EntityModule 初始化失败!");
    return;
}

foreach (var row in tbEntityGroup.All)
{
    if (AddEntityGroup(row)) continue;
    FuLogger.LogWarning($"[EntityModule] 添加实体组 '{row.Id}' 失败.");
}
```

- [ ] **Step 3: 改造 AddEntityGroup 方法签名**

在 `EntityModule.cs` 中，将 `AddEntityGroup(EntityGroupInfo)` 改为 `AddEntityGroup(Hotfix.Game.Tables.EntityGroup)`：

```csharp
// 旧签名：public bool AddEntityGroup(EntityGroupInfo entityGroupSetting)
// 新签名：
public bool AddEntityGroup(Hotfix.Game.Tables.EntityGroup row)
{
    if (m_ObjectPoolModule is null) throw new InvalidOperationException("[EntityModule] 增加实体组失败, 请先设置对象池管理模块.");

    var groupName = row.Id.ToString();
    if (HasEntityGroup(groupName))
    {
        FuLogger.LogWarning($"[EntityModule] 添加实体组'{groupName}'失败, 实体组已存在.");
        return false;
    }

    var entityGroupGo = new GameObject($"Entity Group - {groupName}");
    entityGroupGo.transform.SetParent(m_InstanceRoot);
    entityGroupGo.transform.localScale = Vector3.one;
    var entityGroup = new EntityGroup(row, entityGroupGo, m_ObjectPoolModule);
    m_EntityGroupDict.Add(groupName, entityGroup);
    return true;
}
```

- [ ] **Step 4: Commit**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Entity/EntityModule.cs
git add Unity/Assets/Scripts/Hotfix/Framework/Entity/Info/EntityGroup.cs
git commit -m "refactor: EntityModule 适配 Luban TbEntityGroup 配置表"
```

---

### Task 4: ModuleSetting 清理

**Files:**
- Modify: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/ModuleSetting.cs`
- Modify: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/ModuleSettingInspector.cs`

**Interfaces:**
- Removes: `ModuleSetting.SoundSetting` property
- Removes: `ModuleSetting.EntitySetting` property

- [ ] **Step 1: 移除 ModuleSetting.cs 中的 SoundSetting/EntitySetting**

在 `ModuleSetting.cs` 中：

```csharp
// 删除 using：
using AOT.Framework.ModuleSetting.Runtime.Sound;
using AOT.Framework.ModuleSetting.Runtime.Entity;

// 删除字段：
[Header("音频系统配置")]
[SerializeField] private SoundSetting m_SoundSetting;

[Header("实体系统配置")]
[SerializeField] private EntitySetting m_EntitySetting;

// 删除属性：
public SoundSetting SoundSetting => m_SoundSetting;
public EntitySetting EntitySetting => m_EntitySetting;
```

- [ ] **Step 2: 更新 ModuleSettingInspector.cs**

在 `ModuleSettingInspector.cs` 中：

```csharp
// 删除字段声明：
private SerializedProperty m_SoundSetting;
private SerializedProperty m_EntitySetting;

// 删除 OnEnable 中的 FindProperty：
m_SoundSetting  = serializedObject.FindProperty("m_SoundSetting");
m_EntitySetting = serializedObject.FindProperty("m_EntitySetting");

// 删除 OnInspectorGUI 中的 PropertyField：
EditorGUILayout.PropertyField(m_SoundSetting);
EditorGUILayout.PropertyField(m_EntitySetting);
```

- [ ] **Step 3: Commit**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
git add Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/ModuleSetting.cs
git add Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/ModuleSettingInspector.cs
git commit -m "refactor: 移除 ModuleSetting 中的 SoundSetting/EntitySetting 引用"
```

---

### Task 5: 删除旧 ScriptableObject 文件

**Files:**
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundSetting.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundSetting.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundGroupInfo.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundGroupInfo.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntitySetting.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntitySetting.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntityGroupInfo.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntityGroupInfo.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingCreator.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingCreator.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingEditor.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingEditor.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingCreator.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingCreator.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingEditor.cs`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingEditor.cs.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/SoundSetting.asset`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/SoundSetting.asset.meta`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/EntitySetting.asset`
- Delete: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/EntitySetting.asset.meta`

- [ ] **Step 1: 删除旧文件**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
# Runtime
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundSetting.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundSetting.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundGroupInfo.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Sound/SoundGroupInfo.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntitySetting.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntitySetting.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntityGroupInfo.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Entity/EntityGroupInfo.cs.meta"
# Editor
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingCreator.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingCreator.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingEditor.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Sound/SoundSettingEditor.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingCreator.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingCreator.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingEditor.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Entity/EntitySettingEditor.cs.meta"
# Assets
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/SoundSetting.asset"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/SoundSetting.asset.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/EntitySetting.asset"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/EntitySetting.asset.meta"
```

- [ ] **Step 2: Commit**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
git add -A
git commit -m "refactor: 删除旧的 SoundSetting/EntitySetting ScriptableObject 文件"
```

---

### Task 6: 验证

- [ ] **Step 1: 在 Unity Editor 中打开项目，检查编译是否通过**

预期：无编译错误。

- [ ] **Step 2: 进入 Play 模式，检查 SoundModule 初始化**

观察 Console 日志：
- 无 Fatal/Error 日志
- `[SoundModule]` 相关日志正常
- BGM/SFX/UI 三个声音组创建成功

- [ ] **Step 3: 检查 EntityModule 初始化**

观察 Console 日志：
- 无 Fatal/Error 日志
- Player/Enemy/NPC/Item/Effect 五个实体组创建成功

- [ ] **Step 4: 检查场景中 ModuleSetting GameObject**

Inspector 中 Sound Setting 和 Entity Setting 字段已消失（变为 None 或不再显示），不影响其他模块配置。

- [ ] **Step 5: 功能冒烟测试**

- 播放一个声音（如 UI 点击音效），确认能正常播放
- 创建一个实体，确认能正常显示

- [ ] **Step 6: 如有问题，修复后重新验证**

---

### Task 7: AudioMixer 路径确认（最终修复）

> **前置条件:** 在 Unity Editor 中找到当前使用的 AudioMixer 文件路径

- [ ] **Step 1: 在 Unity Editor 中找到 AudioMixer 文件的 Assets 相对路径**

例如：`Assets/Art/Audio/AudioMixer.mixer`

- [ ] **Step 2: 更新 SoundModule.cs 中的硬编码路径**

```csharp
// 将 TODO 注释替换为实际路径
private const string AudioMixerAssetPath = "Assets/Art/Audio/AudioMixer.mixer";
```

- [ ] **Step 3: 验证 Play 模式 AudioMixer 加载成功**

- [ ] **Step 4: Commit**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.cs
git commit -m "fix: 填入 AudioMixer 实际资源路径"
```
