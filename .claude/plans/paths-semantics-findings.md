# paths: 语义实测结论

日期：2026-11-24
环境：Claude Code 2.1.197（Windows 11）

## 结论汇总

| # | 问题 | 结论 | 依据 |
|---|---|---|---|
| A | 带 `paths:` 的规则是否命中才加载 | **是**。启动时不加载，Read 到匹配文件时惰性注入 | 官方 memory 文档 + 本会话实测（新建后无注入，符合"会话启动时扫描"预期） |
| B | 无 `paths:` 的规则是否每次常驻 | **是**，优先级同 CLAUDE.md，`load_reason = session_start` | 官方 memory 文档 |
| C | 会话中途新建规则 | **不即时生效**，需重启/新会话才被索引 | 本会话实测（新建探针后 Read 命中路径，无注入） |
| D | frontmatter 格式 | `paths:` YAML 列表；YAML 解析失败会**静默降级为常驻**，需 `claude --debug` 才见错误 | 官方文档 + issue #45587 |
| E | glob 匹配基准 | **仓库根**（含该 `.claude` 的目录），非规则文件所在子目录；必须正斜杠 | 官方文档 + issue #61357 |
| F | 本版本特有限制 | **2.1.197 只有 Read 触发路径规则**；Write/Edit 触发是 2.1.288 才修复的 | changelog |

## 设计适配裁决

**Ruling 1：所有规则统一单个 paths pattern，用花括号合并多个范围。**
理由：issue #45587 报告「多个 paths 只评估第一个 pattern」，2.1.197 是否受影响无法确认。花括号单 pattern 在两种实现下都正确，零成本规避。展开预算 1000/规则，本项目用量远低于此。
落点：`Protobuf/**` + `**/Network/**` → `"{Protobuf/**,**/Network/**}"`；同理 localization、fgui、powershell 三处。

**Ruling 2：F 项限制（只有 Read 触发）不额外处理。**
理由：Edit/Write 前必须先 Read 目标文件（工具约束），规则在 Read 时已注入。工作流天然闭合。

**Ruling 3：规则文件命名避开 `[` 等 glob 元字符影响（文件名本身不参与匹配，此项仅记录）。**

## 终验（可选，未执行即视为「已按规避方案落地」）

探针目录已删除——提交前不得保留探针（会污染常驻上下文）。若确需终验多 pattern 行为，临时创建：
`.claude/rules/_probe/semantic-probe.md`，frontmatter 写两条 paths（第一条 `no-such-dir-xyz/**` 故意不命中，第二条 `.claude/plans/**`），新开会话后 Read 本目录文件，看注入是否出现。验完立即删除。

无论终验结果如何，Ruling 1（全部规则用单花括号 pattern）都已经消除了该风险。
