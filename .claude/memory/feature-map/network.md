# 网络与协议

| 功能 | 路径 |
|---|---|
| 网络模块主入口 | `Hotfix/Framework/Network/NetworkModule.cs` |
| 模块 API / 常量 / 重连 / 调试 | `NetworkModule.{API,Const,Reconnect,Debug}.cs` |
| 频道实现 | `Network/Channel/`（含 `NetworkChannelHelper`） |
| Socket 实现（TCP / WebSocket） | `Network/Socket/` |
| 消息契约接口 | `Network/Interface/` |
| 消息基类 | `Network/MessageBase/` |
| 消息 ID 特性 / 注册表 | `Network/Attribute/`、`Network/Register/` |
| 连接状态 | `Network/State/` |
| 网络事件 | `Network/Event/` |
| 协议源文件 | `Protobuf/Proto/*.proto` |
| 导出脚本 | `Protobuf/Proto2CsExport-All.bat`（必须带 `ci`）、`Proto2CsExport_{Client,Server}.bat` |
| 导出器源码 | `Tools/ProtoExport/` |
| 推送注册表生成 | `Protobuf/gen-proto-registry.{bat,py,sh}` |
| 消息 ID 速查表（自动生成） | `Protobuf/MessageIds.md` |
| 客户端生成物 | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/{Proto,MessageIds,Tables}/` |
| 推送注册表守卫 | `Unity/Assets/Editor/FuFramework/Network/NotifyRegistryGuard.cs` |
| 网络菜单统一入口 | `Unity/Assets/Editor/FuFramework/Network/ProtoImporter.cs` |
| 游戏层会话流 / 重连 | `Hotfix/Game/Network/GameSessionFlow.cs`、`ReconnectManager.cs` |
| 服务端协议与处理器 | `Server/`（含 `FuFramework.Proto`） |

相关规则：`rules/framework/network-protocol.md`
