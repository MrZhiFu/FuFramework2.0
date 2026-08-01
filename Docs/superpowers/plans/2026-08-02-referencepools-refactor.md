# ReferencePools 重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将引用池从静态类 `ReferencePool` 重构为实例化模块 `ReferencePoolModule`，同步完成性能优化、代码风格规范化与文档重写。

**Architecture:** 删除静态 `ReferencePool` 类与嵌套 `ReferenceCollection`；池状态（`Dictionary<Type, ReferenceCollection>`）与全部 API 迁入 `ReferencePoolModule` 实例；闲置存储由 `Queue` 改 `Stack`（LIFO）；重复释放检测无条件保留；删除非泛型反射重载与严格检查配置。全部 130+ 调用方由 `ReferencePool.X` 迁移为 `GlobalModule.ReferencePoolModule.X`。

**Tech Stack:** C# (Hotfix / HybridCLR)、Unity、unity-cli（编译与 Play 验证）。

## Global Constraints

- **交流与注释**：全程中文；代码注释全中文，XML 文档注释 `<summary>` 换行。
- **代码风格**（`Docs/代码风格规范.md`）：方法一律 PascalCase（含私有）；私有字段 `m_` 前缀；Tab 缩进；K&R 括号；显式访问修饰符；能用 `readonly` 就加；`new()` 简化语法；局部变量用 `var`。
- **Git 提交**（`Docs/Git提交规范.md`）：Conventional Commits + 中文 + `[AI]` 前缀（`[AI]refactor:` / `[AI]docs:`）；**任何 git add/commit 前必须征得用户同意**；只 add 本任务相关文件，不波及工作区其他未提交改动（当前工作区有 Asset/Entity/Scene/Sound 等改动，勿带入）。
- **编译门禁**：每个任务结束必须经 unity-cli 编译**零错误**。前置检查：`unity-cli system ping` 返回 `"pong"`（若未返回，请用户先打开 Unity 项目）。具体编译触发命令以 `unity-cli tool list` 列出的可用工具为准。
- **模块访问入口**：`GlobalModule.ReferencePoolModule` 已存在于 `GlobalModule.cs:40`，**禁止重复添加**。
- **注册时序约束**：引用池必须在 `ReferencePoolModule` 注册后使用（`HotfixLauncher.RegisterBaseModules()` 中注册顺序第 2，已保证；不得改动该顺序）。
- **范围排除**：`GetAllReferencePoolInfos` 中空集合仍留在字典的现状不改（spec 明确排除）；不引入测试设施（项目无 test asmdef）。

---

### Task 1: 模块实例化 + 调用方迁移

本任务将新的实例化 `ReferencePoolModule` 落地，并把全部 ~90 个调用方文件迁移过去。旧静态 `ReferencePool` 在本任务中**保留**（作为迁移期桥梁，Task 2 删除），保证本任务结束时编译通过。

**Files:**
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePoolModule.cs`
- Create: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePoolModule.ReferenceCollection.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/**/*.cs`（~90 个调用方文件，脚本批量替换）

**Interfaces:**
- Consumes: `ModuleBase`（`Hotfix.Framework.Core`）、`IReference`、`ReferencePoolInfo`（同命名空间）、`GlobalModule`（`Hotfix.Framework.Core`）
- Produces: `ReferencePoolModule` 实例方法 `Acquire<T>()` / `Release(IReference)` / `Add<T>(int)` / `Remove<T>(int)` / `RemoveAll<T>()` / `ClearAll()` / `GetAllReferencePoolInfos()` / `Count`；嵌套 `ReferenceCollection`（`Stack` 存储、无条件查重）。后续任务与调用方依赖这些签名。

- [ ] **Step 1: 重写 `ReferencePoolModule.cs`**

将整个文件替换为以下内容（注意：无 `using UnityEngine;`、无 `using AOT.Framework.Core.Log;`，旧模块已无 `FuLogger`/`SerializeField` 用法）：

```csharp
using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ReferencePools
{
    /// <summary>
    /// 引用池管理模块。
    /// 功能：
    ///     1. 从引用池获取引用。
    ///     2. 将引用归还引用池。
    ///     3. 获取引用池的数量。
    /// </summary>
    public sealed partial class ReferencePoolModule : ModuleBase
    {
        /// <summary>
        /// 记录指定类型下的引用对象集合的字典, key:指定类型--Value:该类型下的引用对象信息集合
        /// </summary>
        private readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();

        /// <summary>
        /// 获取引用池的数量。
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public int Count => m_ReferenceCollectionDict.Count;

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            ClearAll();
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <returns>引用。</returns>
        public T Acquire<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Acquire<T>();
        }

        /// <summary>
        /// 将引用归还引用池。
        /// </summary>
        /// <param name="reference">要归还的引用。</param>
        public void Release(IReference reference)
        {
            if (reference == null) throw new InvalidOperationException("[ReferencePoolModule] 要归还的引用对象为空.");

            var refType = reference.GetType();
            GetReferenceCollection(refType).Release(reference);
        }

        /// <summary>
        /// 向指定类型的引用池中追加指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">追加数量。</param>
        public void Add<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 从指定类型的引用池中移除指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量。</param>
        public void Remove<T>(int count) where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 从指定类型的引用池中移除所有的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public void RemoveAll<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 清除所有引用池。
        /// </summary>
        public void ClearAll()
        {
            lock (m_ReferenceCollectionDict)
            {
                foreach (var (_, refCollection) in m_ReferenceCollectionDict)
                {
                    refCollection.RemoveAll();
                }

                m_ReferenceCollectionDict.Clear();
            }
        }

        /// <summary>
        /// 获取所有引用池的信息。
        /// </summary>
        /// <returns>所有引用池的信息。</returns>
        public ReferencePoolInfo[] GetAllReferencePoolInfos()
        {
            var index = 0;

            ReferencePoolInfo[] results;

            lock (m_ReferenceCollectionDict)
            {
                results = new ReferencePoolInfo[m_ReferenceCollectionDict.Count];
                foreach (var (type, refCollection) in m_ReferenceCollectionDict)
                {
                    results[index++] = new ReferencePoolInfo(type, refCollection.UnusedReferenceCount, refCollection.UsingReferenceCount,
                        refCollection.AcquireReferenceCount, refCollection.ReleaseReferenceCount,
                        refCollection.AddReferenceCount, refCollection.RemoveReferenceCount);
                }
            }

            return results;
        }

        /// <summary>
        /// 获取指定类型下的引用信息集合。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <returns>引用信息集合。</returns>
        private ReferenceCollection GetReferenceCollection(Type refType)
        {
            if (refType == null) throw new InvalidOperationException("[ReferencePoolModule] 引用类型为空.");

            ReferenceCollection referenceCollection;
            lock (m_ReferenceCollectionDict)
            {
                if (m_ReferenceCollectionDict.TryGetValue(refType, out referenceCollection)) return referenceCollection;
                referenceCollection = new ReferenceCollection(refType);
                m_ReferenceCollectionDict.Add(refType, referenceCollection);
            }

            return referenceCollection;
        }
    }
}
```

- [ ] **Step 2: 创建 `ReferencePoolModule.ReferenceCollection.cs`**

新建文件，内容如下（关键变更：`Stack<IReference>`、`Clear()` 移入锁内、查重在 `Clear()` 前、计数器补 `<summary>` XML 注释；不再需要 `MemberHidesStaticFromOuterClass` 抑制——外层已非静态类）：

```csharp
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentlySynchronizedField
namespace Hotfix.Framework.ReferencePools
{
    public sealed partial class ReferencePoolModule
    {
        /// <summary>
        /// 引用集合(使用栈存储)，即一个引用类型对应一个引用信息集合。
        /// 功能：
        ///     1. 管理指定类型下的所有引用，包括闲置的、正在使用的、已获取过的、释放归还的、新增的、被移除的。
        ///     2. 提供获取、归还、增加、移除等操作。
        /// </summary>
        private sealed class ReferenceCollection
        {
            /// <summary>
            /// 引用池内的引用类型。
            /// </summary>
            public Type RefType { get; }

            /// <summary>
            /// 引用池栈, 存储闲置的引用对象。
            /// </summary>
            private readonly Stack<IReference> m_FreeStack = new();

            /// <summary>
            /// 正在使用的引用数量(从引用池中获取的 + 引用池中不存在时new创建的引用数量 - 释放归还的引用数量)。
            /// </summary>
            public int UsingReferenceCount { get; private set; }

            /// <summary>
            /// 已获取的引用数量(从引用池中获取的 + 引用池中不存在时new创建的引用数量）。
            /// </summary>
            public int AcquireReferenceCount { get; private set; }

            /// <summary>
            /// 释放(归还)的引用数量。
            /// </summary>
            public int ReleaseReferenceCount { get; private set; }

            /// <summary>
            /// 新增的引用数量。
            /// </summary>
            public int AddReferenceCount { get; private set; }

            /// <summary>
            /// 被移除的引用数量。
            /// </summary>
            public int RemoveReferenceCount { get; private set; }

            /// <summary>
            /// 闲置未使用的引用数量(即引用池中的元素数量)。
            /// </summary>
            public int UnusedReferenceCount => m_FreeStack.Count;

            /// <summary>
            /// 初始化引用集合的新实例。
            /// </summary>
            /// <param name="refType">引用类型。</param>
            public ReferenceCollection(Type refType)
            {
                RefType = refType;
            }

            /// <summary>
            /// 从引用池获取引用对象(没有则使用new T()创建)。
            /// </summary>
            /// <typeparam name="T">引用类型。</typeparam>
            /// <returns>引用对象。</returns>
            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != RefType) throw new InvalidOperationException("[ReferencePoolModule.ReferenceCollection] 引用获取失败，引用类型无效.");

                UsingReferenceCount++;
                AcquireReferenceCount++;

                lock (m_FreeStack)
                {
                    if (m_FreeStack.Count > 0)
                        return m_FreeStack.Pop() as T;
                }

                AddReferenceCount++;
                return new T();
            }

            /// <summary>
            /// 释放引用, 将引用归还到引用池中。
            /// </summary>
            /// <param name="reference">要释放的引用。</param>
            public void Release(IReference reference)
            {
                if (reference == null) throw new InvalidOperationException("[ReferencePoolModule.ReferenceCollection] 引用释放失败，引用对象为空.");

                lock (m_FreeStack)
                {
                    // 重复释放检测：无条件保留，杜绝同一对象被同时交给多个持有者
                    if (m_FreeStack.Contains(reference))
                        throw new InvalidOperationException($"[ReferencePoolModule.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，该对象已经被释放.");

                    // 清理引用，清除数据后方便重用该对象
                    reference.Clear();
                    m_FreeStack.Push(reference);
                }

                ReleaseReferenceCount++;
                UsingReferenceCount--;
            }

            /// <summary>
            /// 向引用池中添加指定数量的引用(使用new T()创建)。
            /// </summary>
            /// <typeparam name="T">引用类型。</typeparam>
            /// <param name="count">添加数量。</param>
            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != RefType) throw new InvalidOperationException($"[ReferencePoolModule.ReferenceCollection] 添加引用失败，类型{typeof(T).Name}不是引用池类型.");

                lock (m_FreeStack)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        var reference = new T();
                        m_FreeStack.Push(reference);
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除指定数量的引用。
            /// </summary>
            /// <param name="count">移除数量。</param>
            public void Remove(int count)
            {
                lock (m_FreeStack)
                {
                    if (count > m_FreeStack.Count)
                        count = m_FreeStack.Count;

                    RemoveReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_FreeStack.Pop();
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除所有的引用。
            /// </summary>
            public void RemoveAll()
            {
                lock (m_FreeStack)
                {
                    RemoveReferenceCount += m_FreeStack.Count;
                    m_FreeStack.Clear();
                }
            }
        }
    }
}
```

- [ ] **Step 3: 脚本化替换调用方**

在工作区根目录 `D:\_WorkSpace\Unity\FuFramework2.0` 下创建临时脚本 `Tools/tmp/migrate_refpool.py`（完成后删除），内容如下。该脚本按字节替换，**保留文件原有行尾（LF/CRLF）不变**，避免整文件行尾差异污染 diff：

```python
import os

ROOT = r"Unity/Assets/Scripts/Hotfix"

# 替换规则：字节精确匹配，不影响其他字节（含行尾）
REPLACEMENTS = [
    (b"ReferencePool.Acquire<", b"GlobalModule.ReferencePoolModule.Acquire<"),
    (b"ReferencePool.Release(", b"GlobalModule.ReferencePoolModule.Release("),
    (b"ReferencePool.Add<", b"GlobalModule.ReferencePoolModule.Add<"),
    (b"ReferencePool.Remove<", b"GlobalModule.ReferencePoolModule.Remove<"),
    (b"ReferencePool.RemoveAll<", b"GlobalModule.ReferencePoolModule.RemoveAll<"),
    (b"ReferencePool.ClearAll()", b"GlobalModule.ReferencePoolModule.ClearAll()"),
]

for dirpath, _, filenames in os.walk(ROOT):
    for fn in filenames:
        if not fn.endswith(".cs"):
            continue
        path = os.path.join(dirpath, fn)
        with open(path, "rb") as f:
            data = f.read()
        new = data
        for old, new_ in REPLACEMENTS:
            new = new.replace(old, new_)
        if new != data:
            with open(path, "wb") as f:
                f.write(new)
            print(f"updated: {path}")
```

运行：`python Tools/tmp/migrate_refpool.py`

预期输出：~90 个 `updated:` 行。注意：`GlobalModule.ReferencePoolModule` 中的 `ReferencePoolModule` 不含子串 `ReferencePool.`（`ReferencePool` 后是 `Module` 不是 `.`），不会被误替换；旧静态文件 `ReferencePool.cs` / `ReferencePool.ReferenceCollection.cs` 内部无这 6 种调用模式，不会被改动。

- [ ] **Step 4: 修正 using**

对脚本改动过的每个 .cs 文件：
1. **必加**：确保存在 `using Hotfix.Framework.Core;`（`GlobalModule` 所在命名空间），缺失则补入 using 块末尾。
2. **按需保留**：用 grep 检查该文件是否含 `IReference`（如类声明 `: IReference`）。含 → 保留 `using Hotfix.Framework.ReferencePools;`；不含 → 删除该 using。

判断命令示例：`grep -l 'IReference' Unity/Assets/Scripts/Hotfix/Framework/<改动的文件>`

- [ ] **Step 5: 验证调用方已全部迁移**

运行：
```bash
grep -rn 'ReferencePool\.' --include='*.cs' Unity/Assets/Scripts/Hotfix
```

预期：**仅**旧静态实现文件可能命中（`ReferencePool.cs` 的类声明、`ReferencePool.ReferenceCollection.cs` 错误字符串），其余所有调用方文件必须零命中。若还有调用方文件命中，回到 Step 3/4 处理。

- [ ] **Step 6: 编译验证**

经 unity-cli 触发 Unity 编译（`unity-cli tool list` 确认编译/刷新工具；预期 0 编译错误，0 命名空间缺失）。若报 `IReference` 未找到，恢复对应文件的 `using Hotfix.Framework.ReferencePools;`；若报 `GlobalModule` 未找到，补 `using Hotfix.Framework.Core;`。

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePoolModule.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePoolModule.ReferenceCollection.cs \
        <全部改动的调用方文件>
git commit -m "[AI]refactor: 引用池模块实例化并迁移全部调用方到 GlobalModule.ReferencePoolModule"
```

**提交前征得用户同意。** 只 add 上述文件，不 add 工作区其他未提交改动（Asset/Entity/Scene/Sound/HotfixLauncher）。

---

### Task 2: 删除旧静态实现

**Files:**
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.cs`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.cs.meta`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.ReferenceCollection.cs`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.ReferenceCollection.cs.meta`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/EReferenceStrictCheckType.cs`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/EReferenceStrictCheckType.cs.meta`

**Interfaces:**
- Consumes: Task 1 的迁移结果（调用方已不再引用 `ReferencePool` 静态类）
- Produces: 干净的 `ReferencePools` 目录（仅 `ReferencePoolModule.cs`、`ReferencePoolModule.ReferenceCollection.cs`、`IReference.cs`、`ReferencePoolInfo.cs`）

- [ ] **Step 1: 删除文件**

```bash
cd Unity/Assets/Scripts/Hotfix/Framework/ReferencePools
rm ReferencePool.cs ReferencePool.cs.meta \
   ReferencePool.ReferenceCollection.cs ReferencePool.ReferenceCollection.cs.meta \
   EReferenceStrictCheckType.cs EReferenceStrictCheckType.cs.meta
```

- [ ] **Step 2: 验证零残留引用**

```bash
grep -rn 'ReferencePool\.' --include='*.cs' Unity/Assets/Scripts/Hotfix
```

预期：**零命中**（本任务此 grep 是残留检测的最终确认；若仍有命中说明 Task 1 迁移遗漏，回到 Task 1 处理）。

- [ ] **Step 3: 编译验证**

经 unity-cli 编译，预期 0 错误。此时编译若报"找不到 `ReferencePool`"，即为 Task 1 遗漏的调用方，逐个改回 `GlobalModule.ReferencePoolModule.Xxx`。

- [ ] **Step 4: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.cs.meta \
        Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.ReferenceCollection.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/ReferencePool.ReferenceCollection.cs.meta \
        Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/EReferenceStrictCheckType.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/EReferenceStrictCheckType.cs.meta
git commit -m "[AI]refactor: 删除静态 ReferencePool 与严格检查枚举"
```

**提交前征得用户同意。**

---

### Task 3: README 重写与关联文档修正

**Files:**
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/README.md`
- Modify（机械替换示例）：`Unity/Assets/Scripts/Hotfix/Framework/Event/README.md`、`Unity/Assets/Scripts/Hotfix/Framework/FSM/README.md`、`Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md`、`Unity/Assets/Scripts/Hotfix/Framework/TaskPool/README.md`、`Unity/Assets/Scripts/Hotfix/Framework/Variable/README.md`

**Interfaces:**
- Consumes: Task 1/2 后的最终 API（`GlobalModule.ReferencePoolModule` 实例方法）
- Produces: 与代码一致的文档

- [ ] **Step 1: 修正 5 个非 ReferencePools README**

对 `Event/README.md`、`FSM/README.md`、`ObjectPool/README.md`、`TaskPool/README.md`、`Variable/README.md`：把示例中的 `ReferencePool.` 替换为 `GlobalModule.ReferencePoolModule.`（方法调用），并在对应代码示例的 using 区补 `using Hotfix.Framework.Core;`。逐个核对上下文，确保替换后示例语义正确。

- [ ] **Step 2: 重写 `ReferencePools/README.md`**

按以下变更清单全量重写（未列出的章节保持原语义，仅同步 API 写法）：

1. **§2 核心特性**：删除"灵活配置：支持多种严格检查模式"一条。
2. **§3.1 类继承与实现体系图**：
   - 删除 `EReferenceStrictCheckType` 条目。
   - `ReferencePool (静态类)` → `ReferencePoolModule (实例模块)`。
   - `ReferenceCollection` 内部 `m_FreeQueue: Queue<IReference>` → `m_FreeStack: Stack<IReference>`。
3. **§3.2 引用池架构图**：顶层框改为 `ReferencePoolModule (ModuleBase)`，内部列出 `m_ReferenceCollectionDict: Dictionary<Type, ReferenceCollection>`；删除 `m_EnableStrictCheck / EReferenceStrictCheckType` 相关内容。
4. **§3.3 引用池工作流图**：
   - `Acquire` 流程删除"检查类型 (EnableStrictCheck)"步骤；`m_FreeQueue.Dequeue()` → `m_FreeStack.Pop()`。
   - `Release` 流程删除"检查类型 (EnableStrictCheck)"步骤；`m_FreeQueue.Contains(reference)` 标注为无条件检测（重复释放即抛异常）；`m_FreeQueue.Enqueue` → `m_FreeStack.Push`；`Clear()` 在查重之后。
   - 状态流转图 `m_FreeQueue` → `m_FreeStack`，LIFO 语义。
5. **§3.4 生命周期管理**：删除 `OnInit()` 设置严格检查的部分；仅保留 `OnDispose() → ClearAll()`。
6. **§4.2 ReferencePool（静态类）→ ReferencePoolModule（实例）**：API 清单改写为实例方法，删除 `Acquire(Type)` / `Add(Type,int)` / `Remove(Type,int)` / `RemoveAll(Type)` 与 `EnableStrictCheck`；新增 `Count`。示例访问入口统一写 `GlobalModule.ReferencePoolModule.Xxx`。
7. **§4.3 ReferenceCollection**：`Queue` → `Stack`；"检测重复释放"描述改为无条件执行。
8. **§4.4 ReferencePoolModule**：删除 `EReferenceStrictCheckType` 配置说明与 `OnInit`；仅 `OnDispose`。
9. **§5 使用示例**：所有 `ReferencePool.` → `GlobalModule.ReferencePoolModule.`；代码示例开头补 `using Hotfix.Framework.Core;`（例如 `NetworkMessage` 示例需同时引用两个命名空间）。
10. **§6 目录结构**：修正现有格式错乱（多余的 `├── ` 空行），列表改为：
    ```
    ReferencePools/
    ├── ReferencePoolModule.cs                       # 引用池管理模块
    ├── ReferencePoolModule.ReferenceCollection.cs   # 引用集合实现（Stack）
    ├── IReference.cs                                # 引用接口
    ├── ReferencePoolInfo.cs                         # 引用池信息结构体
    └── README.md                                    # 本文档
    ```
11. **§7 依赖**：改为 `Hotfix.Framework.Core` 提供 `ModuleBase` 基类（删除不存在的 `FuException` 与 `FuLogger` 依赖）；补充说明：使用引用池前必须先注册 `ReferencePoolModule`（`HotfixLauncher.RegisterBaseModules()` 已保证），通过 `GlobalModule.ReferencePoolModule` 访问。
12. **§8 最佳实践**：`PooledObject<T>` 包装器示例改用 `GlobalModule.ReferencePoolModule.Acquire<T>()` / `.Release(...)`；删除"严格检查性能"注意事项。

- [ ] **Step 3: 自检文档一致性**

确认 README 中不再出现：`ReferencePool.`（静态调用）、`EReferenceStrictCheckType`、`EnableStrictCheck`、`Queue`。运行：
```bash
grep -n 'ReferencePool\.\|EReferenceStrictCheckType\|EnableStrictCheck' Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/README.md
```
预期：零命中。

- [ ] **Step 4: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/README.md \
        Unity/Assets/Scripts/Hotfix/Framework/Event/README.md \
        Unity/Assets/Scripts/Hotfix/Framework/FSM/README.md \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md \
        Unity/Assets/Scripts/Hotfix/Framework/TaskPool/README.md \
        Unity/Assets/Scripts/Hotfix/Framework/Variable/README.md
git commit -m "[AI]docs: 重写 ReferencePools README 并修正关联模块文档引用"
```

**提交前征得用户同意。**

---

### Task 4: 启动冒烟验证

**Files:** 无（纯验证；如发现运行时问题则修复并追加提交）

**Interfaces:**
- Consumes: Task 1-3 的最终代码
- Produces: 运行时验证结论

- [ ] **Step 1: 连接检查**

`unity-cli system ping` → 返回 `"pong"`。未连接则请用户打开 Unity 项目后重试。

- [ ] **Step 2: 进入 Play 模式**

经 unity-cli 进入 Play 模式，等待 15~30 秒让 `HotfixLauncher.MainAsync` 完成启动流程（期间会大量触发事件参数、实体、声音等引用池对象的获取/释放）。

- [ ] **Step 3: 检查运行时异常**

查看 Unity Console：
- 预期**无**异常。特别关注：`GlobalModule.ReferencePoolModule` 相关 `NullReferenceException`（若出现，说明引用池在某模块注册前被使用，需排查调用时机）、重复释放异常（若出现，说明原逻辑依赖 Queue FIFO 顺序或存在双释放，需定位具体调用方）。
- 若发现异常：定位并修复（追加 `[AI]fix:` 提交，先征得用户同意），重复 Step 2-3。

- [ ] **Step 4: 退出 Play 模式**

经 unity-cli 退出 Play 模式，验证完成。清理 `Tools/tmp/migrate_refpool.py`（若未在 Task 1 删除）。

---

## Self-Review 备注

- spec 中"调用方迁移（130+ 文件）"在 Task 1 落地；"删除项"在 Task 1（非泛型重载、`_CheckReferenceType`、模块静态属性、`[SerializeField]`）与 Task 2（静态类、枚举）落地；"生命周期时序"由注册顺序保证（Task 1 不改注册，Task 4 冒烟验证）；"README 重写"在 Task 3；"验证方式"在 Task 1/2 编译 + Task 4 冒烟。
- spec 的"提交拆分（Commit 1 refactor + Commit 2 docs）"在本计划中细化为 3 个 `refactor:` 提交 + 1 个 `docs:` 提交：Task 1/2/3 各自编译绿、可独立回滚，最终状态与 spec 一致（旧静态类删除、全部调用方迁移、README 对齐）。
