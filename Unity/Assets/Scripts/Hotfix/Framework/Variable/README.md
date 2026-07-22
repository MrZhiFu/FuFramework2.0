# FuFramework Variable Module

## 1. 概述

Variable 模块是 FuFramework 中的变量管理系统，提供基于引用池优化的类型安全变量封装。该模块为常用数据类型提供专门的变量类，支持隐式转换、内存优化和类型安全操作。

### 1.1 核心特性

- **类型安全**：为每种数据类型提供专门的变量类
- **内存优化**：基于引用池技术，减少内存分配和GC压力
- **隐式转换**：支持与原生类型之间的隐式转换
- **统一接口**：所有变量类继承自统一的基类接口
- **完整覆盖**：支持基础类型、Unity类型、数组类型等

## 系统架构

### 类继承体系

```
IReference (引用池接口)
    ↑
VariableBase (抽象基类)
    ├── Type (抽象属性)
    ├── GetValue() (抽象方法)
    ├── SetValue() (抽象方法)
    └── Clear() (抽象方法)
    ↑
GenericVariable<T> (泛型基类)
    ├── Value (属性)
    ├── Type (实现)
    ├── GetValue() (实现)
    ├── SetValue() (实现)
    ├── Clear() (实现)
    └── ToString() (重写)
    ↑
    ├── VarInt32, VarString, VarVector3... (具体实现类)
    │   └── 隐式转换操作符 (双向)
    └── VarObject (通用对象类型)
```

### 技术架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    VariableBase (抽象基类)                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Type (抽象属性)                                     │   │
│  │  GetValue() (抽象方法)                               │   │
│  │  SetValue() (抽象方法)                               │   │
│  │  Clear() (抽象方法)                                  │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   GenericVariable<T> (泛型基类)                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Value (T类型属性)                                   │   │
│  │  Type (返回typeof(T))                                │   │
│  │  GetValue() (返回Value)                              │   │
│  │  SetValue() (设置Value)                              │   │
│  │  Clear() (Value = default)                           │   │
│  │  ToString() (Value.ToString())                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  VarInt32    │    │  VarVector3  │    │  VarString   │
│  - int       │    │  - Vector3   │    │  - string    │
│  - 隐式转换   │    │  - 隐式转换   │    │  - 隐式转换   │
└──────────────┘    └──────────────┘    └──────────────┘
```

## 3. 核心类详解

### 3.1 Variable

变量抽象基类，实现 IReference 接口，定义变量的统一接口。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Type | Type | 获取变量类型（抽象） |

**核心方法：**

```csharp
// 获取变量值
public abstract object GetValue();

// 设置变量值
public abstract void SetValue(object value);

// 清理变量值（实现 IReference 接口）
public abstract void Clear();
```

### 3.2 GenericVariable<T>

泛型变量基类，继承自 VariableBase，提供类型安全的泛型实现。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Value | T | 获取或设置变量值 |
| Type | Type | 返回 typeof(T) |

**核心方法：**

```csharp
// 获取变量值（装箱）
public override object GetValue() => Value;

// 设置变量值（拆箱）
public override void SetValue(object value) => Value = (T)value;

// 清理变量值
public override void Clear() => Value = default;

// 获取字符串表示
public override string ToString() => Value != null ? Value.ToString() : "<Null>";
```

### 3.3 具体变量类

所有具体变量类都继承自 GenericVariable<T>。大多数类型实现了双向隐式转换操作符，但 VarObject 作为通用对象类型，未实现隐式转换，需要显式操作。

**典型实现模式（带隐式转换）：**

```csharp
public sealed class VarInt32 : GenericVariable<int>
{
    public VarInt32() { }

    // 从原生类型到变量类的隐式转换
    public static implicit operator VarInt32(int value)
    {
        var varValue = ReferencePool.Acquire<VarInt32>();
        varValue.Value = value;
        return varValue;
    }

    // 从变量类到原生类型的隐式转换
    public static implicit operator int(VarInt32 value) => value.Value;
}
```

**VarObject 特殊实现（无隐式转换）：**

```csharp
public sealed class VarObject : GenericVariable<object>
{
    public VarObject() { }
    // 未实现隐式转换操作符，需显式使用
}

// 使用方式
var objVar = ReferencePool.Acquire<VarObject>();
objVar.Value = anyObject;
```

## 4. 支持的变量类型

### 4.1 基础类型

| 变量类 | 原生类型 | 说明 |
|--------|----------|------|
| VarBoolean | bool | 布尔类型 |
| VarByte | byte | 无符号字节 |
| VarSByte | sbyte | 有符号字节 |
| VarChar | char | 字符类型 |
| VarInt16 | short | 16位整数 |
| VarInt32 | int | 32位整数 |
| VarInt64 | long | 64位整数 |
| VarUInt16 | ushort | 16位无符号整数 |
| VarUInt32 | uint | 32位无符号整数 |
| VarUInt64 | ulong | 64位无符号整数 |
| VarFloat | float | 单精度浮点数 |
| VarDouble | double | 双精度浮点数 |
| VarDecimal | decimal | 十进制数 |
| VarString | string | 字符串 |
| VarDateTime | DateTime | 日期时间 |

### 4.2 数组类型

| 变量类 | 原生类型 | 说明 |
|--------|----------|------|
| VarByteArray | byte[] | 字节数组 |
| VarCharArray | char[] | 字符数组 |

### 4.3 Unity 类型

| 变量类 | 原生类型 | 说明 |
|--------|----------|------|
| VarVector2 | Vector2 | 二维向量 |
| VarVector3 | Vector3 | 三维向量 |
| VarVector4 | Vector4 | 四维向量 |
| VarQuaternion | Quaternion | 四元数 |
| VarColor | Color | 颜色（浮点） |
| VarColor32 | Color32 | 颜色（字节） |
| VarRect | Rect | 矩形 |
| VarGameObject | GameObject | 游戏对象 |
| VarTransform | Transform | 变换组件 |
| VarMaterial | Material | 材质 |
| VarTexture | Texture | 纹理 |
| VarUnityObject | UnityEngine.Object | Unity对象基类 |

### 4.4 通用类型

| 变量类 | 原生类型 | 说明 |
|--------|----------|------|
| VarObject | object | 通用对象类型（无隐式转换） |

## 5. 使用示例

### 5.1 基本使用流程

```csharp
using Hotfix.Framework.Variable;
using UnityEngine;

public class VariableExample : MonoBehaviour
{
    private void Start()
    {
        // 创建整数变量（隐式转换）
        VarInt32 intVar = 100;
        Debug.Log($"Int Value: {intVar.Value}");
        
        // 隐式转换回原生类型
        int nativeInt = intVar;
        Debug.Log($"Native Int: {nativeInt}");
        
        // 创建字符串变量
        VarString stringVar = "Hello Variable";
        Debug.Log($"String Value: {stringVar}");
        
        // 创建Vector3变量
        VarVector3 vectorVar = new Vector3(1, 2, 3);
        Debug.Log($"Vector: {vectorVar}");
        
        // 使用引用池获取变量
        var pooledVar = ReferencePool.Acquire<VarInt32>();
        pooledVar.Value = 200;
        Debug.Log($"Pooled Value: {pooledVar.Value}");
        
        // 释放回引用池
        ReferencePool.Release(pooledVar);
        
        // VarObject 使用（无隐式转换）
        var objVar = ReferencePool.Acquire<VarObject>();
        objVar.Value = new { Name = "Test", Id = 1 };
        Debug.Log($"Object Value: {objVar.Value}");
        ReferencePool.Release(objVar);
    }
}
```

### 5.2 使用引用池优化

```csharp
public void ProcessVariables()
{
    // 推荐：使用引用池获取变量
    var tempVar = ReferencePool.Acquire<VarInt32>();
    tempVar.Value = 100;
    
    // 使用变量...
    ProcessValue(tempVar.Value);
    
    // 使用后释放回引用池
    ReferencePool.Release(tempVar);
}

// 不推荐：频繁创建新实例（产生GC压力）
public void ProcessVariablesBad()
{
    var tempVar = new VarInt32(); // 每次调用都创建新实例
    tempVar.Value = 100;
    ProcessValue(tempVar.Value);
    // 无法复用，依赖GC回收
}
```

### 5.3 事件系统数据传递

```csharp
using Hotfix.Framework.Event;

public class PlayerLevelUpEventArgs : GameEventArgs
{
    public static readonly string EventId = typeof(PlayerLevelUpEventArgs).FullName;
    public override string Id => EventId;
    
    public VarInt32 OldLevel { get; set; }
    public VarInt32 NewLevel { get; set; }
    public VarString PlayerName { get; set; }
    
    public static PlayerLevelUpEventArgs Create(int oldLevel, int newLevel, string playerName)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.OldLevel = oldLevel;
        args.NewLevel = newLevel;
        args.PlayerName = playerName;
        return args;
    }
    
    public override void Clear()
    {
        OldLevel = null;
        NewLevel = null;
        PlayerName = null;
    }
}

// 使用事件
public void OnPlayerLevelUp()
{
    var args = PlayerLevelUpEventArgs.Create(10, 11, "Player1");
    var eventModule = ModuleManager.GetModule<EventModule>();
    eventModule.Broadcast(this, args);
}
```

### 5.4 配置数据管理

```csharp
public class GameConfig
{
    public VarInt32 MaxPlayerLevel { get; set; }
    public VarFloat PlayerMoveSpeed { get; set; }
    public VarColor PlayerDefaultColor { get; set; }
    public VarVector3 SpawnPosition { get; set; }
    
    public void LoadConfig()
    {
        MaxPlayerLevel = 100;
        PlayerMoveSpeed = 5.5f;
        PlayerDefaultColor = Color.blue;
        SpawnPosition = new Vector3(0, 1, 0);
    }
}
```

### 5.5 游戏状态管理

```csharp
public class PlayerState
{
    public VarInt32 Health { get; set; }
    public VarInt32 MaxHealth { get; set; }
    public VarFloat Stamina { get; set; }
    public VarVector3 Position { get; set; }
    public VarQuaternion Rotation { get; set; }
    
    public float HealthPercentage => Health.Value / (float)MaxHealth.Value;
    
    public void TakeDamage(int damage)
    {
        Health.Value = Mathf.Max(0, Health.Value - damage);
    }
}
```

### 5.6 批量变量处理

```csharp
public void ProcessMultipleVariables()
{
    // 批量创建变量
    var variables = new List<VarInt32>();
    for (int i = 0; i < 100; i++)
    {
        var var = ReferencePool.Acquire<VarInt32>();
        var.Value = i * 10;
        variables.Add(var);
    }
    
    // 批量处理
    foreach (var variable in variables)
    {
        variable.Value *= 2;
        Debug.Log($"Processed Value: {variable.Value}");
    }
    
    // 批量释放
    foreach (var variable in variables)
    {
        ReferencePool.Release(variable);
    }
}
```

### 5.7 自定义变量类型

```csharp
// 自定义枚举变量
public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

public sealed class VarPlayerState : GenericVariable<PlayerState>
{
    public VarPlayerState() { }
    
    public static implicit operator VarPlayerState(PlayerState value)
    {
        var varValue = ReferencePool.Acquire<VarPlayerState>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator PlayerState(VarPlayerState value) => value.Value;
}

// 使用自定义变量
public void TestCustomVariable()
{
    VarPlayerState state = PlayerState.Moving;
    Debug.Log($"Player State: {state}");
    
    PlayerState nativeState = state;
    if (nativeState == PlayerState.Moving)
    {
        Debug.Log("Player is moving!");
    }
}
```

## 6. 目录结构

```
Hotfix.Framework/Variable/
├── Base/
│   ├── Variable.cs              # 变量抽象基类
│   └── GenericVariable.cs       # 泛型变量基类
├── Type/
│   ├── VarBoolean.cs            # 布尔变量
│   ├── VarByte.cs               # 字节变量
│   ├── VarSByte.cs              # 有符号字节变量
│   ├── VarChar.cs               # 字符变量
│   ├── VarInt16.cs              # 短整型变量
│   ├── VarInt32.cs              # 整型变量
│   ├── VarInt64.cs              # 长整型变量
│   ├── VarUInt16.cs             # 无符号短整型变量
│   ├── VarUInt32.cs             # 无符号整型变量
│   ├── VarUInt64.cs             # 无符号长整型变量
│   ├── VarFloat.cs              # 单精度浮点变量
│   ├── VarDouble.cs             # 双精度浮点变量
│   ├── VarDecimal.cs            # 十进制变量
│   ├── VarString.cs             # 字符串变量
│   ├── VarDateTime.cs           # 日期时间变量
│   ├── VarByteArray.cs          # 字节数组变量
│   ├── VarCharArray.cs          # 字符数组变量
│   ├── VarVector2.cs            # Vector2变量
│   ├── VarVector3.cs            # Vector3变量
│   ├── VarVector4.cs            # Vector4变量
│   ├── VarQuaternion.cs         # 四元数变量
│   ├── VarColor.cs              # Color变量
│   ├── VarColor32.cs            # Color32变量
│   ├── VarRect.cs               # Rect变量
│   ├── VarGameObject.cs         # GameObject变量
│   ├── VarTransform.cs          # Transform变量
│   ├── VarMaterial.cs           # Material变量
│   ├── VarTexture.cs            # Texture变量
│   ├── VarUnityObject.cs        # UnityObject变量
│   └── VarObject.cs             # 通用对象变量
└── README.md                    # 本文档
```

## 7. 依赖模块

- **ReferencePool**: 提供引用池管理，用于变量对象的复用

## 8. 设计特点

### 8.1 引用池优化

所有变量类都实现 IReference 接口，通过引用池管理对象生命周期：

- **获取**：`ReferencePool.Acquire<VarInt32>()`
- **释放**：`ReferencePool.Release(var)`
- **复用**：释放的变量会被回收复用，减少GC压力

### 2. 隐式转换

大多数具体变量类都实现了双向隐式转换操作符：

```csharp
// 原生类型 -> 变量类
VarInt32 var = 100;

// 变量类 -> 原生类型
int value = var;
```

这种设计让变量类可以像原生类型一样使用，无需显式转换。

**注意**：VarObject 作为通用对象类型，未实现隐式转换，需显式使用引用池获取和设置 Value 属性。

### 8.2 隐式转换

通过泛型基类 GenericVariable<T> 实现编译期类型检查：

```csharp
VarInt32 intVar = 100;           // 正确
VarInt32 wrongVar = "string";    // 编译错误
```

### 8.3 类型安全

所有变量类都继承自 VariableBase 基类，提供统一的访问方式：

```csharp
Variable baseVar = new VarInt32();
object value = baseVar.GetValue();
baseVar.SetValue(200);
```

### 8.4 统一接口

1. **事件系统数据传递**：事件参数中使用变量类，支持引用池复用
2. **配置数据管理**：游戏配置使用变量类，便于统一管理和序列化
3. **游戏状态管理**：玩家状态、游戏状态等使用变量类跟踪
4. **UI 数据绑定**：UI数据模型使用变量类，支持数据变更通知
5. **网络消息封装**：网络消息中的字段使用变量类，便于类型转换

## 9. 应用场景

1. **事件系统数据传递**：事件参数中使用变量类，支持引用池复用
2. **配置数据管理**：游戏配置使用变量类，便于统一管理和序列化
3. **游戏状态管理**：玩家状态、游戏状态等使用变量类跟踪
4. **UI 数据绑定**：UI数据模型使用变量类，支持数据变更通知
5. **网络消息封装**：网络消息中的字段使用变量类，便于类型转换

## 10. 注意事项

1. **引用池管理**：使用引用池获取的变量，使用后必须释放，否则会导致内存泄漏
2. **隐式转换开销**：隐式转换会触发引用池的获取操作，频繁转换可能产生开销
3. **装箱拆箱**：通过基类接口访问时会涉及装箱拆箱，性能敏感场景建议使用泛型接口
4. **线程安全**：变量类本身不是线程安全的，多线程环境需要额外同步
5. **空值检查**：使用变量前检查是否为 null，避免 NullReferenceException

## 11. 性能对比

### 11.1 内存分配对比

```csharp
// 方式1：使用 new（产生GC）
for (int i = 0; i < 1000; i++)
{
    var var = new VarInt32 { Value = i }; // 每次循环都分配内存
}

// 方式2：使用引用池（无GC）
for (int i = 0; i < 1000; i++)
{
    var var = ReferencePool.Acquire<VarInt32>();
    var.Value = i;
    ReferencePool.Release(var); // 释放后复用
}
```

### 11.2 访问性能对比

```csharp
VarInt32 var = 100;

// 直接访问（最优）
int value1 = var.Value;

// 隐式转换（轻微开销）
int value2 = var;

// 基类访问（涉及装箱）
Variable baseVar = var;
object value3 = baseVar.GetValue();
```

## 12. 常见问题

### Q1: 什么时候应该使用 Variable 模块？

A: 在以下场景推荐使用：
- 需要频繁创建和销毁变量的场景
- 事件系统的参数传递
- 需要统一管理和序列化的数据
- 对内存分配敏感的场景

### Q2: Variable 和原生类型如何选择？

A: 建议：
- 简单场景直接使用原生类型
- 需要引用池优化时使用 Variable
- 事件参数、配置数据等使用 Variable
- 局部临时变量使用原生类型

### Q: 如何创建自定义变量类型？

A: 继承 GenericVariable<T> 并实现隐式转换：

```csharp
public sealed class VarCustomType : GenericVariable<CustomType>
{
    public VarCustomType() { }
    
    public static implicit operator VarCustomType(CustomType value)
    {
        var varValue = ReferencePool.Acquire<VarCustomType>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator CustomType(VarCustomType value) => value.Value;
}
```

### Q: 变量类是否线程安全？

A: 变量类本身不是线程安全的。在多线程环境中使用时，需要额外的同步机制，如锁或线程本地存储。

### Q: 如何避免内存泄漏？

A: 遵循以下原则：
- 使用 `ReferencePool.Acquire` 获取的变量，必须使用 `ReferencePool.Release` 释放
- 不要在静态变量或单例中长时间持有变量引用
- 确保在异常情况下也能正确释放变量
