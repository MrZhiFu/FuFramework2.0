# Protobuf 协议生成

本目录存放**协议源文件**（`.proto`）与**协议生成脚本**。所有脚本最终都调用工具
`Tools/ProtoExport/bin/Debug/net8.0/ProtoExport.dll`（`dotnet` 运行，源码在 `Tools/ProtoExport/`）。

---

## 1. 目录结构

```
Protobuf/
  Proto/                        协议源文件（唯一需要手写/维护的地方）
    Bag_100.proto               package Bag;       option module = 100
    Basic_10.proto              package Basic;     option module = 10
    Common_20.proto             package Common;    option module = 20
    Inner_Basic_2.proto         package Inner_Basic; option module = 2
    User_300.proto              package User;      option module = 300

  Proto2CsExport-All.bat        ★ 一键：客户端 + 服务端 + 注册表
  Proto2CsExport_Client.bat/.sh   仅导出客户端 C#
  Proto2CsExport_Server.bat/.sh   仅导出服务端 C#
  gen-proto-registry.bat/.sh      仅生成协议消息注册表
  gen-proto-registry.py           注册表生成器实现（由上面的 .bat/.sh 调用）
```

---

## 2. 脚本说明

| 脚本 | 作用 | 何时用 |
|---|---|---|
| **`Proto2CsExport-All.bat`** | 客户端导出 → 服务端导出 → 生成注册表（**串行，任一失败即中止**） | **改了 proto，平时只跑这一个** |
| `Proto2CsExport_Client.bat` / `.sh` | 仅导出客户端 C# | 只想单独看/更新客户端 |
| `Proto2CsExport_Server.bat` / `.sh` | 仅导出服务端 C# | 只想单独更新服务端 |
| `gen-proto-registry.bat` / `.sh` | 仅生成协议消息注册表（不碰 proto C#） | **没动 proto**，只新增/改了 `[MessageHandler]` 方法 |

**免交互参数**：`Proto2CsExport-All.bat` 与 `gen-proto-registry.bat` 支持传任意参数（惯例用 `ci`）跳过结尾的 `pause`，便于 CI / 脚本串联：

```bat
Protobuf\Proto2CsExport-All.bat ci
Protobuf\gen-proto-registry.bat ci
```

> `Proto2CsExport_Client/Server.bat/.sh` **不支持**免 pause。

---

## 3. 常用场景

### 3.1 增删改了 proto（最常见）

```bat
cd D:\_WorkSpace\Unity\FuFramework2.0
Protobuf\Proto2CsExport-All.bat
```
一条命令完成：客户端 C# → 服务端 C# → 协议消息注册表。

**跑完后**：
1. 回 Unity 让它**重新导入**（会重新生成 `AutoGen/Proto/*.cs.meta`，见下方「注意事项」）；
2. 提交三样产出物（见 §4）。

### 3.2 只新增了 `[MessageHandler]` 方法（没动 proto）

```bat
Protobuf\gen-proto-registry.bat
```

### 3.3 macOS / Linux

```bash
cd <repo>
bash Protobuf/Proto2CsExport_Client.sh
bash Protobuf/Proto2CsExport_Server.sh
bash Protobuf/gen-proto-registry.sh
```
（`.sh` 结尾为 `read -p` 模拟 `pause`，按回车继续。）

---

## 4. 产出物（需一并提交）

| 产出 | 路径 | 说明 |
|---|---|---|
| 客户端 C# | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Proto/*.cs` | 命名空间 `Hotfix.Game.Proto`；类上带 `[MessageTypeHandler(消息ID)]` |
| 服务端 C# | `Server/FuFramework.Proto/Proto/` | 命名空间 `FuFramework.Proto.Proto` |
| **协议消息注册表** | `Unity/Assets/Scripts/Hotfix/Framework/Network/Generated/ProtoMessageRegistry.g.cs` | 自动生成，**勿手改**；把「消息 ID ↔ 类型」和「消息处理委托」在**编译期**固化，使网络模块**运行时不使用反射** |

> 消息 ID 规则：`ID = (option module << 16) | 序号`。例如 `Bag`（module=100）首个消息为 `6553600 + 10 = 6553610`。

---

## 5. 注意事项

1. **顺序不可颠倒**：注册表生成器扫描的是**导出后的 C#**（`Game/AutoGen/Proto/*.cs`），不是 `.proto`。
   先跑注册表只会扫到旧 C#，产出错误的映射 —— 正因如此，`Proto2CsExport-All.bat` 把两者合成了一条按正确顺序执行的命令。

2. **导出会删除 `AutoGen/Proto/*.cs.meta`**：`ProtoExport` 重写输出目录时不写 `.meta`。
   故跑完脚本后需**回 Unity 触发重新导入**（Unity 会重新生成 `.meta`），再提交 `.cs` 与 `.cs.meta`。

3. **`.bat` 必须保持纯 ASCII**：`cmd.exe` 按系统 ANSI 代码页（中文 Windows 为 **936/GBK**）解析 `.bat`，
   写入非 ASCII（如中文注释）会导致字节错位、**命令解析失败**。修改 `.bat` 时请勿加中文。

4. **`[MessageHandler]` 方法的可见性契约**：生成物会**直接调用**被标注的方法，故该方法必须是
   `internal` 或 `public`；`private` 会在生成期**报错并中止**（报错中指出文件、行号、方法名）。

5. **`Proto2CsExport_Client/Server.bat` 用相对路径 `cd ../Tools/ProtoExport/...`**：
   须在 `Protobuf/` 目录下运行（双击即可）；从其它目录调用会找不到工具。
   （`Proto2CsExport-All.bat` 与 `gen-proto-registry.*` 用脚本自身路径定位，可在任意目录运行。）

6. **CI 建议**：在流水线里跑 `Proto2CsExport-All.bat ci` 后执行 `git diff --exit-code <产出物>`，
   以捕获「改了 proto 却忘记重跑生成」的情况。
