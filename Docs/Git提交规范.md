# Git 提交规范

所有 commit 必须遵循 Conventional Commits 格式，描述使用中文。

## 类型

| 类型 | 用途 |
|------|------|
| `feat` | 新增功能 |
| `fix` | 修复 bug |
| `docs` | 文档更新（README、注释等） |
| `style` | 代码格式调整（不影响功能，如缩进、空格、分号等） |
| `refactor` | 代码重构（不新增功能、不修复 bug，仅优化结构） |
| `perf` | 性能优化 |
| `test` | 测试相关（添加或修改测试用例） |
| `chore` | 构建过程或辅助工具变动（依赖更新、脚本修改等） |
| `build` | 构建系统修改（Webpack、Vite、Makefile 等） |
| `ci` | CI/CD 配置修改（GitHub Actions、Jenkins 等） |
| `revert` | 回退提交（撤销某次提交） |

## 格式

```
<type>: <中文简短描述>
```

## 示例

```
feat: 新增背包道具排序功能
fix: 修复登录界面偶发崩溃
docs: 更新红点系统README
refactor: 拆分 FsmModule 为 partial class
```
