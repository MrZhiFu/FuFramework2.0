# RuleCheck - 组件属性规范检查器

FairyGUI 编辑器插件，检查组件 XML 中的属性规范并输出报告，支持检查 + 一键自动修复。

## 检查项

针对所有包（忽略 `Sample` 包）内组件 XML 的 `text` / `richtext` 节点：

| 检查项 | 规则 | 自动修复行为 |
| ------ | ---- | ------------ |
| 自动清除文本 | 未设置 `autoClearText`（或为 false）时记录错误 | 设置 `autoClearText="true"` 并保存 |
| 字体属性 | 显式设置了 `font="Microsoft YaHei"` 时记录错误 | 移除 `font` 属性并保存 |

> **字体软规范**：字体在 Unity 侧 ConfigSetting 中统一设置，FGUI 编辑器中需标记为"Microsoft YaHei"（由生成/发布链路识别），因此编辑器内不应显式设置该字体属性。

- 错误输出：控制台警告 + 插件目录下 `check_error.txt`（按时间戳分批追加），过程日志写 `check_log.txt`
- 修复后的 XML 回写组件文件，完成后自动刷新工程
- 目前仅检查文本/富文本属性，新检查项可在 `RuleCheck.lua` 中按 `travelXml` 选择器模式扩展

## 使用方式

菜单路径：**工具 → 自定义-组件属性规范检查器**

```
工具 → 自定义-组件属性规范检查器
├── 检查文本属性              ← 仅检查并报告
└── 检查并自动设置文本属性     ← 检查 + 自动修复并保存
```

## 文件结构

```
RuleCheck/
├── main.lua          ← 插件入口（菜单注册、生命周期）
├── RuleCheck.lua     ← 核心逻辑（XML 解析、检查项、自动修复）
├── Tool.lua          ← 日志工具
├── package.json      ← 插件描述
└── README.md         ← 本文件
```
