# Guide 模块配置表化 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Guide 模块数据源从 ScriptableObject 迁移到 Luban 配置表，参照 RedDot 模块模式

**Architecture:** 双表方案（TbGuide + TbGuideStep），int key，NextStepId 可空链式串联步骤，EStepType 由 Luban 枚举生成。GuideModule.OnInit() 从 ConfigModule 获取配置表构建运行时数据

**Tech Stack:** Unity + C# + Luban 配置表系统

## Global Constraints

- 遵循 `Docs/代码风格规范.md`
- 遵循 `Docs/Git提交规范.md`
- int key，非枚举
- NextStepId 可空（null = 结束）

---

### Task 1: Luban Schema 定义 + 代码生成

**文件：**
- 编辑: `Config/Excels/__enums__.xlsx`（二进制 Excel，手动编辑）
- 编辑: `Config/Excels/__beans__.xlsx`（二进制 Excel，手动编辑）
- 编辑: `Config/Excels/__tables__.xlsx`（二进制 Excel，手动编辑）
- 生成: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/Guide.cs`
- 生成: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/GuideStep.cs`
- 生成: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbGuide.cs`
- 生成: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbGuideStep.cs`
- 生成: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/EStepType.cs`
- 更新: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/TableManager.cs`

**生成产物（此任务产生 Luban 自动生成的 C# 文件，后续任务依赖这些类型）：**
- `Tables.Guide` — 类：Id (int), Name (string), StartStepId (int)
- `Tables.GuideStep` — 类：Id (int), GuideId (int), StepType (EStepType), NextStepId (int?), CanJump (bool), TargetWindow (string), TargetUI (string), DialogContent (string), WaitTime (float)
- `Tables.TbGuide : BaseDataTable<Tables.Guide>` — 表类
- `Tables.TbGuideStep : BaseDataTable<Tables.GuideStep>` — 表类
- `EStepType` — 枚举：None=0, ClickUI=1, Dialog=2, Wait=3

- [ ] **Step 1: 在 `__enums__.xlsx` 中添加 EStepType 枚举**

在 `__enums__.xlsx` 的枚举 sheet 中添加一行：

| 字段 | 值 |
|------|-----|
| Name | EStepType |
| Items | None, ClickUI, Dialog, Wait |
| Groups | c |

- [ ] **Step 2: 在 `__beans__.xlsx` 中添加 Guide bean**

在 `__beans__.xlsx` 的 bean sheet 中添加：

| 字段 | 值 |
|------|-----|
| Name | Guide |
| Groups | c |
| Fields | Id:int, Name:string, StartStepId:int |

- [ ] **Step 3: 在 `__beans__.xlsx` 中添加 GuideStep bean**

| 字段 | 值 |
|------|-----|
| Name | GuideStep |
| Groups | c |
| Fields | Id:int, GuideId:int, StepType:EStepType, NextStepId:int?(nullable), CanJump:bool, TargetWindow:string, TargetUI:string, DialogContent:string, WaitTime:float |

- [ ] **Step 4: 在 `__tables__.xlsx` 中添加 TbGuide 表定义**

| 字段 | 值 |
|------|-----|
| Name | TbGuide |
| ValueType | Guide |
| KeyType | int |
| Mode | MAP |
| Input | G-Guide-引导表.xlsx |
| Groups | c |

- [ ] **Step 5: 在 `__tables__.xlsx` 中添加 TbGuideStep 表定义**

| 字段 | 值 |
|------|-----|
| Name | TbGuideStep |
| ValueType | GuideStep |
| KeyType | int |
| Mode | MAP |
| Input | G-GuideStep-引导步骤表.xlsx |
| Groups | c |

- [ ] **Step 6: 运行 Luban 代码生成**

```bash
cd Config
./gen-client-json.bat
```

- [ ] **Step 7: 验证生成文件**

检查以下文件已生成且编译通过：
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/Guide.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/GuideStep.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbGuide.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbGuideStep.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/EStepType.cs`
- `TableManager.cs` 已包含 TbGuide 和 TbGuideStep 注册代码

在 Unity Editor 中打开项目，确认编译无报错。

- [ ] **Step 8: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/Guide.cs
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/GuideStep.cs
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbGuide.cs
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbGuideStep.cs
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/EStepType.cs
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/TableManager.cs
git add Config/Excels/__enums__.xlsx
git add Config/Excels/__beans__.xlsx
git add Config/Excels/__tables__.xlsx
git commit -m "feat: 添加 Guide/GuideStep Luban Schema 定义并生成代码"
```

---

### Task 2: 创建配置表数据 Excel 文件

**文件：**
- 创建: `Config/Excels/Tables/G-Guide-引导表.xlsx`
- 创建: `Config/Excels/Tables/G-GuideStep-引导步骤表.xlsx`

**产生：** 从旧 `GuideSetting.asset` 迁移的配置数据

- [ ] **Step 1: 从 GuideSetting.asset 提取现有数据**

当前 `GuideSetting.asset` 中有一条引导记录：

```
GuideId: "Guide_引导一"
GuideName: "引导一"
StartStepId: "Step_欢迎对话"
Steps:
  - StepId: "Step_欢迎对话", StepType: Dialog(2), NextStepId: "Step_点击开始按钮", DialogContent: "欢迎来到游戏世界！让我来引导你熟悉基本操作。"
  - StepId: "Step_点击开始按钮", StepType: ClickUI(1), NextStepId: "Step_等待2秒", TargetWindow: "WinLogin", TargetUI: "_btnLogin"
  - StepId: "Step_等待2秒", StepType: Wait(3), NextStepId: (空), WaitTime: 2
```

- [ ] **Step 2: 创建 `G-Guide-引导表.xlsx`**

将旧数据映射为 int ID：

| Id | Name | StartStepId |
|----|------|-------------|
| 1 | 引导一 | 101 |

- [ ] **Step 3: 创建 `G-GuideStep-引导步骤表.xlsx`**

| Id | GuideId | StepType | NextStepId | CanJump | TargetWindow | TargetUI | DialogContent | WaitTime |
|----|---------|----------|------------|---------|-------------|----------|---------------|----------|
| 101 | 1 | Dialog | 102 | false | | | 欢迎来到游戏世界！让我来引导你熟悉基本操作。 | 0 |
| 102 | 1 | ClickUI | 103 | false | WinLogin | _btnLogin | | 0 |
| 103 | 1 | Wait | (空) | false | | | | 2 |

- [ ] **Step 4: 重新运行 Luban 代码生成（生成数据 JSON）**

```bash
cd Config
./gen-client-json.bat
```

- [ ] **Step 5: 验证生成的 JSON 数据文件**

确认 `Unity/Assets/Bundles/Config/tables_tbguide.json` 和 `Unity/Assets/Bundles/Config/tables_tbguidestep.json` 内容正确。

- [ ] **Step 6: Commit**

```bash
git add Config/Excels/Tables/G-Guide-引导表.xlsx
git add Config/Excels/Tables/G-GuideStep-引导步骤表.xlsx
git add Unity/Assets/Bundles/Config/tables_tbguide.json
git add Unity/Assets/Bundles/Config/tables_tbguidestep.json
git commit -m "feat: 创建 Guide/GuideStep 配置表数据，迁移旧 ScriptableObject 数据"
```

---

### Task 3: 删除旧 AOT 文件 + 修改 ModuleSetting

**文件：**
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/GuideSetting.cs`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/GuideInfo.cs`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/StepInfo.cs`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide/GuideSettingEditor.cs`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide/GuideSettingCreator.cs`
- 删除: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/GuideSetting.asset`
- 修改: `Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/ModuleSetting.cs`

**产生：** AOT 层不再有 Guide 相关代码（除 OpenGuide 开关）

- [ ] **Step 1: 删除旧文件**

```bash
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/GuideSetting.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/GuideSetting.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/GuideInfo.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/GuideInfo.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/StepInfo.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide/StepInfo.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide/GuideSettingEditor.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide/GuideSettingEditor.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide/GuideSettingCreator.cs"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide/GuideSettingCreator.cs.meta"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/GuideSetting.asset"
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/SettingAssets/GuideSetting.asset.meta"
```

- [ ] **Step 2: 删除空的目录（如果只剩 .meta 文件）**

```bash
rmdir "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide" 2>/dev/null || true
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Runtime/Guide.meta" 2>/dev/null || true
rmdir "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide" 2>/dev/null || true
rm "Unity/Assets/Scripts/AOT/Framework/ModuleSetting/Editor/Guide.meta" 2>/dev/null || true
```

- [ ] **Step 3: 修改 ModuleSetting.cs**

移除 `m_GuideSetting` 字段、`GuideSetting` 属性，以及 `using AOT.Framework.ModuleSetting.Runtime.Guide;` 引用。

读取 `ModuleSetting.cs`，执行以下编辑：

**编辑 1：移除 using 引用**

```csharp
// 删除这一行：
using AOT.Framework.ModuleSetting.Runtime.Guide;
```

**编辑 2：移除 m_GuideSetting 字段和 GuideSetting 属性**

```csharp
// 删除这些行：
[Header("引导模块配置")]
[SerializeField] private GuideSetting m_GuideSetting;

/// <summary>
/// 获取引导模块配置
/// </summary>
public GuideSetting GuideSetting => m_GuideSetting;
```

保留 `[SerializeField] private bool m_OpenGuide = true;` 和 `OpenGuide` 属性不变。

- [ ] **Step 4: 在 Unity Editor 中验证编译**

打开 Unity Editor，确认无编译错误。`ModuleSetting` 组件上不再有 `m_GuideSetting` 序列化字段。

- [ ] **Step 5: Commit**

```bash
git add -u
git commit -m "refactor: 删除旧的 Guide ScriptableObject 文件，移除 ModuleSetting 中的 GuideSetting 引用"
```

---

### Task 4: 改造 GuideModule.cs

**文件：**
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Guide/GuideModule.cs`

**产生：** GuideModule 从配置表加载数据，API 参数改为 int

- [ ] **Step 1: 更新 using 引用**

替换 `using AOT.Framework.ModuleSetting.Runtime.Guide;`：

```csharp
// 删除
using AOT.Framework.ModuleSetting.Runtime.Guide;

// 新增
using Hotfix.Framework.Config;
using Hotfix.Game.Tables.Tables;
using System.Linq;
```

- [ ] **Step 2: 修改运行时数据字段类型**

在 `#region 私有字段` 中：

**删除：**
```csharp
private GuideSetting m_Setting;
```

**替换：**

```csharp
// 旧：
private GuideInfo m_CurrentGuide;
private readonly Dictionary<string, BaseStep> m_AllStepDict = new();
private readonly Dictionary<string, bool> m_GuideCompletionCacheDict = new();

// 新：
/// <summary>
/// 当前引导
/// </summary>
private Guide m_CurrentGuide;

/// <summary>
/// 引导数据字典，key 为引导 ID
/// </summary>
private Dictionary<int, Guide> m_GuideDict;

/// <summary>
/// 步骤数据字典（来自配置表），key 为步骤 ID
/// </summary>
private Dictionary<int, GuideStep> m_StepDataDict;

/// <summary>
/// 当前引导中的所有步骤，key为步骤Id，Value为步骤对象
/// </summary>
private readonly Dictionary<int, BaseStep> m_AllStepDict = new();

/// <summary>
/// 缓存完成的引导，key为引导ID，Value为是否完成
/// </summary>
private readonly Dictionary<int, bool> m_GuideCompletionCacheDict = new();
```

- [ ] **Step 3: 重写 OnInit()**

```csharp
/// <summary>
/// 初始化
/// </summary>
protected internal override void OnInit()
{
    Instance = this;

    m_GuideCompletionCacheDict.Clear();

    var tbGuide = ConfigModule.Instance?.GetConfig<TbGuide>();
    var tbGuideStep = ConfigModule.Instance?.GetConfig<TbGuideStep>();
    if (tbGuide == null || tbGuideStep == null)
    {
        FuLogger.LogError("[GuideModule] 引导配置表不存在，跳过初始化.");
        return;
    }

    m_GuideDict = new Dictionary<int, Guide>();
    foreach (var guide in tbGuide.DataList)
    {
        if (m_GuideDict.ContainsKey(guide.Id))
        {
            FuLogger.LogError($"[GuideModule] 重复的引导 ID: {guide.Id}");
            continue;
        }

        m_GuideDict[guide.Id] = guide;
    }

    m_StepDataDict = new Dictionary<int, GuideStep>();
    foreach (var step in tbGuideStep.DataList)
    {
        if (m_StepDataDict.ContainsKey(step.Id))
        {
            FuLogger.LogError($"[GuideModule] 重复的步骤 ID: {step.Id}");
            continue;
        }

        m_StepDataDict[step.Id] = step;
    }

    FuLogger.LogInfo($"[GuideModule] 引导管理模块初始化完成. 引导数量: {m_GuideDict.Count}, 步骤总数量: {m_StepDataDict.Count}");
}
```

- [ ] **Step 4: 重写 OnDispose()**

```csharp
/// <summary>
/// 释放。
/// </summary>
protected internal override void OnDispose()
{
    // 中断当前引导
    InterruptGuide();

    // 回收当前引导的所有步骤到引用池中
    foreach (var (_, step) in m_AllStepDict)
    {
        ReferencePool.Release(step);
    }

    m_AllStepDict.Clear();
    m_StepHistoryStack.Clear();
    m_GuideCompletionCacheDict.Clear();
    m_GuideDict?.Clear();
    m_StepDataDict?.Clear();
    m_CurrentGuide = null;

    // 清理事件订阅
    OnGuideStarted     = null;
    OnGuideFinished    = null;
    OnStepChanged      = null;
    OnGuideInterrupted = null;
    OnStepExecuting    = null;
    OnStepCompleted    = null;

    Instance = null;
}
```

- [ ] **Step 5: 修改所有公开 API — ID 参数从 string 改为 int**

`StartGuideById` 重命名为 `StartGuide`（int 参数）：

保持原有 `StartGuide` 方法逻辑，但参数类型改为 int：

```csharp
/// <summary>
/// 开始引导流程
/// </summary>
/// <param name="guideId">引导 ID</param>
/// <param name="forceRestart">是否强制重新开始</param>
/// <returns>是否成功开始引导</returns>
public bool StartGuide(int guideId, bool forceRestart = false)
{
    if (!m_GuideDict.TryGetValue(guideId, out var guide))
    {
        FuLogger.LogError($"[GuideModule] 找不到引导: {guideId}");
        return false;
    }

    return StartGuideInternal(guide, forceRestart);
}
```

删除 `StartGuideByName` 方法；删除 `StartGuide(string guideId, ...)` 重载；删除 `StartFirstGuide` 中遍历 `m_Setting.AllGuides` 的旧逻辑：

```csharp
/// <summary>
/// 开始第一个引导
/// </summary>
public bool StartFirstGuide(bool forceRestart = false)
{
    Guide firstGuide = null;
    foreach (var guide in m_GuideDict.Values)
    {
        firstGuide = guide;
        break;
    }

    if (firstGuide == null)
    {
        FuLogger.LogError("[GuideModule] 没有可用的引导");
        return false;
    }

    return StartGuideInternal(firstGuide, forceRestart);
}
```

- [ ] **Step 6: 修改私有方法 `StartGuide(GuideInfo, bool)` → `StartGuideInternal(Guide, bool)`**

```csharp
/// <summary>
/// 开始引导流程(通过 Guide)
/// </summary>
private bool StartGuideInternal(Guide guide, bool forceRestart = false)
{
    if (IsGuiding)
    {
        if (m_CurrentGuide?.Id == guide.Id && !forceRestart)
        {
            FuLogger.LogWarning($"[GuideModule] 引导 {guide.Id} 已在运行中");
            return false;
        }

        FuLogger.LogWarning($"[GuideModule] 中断当前引导 {m_CurrentGuide?.Id}，开始新引导 {guide.Id}");
        InterruptGuide();
    }

    if (IsGuideCompleted(guide.Id) && !forceRestart)
    {
        FuLogger.LogInfo($"[GuideModule] 引导 {guide.Id} 已完成，跳过");
        return false;
    }

    try
    {
        m_CurrentGuide = guide;

        // 构建当前引导下的所有步骤节点
        BuildStepNodes(guide);

        if (guide.StartStepId == 0 || !m_AllStepDict.TryGetValue(guide.StartStepId, out var step))
        {
            throw new System.ArgumentException($"起始步骤 ID 无效: {guide.StartStepId}");
        }

        m_CurrentStep = step;

        // 开始执行当前引导的第一个步骤
        ExecuteCurrentStep();

        OnGuideStarted?.Invoke(guide.Id.ToString());
        FuLogger.LogInfo($"[GuideModule] 开始引导: {guide.Name} ({guide.Id})");

        return true;
    }
    catch (System.Exception e)
    {
        FuLogger.LogError($"[GuideModule] 开始引导失败 {guide.Id}: {e.Message}\n{e.StackTrace}");
        ClearGuideData();
        return false;
    }
}
```

- [ ] **Step 7: 修改 `BuildStepNodes` 方法**

```csharp
/// <summary>
/// 构建步骤节点
/// </summary>
private void BuildStepNodes(Guide guide)
{
    m_AllStepDict.Clear();
    m_StepHistoryStack.Clear();

    var guideSteps = m_StepDataDict.Values
        .Where(s => s.GuideId == guide.Id)
        .ToList();

    foreach (var stepInfo in guideSteps)
    {
        var step = CreateStep(stepInfo);
        if (step == null) continue;

        m_AllStepDict.Add(stepInfo.Id, step);
    }

    // 验证步骤链
    ValidateStepChain();
}
```

- [ ] **Step 8: 修改 `CreateStep` 方法 — 参数类型改为 GuideStep**

```csharp
/// <summary>
/// 创建步骤
/// </summary>
private BaseStep CreateStep(GuideStep stepInfo)
{
    return stepInfo.StepType switch
    {
        EStepType.ClickUI => ClickUIStep.Create(stepInfo),
        EStepType.Dialog  => DialogStep.Create(stepInfo),
        EStepType.Wait    => WaitStep.Create(stepInfo),
        EStepType.None    => DefaultStep.Create(stepInfo),
        _                 => DefaultStep.Create(stepInfo)
    };
}
```

- [ ] **Step 9: 更新所有 StepInfo 字段引用 + NextStepId 可空逻辑**

**字段名映射**（GuideModule.cs 内全部替换）：

| 旧引用 | 新引用 |
|--------|--------|
| `step.StepInfo.m_StepId` | `step.StepInfo.Id` |
| `step.StepInfo.m_NextStepId` | `step.StepInfo.NextStepId` |
| `stepInfo.m_StepId` | `stepInfo.Id` |
| `stepInfo.m_EStepType` | `stepInfo.StepType` |
| `guideInfo.m_GuideId` | `guide.Id` |
| `guideInfo.m_GuideName` | `guide.Name` |
| `guideInfo.m_StartStepId` | `guide.StartStepId` |
| `m_CurrentGuide?.m_GuideId` | `m_CurrentGuide?.Id` |
| `m_CurrentGuide?.m_GuideName` | `m_CurrentGuide?.Name` |

**NextStepId 可空逻辑** — NextStepId 从 `string`（空字符串=结束）变为 `int?`（null=结束），相关判断需改写：

```csharp
// 旧模式（string）：!string.IsNullOrEmpty(nextStepId)
// 新模式（int?）：nextStepId.HasValue

// ForceNextStep() 中：
var nextStepId = m_CurrentStep.StepInfo.NextStepId;  // int?
m_CurrentStep.Cancel();
if (nextStepId.HasValue && m_AllStepDict.TryGetValue(nextStepId.Value, out var nextStep))
{ ... }
else { FinishGuide(); }

// MoveToNextStep() 中：
var nextStepId = m_CurrentStep?.StepInfo?.NextStepId;  // int?
if (nextStepId.HasValue && m_AllStepDict.TryGetValue(nextStepId.Value, out var nextStep))
{ ... }
else { FinishGuide(); }
```

- [ ] **Step 10: 更新 `CurrentGuide` 属性和 `GetCurrentGuideInfo` 返回类型**

```csharp
/// <summary>
/// 当前引导配置
/// </summary>
public Guide CurrentGuide => m_CurrentGuide;

/// <summary>
/// 当前步骤
/// </summary>
public BaseStep CurrentStep => m_CurrentStep;

/// <summary>
/// 当前引导 ID
/// </summary>
public int? CurrentGuideId => m_CurrentGuide?.Id;

/// <summary>
/// 当前步骤 ID
/// </summary>
public int? CurrentStepId => m_CurrentStep?.StepInfo.Id;

/// <summary>
/// 获取当前引导信息
/// </summary>
public Guide GetCurrentGuideInfo() => m_CurrentGuide;
```

`GetAllSteps` 返回类型保持 `Dictionary<string, BaseStep>` 不变（内部 key 是步骤 ID，但步骤 ID 现在是 int 而非字符串... wait）

等一下，`m_AllStepDict` 的 key 类型需要改。当前是 `Dictionary<string, BaseStep>`，因为旧 StepId 是 string。现在 StepId 是 int，需要改为 `Dictionary<int, BaseStep>`。

同时 `GetAllSteps` 返回类型应该改为 `Dictionary<int, BaseStep>`。

`GetStep` 参数也应从 `string` 改为 `int`：

```csharp
/// <summary>
/// 获取步骤实例
/// </summary>
public BaseStep GetStep(int stepId) => m_AllStepDict.GetValueOrDefault(stepId);

/// <summary>
/// 获取所有步骤
/// </summary>
public Dictionary<int, BaseStep> GetAllSteps() => new(m_AllStepDict);
```

`JumpToStep` 参数也改为 int：

```csharp
public bool JumpToStep(int stepId) { ... }
```

`IsGuideCompleted` / `MarkGuideAsCompleted` / `ResetGuide` 参数改为 int：

```csharp
public bool IsGuideCompleted(int guideId)
{
    if (m_GuideCompletionCacheDict.TryGetValue(guideId, out var completed))
        return completed;

    completed = PlayerPrefs.GetInt($"Guide_Completed_{guideId}", 0) == 1;
    m_GuideCompletionCacheDict[guideId] = completed;
    return completed;
}

public void MarkGuideAsCompleted(int guideId) { ... }
public void ResetGuide(int guideId) { ... }
```

`m_GuideCompletionCacheDict` 类型改为 `Dictionary<int, bool>`。

`CurrentGuideId` 和 `CurrentStepId` 属性改为 `int?` 类型。

事件委托参数也要改：
- `OnGuideStarted` — `Action<int>` (原 `Action<string>`)
- `OnGuideFinished` — `Action<int>` (原 `Action<string>`)
- `OnStepChanged` — `Action<int, int>` (原 `Action<string, string>`)
- `OnGuideInterrupted` — `Action<int, bool>` (原 `Action<string, bool>`)

- [ ] **Step 11: 在 Unity Editor 中验证编译**

GuideModule.cs 编译通过，无报错。

- [ ] **Step 12: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Guide/GuideModule.cs
git commit -m "refactor: GuideModule 改为从配置表加载数据，API 参数 string→int"
```

---

### Task 5: 改造 BaseStep + 步骤子类

**文件：**
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Guide/BaseStep.cs`
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Guide/ClickUIStep.cs`
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Guide/DialogStep.cs`
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Guide/WaitStep.cs`
- 修改: `Unity/Assets/Scripts/Hotfix/Framework/Guide/DefaultStep.cs`

**产生：** 步骤类使用 Luban 生成的 GuideStep 替代旧 StepInfo

- [ ] **Step 1: 改造 BaseStep.cs**

**替换 using：**

```csharp
// 删除
using AOT.Framework.ModuleSetting.Runtime.Guide;

// 新增
using Hotfix.Game.Tables.Tables;
```

**修改 StepInfo 属性类型：**

```csharp
// 旧：
public StepInfo StepInfo { get; protected set; }

// 新：
public GuideStep StepInfo { get; protected set; }
```

**修改 Complete() — NextStepId 可空逻辑：**

```csharp
// 旧：
if (!string.IsNullOrEmpty(StepInfo.m_NextStepId))
    GuideModule.Instance.JumpToStep(StepInfo.m_NextStepId);

// 新：
if (StepInfo.NextStepId.HasValue)
    GuideModule.Instance.JumpToStep(StepInfo.NextStepId.Value);
```

**修改 Clear 方法：**

```csharp
public virtual void Clear() => StepInfo = null;
```

- [ ] **Step 2: 改造 ClickUIStep.cs**

**替换 using：**

```csharp
// 删除
using AOT.Framework.ModuleSetting.Runtime.Guide;

// 新增
using Hotfix.Game.Tables.Tables;
```

**修改字段引用：**

```csharp
// StepInfo.m_TargetWindow → StepInfo.TargetWindow
var targetWin = uiModule.GetUI(StepInfo.TargetWindow);
if (targetWin == null)
{
    FuLogger.LogWarning($"[ClickUIStep] 找不到目标界面: {StepInfo.TargetWindow}");
    return;
}

// StepInfo.m_TargetUI → StepInfo.TargetUI
if (targetWin.UIView.GetChild(StepInfo.TargetUI) is not GComponent targetClickUI)
{
    FuLogger.LogWarning($"[ClickUIStep] 找不到目标点击UI: {StepInfo.TargetUI}");
    return;
}
```

**修改 Create 方法签名：**

```csharp
public static ClickUIStep Create(GuideStep stepInfo)
{
    var step = ReferencePool.Acquire<ClickUIStep>();
    step.StepInfo = stepInfo;
    return step;
}
```

- [ ] **Step 3: 改造 DialogStep.cs**

**替换 using：**

```csharp
// 删除
using AOT.Framework.ModuleSetting.Runtime.Guide;

// 新增
using Hotfix.Game.Tables.Tables;
```

**修改字段引用：**

```csharp
// StepInfo.m_DialogContent → StepInfo.DialogContent
GuideAction.DoDialogGuide(StepInfo.DialogContent, Complete);
```

**修改 Create 方法签名：**

```csharp
public static DialogStep Create(GuideStep stepInfo)
{
    var step = ReferencePool.Acquire<DialogStep>();
    step.StepInfo = stepInfo;
    return step;
}
```

- [ ] **Step 4: 改造 WaitStep.cs**

**替换 using：**

```csharp
// 删除
using AOT.Framework.ModuleSetting.Runtime.Guide;

// 新增
using Hotfix.Game.Tables.Tables;
```

**修改字段引用：**

```csharp
// StepInfo.m_WaitTime → StepInfo.WaitTime
if (m_WaitTimer >= StepInfo.WaitTime)
{
    Complete();
}
```

**修改 Create 方法签名：**

```csharp
public static WaitStep Create(GuideStep stepInfo)
{
    var step = ReferencePool.Acquire<WaitStep>();
    step.StepInfo = stepInfo;
    return step;
}
```

- [ ] **Step 5: 改造 DefaultStep.cs**

**替换 using：**

```csharp
// 删除
using AOT.Framework.ModuleSetting.Runtime.Guide;

// 新增
using Hotfix.Game.Tables.Tables;
```

**修改 Create 方法签名：**

```csharp
public static DefaultStep Create(GuideStep stepInfo)
{
    var step = ReferencePool.Acquire<DefaultStep>();
    step.StepInfo = stepInfo;
    return step;
}
```

- [ ] **Step 6: 在 Unity Editor 中验证编译**

全部 .cs 文件编译通过，无报错。

- [ ] **Step 7: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Guide/BaseStep.cs
git add Unity/Assets/Scripts/Hotfix/Framework/Guide/ClickUIStep.cs
git add Unity/Assets/Scripts/Hotfix/Framework/Guide/DialogStep.cs
git add Unity/Assets/Scripts/Hotfix/Framework/Guide/WaitStep.cs
git add Unity/Assets/Scripts/Hotfix/Framework/Guide/DefaultStep.cs
git commit -m "refactor: 步骤类适配 Luban 生成的 GuideStep 数据模型"
```

---

### Task 6: 验证与收尾

**文件：**
- 修改: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs`（如需要适配事件签名变化）

**目的：** 全链路验证 — 编译 → Play 模式 → 引导流程跑通

- [ ] **Step 1: 检查 HotfixLauncher 是否需要适配**

`OnGuideStarted` 和 `OnGuideFinished` 的委托类型从 `Action<string>` 变为 `Action<int>`，检查 `HotfixLauncher.cs` 或任何注册了这些事件的代码是否需要更新。

```bash
grep -rn "OnGuideStarted\|OnGuideFinished\|OnStepChanged\|OnGuideInterrupted" Unity/Assets/Scripts/
```

如果无外部订阅方，无需改动。

- [ ] **Step 2: 在 Unity Editor 中 Play 模式验证**

启动 Play 模式，观察 Console：
- `[GuideModule] 引导管理模块初始化完成. 引导数量: X, 步骤总数量: Y`
- 引导流程正常启动、步骤执行、完成
- 无报错

- [ ] **Step 3: 验证 GuideSetting.asset 移除后场景不报错**

场景中的 `ModuleSetting` GameObject 上不再有 `m_GuideSetting` 字段。Play 模式下 Console 无 MissingReference 相关错误。

- [ ] **Step 4: Commit**

```bash
git add -u
git commit -m "chore: Guide 模块配置表化收尾验证"
```
