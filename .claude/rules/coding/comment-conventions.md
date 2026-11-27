---
paths:
  - "**/*.cs"
---

# 注释约定

本文件与 `coding/coding-standards.md` 共同构成编码规范（原 `Docs/代码风格规范.md` 已并入并删除）；注释细则以本文件为准。

FuFramework 的注释以 XML 文档注释为绝对主导，覆盖度远超常规项目。**全部中文**，无一例外。

## XML 文档注释（`///`）

`<summary>` 标签**必须换行**，禁止 `/// <summary>text</summary>` 单行写法：

```csharp
/// <summary>
/// 热更代码入口
/// </summary>
public static class HotfixLauncher
{
    /// <summary>
    /// 启动入口
    /// </summary>
    public static async UniTask MainAsync()
}
```

**覆盖范围**：每个类、方法、属性、字段、枚举成员都要有 `<summary>`，**包括私有方法**。

## `<param>` / `<returns>` 必须完备

每个方法的每个参数都要有 `<param name="...">`，所有非 `void` 返回都要有 `<returns>`。这两个标签允许单行形式。丢弃参数 `_` 也要注明未使用。

```csharp
/// <summary>
/// 请求使用道具
/// </summary>
/// <param name="itemId">道具ID</param>
/// <param name="count">道具数量</param>
/// <returns>是否使用成功</returns>
public async UniTask<bool> RequestUseItemAsync(int itemId, long count = 1)
```

枚举成员同样要有注释：

```csharp
/// <summary>
/// 白
/// </summary>
White = 1,
```

## 行注释（`//`）

仅用于方法体内的简短说明。固定形态：

| 用途 | 写法 |
|---|---|
| 占位/示例 | `// Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);` |
| 待完成 | `// TODO：刷新逻辑` |
| 格式保护 | `//@formatter:off` / `//@formatter:on` |
| ReSharper 抑制 | `// ReSharper disable once CheckNamespace`（每个文件 namespace 前都要有） |

## 过时注释的四类残留（改代码后必查）

1. **旧命名残留**——被更名/重构掉的类型名、目录名、旧 API 签名。
2. **已删除功能的描述**——搜被删功能的关键词（开关/字段/方法名），注释还在讲它怎么用。
3. **悬空注释块**——删了字段/成员后残留的 `/// <summary>` 孤立块。
4. **语义漂移**——措辞与现行结构不符（如"基类 abstract"但已接口化、"覆盖默认值"但参数层已删）。

这四类不会被编译器发现，只能靠 grep + 逐条判定。

## 何时写、何时省

- **必写**：所有类型与成员声明（XML）、非显然的分支意图、算法不直观处。
- **可省**：显而易见的类型声明内部逻辑、自解释的一行实现。
- **禁止**：复述代码字面意思的注释（`// 加一`）；英文注释；过期未更新的注释（改代码必同步改注释）。
