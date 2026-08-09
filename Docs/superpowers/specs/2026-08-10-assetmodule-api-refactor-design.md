# AssetModule API 提取与接口清理设计

> 日期：2026-08-10
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/Asset/` + `Unity/Assets/Editor/FuFramework/Assets/Inspector/`

## 1. 背景与问题

Asset 模块（基于 YooAsset 的资源管理模块）当前把**公开 API、私有状态、生命周期、初始化辅助**全部堆在单个 `AssetModule.cs`（约 725 行）中，结构与 ReferencePool 模块已完成的「`ReferencePoolModule.cs` + `ReferencePoolModule.API.cs`」拆分模式不一致。且公开面未经审计，存在一批零调用死接口。

具体问题：

**① 结构问题**
- `AssetModule.cs` 单文件混合 public/private/生命周期，无 `AssetModule.API.cs` 分部，与 ReferencePool 模块模式不一致。
- `AssetModule.Initialization.cs` 命名有歧义：装的是**资源包初始化模式**分支逻辑（`InitPackage` 按 `EPlayMode` 分发），而非模块初始化（`OnInit` 在主文件）。
- 空壳 `AssetModuleInspector.cs` 全部逻辑被注释，顶部 TODO「后续考虑使用单独的调试界面去显示模块数据」，但该调试界面已确定不做（YooAsset 自带调试窗口）。

**② 接口冗余**
经全库（`Unity/` 内全部 .cs）外部调用审计，25 个 public 接口/成员零调用，属死接口：
- 纯 YooAsset 透传：`LoadAllAssetsAsync`×4、`LoadSubAssetsAsync`×4、`LoadRawFileAsync`×2、`GetAssetInfos`×2、`CreateResourceDownloader`、`HasPackage`、`GetPackage`。
- AssetInfo 重载无人用：`LoadAssetAsync(AssetInfo)`、`LoadSceneAsync(AssetInfo,…)`。
- 卸载/清理变体无人用：`UnloadAsset(package,path)` 双参、`UnloadUnusedAssetsAsync`、`ClearAllBundleFilesAsync`、`ClearUnusedBundleFilesAsync`。
- 查询无人用：`IsNeedDownload`×2。
- 属性 ×5 无外部读取者（唯一读取方是已删除的 Inspector 注释代码）；其中 `DownloadingMaxNum`/`FailedTryAgainNum` 在 `CreateResourceDownloader` 删除后连内部读取者也没有，成孤儿。

## 2. 目标

1. **提取 public API**：新增 `AssetModule.API.cs` 分部，全部公开成员移入，对齐 ReferencePool 模式。
2. **清理死接口**：删除零调用接口，收窄无外部读取者成员为 private。
3. **消除歧义命名**：`AssetModule.Initialization.cs` → `AssetModule.InitPackageMode.cs`。
4. **清理死代码**：删除空壳 `AssetModuleInspector.cs`。
5. **文档同步**：README 接口清单与增减保持一致。

### 2.1 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| 调试窗口 `AssetModuleWindow` | **不做** | YooAsset 自带调试窗口，避免重复造轮子 |
| 旧 `AssetModuleInspector` | **删除** | 空壳死代码，TODO 指向已放弃的调试界面 |
| `InstantiateAsync` / `ReleaseInstantiate` | **保留 public** | 框架能力（引用计数/并发去重/失败回滚），README 文档化，AssetLoadRegister 只是便捷封装 |
| 包初始化组（`InitDefaultPackageAsync`/`InitPackageAsync`/`CreatePackage`/`TryGetPackage`） | **保留 public** | 热更侧扩展非默认包的入口；`InitPackageMode.cs` 因此保留 |
| 属性 ×5 | `PlayMode`/`DefaultPackageName`/`AsyncSystemMaxSlicePerFrame` 改 private；`DownloadingMaxNum`/`FailedTryAgainNum` **删除** | 前者内部在用（`InitPackage` 分发、包访问、时间片配置）；后者唯一读取方 `CreateResourceDownloader` 已删，成孤儿 |
| `GetDefaultPackage` / `UnloadAllAssetsAsync` | **改 private** | 内部大量使用（加载封装）/ OnDispose 调用，无外部调用方 |
| 死接口删除 | **全部删除** | 纯透传或无人用，YooAsset 直连 API 可随时加回 |

## 3. 目标架构

```
Framework/Asset/
├── AssetModule.cs              # 私有字段/嵌套类 + 生命周期(OnInit/OnDispose) + 私有辅助(CreateHandleTask/TryGetReadyPackage)
├── AssetModule.API.cs          # [新增] 全部 public 成员（partial class AssetModule）
├── AssetModule.InitPackageMode.cs  # [改名] 资源包初始化模式私有方法（原 AssetModule.Initialization.cs）
├── AssetLoadRegister.cs        # 不变
├── RemoteServices.cs           # 不变（已为独立类）
└── README.md                   # [更新] 同步接口增减
```

删除 `Unity/Assets/Editor/FuFramework/Assets/Inspector/AssetModuleInspector.cs` + `.meta`。

### 3.1 重构后 public API 清单（13 个，移入 `AssetModule.API.cs`）

```csharp
public partial class AssetModule : ModuleBase
{
    // 异步加载
    public UniTask<AssetHandle> LoadAssetAsync(string path);
    public UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object;
    public UniTask<AssetHandle> LoadAssetAsync(string path, Type type);

    // 异步加载场景（仅 string 重载，AssetInfo 重载已删）
    public UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true);

    // 异步实例化（引用计数）
    public async UniTask<GameObject> InstantiateAsync(string path);
    public void ReleaseInstantiate(string path);

    // 资源包初始化
    public UniTask<bool> InitDefaultPackageAsync(string downloadURL = null, string downloadBackupURL = null);
    public UniTask<bool> InitPackageAsync(string packageName, string downloadURL = null, string downloadBackupURL = null);
    public ResourcePackage CreatePackage(string packageName);
    public ResourcePackage TryGetPackage(string packageName);

    // 卸载
    public void UnloadAsset(string assetPath);

    // 查询
    public AssetInfo GetAssetInfo(string path);
    public bool HasAssetPath(string path);
}
```

### 3.2 删除项（25 个，含重载与属性）

| 删除 | 原因 |
|---|---|
| `LoadAssetAsync(AssetInfo)` | 零调用（AssetInfo 重载仅 string 重载在用） |
| `LoadAllAssetsAsync` ×4（`<T>`/`(string,Type)`/`(string)`/`(AssetInfo)`） | 零调用 |
| `LoadSubAssetsAsync` ×4 | 零调用 |
| `LoadRawFileAsync` ×2（`(AssetInfo)`/`(string)`） | 零调用（AOT 侧用 `LaunchAssetHelper.LoadDllBytesAsync` 直连包加载原生文件） |
| `LoadSceneAsync(AssetInfo, …)` | 零调用（SceneModule 用 string 重载） |
| `HasPackage` | 零调用 |
| `GetPackage` | 零调用（内部用 `GetDefaultPackage`） |
| `CreateResourceDownloader` | 零调用（AOT 侧 `LaunchAssetHelper.CreateDownloader` 直连包创建下载器） |
| `UnloadAsset(string packageName, string assetPath)` | 零调用（1 参重载在用） |
| `UnloadUnusedAssetsAsync` | 零调用 |
| `ClearAllBundleFilesAsync` | 零调用 |
| `ClearUnusedBundleFilesAsync` | 零调用 |
| `IsNeedDownload` ×2 | 零调用（下载流程走 LaunchAssetHelper） |
| `GetAssetInfos` ×2 | 零调用 |
| 属性 `DownloadingMaxNum` / `FailedTryAgainNum` | 唯一读取方 `CreateResourceDownloader` 已删，成孤儿（`GameSetting` 配置仍由 AOT `LaunchAssetHelper` 消费） |

### 3.3 收窄为 private 项

| 成员 | 原因 |
|---|---|
| 属性 `PlayMode` | `InitPackageMode`（原 Initialization）的 `InitPackage` switch 分发内部读取 |
| 属性 `DefaultPackageName` | 内部大量使用（`GetDefaultPackage`/`UnloadAsset`/`InitPackageMode` 等） |
| 属性 `AsyncSystemMaxSlicePerFrame` | `OnInit` 设置 `SetAsyncOperationMaxTimeSlice` 使用 |
| `GetDefaultPackage()` | 内部大量使用（加载/查询封装），无外部调用方 |
| `UnloadAllAssetsAsync(string)` | `OnDispose` 内部调用，无外部调用方 |

## 4. 迁移要点

- **partial 共享私有状态**：`InstantiateAsync`/`ReleaseInstantiate`/`InitPackageAsync` 等移出后仍可直接访问主文件的 `m_InstantiateRefDict`/`m_InitPackageTasks` 等字段，无需改动实现。
- **usings 按需重分布**：`AssetModule.API.cs` 补齐 `YooAsset`/`Cysharp.Threading.Tasks`/`UnityEngine`/`UnityEngine.SceneManagement`/`Hotfix.Framework.Core` 等；主文件保留 `AOT.Launch`/`AOT.Framework.ModuleSetting.Runtime`/`AOT.Framework.Core.Log` 等初始化相关；双方清理未使用 using。
- **#region 原样搬运**：保留的加载/场景/实例化/资源包/卸载/Get 区域整体迁移，结构与注释不变。
- **`AssetModule.API.cs` 头部文档注释**：仿 ReferencePool（`/// <summary> 资源管理模块的公共 API。… </summary>`）。
- **重命名 `AssetModule.Initialization.cs` → `AssetModule.InitPackageMode.cs`**：同步改名 `.meta`（保留 GUID），更新文件内类文档注释（`/// 初始化加载模式` → `/// 资源包初始化模式`）。
- **删除 Inspector**：`AssetModuleInspector.cs` + `.meta`。
- 新增 `AssetModule.API.cs` 的 `.meta` 由 Unity 生成后提交。

## 5. README 更新

同步 AssetModule README 接口清单：

- **删除**：`LoadAllAssetsAsync`/`LoadSubAssetsAsync`/`LoadRawFileAsync`（含 AssetInfo 重载）、`SetDefaultPackage`（已过期，代码中已不存在）、`CreateResourceDownloader`、`GetAssetInfos`、`IsNeedDownload`、`UnloadAsset(package,path)` 双参、`UnloadUnusedAssetsAsync`/`ClearAllBundleFilesAsync`/`ClearUnusedBundleFilesAsync`、属性表中 `DownloadingMaxNum`/`FailedTryAgainNum`。
- **保留**：`LoadAssetAsync`（3 个 string 重载）、`LoadSceneAsync`（string 重载）、`InstantiateAsync`/`ReleaseInstantiate`、包初始化组、`UnloadAsset`（1 参）、`GetAssetInfo`/`HasAssetPath`。
- 属性表更新为 private 属性（`PlayMode`/`DefaultPackageName`/`AsyncSystemMaxSlicePerFrame`）或删除。

## 6. 验证方式

1. unity-cli 触发 Unity 编译，无错误。
2. 编辑器 Play 冒烟（框架正常启动、资源加载路径可用）。
3. 复核已删接口全局无调用残留（`LoadAllAssetsAsync`/`LoadSubAssetsAsync`/`LoadRawFileAsync`/`IsNeedDownload`/`GetAssetInfos` 等）。

## 7. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`[AI]refactor: AssetModule 提取 public API 至 AssetModule.API，清理零调用接口，收窄私有，重命名 InitPackageMode`（代码重构 + 删除 Inspector，必须同落保证编译通过）。
- **Commit 2**：`[AI]docs: 同步 AssetModule README 接口清单`。
