# RedDot 模块重构设计

## 元信息

- **日期**：2026-07-26
- **范围**：`Unity/Assets/Scripts/Hotfix/Framework/RedDot/` 目录下所有源文件
- **目标**：消除静态/动态双轨制 API 重复，提升代码质量和可维护性

## 动机

当前 RedDot 模块存在两个核心问题：

1. **双轨制 API 重复**：几乎所有公开 API 都有 `ERedDotKey` 和 `string` 两套重载（Register、Unregister、GetState、HasNode、MarkRead、IsRead），内部用两本 Dictionary + 两个 HashSet 分别存储和追踪，维护成本高。
2. **partial 拆分不自然**：`RedDotModule.cs` + `RedDotModule.Feature.cs` 的职责边界模糊（Feature 依赖 Core 的全部私有容器），实际是一个类的两部分而非两个独立关注点。

## 设计方案

### 1. RedDotKey 统一标识符（新增）

引入 `RedDotKey` 值类型，统一替代 `ERedDotKey` 枚举和 `string` 动态 Key：

```csharp
public readonly struct RedDotKey : IEquatable<RedDotKey>
{
    private readonly string m_Value;

    private RedDotKey(string value) => m_Value = value;

    public static implicit operator RedDotKey(ERedDotKey key) => new(key.ToString());
    public static implicit operator RedDotKey(string key) => new(key);

    public override string ToString() => m_Value ?? "";
    public bool Equals(RedDotKey other) => string.Equals(m_Value, other.m_Value);
    public override bool Equals(object obj) => obj is RedDotKey other && Equals(other);
    public override int GetHashCode() => m_Value?.GetHashCode() ?? 0;
}
```

**设计决策**：
- 内部统一为 `string` 存储：枚举值调用 `ToString()`，动态字符串原样保存
- 隐式转换让现有调用 `Register(ERedDotKey.Mail, ...)` 和 `GetState("Mail_1001")` 无需修改
- `GetHashCode` / `Equals` 基于字符串值，可直接用作 `Dictionary` / `HashSet` 的 Key
- 枚举值转字符串后保持唯一映射（`ERedDotKey.Mail` → `"Mail"`），不会与同名动态 Key 冲突是设计上期望的行为：配置表保证枚举 Key 之间不重名，动态 Key 通过 `__dynamic__{parent}_{id}` 格式天然隔离

### 2. 公开 API 统一

所有双轨 API 合并为单一 `RedDotKey` 参数版本：

```csharp
// 注册/注销 — 隐式转换兼容 ERedDotKey 和 string 两种调用方式
void Register(RedDotKey key, Func<int> calculator, params string[] triggerEvents);
void Register(RedDotKey parentKey, RedDotKey dynamicKey, Func<int> calculator, params string[] triggerEvents);
void Unregister(RedDotKey key);

// 状态查询
RedDotState GetState(RedDotKey key);
bool HasNode(RedDotKey key);

// 已读持久化
void MarkRead(RedDotKey key);
bool IsRead(RedDotKey key);

// 清理策略
void TryAutoClean(RedDotKey key);

// 动态红点批量同步
void SyncDynamicNode(RedDotKey parentKey, IReadOnlyList<long> ids, Func<long, int> calculator);
```

**调用方兼容性**：所有现有调用方式不变，隐式转换自动适配：
- `Register(ERedDotKey.Mail, ...)` → 不变
- `GetState("Mail_1001")` → 不变
- `Register(ERedDotKey.Mail, "Mail_1001", ...)` → 不变

### 3. 内部存储统一

| 容器 | 重构前 | 重构后 |
|------|--------|--------|
| 节点存储 | `Dictionary<ERedDotKey, RedDotNode>` + `Dictionary<string, RedDotNode>` | `Dictionary<RedDotKey, RedDotNode>` |
| 变更追踪 | `HashSet<ERedDotKey>` + `HashSet<string>` | `HashSet<RedDotKey>` |
| 事件→节点映射 | `Dictionary<string, HashSet<RedDotNode>>` | 不变 |

重算流程中 `OnNodeTotalCountChanged` 的回调只需从 `node.Key` 获取标识，无需再判断 `StaticKey.HasValue`。

### 4. 文件重组

取消 partial，合并为 4 个文件：

```
RedDot/
├── RedDotKey.cs               # 新增：统一标识符结构体
├── RedDotModule.cs             # 重构：核心协调（合并原 .Feature.cs）
├── RedDotNode.cs               # 修改：Key 字段统一，构造方法简化
├── RedDotState.cs              # 不变
└── RedDotChangedEventArgs.cs   # 微调：双 List → 单 List<RedDotKey>
```

**合并理由**：`RedDotModule.Feature.cs` 中的 SyncDynamicNode、MarkRead、TryAutoClean 等方法与核心逻辑共享同一套私有容器（节点字典、脏标记集合、事件映射），拆分为独立类需要大量 internal 暴露，收益不成比例。合并后约 350-400 行，仍在合理范围。

### 5. RedDotNode 调整

- `StaticKey` (`ERedDotKey?`) + `DynamicKey` (`string`) → `Key` (`RedDotKey`)
- `Create()` 从 6 个独立参数改为接收配置行对象 `TbRedDotRow`，减少参数个数，新增配置字段时无需改 `Create` 签名
- `CreateDynamic()` 参数从 `string key` 改为 `RedDotKey key`
- `AddChild` 日志中的 `child.StaticKey` 改为 `child.Key`
- `IsRead` / `IsActive` / `IsDirty` 等运行时字段不变

### 6. RedDotChangedEventArgs 调整

- `List<ERedDotKey> ChangedStaticKeys` + `List<string> ChangedDynamicKeys` → `List<RedDotKey> ChangedKeys`
- 广播时从单一 `m_ChangedKeySet` 填充，UI 端统一遍历

### 7. CompRedDot 适配（不在 Framework 目录，仅做 API 适配）

- `m_StaticKey` (`ERedDotKey?`) + `m_DynamicKey` (`string`) → `m_Key` (`RedDotKey`)
- `OnRedDotChanged` 中不再遍历两个 List，改为检查 `m_Key` 是否在 `args.ChangedKeys` 中
- `customData` 解析逻辑不变，枚举解析成功用 `ERedDotKey` 构造 `RedDotKey`，否则用 string 构造

## 不涉及的范围

- 红点的计算模型和聚合逻辑不变（Pull + Calculator + Any/Sum）
- OnUpdate 批处理机制不变
- 对象池（IReference）机制不变
- 已读持久化的 StorageModule 交互方式不变
- 配置表 `TbRedDot` 结构不变
- CompRedDot 的 UI 刷新逻辑不变

## 风险与注意事项

1. **RedDotKey 哈希冲突**：枚举值和动态字符串在 `Dictionary` 中共存，需确保 `FormatDynamicKey` 生成的 Key 不会与枚举名碰撞。当前 `__dynamic__{parent}_{id}` 格式可避免此问题。
2. **序列化兼容**：`MarkRead` 持久化的 `m_ReadSet` 当前存 `int`（枚举值），改为 `RedDotKey` 后需存储 `string` 或保持 `int` 仅对静态键。**决定**：已读仅对静态枚举键有意义（动态节点不持久化），保持 `HashSet<int>` 不变。
3. **向后兼容**：所有现有调用方通过隐式转换保持兼容，无 breaking change。
