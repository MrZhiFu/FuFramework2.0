## HOMEPAGE

GameFrameX 的 Setting 配置信息组件

**Setting 配置信息组件 (Setting Component)** - 负责管理游戏的配置信息，允许您保存和获取各种类型的配置数据。

# 使用文档(文档编写于GPT4)

# SettingComponent 类说明文档

**SettingComponent** 是游戏框架的一个核心组件，它负责管理游戏的配置信息，允许您保存和获取各种类型的配置数据。

## 功能概述

- 负责管理游戏设置，提供保存和加载的功能。
- 提供对设置项的查询、添加、删除操作。
- 支持不同数据类型的设置项，如布尔值、整数、浮点数、字符串及对象。

## 主要方法和属性

### 属性

- **Count**  
  获取游戏配置项的数量。

### 方法

- **Awake()**  
  初始化设置管理器和设置助手。

- **Start()**  
  负责加载设置。

- **Save()**  
  保存当前所有游戏配置项。

- **GetAllSettingNames() / GetAllSettingNames(List<string>)**  
  获取所有游戏配置项的名称，可选列表形式返回。

- **HasSetting(string)**  
  检查是否存在指定名称的游戏配置项。

- **RemoveSetting(string)**  
  移除指定的游戏配置项。

- **RemoveAllSettings()**  
  清空所有游戏配置项。

- **GetBool(string), GetBool(string, bool)**  
  获取布尔类型的配置信息，可以提供默认值。

- **SetBool(string, bool)**  
  设置布尔类型的配置信息。

- **GetInt(string), GetInt(string, int)**  
  获取整数类型的配置信息，可以提供默认值。

- **SetInt(string, int)**  
  设置整数类型的配置信息。

- **GetFloat(string), GetFloat(string, float)**  
  获取浮点数类型的配置信息，可以提供默认值。

- **SetFloat(string, float)**  
  设置浮点数类型的配置信息。

- **GetString(string), GetString(string, string)**  
  获取字符串类型的配置信息，可以提供默认值。

- **SetString(string, string)**  
  设置字符串类型的配置信息。

- **GetObject<T>(string), GetObject(Type, string), GetObject<T>(string, T), GetObject(Type, string, object)**  
  获取对象类型的配置信息，可以指定类型和默认值。

- **SetObject<T>(string, T), SetObject(string, object)**  
  设置对象类型的配置信息。

## 使用示例

通过 **SettingComponent**，您可以轻松地管理游戏设置。以下是一些使用示例：

### 保存和加载设置

```cs
// 创建SettingComponent实例
SettingComponent settingComponent = new SettingComponent();

// 加载设置
settingComponent.Start();

// 修改一些设置
settingComponent.SetBool("IsFullScreen", true);
settingComponent.SetInt("ResolutionWidth", 1920);
settingComponent.SetFloat("Volume", 0.8f);
settingComponent.SetString("PlayerName", "PlayerOne");

// 保存修改后的设置
settingComponent.Save();
```

### 查询和获取设置值

```cs
// 检查是否存在某个设置
bool hasVolumeSetting = settingComponent.HasSetting("Volume");

// 获取设置项的值
float volume = settingComponent.GetFloat("Volume", 0.5f); // 如果不存在，则返回0.5f
```

### 删除设置

```cs
// 移除某个设置
settingComponent.RemoveSetting("PlayerName");

// 移除所有设置
settingComponent.RemoveAllSettings();
```

请注意，本示例仅用于演示目的，实际使用时您需要将 **SettingComponent** 添加到您的游戏对象中，并通过Unity的生命周期管理其状态。

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.setting": "https://github.com/AlianBlank/com.gameframex.unity.setting.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.setting.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别