# FuFramework Model Module

## 简介
FuFramework Model 模块是游戏数据模型的管理系统。该模块提供了统一的数据模型管理、事件驱动机制和自动序列化功能，帮助开发者构建结构清晰、易于维护的游戏数据层。

## 核心特性

- **事件驱动**：内置事件订阅和广播机制，支持线程安全的事件分发
- **自动序列化**：基于 JSON 的自动数据持久化，支持本地存储
- **生命周期管理**：统一的模型初始化和销毁管理
- **类型安全**：强类型的模型获取和管理
- **模块化设计**：依赖事件和数据保存模块，架构清晰

## 核心类说明

### BaseModel
模型基类，提供基础的事件管理和生命周期控制。
- **职责**：
  1. 提供事件订阅、取消订阅和广播功能
  2. 管理模型的初始化和销毁生命周期
  3. 支持线程安全的事件分发机制

### BaseSerializerModel
可序列化的模型基类，继承自 `BaseModel`。
- **职责**：
  1. 提供自动的 JSON 序列化和反序列化功能
  2. 支持本地数据存储和加载
  3. 管理模型数据的持久化

### ModelModule
模型管理器，继承自 `FuModule`，统一管理所有模型实例。
- **职责**：
  1. 管理模型实例的创建、获取和销毁
  2. 提供类型安全的模型访问接口
  3. 确保模块依赖的正确初始化

## 技术架构

### 依赖关系
- **FuFramework.Core**：基础框架模块
- **FuFramework.Event**：事件管理模块
- **FuFramework.SaveData**：数据保存模块
- **Newtonsoft.Json**：JSON 序列化支持

### 模块优先级
ModelModule 的优先级为 `ModulePriority.Game`，确保在游戏逻辑模块中正确初始化。

## 使用指南

### 1. 基础模型创建和使用
```csharp
using FuFramework.Model.Runtime;
using FuFramework.Event.Runtime;

// 创建基础模型类
public class PlayerModel : BaseModel
{
    public string PlayerName { get; set; } = "Unknown";
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    
    // 初始化数据
    protected override void OnInitData()
    {
        FuLogger.LogInfo($"PlayerModel 初始化完成: {PlayerName}, 等级: {Level}");
    }
    
    // 注册事件
    protected override void RegisterEvents()
    {
        Subscribe("PlayerLevelUp", OnPlayerLevelUp);
        Subscribe("PlayerExpGain", OnPlayerExpGain);
    }
    
    // 事件处理方法
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        Level++;
        FuLogger.LogInfo($"玩家升级! 当前等级: {Level}");
        
        // 广播升级完成事件
        Broadcast(this, "PlayerLevelUpComplete");
    }
    
    private void OnPlayerExpGain(object sender, GameEventArgs e)
    {
        if (e is ExpGainEventArgs expArgs)
        {
            Experience += expArgs.ExpAmount;
            FuLogger.LogInfo($"获得经验: {expArgs.ExpAmount}, 总经验: {Experience}");
            
            // 检查是否升级
            if (Experience >= GetRequiredExpForNextLevel())
            {
                Broadcast(this, "PlayerLevelUp");
            }
        }
    }
    
    private int GetRequiredExpForNextLevel()
    {
        return Level * 100; // 简单的经验公式
    }
    
    // 自定义事件参数类
    public class ExpGainEventArgs : GameEventArgs
    {
        public static readonly string EventId = "PlayerExpGain";
        
        public int ExpAmount { get; set; }
        
        public override string GetEventId() => EventId;
    }
}

// 使用示例
public class GameController : MonoBehaviour
{
    private void Start()
    {
        // 获取模型管理器
        var modelModule = GlobalModule.ModelModule;
        
        // 获取或创建玩家模型
        var playerModel = modelModule.GetModel<PlayerModel>();
        
        // 修改模型数据
        playerModel.PlayerName = "Hero";
        playerModel.Level = 5;
        
        // 触发事件
        var expArgs = new PlayerModel.ExpGainEventArgs { ExpAmount = 50 };
        playerModel.Broadcast(this, expArgs);
    }
}
```

### 2. 可序列化模型的使用
```csharp
using FuFramework.Model.Runtime;
using Newtonsoft.Json;

// 创建可序列化的模型类
public class GameSettingsModel : BaseSerializerModel
{
    // 这些属性会自动序列化到本地存储
    public float MusicVolume { get; set; } = 0.8f;
    public float SoundVolume { get; set; } = 1.0f;
    public int GraphicsQuality { get; set; } = 2;
    public bool Fullscreen { get; set; } = true;
    public string Language { get; set; } = "zh-CN";
    
    // 不会被序列化的属性（临时数据）
    [JsonIgnore]
    public bool IsDirty { get; set; } = false;
    
    // 自定义文件名
    protected override string GetFileName() => "GameSettings";
    
    // 首次初始化（当数据文件不存在时调用）
    protected override void OnFirstInitDate()
    {
        FuLogger.LogInfo("首次初始化游戏设置，使用默认值");
        // 可以在这里设置默认值
        MusicVolume = 0.7f;
        SoundVolume = 0.9f;
        GraphicsQuality = 1;
        Fullscreen = false;
        Language = "en-US";
    }
    
    // 注册设置相关事件
    protected override void RegisterEvents()
    {
        Subscribe("SettingsChanged", OnSettingsChanged);
        Subscribe("ResetSettings", OnResetSettings);
    }
    
    private void OnSettingsChanged(object sender, GameEventArgs e)
    {
        IsDirty = true;
        FuLogger.LogInfo("设置已修改，标记为需要保存");
    }
    
    private void OnResetSettings(object sender, GameEventArgs e)
    {
        // 重置为默认值
        MusicVolume = 0.7f;
        SoundVolume = 0.9f;
        GraphicsQuality = 1;
        Fullscreen = false;
        Language = "en-US";
        
        FuLogger.LogInfo("设置已重置为默认值");
        Broadcast(this, "SettingsChanged");
    }
}

// 使用示例
public class SettingsManager : MonoBehaviour
{
    private GameSettingsModel settingsModel;
    
    private void Start()
    {
        // 获取设置模型（会自动加载本地数据）
        settingsModel = GlobalModule.ModelModule.GetModel<GameSettingsModel>();
        
        // 应用设置
        ApplySettings();
    }
    
    private void ApplySettings()
    {
        // 应用音频设置
        AudioListener.volume = settingsModel.SoundVolume;
        
        // 应用图形设置
        QualitySettings.SetQualityLevel(settingsModel.GraphicsQuality);
        Screen.fullScreen = settingsModel.Fullscreen;
        
        FuLogger.LogInfo("游戏设置已应用");
    }
    
    private void OnApplicationQuit()
    {
        // 游戏退出时，模型会自动保存（BaseSerializerModel 的 OnDispose 方法）
    }
    
    // 修改设置的方法
    public void ChangeVolume(float musicVolume, float soundVolume)
    {
        settingsModel.MusicVolume = musicVolume;
        settingsModel.SoundVolume = soundVolume;
        settingsModel.Broadcast(this, "SettingsChanged");
        ApplySettings();
    }
}
```

### 3. 复杂数据模型示例
```csharp
using FuFramework.Model.Runtime;
using System.Collections.Generic;
using Newtonsoft.Json;

// 复杂的游戏数据模型
public class GameDataModel : BaseSerializerModel
{
    // 玩家数据
    public PlayerData Player { get; set; } = new();
    
    // 库存数据
    public List<InventoryItem> Inventory { get; set; } = new();
    
    // 游戏进度
    public Dictionary<string, bool> Achievements { get; set; } = new();
    
    // 游戏统计
    public GameStatistics Statistics { get; set; } = new();
    
    // 不会被保存的临时状态
    [JsonIgnore]
    public bool IsInCombat { get; set; } = false;
    
    [JsonIgnore]
    public string CurrentScene { get; set; } = "MainMenu";
    
    protected override string GetFileName() => "GameData";
    
    protected override void OnFirstInitDate()
    {
        FuLogger.LogInfo("首次初始化游戏数据");
        
        // 初始化默认玩家数据
        Player = new PlayerData
        {
            Name = "New Player",
            Level = 1,
            Health = 100,
            MaxHealth = 100,
            Gold = 100
        };
        
        // 初始化默认物品
        Inventory.Add(new InventoryItem { Id = "potion_health", Count = 3 });
        Inventory.Add(new InventoryItem { Id = "potion_mana", Count = 2 });
        
        // 初始化成就系统
        Achievements["first_login"] = true;
    }
    
    // 数据操作方法
    public void AddItem(string itemId, int count = 1)
    {
        var existingItem = Inventory.Find(item => item.Id == itemId);
        if (existingItem != null)
        {
            existingItem.Count += count;
        }
        else
        {
            Inventory.Add(new InventoryItem { Id = itemId, Count = count });
        }
        
        Broadcast(this, "InventoryUpdated");
    }
    
    public void UnlockAchievement(string achievementId)
    {
        if (!Achievements.ContainsKey(achievementId))
        {
            Achievements[achievementId] = true;
            Broadcast(this, "AchievementUnlocked");
        }
    }
    
    // 嵌套数据类
    [System.Serializable]
    public class PlayerData
    {
        public string Name { get; set; } = "";
        public int Level { get; set; } = 1;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Gold { get; set; } = 0;
        public int Experience { get; set; } = 0;
    }
    
    [System.Serializable]
    public class InventoryItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Count { get; set; } = 0;
        public string Description { get; set; } = "";
    }
    
    [System.Serializable]
    public class GameStatistics
    {
        public int TotalPlayTime { get; set; } = 0; // 秒
        public int EnemiesDefeated { get; set; } = 0;
        public int QuestsCompleted { get; set; } = 0;
        public int DeathCount { get; set; } = 0;
    }
}

// 使用复杂模型的控制器
public class GameDataController : MonoBehaviour
{
    private GameDataModel gameData;
    
    private void Start()
    {
        gameData = GlobalModule.ModelModule.GetModel<GameDataModel>();
        
        // 订阅数据变化事件
        GlobalModule.EventModule.Subscribe("InventoryUpdated", OnInventoryUpdated);
        GlobalModule.EventModule.Subscribe("AchievementUnlocked", OnAchievementUnlocked);
    }
    
    private void OnInventoryUpdated(object sender, GameEventArgs e)
    {
        // 更新UI显示
        UpdateInventoryUI();
    }
    
    private void OnAchievementUnlocked(object sender, GameEventArgs e)
    {
        // 显示成就解锁提示
        ShowAchievementPopup();
    }
    
    // 游戏逻辑方法
    public void DefeatEnemy(string enemyType, int expReward)
    {
        gameData.Statistics.EnemiesDefeated++;
        
        // 随机掉落物品
        if (Random.Range(0f, 1f) > 0.7f) // 30% 掉落率
        {
            gameData.AddItem("gold", Random.Range(5, 20));
        }
        
        // 检查成就
        if (gameData.Statistics.EnemiesDefeated >= 100)
        {
            gameData.UnlockAchievement("enemy_slayer");
        }
    }
    
    private void UpdateInventoryUI()
    {
        // 更新库存UI的逻辑
    }
    
    private void ShowAchievementPopup()
    {
        // 显示成就弹窗的逻辑
    }
}
```

### 4. 模型管理器的高级用法
```csharp
using FuFramework.Model.Runtime;

public class AdvancedModelManagement : MonoBehaviour
{
    private void Start()
    {
        var modelModule = GlobalModule.ModelModule;
        
        // 1. 获取多个模型
        var playerModel = modelModule.GetModel<PlayerModel>();
        var settingsModel = modelModule.GetModel<GameSettingsModel>();
        var gameDataModel = modelModule.GetModel<GameDataModel>();
        
        // 2. 模型间通信示例
        playerModel.Subscribe("PlayerLevelUp", OnAnyPlayerLevelUp);
        
        // 3. 批量操作模型
        // 可以在游戏退出时手动保存所有可序列化模型
    }
    
    private void OnAnyPlayerLevelUp(object sender, GameEventArgs e)
    {
        // 当任意玩家升级时触发的逻辑
        var gameData = GlobalModule.ModelModule.GetModel<GameDataModel>();
        gameData.Statistics.TotalPlayTime += 3600; // 假设升级需要1小时
        
        FuLogger.LogInfo("玩家升级，更新游戏统计");
    }
    
    // 场景切换时的模型管理
    private void OnSceneLoaded(string sceneName)
    {
        if (sceneName == "MainMenu")
        {
            // 主菜单场景，可以移除一些游戏内模型以节省内存
            // modelModule.RemoveModel<BattleModel>();
        }
        else if (sceneName == "Battle")
        {
            // 战斗场景，创建战斗相关模型
            // var battleModel = modelModule.GetModel<BattleModel>();
        }
    }
}
```

## 高级用法

### 1. 自定义序列化控制
```csharp
using Newtonsoft.Json;

public class AdvancedSerializerModel : BaseSerializerModel
{
    // 使用特性精确控制序列化
    
    // 公共属性，默认会被序列化
    public string PublicData { get; set; } = "This will be saved";
    
    // 使用 JsonIgnore 忽略公共属性
    [JsonIgnore]
    public string TemporaryData { get; set; } = "This will NOT be saved";
    
    // 使用 JsonProperty 强制序列化私有字段
    [JsonProperty]
    private string secretCode = "SECRET123";
    
    // 自定义序列化名称
    [JsonProperty("player_name")]
    public string PlayerName { get; set; } = "Player";
    
    // 只读属性，默认不会被序列化
    public string DisplayName => $"{PlayerName} (Level {Level})";
    
    public int Level { get; set; } = 1;
    
    // 自定义序列化逻辑
    protected override void OnFirstInitDate()
    {
        // 可以在这里进行复杂的数据初始化
        if (string.IsNullOrEmpty(secretCode))
        {
            secretCode = GenerateSecretCode();
        }
    }
    
    private string GenerateSecretCode()
    {
        return $"CODE_{System.DateTime.Now.Ticks}";
    }
}
```

### 2. 模型间的事件通信
```csharp
public class ModelCommunicationExample
{
    public class PlayerModel : BaseModel
    {
        public int Health { get; set; } = 100;
        
        protected override void RegisterEvents()
        {
            Subscribe("PlayerDamaged", OnPlayerDamaged);
        }
        
        private void OnPlayerDamaged(object sender, GameEventArgs e)
        {
            if (e is DamageEventArgs damageArgs)
            {
                Health -= damageArgs.DamageAmount;
                FuLogger.LogWarning($"玩家受到 {damageArgs.DamageAmount} 点伤害，剩余生命: {Health}");
                
                if (Health <= 0)
                {
                    Broadcast(this, "PlayerDied");
                }
            }
        }
    }
    
    public class BattleModel : BaseModel
    {
        protected override void RegisterEvents()
        {
            Subscribe("PlayerDied", OnPlayerDied);
        }
        
        private void OnPlayerDied(object sender, GameEventArgs e)
        {
            FuLogger.LogError("玩家死亡，战斗结束!");
            Broadcast(this, "BattleLost");
        }
    }
    
    public class DamageEventArgs : GameEventArgs
    {
        public static readonly string EventId = "PlayerDamaged";
        public int DamageAmount { get; set; }
        
        public override string GetEventId() => EventId;
    }
}
```

### 3. 性能优化和内存管理
```csharp
public class OptimizedModelUsage : MonoBehaviour
{
    private ModelModule modelModule;
    
    private void Start()
    {
        modelModule = GlobalModule.ModelModule;
        
        // 按需加载模型，避免不必要的内存占用
        if (NeedsPlayerData())
        {
            var playerModel = modelModule.GetModel<PlayerModel>();
        }
        
        // 及时清理不再需要的模型
        if (SceneModule.GetActiveScene().name == "MainMenu")
        {
            // 在主菜单场景，移除战斗相关模型
            RemoveBattleModels();
        }
    }
    
    private bool NeedsPlayerData()
    {
        // 根据游戏状态判断是否需要玩家数据
        return true;
    }
    
    private void RemoveBattleModels()
    {
        // 注意：在实际项目中需要谨慎使用，确保模型不再被需要
        // modelModule.RemoveModel<BattleModel>();
        // modelModule.RemoveModel<EnemyModel>();
    }
    
    private void OnDestroy()
    {
        // 组件销毁时，可以清理相关的事件订阅
    }
}
```

## 性能优化建议

### 1. 模型生命周期管理
- 按需创建模型，避免不必要的内存占用
- 及时清理不再需要的模型实例
- 合理使用可序列化模型的自动保存功能

### 2. 事件系统优化
- 避免在频繁调用的方法中频繁广播事件
- 及时取消订阅不再需要的事件
- 使用特定的事件参数类，避免创建过多的临时对象

### 3. 序列化优化
- 对于大型数据集，考虑分块序列化
- 使用 `[JsonIgnore]` 避免序列化临时数据
- 定期清理不再需要的序列化数据

## 注意事项

### 1. 线程安全
- 事件广播是线程安全的，可以在非主线程中调用
- 但事件处理会在主线程中执行
- 直接修改模型数据时需要注意线程同步

### 2. 序列化限制
- Unity 对象引用（如 GameObject、Component）不会被正确序列化
- 循环引用可能导致序列化失败
- 大型二进制数据不适合使用 JSON 序列化

### 3. 模块依赖
- ModelModule 依赖 EventModule 和 DataSaveModule
- 确保这些模块在 ModelModule 之前正确初始化
- 使用 `GlobalModule.ModelModule` 访问模型管理器

### 4. 错误处理
- 模型初始化失败时会有详细的错误日志
- 序列化失败时会尝试使用默认值初始化
- 事件处理异常会被捕获并记录

## 依赖模块

- **FuFramework.Core**：框架核心模块
- **FuFramework.Event**：事件管理模块
- **FuFramework.SaveData**：数据保存模块
- **Newtonsoft.Json**：JSON 序列化库

## 技术支持

如遇到模型相关问题，请检查：
1. 模型类是否正确继承自 BaseModel 或 BaseSerializerModel
2. 事件订阅和广播的 EventId 是否匹配
3. 序列化特性使用是否正确
4. 模块依赖是否满足
5. 线程安全操作是否符合预期