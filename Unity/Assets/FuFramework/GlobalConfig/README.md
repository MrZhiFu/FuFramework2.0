## HOMEPAGE

GameFrameX 的 GlobalConfig 全局配置组件

**GlobalConfig 全局配置组件 (GlobalConfig Component)** - 提供全局配置组件相关的接口。

# 使用文档(文档编写于GPT4)

`GlobalConfigComponent`组件：

## 组件说明

`GlobalConfigComponent` 是一个用于规划和使用全局配置的组件。该组件在 Unity 游戏项目中管理应用程序版本和资源版本检查的URL，以及其他可能需要全局访问的内容或配置。

## 功能概述

1. `CheckAppVersionUrl` 属性用于存储检查应用程序版本的URL地址。它允许游戏能够确定是否有新的应用版本可用。

2. `CheckResourceVersionUrl` 属性用于存储检查游戏资源版本的URL地址。这确保了游戏可以检查并下载新的或更新的资源。

3. `Content` 属性允许保存任何附加的内容或数据，可能用于在游戏内显示或用于其它业务逻辑。

4. `HostServerUrl` 属性包含主机服务的URL，可能用作连接后端服务器的地址。

## 使用指南

1. **添加组件**： 将`GlobalConfigComponent`作为组件添加到Unity场景中的任意一个游戏对象上。

2. **设置属性**： 在 Unity 的 Inspector 面板中设置`CheckAppVersionUrl`，`CheckResourceVersionUrl`，`Content`和`HostServerUrl`的值，或者通过代码在运行时动态设置。

3. **资源管理**： 使用`CheckAppVersionUrl`和`CheckResourceVersionUrl`来管理游戏的版本控制和资源更新流程。

4. **全局访问**： 你可以在游戏的任何地方访问`GlobalConfigComponent`实例来获取所需的全局配置信息。

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.globalconfig": "https://github.com/AlianBlank/com.gameframex.unity.globalconfig.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.globalconfig.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别