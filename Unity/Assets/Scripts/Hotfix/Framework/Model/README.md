# FuFramework Model Module

## 1. 简介

FuFramework Model 模块是游戏框架的 MVVM 架构中 Model 层的管理器，负责统一管理所有数据模型的创建、获取和清理。该模块提供了基础的事件通信能力和可选的 JSON 序列化/持久化能力，方便数据模型进行模块间通信和本地存储。

## 2. 核心特性

- **统一管理**：`ModelModule` 集中管理所有 Model 实例的生命周期
- **事件支持**：`BaseModel` 内置事件订阅和广播能力
- **JSON 持久化**：`BaseSerializerModel` 支持 JSON 序列化，配合 `StorageModule` 实现数据本地持久化
- **Newtonsoft.Json**：基于 Newtonsoft.Json 实现序列化，支持 `[JsonIgnore]` 和 `[JsonProperty]` 控制序列化行为
- **引用池兼容**：Model 实例支持通过引用池管理

## 3. 核心概念

### 3.1 类继承体系

```
IReference (引用池接口)
    └── BaseModel (Model 基类，事件支持)
        └── BaseSerializerModel (可序列化 Model，JSON 持久化)
            └── 用户自定义 Model

ModuleBase (框架模块基类)
    └── ModelModule (Model 管理模块)
```

### 3.2 Model 架构

```
┌─────────────────────────────────────────────────────────────┐
│                     ModelModule                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_ModelDict (Dictionary<Type, BaseModel>)          │   │
│  │  - 按类型管理所有 Model 实例                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┼─────────────┐
                ▼             ▼             ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │ PlayerModel│  │ ShopModel │  │SettingModel│
        │ (BaseSer   │  │ (BaseSer  │  │ (BaseSer   │
        │  Model)    │  │  Model)   │  │  Model)    │
        └──────────┘  └──────────┘  └──────────┘
```

## 4. 核心类说明

### 4.1 ModelModule

Model 管理模块，继承自 `ModuleBase`。

**核心方法：**

```csharp
// 创建/获取 Model
T CreateModel<T>() where T : BaseModel, new()
T GetModel<T>() where T : BaseModel

// 检查 Model
bool HasModel<T>() where T : BaseModel

// 清理 Model
void RemoveModel<T>() where T : BaseModel
void ClearAllModels()
```

### 4.2 BaseModel

Model 基类，实现 `IReference` 接口，内置事件通信能力。

**核心属性/方法：**

```csharp
public abstract class BaseModel : IReference
{
    // 事件通信
    protected void Subscribe(string eventId, EventHandler<GameEventArgs> handler)
    protected void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler)
    protected void Broadcast(object sender, GameEventArgs eventArgs)
    protected void BroadcastNow(object sender, GameEventArgs eventArgs)

    // 生命周期
    protected virtual void OnInit() { }
    protected virtual void OnDispose() { }

    // IReference
    public virtual void Clear() { }
}
```

### 4.3 BaseSerializerModel

可序列化的 Model 基类，继承 `BaseModel`，增加 JSON 序列化/持久化能力。

**核心功能：**

```csharp
public abstract class BaseSerializerModel : BaseModel
{
    // 序列化/反序列化
    protected virtual string Serialize()           // 序列化为 JSON 字符串
    protected virtual void Deserialize(string json) // 从 JSON 字符串反序列化

    // 持久化
    protected virtual void Save()                  // 保存到本地
    protected virtual void Load()                  // 从本地加载

    // Newtonsoft.Json 特性支持
    // [JsonIgnore]  - 忽略序列化
    // [JsonProperty("name")] - 自定义序列化名称
}
```

## 5. 使用示例

### 5.1 定义 Model

```csharp
using Hotfix.Framework.Model;
using Newtonsoft.Json;

// 简单 Model
public class PlayerModel : BaseModel
{
    public int Level { get; set; }
    public int Gold { get; set; }
    public string PlayerName { get; set; }

    protected override void OnInit()
    {
        base.OnInit();
        // 订阅事件
        Subscribe("LevelUp", OnLevelUp);
    }

    private void OnLevelUp(object sender, GameEventArgs e)
    {
        Level++;
        // 通知 UI 刷新...
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        UnSubscribe("LevelUp", OnLevelUp);
    }

    public override void Clear()
    {
        Level = 0;
        Gold = 0;
        PlayerName = null;
    }
}

// 可持久化的 Model
public class SettingModel : BaseSerializerModel
{
    [JsonProperty("volume")]
    public float MusicVolume { get; set; } = 1f;

    [JsonProperty("sfx")]
    public float SFXVolume { get; set; } = 1f;

    [JsonIgnore]  // 不序列化此字段
    public bool IsDirty { get; set; }

    protected override void OnInit()
    {
        base.OnInit();
        Load();  // 启动时加载
    }

    public void SaveSettings()
    {
        Save(); // 保存到本地
    }
}
```

### 5.2 使用 Model

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Model;

public class ModelExample
{
    private ModelModule m_ModelModule;

    public void Init()
    {
        m_ModelModule = ModuleManager.GetModule<ModelModule>();

        // 创建并获取 PlayerModel
        var playerModel = m_ModelModule.CreateModel<PlayerModel>();
        playerModel.PlayerName = "Player1";
        playerModel.Level = 10;

        // 创建并获取 SettingModel
        var settingModel = m_ModelModule.CreateModel<SettingModel>();
        settingModel.MusicVolume = 0.8f;
        settingModel.SaveSettings();
    }

    public void AccessModel()
    {
        // 获取已存在的 Model
        if (m_ModelModule.HasModel<PlayerModel>())
        {
            var playerModel = m_ModelModule.GetModel<PlayerModel>();
            Debug.Log($"玩家等级: {playerModel.Level}");
        }
    }
}
```

## 6. 目录结构

```text
Model/
├── Runtime/
│   ├── ModelModule.cs          # Model 管理模块
│   ├── BaseModel.cs            # Model 基类 (事件支持)
│   ├── BaseSerializerModel.cs  # 可序列化 Model (JSON 持久化)
└── README.md                   # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类
- **Hotfix.Framework.Event**：事件系统
- **Hotfix.Framework.ReferencePool**：引用池
- **Hotfix.Framework.Storage**：本地存储（BaseSerializerModel 持久化）
- **Newtonsoft.Json**（外部）：JSON 序列化

## 8. 最佳实践

1. **Model 职责单一**：每个 Model 只管理一个领域的数据
2. **事件解耦**：Model 之间通过事件通信，避免直接引用
3. **按需持久化**：只有需要保存的数据才继承 `BaseSerializerModel`
4. **Clear 实现**：正确实现 `Clear()` 方法，重置所有字段
5. **生命周期管理**：在 `OnInit` 中订阅事件、加载数据，在 `OnDispose` 中取消订阅

## 9. 注意事项

1. Model 实例由 `ModelModule` 统一管理，不要手动 new 和销毁
2. `BaseSerializerModel` 的 `Save()` 会触发 JSON 序列化，频繁调用有性能开销
3. 序列化字段必须是 public 或标记 `[JsonProperty]`
4. 引用类型字段需要正确处理序列化和反序列化
