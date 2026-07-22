# AssetSetting/StorageSetting 内嵌到 GameSetting 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 移除 AssetSetting/StorageSetting ScriptableObject，字段直接内嵌到 GameSetting MonoBehaviour

**Architecture:** 将两个 SO 的字段展平到 GameSetting 中，`[Header]` 分组，删掉独立 .asset 和 Creator/Editor 脚本

**Tech Stack:** Unity + C#

## Global Constraints

- 遵循 `Docs/代码风格规范.md`
- 遵循 `Docs/Git提交规范.md`
- 字段属性名与原 SO 保持一致
- 默认值与原 SO 保持一致

---

### Task 1: 修改 GameSetting.cs + 删除旧文件

**文件：**
- 修改: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/GameSetting.cs`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Asset/AssetSetting.cs` + `.meta`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/DataSave/StorageSetting.cs` + `.meta`
- 删除: `Unity/Assets/Editor/FuFramework/ModuleSetting/Asset/AssetSettingCreator.cs` + `.meta`
- 删除: `Unity/Assets/Editor/FuFramework/ModuleSetting/Asset/AssetSettingEditor.cs` + `.meta`
- 删除: `Unity/Assets/Editor/FuFramework/ModuleSetting/Storage/StorageSettingCreator.cs` + `.meta`
- 删除: `Unity/Assets/Editor/FuFramework/ModuleSetting/Storage/StorageSettingEditor.cs` + `.meta`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/AssetSetting.asset` + `.meta`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/StorageSetting.asset` + `.meta`

**产生：** GameSetting 直接持有原 SO 所有字段

- [ ] **Step 1: 删除旧文件**

```bash
rm -v "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Asset/AssetSetting.cs" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Asset/AssetSetting.cs.meta" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/DataSave/StorageSetting.cs" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/DataSave/StorageSetting.cs.meta" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Asset/AssetSettingCreator.cs" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Asset/AssetSettingCreator.cs.meta" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Asset/AssetSettingEditor.cs" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Asset/AssetSettingEditor.cs.meta" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Storage/StorageSettingCreator.cs" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Storage/StorageSettingCreator.cs.meta" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Storage/StorageSettingEditor.cs" \
       "Unity/Assets/Editor/FuFramework/ModuleSetting/Storage/StorageSettingEditor.cs.meta" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/AssetSetting.asset" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/AssetSetting.asset.meta" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/StorageSetting.asset" \
       "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/StorageSetting.asset.meta"
```

- [ ] **Step 2: 修改 GameSetting.cs — 移除旧 using 和 SO 字段/属性**

删除 `using` 引用：
```csharp
// 删除这两行：
using AOT.Framework.ModuleSetting.Runtime.Asset;
using AOT.Framework.ModuleSetting.Runtime.DataSave;
```

新增 `using YooAsset;`（EPlayMode 需要）。

删除旧的 SO 字段和属性：
```csharp
// 删除：
[Header("资源系统配置")]
[SerializeField] private AssetSetting m_AssetSetting;

[Header("本地数据存储系统配置")]
[SerializeField] private StorageSetting m_StorageSetting;

public AssetSetting AssetSetting => m_AssetSetting;
public StorageSetting StorageSetting => m_StorageSetting;
```

- [ ] **Step 3: 添加内嵌字段到 GameSetting.cs**

在 `m_OpenGuide` 之后、`m_GameSpeedBeforePause` 之前添加：

```csharp
        #region 资源系统配置

        [Header("资源系统配置")]
        [SerializeField] private EPlayMode m_PlayMode = EPlayMode.EditorSimulateMode;

        [SerializeField] private string m_DefaultPackageName = "DefaultPackage";

        [SerializeField] private int m_DownloadingMaxNum = 10;

        [SerializeField] private int m_FailedTryAgainNum = 3;

        [SerializeField] private int m_AsyncSystemMaxSlicePerFrame = 30;

        [SerializeField] private string m_ResCdnRootURL = "http://localhost:8080/CDN/";

        public EPlayMode PlayMode => m_PlayMode;

        public string DefaultPackageName => m_DefaultPackageName;

        public int DownloadingMaxNum => m_DownloadingMaxNum;

        public int FailedTryAgainNum => m_FailedTryAgainNum;

        public int AsyncSystemMaxSlicePerFrame => m_AsyncSystemMaxSlicePerFrame;

        public string ResCdnRootRootURL => m_ResCdnRootURL;

        #endregion

        #region 本地数据存储系统配置

        [Header("本地数据存储系统配置")]
        [SerializeField] private bool m_EnableAutoSave = true;

        [SerializeField] private float m_AutoSaveInterval = 300f;

        [SerializeField] private bool m_EnableEncrypt = false;

        [SerializeField] private string m_EncryptKey = "FuFrameworkStorageKey";

        public bool EnableAutoSave => m_EnableAutoSave;

        public float AutoSaveInterval => m_AutoSaveInterval;

        public bool EnableEncrypt => m_EnableEncrypt;

        public string EncryptKey => m_EncryptKey;

        #endregion
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: AssetSetting/StorageSetting 字段内嵌到 GameSetting，删除独立 SO"
```

---

### Task 2: 更新调用方引用

**文件：**
- 修改: `Unity/Assets/Scripts/AOT/Bootstrap/BootstrapProcess.cs`
- 修改: `Unity/Assets/Scripts/AOT/Bootstrap/BootstrapAssetHelper.cs`
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Storage/StorageModule.cs`

**产生：** 4 个文件展平链式访问

- [ ] **Step 1: 更新 BootstrapProcess.cs**

```csharp
// Line 47:
// 旧: var playMode = GameSetting.Instance.AssetSetting.PlayMode;
// 新:
var playMode = GameSetting.Instance.PlayMode;

// Line 111-112:
// 旧: var assetSetting = GameSetting.Instance.AssetSetting;
//     var configUrl = $"{assetSetting.ResCdnRootRootURL}...
// 新:
var configUrl = $"{GameSetting.Instance.ResCdnRootRootURL}{UtilityAOT.Application.PlatformName}/{RemoteUpdateConfigName}";
```

- [ ] **Step 2: 更新 BootstrapAssetHelper.cs**

```csharp
// Line 52-57:
// 旧: var assetSetting = GameSetting.Instance.AssetSetting;
//     PlayMode = assetSetting.PlayMode;
//     DefaultPackageName = assetSetting.DefaultPackageName;
//     ...
// 新:
PlayMode            = GameSetting.Instance.PlayMode;
DefaultPackageName  = GameSetting.Instance.DefaultPackageName;
m_DownloadingMaxNum = GameSetting.Instance.DownloadingMaxNum;
m_FailedTryAgainNum = GameSetting.Instance.FailedTryAgainNum;
// Line 63:
// 旧: YooAssets.SetAsyncOperationMaxTimeSlice(assetSetting.AsyncSystemMaxSlicePerFrame);
// 新:
YooAssets.SetAsyncOperationMaxTimeSlice(GameSetting.Instance.AsyncSystemMaxSlicePerFrame);
```

- [ ] **Step 3: 更新 AssetModule.cs**

```csharp
// Line 61-68:
// 旧: var assetSetting = GameSetting.Instance.AssetSetting;
//     if (!assetSetting) throw new InvalidOperationException(...);
//     PlayMode = assetSetting.PlayMode;
//     ...
// 新: 直接访问，无需 null 检查（字段内嵌不会为 null）
PlayMode                    = GameSetting.Instance.PlayMode;
DefaultPackageName          = GameSetting.Instance.DefaultPackageName;
DownloadingMaxNum           = GameSetting.Instance.DownloadingMaxNum;
FailedTryAgainNum           = GameSetting.Instance.FailedTryAgainNum;
AsyncSystemMaxSlicePerFrame = GameSetting.Instance.AsyncSystemMaxSlicePerFrame;
```

- [ ] **Step 4: 更新 StorageModule.cs**

```csharp
// Line 74-78:
// 旧: var storageSetting = GameSetting.Instance.StorageSetting;
//     m_EnableAutoSave = storageSetting.EnableAutoSave;
//     ...
// 新:
m_EnableAutoSave   = GameSetting.Instance.EnableAutoSave;
m_AutoSaveInterval = GameSetting.Instance.AutoSaveInterval;
m_EnableEncryption = GameSetting.Instance.EnableEncrypt;
m_EncryptKey       = GameSetting.Instance.EncryptKey;
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor: 展平 AssetSetting/StorageSetting 链式访问为直接属性"
```
