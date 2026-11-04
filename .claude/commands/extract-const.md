# 运行时常量收敛

将 $ARGUMENTS 模块中散落的运行时参数常量，收敛到该模块的 Const 分部（`<模块目录>/<模块名>.Const.cs`），作为 `<模块名>` 分部类的常量成员（单一事实源）。

参考先例：网络模块的 `NetworkModule.Const.cs`（5 分区 16 常量，散落 11 处 + 2 处重复定义归一）。

## 执行流程

### 1. 盘点（先只扫不改，结果汇报给用户拍板排除项）

对模块目录全量扫描（`grep -rn "const \|static readonly" <模块目录> --include="*.cs" | grep -v meta`），另扫内联魔法数：

```
grep -rnE "Delay\([0-9]{2,}|CancelAfter\([0-9]+\)|BufferSize|= [0-9]{2,}f?;" <模块目录> --include="*.cs" | grep -v meta
```

**判定标准（按此分类，排除项列出来让用户确认）**：

| 搬入 Const 分部 | 不搬（就地保留） |
|---|---|
| 运行时可调参数：间隔/超时/上限/阈值/缓冲大小/容量 | 方法内 `const string errorMessage`（就地错误文案） |
| 重复定义的同一常量（多处各自声明同一值——重点揪出） | 单位换算式（`* 1000` 等） |
| 可写 `static` 属性包装的参数（转 const） | 类型标识符（`EventId = typeof(X).FullName` 等） |
| 带默认值的 API 参数且全项目无定制调用方（配合第 4 步参数链删除） | Editor-only 工具常量（调试面板自包含） |
| | 生成物（AutoGen） |

注意 `static readonly` 可写属性包装的参数（如原 `MaxPacketBodyLength { get; set; }`）——先 grep 全项目确认无写入方，才能转 const。

### 2. 设计

- 位置：`<模块目录>/<模块名>.Const.cs`，作为 `<模块名>` partial 分部类的常量成员（引用形态 `<模块名>.XXX`，最短且与 API/其他分部并列）
- 分区组织：`#region` 按语义分组（如：缓冲/超时/间隔/上限），每常量带 `/// <summary>` 写明**取值依据与单位**
- 命名：PascalCase；语义完整的名字（带 DEFAULT 前缀的仅限"确实是兜底语义"的值）
- 若模块分部类不存在（非 partial），将其改为 partial

### 3. 迁移

- 创建 Const 分部文件 → 逐常量搬入 → 全项目引用切换为 `<模块名>.XXX`（含 `<see cref>` 注释引用）→ **删除各文件中的本地死声明**（const 行 + 前置 `///` 注释块 + 后置空行）
- 多文件批量文本替换用 **python 临时脚本文件**执行（不要内嵌 heredoc——多行字符串转义易错）；替换必须大小写敏感（`.`replace 天然敏感，禁用 PowerShell `-replace`）

### 4. 参数链删除（如有 API 默认参数被收编）

若盘点发现 API 带默认值参数且全项目**无定制调用方**（所有调用都用默认值）：删除参数，内部直取 Const 分部常量（YAGNI——不为假想灵活性留参数；真要差异化时再加重载）。沿调用链删干净：方法签名 → 内部实现 → 构造函数 → 下游 Create 工厂，文档注释中的 `<param>` 同步删。

### 5. 验证

- grep 旧常量名 / `ChannelConst` 式旧名：**零残留**
- `unity-cli tool call refresh_assets` → 轮询 `get_compilation_state` 至 `isCompiling: false` → `errorCount: 0`
- 常量声明复查：模块内非 Const 分部的 const 声明仅剩"排除项"

### 6. 提交

提交信息：`[AI]refactor: <模块名>运行时常量收敛至 <模块名>.Const 分部（单一事实源[，API 删可配参数直取常量]）`
**提交前必须先询问用户**。

## 纪律

- 盘点结果与排除项先汇报确认，再动手
- 每步编译验证（0 错才进下一步）
- 不搬运"看起来像常量"的非参数内容——判定标准说了算，拿不准的列出来问用户
