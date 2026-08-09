# AssetModule API 提取与接口清理 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 AssetModule 的 public API 提取到 `AssetModule.API.cs` 分部文件，删除 25 个零调用死接口，无外部读取者成员收窄为 private，并重命名初始化文件、删除空 Inspector、同步 README。

**Architecture:** 参考 ReferencePool 模块已完成的「`ReferencePoolModule.cs`（私有状态+生命周期）+ `ReferencePoolModule.API.cs`（全部 public）」partial 拆分模式。纯重构，保留接口签名与行为完全不变（仅位置迁移与可见性收窄）。

**Tech Stack:** C# (Hotfix / HybridCLR)、Unity、YooAsset、UniTask、unity-cli（编译验证）。

## Global Constraints

- **编译门禁**：每个代码任务结束必须经 unity-cli 编译**零错误**。前置检查：`unity-cli system ping` 返回 `"pong"`（若未返回，请用户先打开 Unity 项目）。具体编译触发命令以 `unity-cli tool list` 列出的可用工具为准。
- **行为不变**：保留的 13 个 public 接口签名与实现**逐字迁移**，禁止改动逻辑。
- **partial 共享**：`AssetModule` 是 partial class，API 文件可无障碍访问主文件的私有字段（`m_InstantiateRefDict`/`m_InitPackageTasks` 等）与私有方法（`GetDefaultPackage`/`CreateHandleTask`）。
- **提交规范**：遵循 `Docs/Git提交规范.md`，消息格式 `[AI]refactor: ...` / `[AI]docs: ...`。
- **代码风格**：遵循 `Docs/代码风格规范.md`（注释用 `/// <summary>`、方法 PascalCase）。
- **交流语言**：代码注释与提交信息使用中文。

**当前文件基线**（重构前）：
- `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`（725 行，public/private/生命周期混合）
- `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.Initialization.cs`（私有初始化模式，待改名）
- `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.Initialization.cs.meta`
- `Unity/Assets/Editor/FuFramework/Assets/Inspector/AssetModuleInspector.cs` + `.meta`（空壳，待删）
- `Unity/Assets/Scripts/Hotfix/Framework/Asset/README.md`

---

### Task 1: 重命名 `AssetModule.Initialization.cs` → `AssetModule.InitPackageMode.cs`

**Files:**
- Rename: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.Initialization.cs` → `AssetModule.InitPackageMode.cs`
- Rename: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.Initialization.cs.meta` → `AssetModule.InitPackageMode.cs.meta`
- Modify: `AssetModule.InitPackageMode.cs` 内类文档注释（第 8 行）

**Interfaces:**
- Consumes: 无（纯文件改名，partial class 名不变）
- Produces: 新文件名 `AssetModule.InitPackageMode.cs`，内容与类名不变

该 partial 装的不是模块初始化（`OnInit` 在主文件），而是**资源包初始化模式**分支逻辑（`InitPackage` 按 `EPlayMode` 分发到 `InitInEditorSimulateMode`/`InitInOfflinePlayMode`/`InitInHostPlayMode`/`InitInWebPlayMode`），改名消除歧义。

- [ ] **Step 1: 用 git mv 改名文件与 meta**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.Initialization.cs" "Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.InitPackageMode.cs"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.Initialization.cs.meta" "Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.InitPackageMode.cs.meta"
```

> 用 `git mv` 保证 `.meta` 内容（含 GUID）原样保留，Unity 不重新生成 GUID。

- [ ] **Step 2: 更新类文档注释**

在 `AssetModule.InitPackageMode.cs` 中，把类文档注释由「初始化加载模式」改为「资源包初始化模式」：

```csharp
    /// <summary>
    /// 资源包初始化模式。
    /// 根据运行模式（EditorSimulate/Offline/Host/Web）初始化资源包的文件系统与参数。
    /// </summary>
```

（原第 8 行为 `/// <summary>\n    /// 初始化加载模式\n    /// </summary>`，仅替换说明文字，保留其余注释。）

- [ ] **Step 3: 编译验证**

经 unity-cli 触发 Unity 编译。预期 0 错误（partial 类名未变，纯文件改名不影响编译）。若报找不到 `AssetModule`，检查是否误改类名。

- [ ] **Step 4: 提交**

```bash
git add -A "Unity/Assets/Scripts/Hotfix/Framework/Asset/"
git commit -m "[AI]refactor: AssetModule 初始化文件重命名为 InitPackageMode，明确资源包初始化模式语义"
```

---

### Task 2: 提取 public API 至 `AssetModule.API.cs`，删除死接口，收窄私有

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.API.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`

**Interfaces:**
- Consumes: `AssetModule.cs` 保留的私有成员（字段 `m_InstantiateRefDict`/`m_InstantiateLoadingTasks`/`m_IsDisposed`/`m_InitedPackageSet`/`m_InitPackageTasks`、嵌套类 `InstantiateRef`、私有方法 `GetDefaultPackage()`/`CreateHandleTask()`/`TryGetReadyPackage()`、私有属性 `PlayMode`/`DefaultPackageName`/`AsyncSystemMaxSlicePerFrame`）
- Produces: 新的 `AssetModule.API.cs`，含 13 个 public 成员（签名见下方清单）

**迁移总览（3 类操作，行号基于重构前 `AssetModule.cs`）：**

**① 移入 `AssetModule.API.cs`（13 个 public，逐字迁移，body 一字不改）：**

| 成员 | 原行号 |
|---|---|
| `LoadAssetAsync(string)` | 193-194 |
| `LoadAssetAsync<T>(string)` | 202-203 |
| `LoadAssetAsync(string, Type)` | 211-212 |
| `LoadSceneAsync(string, LoadSceneMode, bool)` | 266-267 |
| `InstantiateAsync(string)` | 346-406 |
| `ReleaseInstantiate(string)` | 414-427 |
| `InitDefaultPackageAsync(string, string)` | 439-442 |
| `InitPackageAsync(string, string, string)` | 451-508 |
| `CreatePackage(string)` | 515 |
| `TryGetPackage(string)` | 522-526 |
| `UnloadAsset(string)` | 575-581 |
| `GetAssetInfo(string)` | 709 |
| `HasAssetPath(string)` | 717-721 |

**② 删除（25 个零调用死接口/成员）：**

| 删除 | 原行号 |
|---|---|
| 属性 `DownloadingMaxNum` | 38-41 |
| 属性 `FailedTryAgainNum` | 43-46 |
| `LoadAssetAsync(AssetInfo)` | 219-220 |
| `LoadAllAssetsAsync<T>` | 227-228 |
| `LoadAllAssetsAsync(string, Type)` | 236-237 |
| `LoadAllAssetsAsync(string)` | 243-244 |
| `LoadAllAssetsAsync(AssetInfo)` | 250-251 |
| `LoadSceneAsync(AssetInfo, …)` | 276-277 |
| `LoadSubAssetsAsync(string)` | 287-288 |
| `LoadSubAssetsAsync<T>` | 295-296 |
| `LoadSubAssetsAsync(string, Type)` | 304-305 |
| `LoadSubAssetsAsync(AssetInfo)` | 312-313 |
| `LoadRawFileAsync(AssetInfo)` | 324-325 |
| `LoadRawFileAsync(string)` | 332-333 |
| `HasPackage(string)` | 533-537 |
| `GetPackage(string)` | 550 |
| `CreateResourceDownloader(params string[])` | 559-564 |
| `UnloadAsset(string, string)` 双参 | 589-596 |
| `UnloadUnusedAssetsAsync(string)` | 603-609 |
| `ClearAllBundleFilesAsync(string)` | 630-636 |
| `ClearUnusedBundleFilesAsync(string)` | 642-648 |
| `IsNeedDownload(AssetInfo)` | 671-675 |
| `IsNeedDownload(string)` | 683-687 |
| `GetAssetInfos(string[])` | 695 |
| `GetAssetInfos(string)` | 703 |

**③ 收窄为 private（留在主文件，无外部读取者）：**

| 成员 | 操作 |
|---|---|
| 属性 `PlayMode` | `public EPlayMode PlayMode { get; private set; }` → `private EPlayMode PlayMode { get; set; }` |
| 属性 `DefaultPackageName` | 同上 |
| 属性 `AsyncSystemMaxSlicePerFrame` | 同上 |
| `GetDefaultPackage()` | `public` → `private` |
| `UnloadAllAssetsAsync(string)` | `public` → `private` |

- [ ] **Step 1: 创建 `AssetModule.API.cs`**

写入以下文件骨架，并按迁移表 ① 把对应 public 成员的 body **逐字**（含全部 XML 注释）粘贴到对应 region 下：

```csharp
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源管理模块的公共 API。
    /// 功能：
    ///     1. 异步加载资源/场景，异步实例化游戏物体。
    ///     2. 资源包初始化与访问。
    ///     3. 资源卸载与查询。
    /// </summary>
    public partial class AssetModule : ModuleBase
    {
        #region 异步加载资源

        // LoadAssetAsync(string) / <T> / (string, Type) 三个重载逐字移入

        #endregion

        #region 异步加载场景

        // LoadSceneAsync(string, LoadSceneMode, bool activateOnLoad = true) 逐字移入

        #endregion

        #region 异步实例化游戏物体

        // InstantiateAsync(string) / ReleaseInstantiate(string) 逐字移入

        #endregion

        #region 资源包

        // InitDefaultPackageAsync / InitPackageAsync / CreatePackage / TryGetPackage 逐字移入

        #endregion

        #region 卸载资源

        // UnloadAsset(string) 逐字移入

        #endregion

        #region Get

        // GetAssetInfo(string) / HasAssetPath(string) 逐字移入

        #endregion
    }
}
```

> 提示：body 中引用的 `GetDefaultPackage()`/`CreateHandleTask()` 是主文件私有成员，partial 可直接访问，无需改动。`InitPackageAsync` body 中 `DefaultPackageName` 现在是私有属性，partial 同样可访问。

- [ ] **Step 2: 从 `AssetModule.cs` 删除迁移①的 13 个成员**（连同各自 XML 注释与所在 region 头，保留主文件私有成员）

- [ ] **Step 3: 从 `AssetModule.cs` 删除迁移②的 25 个成员**（连同 XML 注释；删除后空 region 头一并移除，如"异步加载子资源对象""异步加载原生文件"整个 region）

- [ ] **Step 4: 收窄迁移③的 5 个成员为 private**（改修饰符）

- [ ] **Step 5: 调整 `AssetModule.cs` 的 using**

重构后主文件只保留私有状态/生命周期/私有辅助，不再引用 `UnityEngine`/`UnityEngine.SceneManagement`/`Object`，删除后最终 using 应为：

```csharp
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
using AOT.Launch;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
```

同时核对：主文件应保留的成员只有 3 个私有属性、5 个字段、`InstantiateRef` 嵌套类、`OnInit`/`OnDispose`、`CreateHandleTask`、`GetDefaultPackage`（private）、`UnloadAllAssetsAsync`（private）、`TryGetReadyPackage`（private）。

- [ ] **Step 6: 编译验证**

经 unity-cli 触发 Unity 编译，预期 0 错误。常见报错排查：
- 报 `Object`/`LoadSceneMode`/`GameObject` 找不到 → API 文件缺 `UnityEngine`/`UnityEngine.SceneManagement`/`Object` alias using。
- 报 `NotNull` 找不到 → API 文件缺 `using Hotfix.Framework.Core;`。
- 报 `m_InstantiateRefDict` 找不到 → API 文件缺 `using System.Collections.Generic;`。
- 报某成员不存在 → 删除清单里误删了仍在调用的接口；`grep` 复核。

- [ ] **Step 7: 复核删除接口无调用残留**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -rnE "LoadAllAssetsAsync|LoadSubAssetsAsync|LoadRawFileAsync|IsNeedDownload|GetAssetInfos|CreateResourceDownloader|UnloadUnusedAssetsAsync|ClearAllBundleFilesAsync|ClearUnusedBundleFilesAsync|HasPackage\b|GetPackage\b|UnloadAsset\([^)]*," Unity/Assets/Scripts --include=*.cs | grep -v "AssetModule.API.cs"
```

预期仅命中 `AssetModule.API.cs` 以外的 0 行。若命中，说明有遗漏调用方，需在 Task 2 内一并修正（改用保留接口或直接走 YooAsset）。

- [ ] **Step 8: 提交**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/Asset/"
git commit -m "[AI]refactor: AssetModule 提取 public API 至 AssetModule.API，清理 25 个零调用接口，收窄私有"
```

> `.meta`：新建的 `AssetModule.API.cs` 的 `.meta` 由 Unity 编译时生成，编译后 `git status` 若出现则一并 `git add`（若未生成，则等 Task 3 后统一提交）。

---

### Task 3: 删除空壳 `AssetModuleInspector.cs`

**Files:**
- Delete: `Unity/Assets/Editor/FuFramework/Assets/Inspector/AssetModuleInspector.cs`
- Delete: `Unity/Assets/Editor/FuFramework/Assets/Inspector/AssetModuleInspector.cs.meta`

**Interfaces:**
- Consumes: 无（该 Inspector 为空壳，全部逻辑被注释，顶部 TODO 指向已放弃的调试界面）
- Produces: 删除后 `FuFramework/Assets/Inspector/` 目录为空 → 连同空目录 `.meta` 一并删除（若存在）

- [ ] **Step 1: 删除文件与 meta**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git rm "Unity/Assets/Editor/FuFramework/Assets/Inspector/AssetModuleInspector.cs"
git rm "Unity/Assets/Editor/FuFramework/Assets/Inspector/AssetModuleInspector.cs.meta"
rmdir "Unity/Assets/Editor/FuFramework/Assets/Inspector" 2>/dev/null || true
git rm "Unity/Assets/Editor/FuFramework/Assets/Inspector.meta" 2>/dev/null || true
```

> 若 `Inspector.meta` 不存在或 `rm` 报目录非空，保留原样；Unity 会在下次导入时清理孤儿 meta。参考 ReferencePool 重构中"移除空 Inspector 目录 meta"的处理。

- [ ] **Step 2: 编译验证**

经 unity-cli 触发 Unity 编译，预期 0 错误（Editor 代码删除不影响 Hotfix）。

- [ ] **Step 3: 提交**

```bash
git add -A "Unity/Assets/Editor/FuFramework/Assets/"
git commit -m "[AI]refactor: 移除空壳 AssetModuleInspector（调试界面已确定不做）"
```

---

### Task 4: 同步 AssetModule README 接口清单（至最终代码形态）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/README.md`

**Interfaces:**
- Consumes: 重构后最终代码形态——9 个 public 方法（`LoadAssetAsync` ×3 / `LoadSceneAsync` / `InstantiateAsync` / `ReleaseInstantiate` / `UnloadAsset` / `GetAssetInfo` / `HasAssetPath`）；**无公开属性**（`DefaultPackageName` 私有，`PlayMode`/`AsyncSystemMaxSlicePerFrame`/`DownloadingMaxNum`/`FailedTryAgainNum` 已删）；**无包初始化接口**（InitPackageAsync 组已删，包初始化由 AOT `LaunchAssetHelper` 完成）；`GetAssetInfo` 默认包未就绪返回 null
- Produces: README 与最终代码一致（无已删接口残留、无过期语义）

- [ ] **Step 1: 删除「主要属性」表**

AssetModule 重构后无任何公开属性（`PlayMode`/`AsyncSystemMaxSlicePerFrame`/`DownloadingMaxNum`/`FailedTryAgainNum` 已删，`DefaultPackageName` 为私有），整表删除。

- [ ] **Step 2: 更新「资源加载方法」**

- 异步加载资源块：仅保留 `LoadAssetAsync` 3 个 string 重载；删除 `LoadAssetAsync(AssetInfo)`、`LoadAllAssetsAsync` ×4、`LoadSubAssetsAsync` ×4、`LoadRawFileAsync` ×2。
- 异步加载场景块：仅保留 `LoadSceneAsync(string, ...)`；删除 `LoadSceneAsync(AssetInfo, ...)`。
- 实例化块：`InstantiateAsync` 与 `ReleaseInstantiate` 保留不变。

- [ ] **Step 3: 「资源包管理」整节改写为「资源查询」**

删除 `InitPackageAsync`/`InitDefaultPackageAsync`/`CreatePackage`/`TryGetPackage`/`GetPackage`/`HasPackage`/`SetDefaultPackage`/`CreateResourceDownloader`/`GetDefaultPackage`/`IsNeedDownload` ×2/`GetAssetInfos` ×2（全部已删或私有）；节标题改为「资源查询」，仅保留：

```csharp
AssetInfo GetAssetInfo(string path)   // 默认包未就绪返回 null
bool HasAssetPath(string path)
```

- [ ] **Step 4: 「资源卸载」仅保留一行**

仅保留 `void UnloadAsset(string assetPath)`；删除双参 `UnloadAsset(package,path)`、`UnloadAllAssetsAsync`、`UnloadUnusedAssetsAsync`、`ClearAllBundleFilesAsync`、`ClearUnusedBundleFilesAsync`。

- [ ] **Step 5: 更新「初始化流程」**

原文「从 AssetSetting 读取配置参数 → 初始化 YooAsset」已过时（AssetModule 不再初始化 YooAsset/默认包）。改为：**YooAsset 与默认资源包初始化由 AOT 启动流程 `LaunchAssetHelper` 完成**，AssetModule 仅缓存默认包名、提供资源加载/卸载/查询。

- [ ] **Step 6: 更新「使用示例」**

- 删除「初始化资源包」示例（`InitPackageAsync`）。
- 删除「加载原生文件」示例（`LoadRawFileAsync`）。
- 删除「子资源加载」示例（`LoadSubAssetsAsync<Sprite>`）。
- 保留「异步加载资源」（`LoadAssetAsync<GameObject>`/`InstantiateAsync`/`AssetLoadRegister`）与「加载场景」（`LoadSceneAsync`）。

- [ ] **Step 7: 修正 GetAssetInfo 语义注释**

README 中凡描述 `GetAssetInfo`「默认包必须已初始化，否则同步抛 YooPackageInvalidException」处，改为「默认包未就绪时返回 null」。

- [ ] **Step 8: 修正「注意事项」中引用已删接口的条目**

`注意事项`第 1/2/4/6/7 条引用了 `UnloadAllAssetsAsync`/`UnloadUnusedAssetsAsync`/`ClearAllBundleFilesAsync`/`ClearUnusedBundleFilesAsync`/`IsNeedDownload`/`GetAssetInfos` 等已删接口，删除或改写为仍存在的接口（`UnloadAsset`/`AssetLoadRegister`）。逐条核对，确保 README 全文不再出现已删 API 名。

- [ ] **Step 9: 提交**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/Asset/README.md"
git commit -m "[AI]docs: 同步 AssetModule README 至最终 9 接口（删已删接口、改初始化流程与 GetAssetInfo 语义）"
```

---

### Task 5: 删除热更侧包初始化子系统（包初始化完全委托 AOT 启动流程）

> 执行期新增（2026-08-10）。用户确认：热更侧包初始化子系统与 AOT `LaunchAssetHelper` 启动初始化几乎 1:1 重复且**全库零调用**（默认包已由启动流程初始化），故整体删除。包初始化能力由 `LaunchAssetHelper.InitPackageAsync` 独占。

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.API.cs`（删 4 个包初始化 public 方法 + `#region 资源包`，更新类文档）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`（删 2 属性 + 2 字段 + OnInit if/else 块 + OnDispose 2 行清理，去 `using AOT.Launch;`）
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.InitPackageMode.cs` + `.meta`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/Asset/RemoteServices.cs` + `.meta`

**Interfaces:**
- Consumes: Task 2 重构后的 `AssetModule.API.cs`（含 4 个待删包方法）与 `AssetModule.cs`（含 `PlayMode`/`AsyncSystemMaxSlicePerFrame` 私有属性）
- Produces: AssetModule 公开 API 收敛为 **9 个**：`LoadAssetAsync` ×3、`LoadSceneAsync`、`InstantiateAsync`、`ReleaseInstantiate`、`UnloadAsset`、`GetAssetInfo`、`HasAssetPath`

**删除明细（行号基于 Task 2 提交 `308b4058` 后的文件）：**

| 位置 | 删除 |
|---|---|
| `AssetModule.API.cs` | `InitDefaultPackageAsync`（171-174）、`InitPackageAsync`（183-240）、`CreatePackage`（247）、`TryGetPackage`（254-258）、整个 `#region 资源包`（163-260） |
| `AssetModule.cs` | 属性 `PlayMode`（25）、属性 `AsyncSystemMaxSlicePerFrame`（35）、字段 `m_InitedPackageSet`（41）、字段 `m_InitPackageTasks`（48）、OnInit 的 if/else YooAssets 初始化块（102-116）及其两行属性赋值（96、98）、OnDispose 的 `m_InitedPackageSet.Clear()`/`m_InitPackageTasks.Clear()` 两行（137-138） |
| 文件删除 | `AssetModule.InitPackageMode.cs` + `.meta`、`RemoteServices.cs` + `.meta` |

- [ ] **Step 1: 修改 `AssetModule.API.cs`**

- 删除 `#region 资源包` 整块（`InitDefaultPackageAsync`/`InitPackageAsync`/`CreatePackage`/`TryGetPackage` 及其 XML 注释）。
- 类文档注释「功能」去掉「2. 资源包初始化与访问。」一行，重排序号为「1. 异步加载资源/场景，异步实例化游戏物体。2. 资源卸载与查询。」。
- 核对 usings：删包方法后仍全部需要（`YooAsset` 仍用于 `AssetHandle`/`SceneHandle`/`AssetInfo`/`YooAssets`；`Hotfix.Framework.Core` 仍用于 `ModuleBase`/`NotNull`）。

- [ ] **Step 2: 修改 `AssetModule.cs`**

- 删除 `PlayMode`/`AsyncSystemMaxSlicePerFrame` 两个私有属性。
- 删除 `m_InitedPackageSet`/`m_InitPackageTasks` 两个字段。
- `OnInit` 简化为（其余注释保留）：

```csharp
protected internal override void OnInit()
{
    // 热更重载场景下 OnDispose 后可能再次 OnInit（ModuleManager.ReInit），重置销毁标记
    m_IsDisposed = false;

    // 默认包初始化由 AOT 启动流程 LaunchAssetHelper 完成，此处仅缓存默认包名
    DefaultPackageName = GameSetting.Instance.DefaultPackageName;

    FuLogger.LogInfo($"[AssetModule]资源系统运行模式：{GameSetting.Instance.PlayMode}");
    FuLogger.LogInfo("[AssetModule]资源系统初始化完毕！");
}
```

- `OnDispose` 删除 `m_InitedPackageSet.Clear();`/`m_InitPackageTasks.Clear();` 两行及对应注释。
- 删除 `using AOT.Launch;`（`LaunchAssetHelper` 不再被引用）；保留其余 7 条 using（`GameSetting`/`FuLogger` 仍用）。

- [ ] **Step 3: 删除两个文件及 meta**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git rm "Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.InitPackageMode.cs"
git rm "Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.InitPackageMode.cs.meta"
git rm "Unity/Assets/Scripts/Hotfix/Framework/Asset/RemoteServices.cs"
git rm "Unity/Assets/Scripts/Hotfix/Framework/Asset/RemoteServices.cs.meta"
```

- [ ] **Step 4: 复核无调用残留**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -rnE "InitPackageAsync|InitDefaultPackageAsync|CreatePackage|TryGetPackage|RemoteServices|InitPackage\b|InitInEditorSimulateMode|InitInOfflinePlayMode|InitInHostPlayMode|InitInWebPlayMode" Unity/Assets/Scripts/Hotfix --include=*.cs
```

预期 0 命中（AOT `LaunchAssetHelper.InitPackageAsync`/`CreateDownloader` 与私有 `RemoteServices` 属 AOT 层，不在此 grep 路径内）。

- [ ] **Step 5: 提交**

```bash
git add -A "Unity/Assets/Scripts/Hotfix/Framework/Asset/"
git commit -m "[AI]refactor: 删除热更侧包初始化子系统（InitPackageAsync/InitPackageMode.cs/RemoteServices.cs），包初始化委托 AOT 启动流程"
```

---

### Task 6: 移除 `GetDefaultPackage()`，改用 `YooAssets.TryGetPackage` + 判空

> 执行期新增（2026-08-10）。用户确认：`GetDefaultPackage()` 用 `YooAssets.GetPackage`（包不存在**同步抛异常**），改为 `YooAssets.TryGetPackage(DefaultPackageName, out var package)` + 判空，防御风格与 `UnloadAsset`/`TryGetReadyPackage` 一致。注意 `TryGetPackage` 内部 `CheckInitialized()` 在 YooAssets 未初始化时抛异常，必须先判 `!YooAssets.IsInitialized` 短路。

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.API.cs`（4 个加载方法 + GetAssetInfo 改用 TryGetPackage 模式）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`（删除 `GetDefaultPackage()` 方法）

**Interfaces:**
- Consumes: Task 5 后 `AssetModule.API.cs`（9 个 public）与 `AssetModule.cs`（含 `GetDefaultPackage()` 私有方法，行 127）
- Produces: `GetDefaultPackage()` 删除；4 个加载方法在包未就绪时返回 faulted UniTask（`UniTask.FromException<T>`，保持 CreateHandleTask 契约）；`GetAssetInfo` 包未就绪返回 null（调用方 `CustomLoader.cs:249` 已判空）

- [ ] **Step 1: 删除 `AssetModule.cs` 的 `GetDefaultPackage()` 方法**

删除（`AssetModule.cs` 约 125-127 行）：

```csharp
        /// <summary>
        /// 获取默认资源包
        /// </summary>
        /// <returns></returns>
        private ResourcePackage GetDefaultPackage() => YooAssets.GetPackage(DefaultPackageName);
```

核对 `using YooAsset;` 仍需要（`TryGetReadyPackage` 用 `out ResourcePackage`）。

- [ ] **Step 2: 改 `AssetModule.API.cs` 的 4 个加载方法**

`LoadAssetAsync(string)` / `<T>(string)` / `(string, Type)` / `LoadSceneAsync(string, ...)` 均改为：

```csharp
public UniTask<AssetHandle> LoadAssetAsync(string path)
{
    // 默认包未就绪：转 faulted UniTask（保持 CreateHandleTask 的契约），不同步抛
    if (!YooAssets.IsInitialized || !YooAssets.TryGetPackage(DefaultPackageName, out var package))
        return UniTask.FromException<AssetHandle>(new InvalidOperationException($"[AssetModule]默认资源包未就绪：{DefaultPackageName}"));
    return CreateHandleTask(() => package.LoadAssetAsync(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });
}
```

- `LoadAssetAsync<T>`：返回类型 `UniTask<AssetHandle>`，lambda 调 `package.LoadAssetAsync<T>(path)`。
- `LoadAssetAsync(string, Type)`：返回 `UniTask<AssetHandle>`，lambda 调 `package.LoadAssetAsync(path, type)`。
- `LoadSceneAsync`：返回 `UniTask<SceneHandle>`，fail 路径 `UniTask.FromException<SceneHandle>`，lambda 调 `package.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, activateOnLoad)`。

- [ ] **Step 3: 改 `GetAssetInfo` 为防御返回 null**

```csharp
public AssetInfo GetAssetInfo(string path)
{
    // 默认包未就绪返回 null（调用方已判空），避免同步抛异常
    if (!YooAssets.IsInitialized || !YooAssets.TryGetPackage(DefaultPackageName, out var package)) return null;
    return package.GetAssetInfo(path);
}
```

- [ ] **Step 4: 复核无 `GetDefaultPackage` 残留**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -rn "GetDefaultPackage" Unity/Assets/Scripts --include=*.cs
```

预期 0 命中。

- [ ] **Step 5: 提交**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/Asset/"
git commit -m "[AI]refactor: AssetModule 移除 GetDefaultPackage()，改用 YooAssets.TryGetPackage + 判空（防御包未就绪）"
```

---

### Task 7: 提取 `CreateHandleTask` 守卫，加载方法恢复单行表达式体

> 执行期新增（2026-08-10）。用户确认：Task 6 在 4 个加载方法处各写了一段 `if (!YooAssets.IsInitialized || !TryGetPackage(...)) return UniTask.FromException<T>(...)` 守卫，重复 4 次。提取到收口方法 `CreateHandleTask`（4 处加载全部走它），调用点恢复单行表达式体。守卫与 faulted-task 契约集中一处。

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`（`CreateHandleTask` 去 `static`、`Func<T>` → `Func<ResourcePackage, T>`、顶部加守卫）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.API.cs`（4 个加载方法恢复单行）

**Interfaces:**
- Consumes: Task 6 后 `CreateHandleTask`（static，`Func<T>`）与 4 个加载方法（6 行守卫块）
- Produces: `CreateHandleTask<T>(Func<ResourcePackage, T> load, Action<T, UniTaskCompletionSource<T>> bind)`（实例方法，含包未就绪守卫）；4 个加载方法单行 `p => p.Xxx(...)`

- [ ] **Step 1: 改 `AssetModule.cs` 的 `CreateHandleTask`**

替换为（含去 `static`、签名改 `Func<ResourcePackage, T>`、顶部守卫；`DefaultPackageName` 是实例属性，故去 `static`）：

```csharp
        /// <summary>
        /// 将默认资源包上发起的 YooAsset 异步句柄包装为 UniTask。
        /// YooAssets 未初始化或默认包不存在时返回 faulted UniTask，保持"不同步抛异常"契约；
        /// 同步异常（句柄加载后立即失效等）同样转为 faulted UniTask。
        /// </summary>
        /// <typeparam name="T">句柄类型。</typeparam>
        /// <param name="load">以默认资源包发起加载并返回句柄。</param>
        /// <param name="bind">将句柄的 Completed 事件绑定到完成源。</param>
        private UniTask<T> CreateHandleTask<T>(Func<ResourcePackage, T> load, Action<T, UniTaskCompletionSource<T>> bind) where T : HandleBase
        {
            // 默认包未就绪：转 faulted UniTask（不同步抛，保持契约）
            if (!YooAssets.IsInitialized || !YooAssets.TryGetPackage(DefaultPackageName, out var package))
                return UniTask.FromException<T>(new InvalidOperationException($"[AssetModule]默认资源包未就绪：{DefaultPackageName}"));

            var taskCompletionSource = new UniTaskCompletionSource<T>();
            T   handle               = null;
            try
            {
                handle = load(package);
                bind(handle, taskCompletionSource);
            }
            catch (Exception e)
            {
                // bind 失败（如句柄加载后立即失效）时释放已创建的句柄，避免残留
                handle?.Release();
                taskCompletionSource.TrySetException(e);
            }

            return taskCompletionSource.Task;
        }
```

- [ ] **Step 2: 改 `AssetModule.API.cs` 的 4 个加载方法为单行**

```csharp
public UniTask<AssetHandle> LoadAssetAsync(string path)
    => CreateHandleTask(p => p.LoadAssetAsync(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

public UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
    => CreateHandleTask(p => p.LoadAssetAsync<T>(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

public UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
    => CreateHandleTask(p => p.LoadAssetAsync(path, type), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

public UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true)
    => CreateHandleTask(p => p.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, activateOnLoad), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });
```

`GetAssetInfo` 保持 Task 6 版本不动（fail 返回 null，语义不同）。

- [ ] **Step 3: 复核 `CreateHandleTask` 调用方**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -rn "CreateHandleTask" Unity/Assets/Scripts --include=*.cs
```

预期仅 5 处：`AssetModule.cs` 定义 + `AssetModule.API.cs` 4 个加载方法。确认没有其他 `Func<T>` 形式调用残留。

- [ ] **Step 4: 提交**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/Asset/"
git commit -m "[AI]refactor: 提取 CreateHandleTask 包未就绪守卫，加载方法恢复单行表达式体"
```

---

## 完成标准（全局验证）

- [ ] Task 1-3 各经 unity-cli 编译零错误
- [ ] `AssetModule.cs` 不再含任何 public 方法（仅私有成员 + 生命周期）
- [ ] 全库 `grep` 无已删接口调用残留
- [ ] 编辑器 Play 冒烟：框架正常启动，资源加载路径可用（`HotfixLauncher.MainAsync` 完成启动流程，LoadAssetAsync 链路正常）
- [ ] README 与重构后接口一致
