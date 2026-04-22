# Game Hotfix 模块

## 1. 简介

Game Hotfix 模块是游戏的可热更新代码部分，通过 HybridCLR 技术实现代码热修复。该模块包含游戏的核心业务逻辑，可在不重新发布 App 的情况下更新修复。

## 2. 核心类说明

### 2.1 HotfixLauncher - 热更代码入口

热更代码的启动入口，由 AOT 部分的 `ProcedureCodeHotfix` 调用。

**主要功能**：
- 初始化协议消息处理器
- 异步加载配置表
- 加载初始 UI 资源
- 绑定 FairyGUI 自定义组件
- 设置多语言提供器
- 打开登录界面
- 初始化引导系统

**入口方法**：
```csharp
public static async UniTask Main()
```

### 2.2 HotfixProtoHandler - 协议程序集标记

用于标记协议所在的程序集，供 `ProtoMessageIdHandler` 初始化时使用。

```csharp
public static class HotfixProtoHandler
{
    public static Assembly CurrentAssembly => typeof(HotfixProtoHandler).Assembly;
}
```

## 3. 使用说明

### 3.1 添加新界面

1. 在 FairyGUI 编辑器中设计界面
2. 导出代码到 `UI/XXX/Gen/`
3. 在 `UI/XXX/Impl/` 实现类中写具体的业务逻辑代码

### 3.2 添加配置表

1. 在 Luban 配置表中定义数据结构
2. 导出配置表代码到 `Config/Generate/`
3. 通过 `GlobalModule.ConfigModule.GetConfig<T>()` 获取

### 3.3 添加协议

1. 定义协议数据结构
2. 导出协议代码到 `Proto`
3. 使用 `[MessageHandler]` 处理接收消息
4. 使用 `Call<T>()` 发送请求

### 3.4 添加红点

1. 在 `ModuleSetting/RedDotSetting` 中定义新的红点节点后保存，自动生成 `RedDotKeys` 中的 Key
2. 在界面 `OnInit` 中使用 `RedDotRegister.RegisterRedDot` 注册
3. 在适当时机调用 `GlobalModule.RedDotModule.SetRedDotCount()` 更新状态

## 4. 程序集定义

`Game.HotFix.asmdef` 定义了热更程序集的引用关系：

- `FuFramework.Core`
- `FuFramework.Launcher`
- `FuFramework.UI`
- `FuFramework.Network`
- `FuFramework.Config`
- `FuFramework.Localization`
- `FuFramework.Guide`
- `FuFramework.Event`
- `FuFramework.Sound`
- `FuFramework.ReferencePool`
- `FairyGUI`
- `YooAsset`
- `UniTask`
- `UnityEngine.CoreModule`
- `Game.AOT` (AOT 程序集)

## 5. 注意事项

1. **热更限制**：热更代码不能修改 AOT 部分的公共 API
2. **程序集引用**：热更程序集可以引用 AOT 程序集，反之不行
3. **反射注意**：使用反射访问 AOT 类型时需要注意性能
4. **配置表缓存**：`TableManager` 加载后会缓存配置表，避免重复加载
5. **多语言缓存**：`LocalizationProvider` 会缓存 `TbLocalization` 引用
6. **事件释放**：界面销毁时需要取消订阅事件，避免内存泄漏
7. **红点释放**：界面关闭时会自动释放红点，无需手动处理
