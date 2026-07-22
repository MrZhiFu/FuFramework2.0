# AssetSetting/StorageSetting 内嵌到 GameSetting

## 目标

移除 `AssetSetting` 和 `StorageSetting` 两个 ScriptableObject，将其字段直接内嵌到 `GameSetting` MonoBehaviour 中，在 GameSetting Inspector 面板中统一编辑。

## 改动

### GameSetting.cs 新增字段（`[Header]` 分组）

```csharp
// 资源系统配置（原 AssetSetting）
[Header("资源系统配置")]
[SerializeField] private EPlayMode m_PlayMode = EPlayMode.EditorSimulateMode;
[SerializeField] private string m_DefaultPackageName = "DefaultPackage";
[SerializeField] private int m_DownloadingMaxNum = 10;
[SerializeField] private int m_FailedTryAgainNum = 3;
[SerializeField] private int m_AsyncSystemMaxSlicePerFrame = 30;
[SerializeField] private string m_ResCdnRootURL = "http://localhost:8080/CDN/";

// 本地数据存储配置（原 StorageSetting）
[Header("本地数据存储系统配置")]
[SerializeField] private bool m_EnableAutoSave = true;
[SerializeField] private float m_AutoSaveInterval = 300f;
[SerializeField] private bool m_EnableEncrypt = false;
[SerializeField] private string m_EncryptKey = "FuFrameworkStorageKey";
```

每个字段添加只读属性（与原 SO 一致）。

### 删除

- `AssetSetting.cs`、`AssetSetting.asset`
- `StorageSetting.cs`、`StorageSetting.asset`
- `AssetSettingCreator.cs`、`StorageSettingCreator.cs`
- `AssetSettingEditor.cs`、`StorageSettingEditor.cs`

### 引用更新（4 文件）

- `BootstrapProcess.cs` — `GameSetting.Instance.AssetSetting.Xxx` → `GameSetting.Instance.Xxx`
- `BootstrapAssetHelper.cs` — 同上
- `AssetModule.cs` — 同上，去掉 null 检查
- `StorageModule.cs` — 同上

## 决策

- `AssetSetting` / `StorageSetting` 无独立复用场景，作为 ScriptableObject 是过度抽象
- 与 Guide 迁移模式一致：去掉中间层，字段直接挂 MonoBehaviour
