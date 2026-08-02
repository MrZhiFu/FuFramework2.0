# UI 生命周期重构设计（OnClose 清理 + OnOpen 重建订阅）

> 日期：2026-08-02
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：WinBase 框架 + FGUI 代码生成模板 + 现有 UI/Comp 业务类

## 1. 背景与问题

UI 界面（`WinBase` 子类）的 `EventRegister`/`TimerRegister`/`FuiEventRegister` 只在对象池淘汰（`_OnDispose`）时释放，**关闭（复用）期间订阅与计时器保留在全局系统**：

- `EventRegister.Subscribe` → `m_EventModule.Subscribe`（全局事件系统）——UI 关闭后事件触发仍调用 invisible UI 的 handler
- `TimerRegister.StartIntervalTimer` 等——UI 关闭后计时器持续触发

**后果**：关闭的 UI（invisible）持续响应事件/计时器，浪费 CPU；生命周期不清晰（关闭≠不活跃）。

**另**：红点系统已改为配置驱动，UI 代码中的 `InitRedDot()` 方法（仅含 `// Example: RedDotRegister.RegisterRedDot(...)` 注释示例）为遗留残留，`RedDotRegister` 类已不存在，应清理。

## 2. 目标

1. **生命周期规范化**：UI 关闭自动清理（`StopAllTimers` + `UnSubscribeAll`），打开重建订阅（`OnOpen`）。
2. **模板引导**：FGUI 代码生成模板（`WinTemplate.txt`）调整，新生成的 UI 类默认遵循该模式。
3. **现有代码迁移**：11 个 Win 类 `InitEvent` 从 `OnInit` 移到 `OnOpen`。
4. **红点清理**：删除 16 个文件中的 `InitRedDot()` 方法与调用（红点不再代码注册）。

## 3. 现状调查

- 12 个 WinBase 业务类，其中 **WinGlobalMask 无 InitEvent**（不需迁移），**11 个有 InitEvent**（含 WinPlayerList 的 2 个实际 `Subscribe`，其余为注释引导）。
- 6 个 Comp 类（组件）有自己的**私有 `OnInit`**（非 WinBase 生命周期），`InitEvent` 不迁移，但 `InitRedDot` 需清理。
- 无 UI 使用计时器；WinPlayerList 订阅 `NetworkConnected`/`NetworkClosed`（低频）。

> 结论：**本改动在现有代码上无可感知的性能收益**（无计时器、仅低频订阅）。价值在于生命周期规范化（正确性）与模板引导（防未来 UI 泄漏计时器/订阅）。用户已确认此为"正确性+约定改进"，同意实施。

## 4. 设计

### 4.1 框架 `WinBase._OnClose` 自动清理（`WinBase.Life.cs`）

在 `Visible = false` 之后、`switch (TweenType)` 之前插入：

```csharp
// 关闭时自动清理：停止计时器、注销事件订阅，避免 invisible UI 持续响应
StopAllTimers();
UnSubscribeAll();
```

放在动画前（同步立即清理，不受 Fade 异步时序影响）。

### 4.2 模板 `WinTemplate.txt` 调整

```csharp
protected override void OnInit()
{
    InitUIComp();
    InitUIEvent();
    // 事件订阅已移至 OnOpen（每次打开重建），OnClose 框架自动清理
}

protected override void OnOpen()
{
    InitEvent();   // 每次打开重建订阅
    Refresh();
}

protected override void OnClose() { }  // 框架已自动 StopAllTimers + UnSubscribeAll，可追加业务清理
```

同时删除模板中"注册界面相关红点"的空注释段落（红点不再代码注册）。

### 4.3 现有 11 个 Win 类迁移（`InitEvent` 从 `OnInit` 移到 `OnOpen`）

```csharp
// 迁移前                        // 迁移后
OnInit()                        OnInit()
{                               {
    InitUIComp();                   InitUIComp();
    InitUIEvent();                  InitUIEvent();
    InitEvent();  // ← 移除         // InitEvent 已移至 OnOpen
    InitRedDot(); // ← 删除         // InitRedDot 删除（红点清理）
}                               }
OnOpen()                        OnOpen()
{                               {
                                    InitEvent();   // ← 新增
    Refresh();                      Refresh();
}                               }
```

**涉及文件**（11 个，WinGlobalMask 除外）：
`WinBag`、`WinGlobalLoading`、`WinClickGuide`、`WinDialogGuide`、`WinLoadingScene`、`WinLogin`、`WinLoginAnnouncement`、`WinPlayerCreate`、`WinPlayerList`、`WinMain`、`WinDialogMessageBox`

### 4.4 红点清理（16 个文件）

删除 `InitRedDot()` 方法（含 `// Example: RedDotRegister...` 注释）与 `OnInit` 中的 `InitRedDot();` 调用。涉及 11 个 Win 类 + 6 个 Comp 类中所有含 `InitRedDot` 的文件。Comp 类的 `InitEvent` 不移（无开关生命周期）。

### 4.5 README 更新

`Hotfix/README.md` 中 `RedDotRegister` 相关文档引用清理/更新。

## 5. 验证方式

1. 编译零错误（用户手动编译）。
2. Play 冒烟：UI 打开→关闭→再打开，确认复用后订阅重建、无重复订阅、Console 无异常。

## 6. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`refactor:` 框架 `_OnClose` 自动清理（WinBase.Life.cs）
- **Commit 2**：`refactor:` FGUI 模板调整（WinTemplate.txt）
- **Commit 3**：`refactor:` 现有 Win/Comp 类迁移 + 红点清理（~17 个 .cs + README）

每个 commit 前征得用户同意；只 add 本任务相关文件，不波及工作区其他未提交改动。
