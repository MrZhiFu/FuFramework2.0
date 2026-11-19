# 模块常量收敛至 Const 分部——设计文档

日期：2026-11-19
状态：已批准（2026-11-19 会话中用户拍板两个裁决）

## 意图与动因

将 Hotfix/Framework 下各模块散落的运行时参数常量收敛到 `<模块目录>/<模块名>.Const.cs`（partial 分部类常量成员，单一事实源），与先例 `NetworkModule.Const.cs` 对齐。流程为循环式：每完成一个模块即汇报，用户确认后提交该模块，再进入下一个，直至全部处理完毕。

## 裁决记录

| 裁决点 | 结论 |
|---|---|
| Core 模块处理 | **整体跳过**——GlobalModule + 20 个工具类常量均为工具/算法内部值，不做整理 |
| A4 参数链删除 | **纳入工作范围**——API 默认值参数若全项目无定制调用方，删参数、内部直取 Const；沿调用链删干净 |
| Editor-only 工具常量 | 就地保留（调试面板自包含），但模块提交前检查其引用切换 |
| 算法固定参数 | 就地保留（XxHash/Hash3/Crc32/Aes 等算法自洽，Core 已整体跳过故不影响） |
| 就地错误文案 | `const string errorMessage` 就地保留（先例标准） |
| 单位换算式 | `* 1000` 等就地保留 |

## 篇章（用户确认提交顺序）

| 序 | 模块 | const 声明 | 说明 |
|---|---|---|---|
| 1 | Web | 3 | — |
| 2 | Sound | 2 | — |
| 3 | ObjectPool | 4 | — |
| 4 | Download | 5 | — |
| 5 | Storage | 5 | — |
| 6 | RedDot | 5 | — |
| 7 | UI | 13 | — |
| 8 | 12 个零常量模块 | 0 | 逐个快速确认后跳过（Asset/Config/Entity/Event/FSM/Guide/Localization/Model/Mono/Procedure/Scene/Timer） |

Network 视作已完成（先例），不再动。

## 每模块流程

1. 盘点甄别（const 声明 + 魔法数字扫描，按判定标准分类）
2. 建 `<模块名>Module.Const.cs`，`#region` 语义分组、每常量 `/// <summary>` 写取值依据与单位
3. 迁移常量 + 全项目引用切换（含调试窗口代码）
4. A4：API 默认值参数无定制调用方 → 删参改直取 Const，沿调用链删干净
5. 编译验证：`unity-cli tool call refresh_assets` → 轮询 `get_compilation_state` 至 `errorCount: 0`
6. README 检查：有变化则更新
7. 汇报修正清单 → 用户确认 → 提交该模块 → 下一模块

## 验收标准

- 全部 8 个篇章完成后：全 Framework 下（除 Core/Network/Editor-only/算法常量外）零散落运行时参数常量
- 每模块提交前编译归零
- 旧常量名全项目零残留
