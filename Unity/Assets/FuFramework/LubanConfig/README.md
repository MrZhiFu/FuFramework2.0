# FuFramework LubanConfig Module

## 简介
FuFramework LubanConfig 模块是基于 Luban 配置工具的核心运行时库(从Luaban工具运行时复制过来的)，为 Unity 游戏提供高性能的配置数据序列化和反序列化功能。该模块是游戏配置系统的底层基础，支持二进制、JSON 等多种数据格式的高效处理。

## 核心特性

- **高性能序列化**：提供高效的二进制序列化和反序列化功能
- **类型安全**：基于接口的类型标识系统，确保类型安全
- **内存优化**：优化的字节缓冲区管理，减少内存分配
- **跨平台支持**：支持所有 Unity 平台，包括 IL2CPP
- **AOT 兼容**：通过 link.xml 确保 AOT 编译时的类型保留
- **错误处理**：完善的序列化异常处理机制

## 核心类说明

### BeanBase
配置数据基类，继承自 `ITypeId` 接口。
- **职责**：
  1. 提供类型标识的基础实现
  2. 作为所有配置数据类的基类
  3. 支持序列化和反序列化操作

### ByteBuf
高性能字节缓冲区类，实现序列化核心功能。
- **职责**：
  1. 管理字节数据的读写操作
  2. 提供各种数据类型的序列化方法
  3. 支持缓冲区状态保存和恢复
  4. 实现内存优化和性能优化

### ITypeId
类型标识接口，定义类型识别机制。
- **职责**：
  1. 提供类型唯一标识符
  2. 支持运行时类型识别
  3. 确保序列化时的类型安全

### StringUtil
字符串工具类，提供对象序列化到字符串的功能。
- **职责**：
  1. 将对象转换为可读字符串
  2. 支持数组和集合的字符串表示
  3. 提供字典类型的字符串格式化

### SerializationException
序列化异常类，处理序列化过程中的错误。
- **职责**：
  1. 封装序列化错误信息
  2. 提供详细的错误上下文
  3. 支持异常链传递

### EDeserializeError
反序列化错误枚举，定义反序列化过程中的错误类型。
- **职责**：
  1. 定义反序列化错误代码
  2. 提供错误状态标识
  3. 支持错误处理逻辑

## 技术架构

### 命名空间
所有核心类都位于 `LuBan.Runtime` 命名空间下。

### 依赖关系
- **SimpleJSON.Runtime**：JSON 解析支持
- **Unity Engine**：基础运行环境

### AOT 兼容性
通过 `link.xml` 文件确保在 IL2CPP AOT 编译时保留所有必要的类型信息。

## 使用指南

### 1. 基础类型序列化
```csharp
using LuBan.Runtime;

public class BasicSerializationExample : MonoBehaviour
{
    private void Start()
    {
        // 创建字节缓冲区
        var byteBuf = new ByteBuf(1024);
        
        // 序列化基本数据类型
        byteBuf.WriteBool(true);
        byteBuf.WriteInt(42);
        byteBuf.WriteLong(123456789L);
        byteBuf.WriteFloat(3.14f);
        byteBuf.WriteDouble(2.71828);
        byteBuf.WriteString("Hello, Luban!");
        
        // 重置读取位置
        byteBuf.ReaderIndex = 0;
        
        // 反序列化数据
        bool boolValue = byteBuf.ReadBool();
        int intValue = byteBuf.ReadInt();
        long longValue = byteBuf.ReadLong();
        float floatValue = byteBuf.ReadFloat();
        double doubleValue = byteBuf.ReadDouble();
        string stringValue = byteBuf.ReadString();
        
        Debug.Log($"反序列化结果: {boolValue}, {intValue}, {longValue}, {floatValue}, {doubleValue}, {stringValue}");
    }
}
```

### 2. 自定义配置类序列化
```csharp
using LuBan.Runtime;

// 自定义配置类，继承自 BeanBase
public class PlayerConfig : BeanBase
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Level { get; set; }
    public float Health { get; set; }
    public List<int> Skills { get; set; } = new();
    
    public override int GetTypeId()
    {
        return 1001; // 唯一的类型标识符
    }
    
    // 序列化方法
    public void Serialize(ByteBuf byteBuf)
    {
        byteBuf.WriteInt(PlayerId);
        byteBuf.WriteString(PlayerName);
        byteBuf.WriteInt(Level);
        byteBuf.WriteFloat(Health);
        
        // 序列化列表
        byteBuf.WriteInt(Skills.Count);
        foreach (var skill in Skills)
        {
            byteBuf.WriteInt(skill);
        }
    }
    
    // 反序列化方法
    public void Deserialize(ByteBuf byteBuf)
    {
        PlayerId = byteBuf.ReadInt();
        PlayerName = byteBuf.ReadString();
        Level = byteBuf.ReadInt();
        Health = byteBuf.ReadFloat();
        
        // 反序列化列表
        int skillCount = byteBuf.ReadInt();
        Skills.Clear();
        for (int i = 0; i < skillCount; i++)
        {
            Skills.Add(byteBuf.ReadInt());
        }
    }
}

public class CustomConfigExample : MonoBehaviour
{
    private void Start()
    {
        // 创建配置对象
        var playerConfig = new PlayerConfig
        {
            PlayerId = 1001,
            PlayerName = "Hero",
            Level = 10,
            Health = 100.0f,
            Skills = new List<int> { 1, 3, 5 }
        };
        
        // 序列化
        var byteBuf = new ByteBuf(256);
        playerConfig.Serialize(byteBuf);
        
        // 反序列化
        byteBuf.ReaderIndex = 0;
        var newConfig = new PlayerConfig();
        newConfig.Deserialize(byteBuf);
        
        // 验证结果
        Debug.Log($"反序列化玩家: {newConfig.PlayerName}, 等级: {newConfig.Level}");
        Debug.Log($"技能列表: {StringUtil.CollectionToString(newConfig.Skills)}");
    }
}
```

### 3. 高级缓冲区操作
```csharp
using LuBan.Runtime;

public class AdvancedBufferExample : MonoBehaviour
{
    private void Start()
    {
        var byteBuf = new ByteBuf(512);
        
        // 保存当前状态
        var saveState = byteBuf.SaveState();
        
        // 写入一些数据
        byteBuf.WriteInt(100);
        byteBuf.WriteString("Test Data");
        
        // 恢复到保存的状态
        byteBuf.RestoreState(saveState);
        
        // 现在缓冲区回到原始状态
        Debug.Log($"缓冲区大小: {byteBuf.Size}, 剩余: {byteBuf.Remaining}");
        
        // 使用克隆功能
        var clonedBuf = byteBuf.Clone();
        clonedBuf.WriteInt(200);
        
        // 原始缓冲区不受影响
        Debug.Log($"原始缓冲区大小: {byteBuf.Size}");
        Debug.Log($"克隆缓冲区大小: {clonedBuf.Size}");
    }
}
```

### 4. 错误处理和异常管理
```csharp
using LuBan.Runtime;

public class ErrorHandlingExample : MonoBehaviour
{
    private void Start()
    {
        try
        {
            var byteBuf = new ByteBuf(10); // 小缓冲区
            
            // 尝试写入超过缓冲区容量的数据
            for (int i = 0; i < 100; i++)
            {
                byteBuf.WriteInt(i);
            }
        }
        catch (SerializationException ex)
        {
            Debug.LogError($"序列化错误: {ex.Message}");
            // 处理序列化错误
        }
        
        // 检查反序列化错误
        var smallBuf = new ByteBuf(new byte[4]); // 只有4字节
        
        try
        {
            int value = smallBuf.ReadInt();
            string str = smallBuf.ReadString(); // 这里会出错
        }
        catch (SerializationException)
        {
            Debug.Log("检测到反序列化错误，数据不足");
        }
    }
}
```

### 5. 字符串工具使用
```csharp
using LuBan.Runtime;

public class StringUtilExample : MonoBehaviour
{
    private void Start()
    {
        // 基础对象转字符串
        var vector = new Vector3(1, 2, 3);
        string vectorStr = StringUtil.ToStr(vector);
        Debug.Log($"Vector3字符串: {vectorStr}");
        
        // 数组转字符串
        int[] numbers = { 1, 2, 3, 4, 5 };
        string arrayStr = StringUtil.ArrayToString(numbers);
        Debug.Log($"数组字符串: {arrayStr}");
        
        // 集合转字符串
        var names = new List<string> { "Alice", "Bob", "Charlie" };
        string collectionStr = StringUtil.CollectionToString(names);
        Debug.Log($"集合字符串: {collectionStr}");
        
        // 字典转字符串
        var scores = new Dictionary<string, int>
        {
            ["Alice"] = 95,
            ["Bob"] = 87,
            ["Charlie"] = 92
        };
        string dictStr = StringUtil.CollectionToString(scores);
        Debug.Log($"字典字符串: {dictStr}");
    }
}
```

## 高级用法

### 1. 性能优化的序列化
```csharp
using LuBan.Runtime;
using System;

public class OptimizedSerialization : MonoBehaviour
{
    private ByteBuf reusableBuffer;
    
    private void Start()
    {
        reusableBuffer = new ByteBuf(1024);
        
        // 性能测试：重复使用缓冲区
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            reusableBuffer.ReaderIndex = 0;
            reusableBuffer.WriterIndex = 0;
            
            // 序列化数据
            reusableBuffer.WriteInt(i);
            reusableBuffer.WriteFloat(i * 1.5f);
            reusableBuffer.WriteString($"Item_{i}");
            
            // 反序列化
            reusableBuffer.ReaderIndex = 0;
            int id = reusableBuffer.ReadInt();
            float value = reusableBuffer.ReadFloat();
            string name = reusableBuffer.ReadString();
        }
        
        stopwatch.Stop();
        Debug.Log($"1000次序列化/反序列化耗时: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

### 2. 自定义序列化协议
```csharp
using LuBan.Runtime;

public class CustomProtocol
{
    public static void SerializeComplexObject(ByteBuf buf, object obj)
    {
        if (obj == null)
        {
            buf.WriteByte(0); // null 标记
            return;
        }
        
        buf.WriteByte(1); // 非null标记
        
        switch (obj)
        {
            case int intValue:
                buf.WriteByte(1); // 类型标记：int
                buf.WriteInt(intValue);
                break;
                
            case string stringValue:
                buf.WriteByte(2); // 类型标记：string
                buf.WriteString(stringValue);
                break;
                
            case Vector3 vectorValue:
                buf.WriteByte(3); // 类型标记：Vector3
                buf.WriteFloat(vectorValue.x);
                buf.WriteFloat(vectorValue.y);
                buf.WriteFloat(vectorValue.z);
                break;
                
            default:
                throw new SerializationException($"不支持的序列化类型: {obj.GetType()}");
        }
    }
    
    public static object DeserializeComplexObject(ByteBuf buf)
    {
        byte nullMarker = buf.ReadByte();
        if (nullMarker == 0) return null;
        
        byte typeMarker = buf.ReadByte();
        return typeMarker switch
        {
            1 => buf.ReadInt(),        // int
            2 => buf.ReadString(),     // string
            3 => new Vector3(          // Vector3
                buf.ReadFloat(),
                buf.ReadFloat(),
                buf.ReadFloat()),
            _ => throw new SerializationException($"未知的类型标记: {typeMarker}")
        };
    }
}
```

### 3. 网络数据包处理
```csharp
using LuBan.Runtime;
using System;

public class NetworkPacketHandler : MonoBehaviour
{
    public class NetworkPacket : BeanBase
    {
        public int PacketId { get; set; }
        public int Command { get; set; }
        public byte[] Payload { get; set; }
        public DateTime Timestamp { get; set; }
        
        public override int GetTypeId() => 2001;
        
        public void Serialize(ByteBuf buf)
        {
            buf.WriteInt(PacketId);
            buf.WriteInt(Command);
            buf.WriteBytes(Payload);
            buf.WriteLong(Timestamp.Ticks);
        }
        
        public void Deserialize(ByteBuf buf)
        {
            PacketId = buf.ReadInt();
            Command = buf.ReadInt();
            Payload = buf.ReadBytes();
            Timestamp = new DateTime(buf.ReadLong());
        }
    }
    
    public void ProcessIncomingData(byte[] networkData)
    {
        var byteBuf = new ByteBuf(networkData);
        
        try
        {
            var packet = new NetworkPacket();
            packet.Deserialize(byteBuf);
            
            Debug.Log($"收到数据包: ID={packet.PacketId}, 命令={packet.Command}");
            Debug.Log($"载荷大小: {packet.Payload?.Length ?? 0} bytes");
            Debug.Log($"时间戳: {packet.Timestamp}");
            
            // 处理数据包内容
            HandlePacket(packet);
        }
        catch (SerializationException ex)
        {
            Debug.LogError($"数据包反序列化失败: {ex.Message}");
        }
    }
    
    private void HandlePacket(NetworkPacket packet)
    {
        // 根据命令类型处理数据包
        switch (packet.Command)
        {
            case 1:
                HandlePlayerUpdate(packet);
                break;
            case 2:
                HandleGameState(packet);
                break;
            default:
                Debug.LogWarning($"未知命令: {packet.Command}");
                break;
        }
    }
    
    private void HandlePlayerUpdate(NetworkPacket packet)
    {
        // 处理玩家更新逻辑
        var payloadBuf = new ByteBuf(packet.Payload);
        int playerId = payloadBuf.ReadInt();
        float health = payloadBuf.ReadFloat();
        
        Debug.Log($"玩家 {playerId} 生命值更新: {health}");
    }
}
```

## 性能优化建议

### 1. 缓冲区重用
```csharp
// 避免频繁创建新的 ByteBuf 对象
private ByteBuf sharedBuffer = new ByteBuf(1024);

public void ProcessData(byte[] data)
{
    sharedBuffer.Replace(data);
    // 处理数据...
    sharedBuffer.ReaderIndex = 0;
    sharedBuffer.WriterIndex = 0;
}
```

### 2. 预分配缓冲区大小
```csharp
// 根据数据大小预分配缓冲区
public ByteBuf CreateOptimizedBuffer(int expectedSize)
{
    // 添加一些余量避免频繁扩容
    return new ByteBuf(expectedSize + 64);
}
```

### 3. 使用对象池
```csharp
using System.Collections.Generic;

public class ByteBufPool
{
    private Stack<ByteBuf> pool = new Stack<ByteBuf>();
    
    public ByteBuf GetBuffer(int minSize)
    {
        if (pool.Count > 0)
        {
            var buf = pool.Pop();
            if (buf.Capacity >= minSize)
            {
                buf.ReaderIndex = 0;
                buf.WriterIndex = 0;
                return buf;
            }
        }
        
        return new ByteBuf(minSize);
    }
    
    public void ReturnBuffer(ByteBuf buffer)
    {
        if (buffer != null)
        {
            buffer.ReaderIndex = 0;
            buffer.WriterIndex = 0;
            pool.Push(buffer);
        }
    }
}
```

## 注意事项

### 1. 类型安全
- 确保所有自定义配置类正确实现 `GetTypeId()` 方法
- 类型标识符在项目中必须唯一
- 序列化和反序列化方法要保持一致

### 2. 内存管理
- 及时释放不再使用的 ByteBuf 对象
- 避免在频繁调用的方法中创建新的缓冲区
- 注意大对象的序列化可能带来的内存压力

### 3. 错误处理
- 始终对序列化操作进行异常处理
- 检查反序列化时的数据完整性
- 记录序列化错误以便调试

### 4. 平台兼容性
- 确保在 IL2CPP 平台下类型信息正确保留
- 测试在不同平台上的序列化性能
- 注意字节序问题（当前实现使用小端序）

## 依赖模块

- **SimpleJSON.Runtime**：JSON 序列化支持
- **Unity Engine**：基础运行环境
- **System**：基础类型支持

## 技术支持

如遇到序列化问题，请检查：
1. 类型标识符是否唯一且正确
2. 序列化和反序列化方法是否匹配
3. 缓冲区大小是否足够
4. 数据格式是否符合预期
5. 平台兼容性是否满足要求