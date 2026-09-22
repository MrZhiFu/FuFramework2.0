---
description: Luban 配置表自动化——按 luban-excel skill 的规范处理配置表任务
argument-hint: [配置表任务描述，可留空]
---

处理本次配置表任务，严格遵循项目 `luban-excel` skill：

1. 通过 Skill 工具调用 `luban-excel` skill；若当前会话的可用 skill 列表中没有它，则退化为直接读取并完整遵循 `.claude/skills/luban-excel/SKILL.md`（硬规则速查、表头结构、枚举/bean 定义、生成流程均以该文件为准）。
2. 本次任务：$ARGUMENTS
3. 若任务为空（$ARGUMENTS 无内容），询问用户要执行哪类配置表操作（新增表 / 修改表 / 本地化填词 / 重新生成）再动手。
4. 改完 xlsx 必须走 SKILL.md 的「生成与验证」流程（`Config/` 下跑 `gen-client-json.bat`），不得只改表不生成。
