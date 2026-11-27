---
paths:
  - "{Protobuf/**,**/Network/**,Tools/ProtoExport/**}"
---

# 网络协议

## proto 文件格式约定

- **分区横幅**：文件按「共享定义 → 请求/响应（Req/Rsp）→ 服务器推送（Notify）」三大区块组织，区块间用 `// ====...====` 包裹标题。单一区块的文件不加大横幅。
- **Req/Rsp 成对紧邻**放置。
- **大括号统一换行 `{`**（与 C# 规范一致）；空消息体写 `message ReqXxx {}`。
- 注释：message/enum 上方 `//` 说明 + 字段行尾 `//` 注释列对齐。
- **文件名不带 `_模块号` 后缀**（`User.proto` 而非 `User_1.proto`）；模块号由文件内 `option module = N;` 声明。

## 消息 ID（显式锚定）

```
ID = (module << 16) + opcode
```

- **每条消息体内必须写 `option opcode = N;`**，范围 [10, 65535]。缺失或越界 → 导出中止。
- **非消息 DTO（PlayerInfo/BagItem 等）禁止标注 opcode**。
- 隐式顺序分配已删除——**删/插消息不再导致后续 ID 漂移**。
- 消息角色**按名字前缀判定**：`Req*` / `Rsp*` / `Notify*`。Req 与 Rsp 各自独立占 opcode（配对靠 UniqueId 侧信道）。
- 导出器生成 `MessageIds.gen.cs`（`MessageIds.<模块>.<消息>` 常量），特性写 `[MessageId(MessageIds.User.ReqUserLogin)]`。
- **改 ID 数值 = 破坏线上兼容**。速查表 `Protobuf/MessageIds.md` 自动生成。

## 导出流程

```bash
# 改了 ProtoExport 源码 → 必须先构建（bat 跑的是 bin 里的旧 dll）
dotnet build Tools/ProtoExport
# 导出（必须带 ci 参数，无参会在结尾 pause 挂起）
cd Protobuf && ./Proto2CsExport-All.bat ci
```

导出后确认推送注册表：`FuFramework/网络/重新生成推送注册表`；守卫会在编译时比对业务 `[NotifyMessageHandler]` 声明与生成物，不一致即 LogError。

## 推送处理器

```csharp
// 构造参数：消息类型 + 处理方法名；Priority 是可写属性（数字小者先，默认 0）
[NotifyMessageHandler(typeof(NotifyBagInfoChanged), nameof(OnNotifyBagInfoChanged), Priority = 0)]
internal void OnNotifyBagInfoChanged(NotifyBagInfoChanged msg) { }
```

- 处理方法**必须是 `internal` 或 `public`**——`private`/`protected` 会让生成脚本报错并中止。
- 方法名用 `On` 前缀。
- 生成器要求宿主可达（继承 `Singleton<T>` 或有 `static Instance`）。
- **声明即生效**——无需手动 Register。手动 `Register()` 已删除，勿恢复。

## 网络模块（单频道门面）

- `NetworkModule` **直接持有唯一连接**。频道字典、频道名字、`GetNetworkChannel` 整套已拆（GameFramework 遗产），**勿加回**。
- 业务侧只写 `NetworkModule.Instance.Call(req)` / `.Send(x)`。
- TCP/WS 由编译宏在 `CreateChannel` 内决定（WebGL → WebSocket），与业务无关。
- **换线由服务器决定**。未来加多连接时封装在 `NetworkModule` 内部，不要把选线决策摊回业务调用点。
- 消息契约接口在 `Interface/`：`IRequestMessage(+泛型)`、`IResponseMessage`、`INotifyMessage`、`IHeartBeatMessage`。**只有真多态才配接口**。
- 推送派发阀门：`SuspendNotifyDispatch` / `ResumeNotifyDispatch` / `ClearNotifyBuffer`——登录/重连在"基线建立前挂起、拉取全量后恢复"时调用。

## 已知坑

- `Tools/ProtoExport/MessageHelper.cs` 的 `MessagePattern` 曾用 `([^}]+)` 导致空消息体 `{}` 被**静默跳过**（不导出、不报错）。已修为 `([^}]*)`。若"消息没进注册表"重现，先查这条正则。

## 心跳

- 状态机：`Network/State/HeartBeatState.cs`（`UpdateByRealTime` 用墙钟推进，后台回前台立即补发体检）。
- 频道侧：`Network/Channel/NetworkChannelBase.HeartBeat.cs`。
- 请求构造：`Network/MessageBase/IHeartBeatReqCreator.cs`；消息接口 `Interface/IHeartBeatMessage.cs`；心跳判定走接口而非枚举。
