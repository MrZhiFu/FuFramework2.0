# FuFramework Variable 模块

## 概述

Variable 模块是 FuFramework 中的变量管理系统，提供基于引用池优化的类型安全变量封装。该模块通过统一的变量接口和隐式转换操作符，让开发者可以像使用原生类型一样使用变量类，同时享受引用池带来的内存优化优势。

### 核心特性

- **类型安全**：为每种数据类型提供专门的变量类
- **内存优化**：基于引用池技术，减少内存分配和GC压力
- **隐式转换**：支持与原生类型之间的隐式转换
- **统一接口**：所有变量类继承自统一的基类接口
- **完整覆盖**：支持基础类型、Unity类型、数组类型等

## 系统架构

### 核心类说明

#### 1. Variable (变量基类)
位于 `Base/Variable.cs`，是所有变量类的抽象基类，实现 `IReference` 接口。

**主要方法：**
- `Type Type` - 获取变量类型
- `object GetValue()` - 获取变量值
- `void SetValue(object value)` - 设置变量值
- `void Clear()` - 清理变量值

#### 2. Variable<T> (泛型变量基类)
位于 `Base/GenericVariable.cs`，提供泛型变量实现。

**主要属性：**
- `T Value` - 获取或设置变量值
- `override Type Type` - 返回泛型类型

#### 3. 具体变量类
模块提供了丰富的变量类型实现，覆盖了常用的数据类型。

## 快速开始

### 基本使用

```csharp
using FuFramework.Variable.Runtime;

// 创建整数变量
var intVar = new VarInt32();
intVar.Value = 100;

// 使用隐式转换
VarInt32 varFromInt = 200;  // 从int隐式转换为VarInt32
int intFromVar = varFromInt; // 从VarInt32隐式转换为int

// 使用引用池获取变量
var pooledVar = ReferencePool.Runtime.ReferencePool.Acquire<VarInt32>();
pooledVar.Value = 300;

// 使用后释放回引用池
ReferencePool.Runtime.ReferencePool.Release(pooledVar);
```

### Unity 类型变量使用

```csharp
using UnityEngine;
using FuFramework.Variable.Runtime;

// 创建Vector3变量
var vectorVar = new VarVector3();
vectorVar.Value = new Vector3(1, 2, 3);

// 隐式转换
VarVector3 varFromVector = new Vector3(4, 5, 6);
Vector3 vectorFromVar = varFromVector;

// GameObject变量
var gameObjectVar = new VarGameObject();
gameObjectVar.Value = gameObject;
```

## 详细使用指南

### 1. 基础类型变量

#### 数值类型
```csharp
// 整数类型
VarInt32 int32Var = 100;
VarInt64 int64Var = 1000L;
VarUInt32 uint32Var = 200u;

// 浮点类型
VarFloat floatVar = 3.14f;
VarDouble doubleVar = 3.14159;

// 其他数值类型
VarDecimal decimalVar = 123.45m;
VarByte byteVar = 255;
```

#### 布尔和字符类型
```csharp
VarBoolean boolVar = true;
VarChar charVar = 'A';
```

#### 字符串类型
```csharp
VarString stringVar = "Hello World";
string normalString = stringVar; // 隐式转换回string
```

### 2. Unity 类型变量

#### 向量和矩阵
```csharp
VarVector2 vector2Var = new Vector2(1, 2);
VarVector3 vector3Var = new Vector3(1, 2, 3);
VarVector4 vector4Var = new Vector4(1, 2, 3, 4);
VarQuaternion quaternionVar = Quaternion.identity;
```

#### 颜色类型
```csharp
VarColor colorVar = Color.red;
VarColor32 color32Var = new Color32(255, 0, 0, 255);
```

#### Unity 对象类型
```csharp
VarGameObject gameObjectVar = gameObject;
VarTransform transformVar = transform;
VarMaterial materialVar = material;
VarTexture textureVar = texture;
```

### 3. 数组类型变量

```csharp
VarByteArray byteArrayVar = new byte[] { 1, 2, 3 };
VarCharArray charArrayVar = new char[] { 'a', 'b', 'c' };
```

### 4. 通用对象类型

```csharp
VarObject objectVar = new object();
VarUnityObject unityObjectVar = anyUnityObject;
```

## 实际应用场景

### 1. 事件系统数据传递

```csharp
// 定义事件参数
public class PlayerLevelUpEventArgs : GameEventArgs
{
    public VarInt32 OldLevel { get; set; }
    public VarInt32 NewLevel { get; set; }
    public VarString PlayerName { get; set; }
}

// 使用事件
var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
args.OldLevel = 10;
args.NewLevel = 11;
args.PlayerName = "Player1";

GameEntry.Event.Fire(this, args);
```

### 2. 配置数据管理

```csharp
public class GameConfig
{
    public VarInt32 MaxPlayerLevel { get; set; }
    public VarFloat PlayerMoveSpeed { get; set; }
    public VarColor PlayerDefaultColor { get; set; }
    public VarVector3 SpawnPosition { get; set; }
}
```

### 3. 游戏状态管理

```csharp
public class PlayerState
{
    public VarInt32 Health { get; set; }
    public VarInt32 Mana { get; set; }
    public VarFloat Stamina { get; set; }
    public VarVector3 Position { get; set; }
    public VarQuaternion Rotation { get; set; }
}
```

### 4. UI 数据绑定

```csharp
public class UIDataModel
{
    public VarString PlayerName { get; set; }
    public VarInt32 PlayerLevel { get; set; }
    public VarFloat HealthPercentage { get; set; }
    public VarColor HealthBarColor { get; set; }
}
```

## 性能优化建议

### 1. 合理使用引用池

```csharp
// 推荐：使用引用池获取变量
var tempVar = ReferencePool.Acquire<VarInt32>();
tempVar.Value = 100;
// 使用变量...
ReferencePool.Release(tempVar);

// 不推荐：频繁创建新实例
var tempVar = new VarInt32(); // 会产生GC压力
```

### 2. 批量操作优化

```csharp
// 批量处理变量
public void ProcessMultipleVariables(List<VarInt32> variables)
{
    foreach (var variable in variables)
    {
        // 处理逻辑
        variable.Value *= 2;
    }
}
```

### 3. 避免不必要的装箱拆箱

```csharp
// 推荐：直接使用泛型变量
VarInt32 intVar = 100;
int value = intVar; // 无装箱拆箱

// 不推荐：频繁使用object接口
Variable baseVar = new VarInt32();
baseVar.SetValue(100); // 涉及装箱
object value = baseVar.GetValue(); // 涉及拆箱
```

## API 参考

### Variable 基类

| 方法 | 说明 |
|------|------|
| `Type Type` | 获取变量类型 |
| `object GetValue()` | 获取变量值 |
| `void SetValue(object value)` | 设置变量值 |
| `void Clear()` | 清理变量值 |

### Variable<T> 泛型基类

| 属性/方法 | 说明 |
|-----------|------|
| `T Value` | 获取或设置变量值 |
| `override Type Type` | 返回泛型类型 |
| `override object GetValue()` | 返回变量值 |
| `override void SetValue(object value)` | 设置变量值 |
| `override void Clear()` | 清理变量值 |
| `override string ToString()` | 返回变量字符串表示 |

### 支持的变量类型

#### 基础类型
- `VarBoolean` - bool
- `VarByte` - byte
- `VarSByte` - sbyte
- `VarChar` - char
- `VarInt16` - short
- `VarInt32` - int
- `VarInt64` - long
- `VarUInt16` - ushort
- `VarUInt32` - uint
- `VarUInt64` - ulong
- `VarFloat` - float
- `VarDouble` - double
- `VarDecimal` - decimal
- `VarString` - string
- `VarDateTime` - DateTime

#### 数组类型
- `VarByteArray` - byte[]
- `VarCharArray` - char[]

#### Unity 类型
- `VarVector2` - Vector2
- `VarVector3` - Vector3
- `VarVector4` - Vector4
- `VarQuaternion` - Quaternion
- `VarColor` - Color
- `VarColor32` - Color32
- `VarRect` - Rect
- `VarGameObject` - GameObject
- `VarTransform` - Transform
- `VarMaterial` - Material
- `VarTexture` - Texture
- `VarUnityObject` - UnityEngine.Object

#### 通用类型
- `VarObject` - object

## 注意事项

### 1. 内存管理
- 变量类使用引用池管理，使用后应及时释放
- 避免长时间持有变量引用，防止内存泄漏
- 在频繁创建变量的场景中，优先使用引用池

### 2. 类型安全
- 使用隐式转换时注意类型匹配
- 设置值时确保类型兼容性
- 对于复杂类型，建议使用泛型变量类

### 3. 性能考虑
- 在性能敏感的场景中，避免频繁的变量创建和释放
- 对于简单的值类型，考虑直接使用原生类型
- 批量操作时使用列表或数组存储变量

### 4. 线程安全
- 变量类本身不是线程安全的
- 在多线程环境中使用时需要额外的同步机制
- 建议在单线程环境中使用变量类

## 常见问题解答

### Q: 什么时候应该使用 Variable 模块？
A: 在需要频繁创建和销毁变量对象的场景中，特别是事件系统、数据绑定、配置管理等需要类型安全且内存高效的情况。

### Q: Variable 模块和直接使用原生类型有什么区别？
A: Variable 模块提供了引用池优化，减少GC压力，同时提供统一的接口和隐式转换，让使用更加方便。

### Q: 如何选择使用哪种变量类型？
A: 根据实际数据类型选择对应的变量类，对于自定义类型可以使用 `VarObject` 或创建自定义变量类。

### Q: 变量类的性能如何？
A: 通过引用池优化，变量类在频繁创建和销毁的场景中性能优于直接实例化，但在简单场景中可能略有开销。

### Q: 是否支持自定义变量类型？
A: 是的，可以通过继承 `Variable<T>` 基类来创建自定义变量类型。

## 扩展功能

### 自定义变量类型

```csharp
// 自定义枚举变量
public sealed class VarPlayerState : Variable<PlayerState>
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

public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dead
}
```

### 变量工具类

```csharp
public static class VariableHelper
{
    public static TValue GetValueOrDefault<TValue>(this Variable<TValue> variable, TValue defaultValue)
    {
        return variable != null ? variable.Value : defaultValue;
    }
    
    public static bool TryGetValue<TValue>(this Variable<TValue> variable, out TValue value)
    {
        if (variable != null)
        {
            value = variable.Value;
            return true;
        }
        value = default;
        return false;
    }
}
```

Variable 模块为 FuFramework 提供了强大而灵活的变量管理系统，通过类型安全和内存优化的设计，让开发者可以更加高效地处理各种数据类型，特别适合在游戏开发中的事件系统、配置管理、状态跟踪等场景中使用。