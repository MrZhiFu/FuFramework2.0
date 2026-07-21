# Guide 模块配置表化设计

## 目标

将 Guide 模块的数据源从 ScriptableObject (`GuideSetting.asset`) 迁移到 Luban 配置表，参照 RedDot 模块的配置驱动模式，不再挂载到 Launcher/ModuleSetting 的序列化字段中。

## 配置表结构（双表方案）

### 引导主表 TbGuide

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (key) | 引导 ID |
| Name | string | 引导名称 |
| StartStepId | int | 起始步骤 ID |

### 步骤表 TbGuideStep

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (key) | 步骤 ID |
| GuideId | int | 所属引导 ID（外键，关联 TbGuide.Id） |
| StepType | EStepType (Luban enum) | 步骤类型 |
| NextStepId | int? | 下一步骤 ID（null = 结束） |
| CanJump | bool | 是否可跳过 |
| TargetWindow | string | 目标窗口（ClickUI 用） |
| TargetUI | string | 目标 UI（ClickUI 用） |
| DialogContent | string | 对话内容（Dialog 用） |
| WaitTime | float | 等待时间（Wait 用） |

### Luban 枚举 EStepType

| 值 | 说明 |
|----|------|
| None | 无类型 |
| ClickUI | 点击 UI 引导 |
| Dialog | 对话引导 |
| Wait | 等待步骤 |

## 代码改动

### AOT 删除

- `GuideSetting.cs` — ScriptableObject，不再需要
- `GuideInfo.cs` — 运行时数据结构，由 Luban 生成的 `Guide` 行数据替代
- `StepInfo.cs` — 运行时数据结构，由 Luban 生成的 `GuideStep` 行数据替代；`EStepType` 枚举改为 Luban 生成
- `GuideSettingEditor.cs` — Inspector 编辑器，不再需要
- `GuideSettingCreator.cs` — 创建器，不再需要
- `GuideSetting.asset` — 配置文件，不再需要

### AOT 修改

- `ModuleSetting.cs` — 移除 `m_GuideSetting` 字段及 `GuideSetting` 属性，保留 `m_OpenGuide` 开关

### Hotfix 修改

- `GuideModule.cs` — `OnInit()` 中从 `ConfigModule` 获取 `TbGuide` 和 `TbGuideStep` 构建运行时数据；公开 API 的 ID 参数从 `string` 改为 `int`
- `BaseStep.cs` — 数据引用从 `StepInfo` 改为 Luban 生成的 `GuideStep` 行数据
- 各步骤子类（`ClickUIStep.cs`、`DialogStep.cs`、`WaitStep.cs`、`DefaultStep.cs`）— 适配新数据模型

### Launcher 保持不变

`HotfixLauncher.EnterGame()` 中 `OpenGuide` 判断、`GuideAction` 注入和 `StartFirstGuide()` 调用保留不动。

### Luban 配置

- 新增 Excel 配置文件（策划填写引导和步骤数据）
- `TableManager.cs` — 注册 `TbGuide` 和 `TbGuideStep`
- Luban 自动生成：`Guide.cs`、`GuideStep.cs`、`TbGuide.cs`、`TbGuideStep.cs`、`EStepType` 枚举

## GuideModule 改造后加载流程

```
OnInit() →
  1. 从 ConfigModule 获取 TbGuide、TbGuideStep 两张配置表
  2. 以 Id 为 key 构建运行时 Guide 字典和 Step 字典
  3. 验证步骤链完整性（NextStepId 指向的步骤是否存在）
  4. null 检查：配置表不存在则打错误日志并跳过
```

## 已确认的决策

- 双表方案（TbGuide + TbGuideStep）
- 移除所有旧的 ScriptableObject 相关文件
- int key，非枚举
- NextStepId 可空，null 表示结束
- EStepType 由 Luban 生成
- OpenGuide 开关保留在 ModuleSetting/GameSetting 中
- Launcher 中 GuideAction 注入和自动启动逻辑保持不变
