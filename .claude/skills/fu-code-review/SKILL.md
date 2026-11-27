---
name: fu-code-review
description: FuFramework 代码评审清单：五条铁律扫描、三项机械检查（m_ 前缀 / Async 后缀 / E 前缀）、风格核对、项目 traps 复核、范围检查。提交前、评审 PR、或自查改动时使用。
---

# FuFramework 代码评审

按顺序过五关。每关给出**具体证据**（文件:行号），不要只说"通过"。

## 第 1 关：五条铁律（阻塞项）

| 检查 | 怎么查 |
|---|---|
| 无原生 `Task` | grep `\bTask\b`（排除 `UniTask`），看 `using System.Threading.Tasks` |
| 无协程 | grep `IEnumerator` / `StartCoroutine` / `yield return` |
| 无 LINQ | grep `using System.Linq`；`.Where(` / `.Select(` / `.Any(` / `.FirstOrDefault(` |
| 运行时无反射 | grep `Type.GetType` / `GetMethod` / `Activator.CreateInstance` / `GetField`（Editor 程序集豁免） |
| 异步链有所有者 | 每个 `async UniTask` 方法的调用方是否持 Token；API 的 `CancellationToken` 是否必传无默认值 |

**任一违反 = Critical，必须修。**

## 第 2 关：三项机械检查（原 check-style.ps1 移交）

1. **私有字段必须 `m_` 前缀**——不是 `_` 前缀。正则：`^\s*(private|protected)\s+\S+\s+(_\w+)`
2. **`UniTask`/`UniTaskVoid` 方法必须有 `Async` 后缀**。
3. **手写枚举必须有 `E` 前缀**（Luban/Proto/FGUI 生成代码豁免）。

## 第 3 关：风格（rules/coding/coding-standards.md）

- Tab 缩进（不是空格）
- K&R 括号；空方法体同行
- 中文 XML 注释，`<summary>` 换行，`<param>`/`<returns>` 完备
- 访问修饰符显式
- 无魔法数字/字符串
- 单文件 ≤ 500 行
- 文件末尾无换行（约定，不是缺陷）

## 第 4 关：项目 traps 复核

读对应域 `memory/traps-*.md`，逐条对照本次改动。尤其：

- 事件订阅是否成对退订
- 对象池归还是否复位字段
- 资源/网络异步是否传了 Token

## 第 5 关：范围与结构

- 有没有顺手改了无关代码
- 新方法/类是否符合单一职责、边界干净、依赖单向
- 同一逻辑第三次出现是否抽了公共方法

## 输出格式

```
## 评审结果

### Critical（必须修）
- 文件:行 — 问题 — 建议

### Important（应该修）
- ...

### Minor（可延后）
- ...

### 通过项
- 五条铁律 / 三项机械检查 / 风格 / traps / 范围
```

**不要为了显得有用而编造问题**；也不要因为"看起来没问题"就说通过——每关必须真的查。
