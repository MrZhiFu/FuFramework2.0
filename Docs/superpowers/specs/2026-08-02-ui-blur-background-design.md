# UI 界面背景模糊功能设计

> 日期：2026-08-02
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/UI/` + `Unity/Assets/Bundles/Shader/`

## 1. 背景与目标

为项目实现移动端友好的 UI 界面背景模糊效果：对话框打开时，其背后的**全屏画面**（3D 场景 + 对话框下方的所有 UI 层）从清晰渐变为模糊（"快速结冰"效果）。

模糊与否由 `UIConfig` 配置表的 `Blur` 布尔字段驱动（已由用户加入并导出，生成代码 `UIConfig.cs` 已含 `Blur` 属性，当前仅 `WinDialogGuide` 为 true）。参考实现方案见 `C:\Users\Administrator\Desktop\UI模糊背景三级星型模糊实现方案.md`（三级星型模糊 Shader）。

**已确认的关键决策：**

| 决策点 | 结论 | 理由 |
|---|---|---|
| 模糊范围 | 全屏画面（3D 场景 + 下方 UI） | 最符合"背景模糊"直觉；经用户确认 |
| 配置粒度 | 布尔 `Blur`，统一一种模糊风格 | 符合"是否模糊"的驱动语义；后续需要再扩展 |
| 截屏方案 | StageCamera 挂载 `OnRenderImage` 组件 + 冻结帧 | GPU 侧零回读、真全屏合成、冻结帧天然不含对话框（无自反馈） |
| 模块归属 | 并入 `UIModule`（新增 partial `UIModule.Blur.cs`） | 模糊仅由 UI 开/关生命周期触发；UIModule 本为"一关注点一 partial"结构 |
| Shader 加载 | YooAsset 异步加载（`Bundles/Shader`） | 项目资源惯例；配合 Always Included Shaders 防剥离 |
| 挂载时机 | `UIModule.OnInit` 挂载 BlurCapture | 确定性、时序成立（OnInit 已依赖 GRoot.inst） |
| 资源预热 | OnInit 异步预热 Shader+材质，OnWinOpening 惰性等待 | 无冷启动卡顿，失败可降级 |

## 2. 架构总览

### 2.1 文件清单

| 文件 | 类型 | 职责 |
|---|---|---|
| `Assets/Bundles/Shader/UIBlurBackground.shader` | 资源（新增） | 三级星型模糊 + 压暗 + 渐变，BRP 版 |
| `Hotfix/Framework/UI/UIModule.Blur.cs` | partial（新增） | 协调器：RT / 材质 / 覆盖层 / BlurCapture / 模糊层级集合 / 渐入动画 |
| `Hotfix/Framework/UI/Utility/BlurCapture.cs` | MonoBehaviour（新增） | StageCamera 上的 `OnRenderImage` 截屏 + 冻结开关 |
| `UIModule.cs` / `UIModule.Open.cs` / `UIModule.Close.cs` | 既有（改 3 处挂点） | 插入模糊生命周期调用 |
| `HotfixLauncher.cs` | 既有（不改） | 无新模块注册 |

### 2.2 UIModule 集成点

| 挂点 | 调用 | 时机 |
|---|---|---|
| `UIModule.OnInit` | `EnsureBlurResources()` + `GetOrAddComponent<BlurCapture>()` | 启动预热 |
| `UIModule.Open.cs` `_OpenAsync`（`CreateFuiWin` 前） | `await OnWinOpening(uiCfg.Layer)` | 截屏冻结（对话框出现前） |
| `UIModule.Open.cs` `CreateFuiWin`（`_OnOpen()` 后） | `OnWinOpened(win)` | 显示覆盖层 + 渐入 |
| `UIModule.Close.cs` `Close`/`CloseNow`（`_OnClose()` 后） | `OnWinClosed(win)` | 隐藏/重定位覆盖层 |

`UIModule._OpenAsync` 中通过 `ConfigModule` 按 `winName` 查询 `UIConfig.Blur`（与 `WinBase.Init` 同款查表方式），仅在 `Blur=true` 时走模糊分支，`Blur=false` 界面零影响。

## 3. Shader（UIBlurBackground）

- **文件**：`Assets/Bundles/Shader/UIBlurBackground.shader`
- **Shader 名**：`Shader "UIBlurBackground"`
- **BRP 版（CGPROGRAM）**：直接采用参考文档章节 3.1 的完整 Shader，适配 FairyGUI quad 顶点格式
- **参数**：
  - `_BlurSize = 1.6`（半分辨率截屏纹素翻倍，URP 版 0.8 × 2）
  - `_MaskPower = 0.7`（参考文档 Blur 类型：强模糊 + 强压暗）
  - `_BlurProgress`（0→1 渐变，逐帧驱动）
  - 模板测试 / 混合等状态与 FairyGUI 渲染管线配合（文档 3.1 原样）
- **⚠️ 防剥离**：Shader 仅运行时经 YooAsset 加载、构建时无材质/场景引用，会被 Unity Shader Stripping 裁掉。**必须**加入 `ProjectSettings → Graphics → Always Included Shaders`。

## 4. BlurCapture（截屏与冻结）

- **位置**：`Hotfix/Framework/UI/Utility/BlurCapture.cs`
- **挂载**：`UIModule.OnInit` 执行 `GetOrAddComponent<BlurCapture>()` 挂到 `StageCamera.main.gameObject`（FairyGUI 静态引用，OnInit 时 GRoot 已就绪 → StageCamera 必已存在）。幂等防热更重载重复挂载。组件默认 `enabled=false`，零开销。
- **OnRenderImage**：
  ```csharp
  void OnRenderImage(RenderTexture source, RenderTexture destination)
  {
      if (Armed && SinkRT != null)
          Graphics.Blit(source, SinkRT);   // 武装时截全屏合成帧到半分辨率 RT
      Graphics.Blit(source, destination);  // 直通，保证画面正常显示
  }
  ```
  依赖的 `Armed`（静态）与 `SinkRT`（静态，指向 UIModule 持有的半分辨率 RT）由 UIModule.Blur 控制。
- **冻结帧原理**：StageCamera `clearFlags=Depth`（只清深度保留颜色），故 `OnRenderImage` 的 `source` 即"场景+UI"完整合成帧。武装一帧（对话框未上屏）→ RT 捕获"对话框出现前"画面 → 取消武装（RT 冻结）。冻结帧不含对话框本身 → 无自反馈。
- **实现期风险**：Hotfix MonoBehaviour 的 `OnRenderImage` 消息在 HybridCLR 下是否可靠需验证；若不支持，BlurCapture 降级为 AOT 薄组件（UIModule 经静态字段控制），改动很小。

## 5. UIModule.Blur（协调器）

新增 partial `UIModule.Blur.cs`，持有：

- `BlurCapture m_BlurCapture` —— 截屏组件引用
- `Shader m_BlurShader` + `UniTaskCompletionSource<Shader> m_ShaderLoadTask` —— Shader 及在途加载任务
- `Material m_BlurMaterial` —— 模糊材质单例（`HideFlags.HideAndDontSave`）
- `RenderTexture m_BlurRT` —— 半分辨率截屏 RT（懒创建，分辨率变化重建）
- `GImage m_BlurOverlay` —— 全屏覆盖层单例
- `List<EUILayer> m_ActiveBlurLayers` —— 当前可见模糊界面的层级集合（判断叠加 + 定位最上层）
- 渐入动画任务（UniTask，新动画/关闭时取消）

### 5.1 生命周期

**`EnsureBlurResources()`**（OnInit 调用，异步预热 `.Forget()`）：
1. 挂载 BlurCapture（见第 4 节）
2. `m_ShaderLoadTask = LoadAssetAsync<Shader>(UtilityAOT.AssetPath.GetShaderPath("UIBlurBackground.shader"))`，成功后 `m_BlurMaterial = new Material(shader)`
3. 失败 → `FuLogger` 警告，模糊功能禁用（后续界面照常打开）

**`OnWinOpening(EUILayer layer)`**（async）：
1. 若 Shader 未就绪，`await m_ShaderLoadTask`（异常 → 本次模糊禁用，返回）
2. 若已有可见模糊界面（`m_ActiveBlurLayers.Count > 0`），先隐藏覆盖层——保证重截帧不含旧覆盖层，**让新打开的顶层对话框的背板永远反映"其背后的当前画面"**
3. 确保 RT 尺寸正确 → 武装 BlurCapture → `await UniTask.Yield()`（捕获对话框出现前画面）→ 取消武装（冻结）

**`OnWinOpened(WinBase win)`**：
1. `m_ActiveBlurLayers.Add(win.UIConfig.Layer)`（`WinBase.Layer` 为 private，须经公开的 `win.UIConfig` 读取）
2. 确保覆盖层 GImage → 定位 `sortingOrder = (int)最上层模糊界面.Layer - 1`（即 `m_ActiveBlurLayers.Max()`）
3. `m_BlurMaterial.SetTexture("_BlurBGTex", m_BlurRT)`；`SetFloat("_BlurSize", 1.6f)`；`SetFloat("_MaskPower", 0.7f)`
4. `_BlurProgress = 0` → 覆盖层 `visible = true` → 渐入动画（时长 = `win.UIConfig?.TweenDuration ?? 0.3f`，见第 7 节）

**`OnWinClosed(WinBase win)`**：
1. `m_ActiveBlurLayers.Remove(win.UIConfig.Layer)`
2. 集合为空 → 隐藏覆盖层，取消渐入动画
3. 集合非空（叠加模糊界面）→ 覆盖层重定位到 `(int)剩余最上层.Layer - 1`（复用已冻结 RT，不重截）

> **叠加模糊界面的已知限制**：顶层对话框关闭后，被揭示的下层模糊对话框的背板可能出现"自身被再次模糊"的视觉瑕疵（冻结帧中包含该对话框自身，属单 Pass 冻结帧的固有限制）。v1 接受此限制，叠加属低频场景。

**`OnDispose`**（UIModule 释放）：
- 置空 BlurCapture 引用（组件保留在 StageCamera 上，禁用无害）
- 销毁材质、释放 RT、销毁覆盖层

## 6. 模糊覆盖层

- **载体**：GRoot 级 `GImage` 单例，`touchable = false`（点击穿透）
- **材质**：`UIModule.Blur` 持有的模糊材质（Shader 采样 `_BlurBGTex`，GImage 自身 texture 用白图 `Texture2D.whiteTexture` 包 NTexture）
- **全屏**：`m_BlurOverlay.IgnoreSafeArea()`（复用 `Utility/GObjectSafeAreaExt.cs`，含刘海适配 + 方向变化监听）
- **定位**：`sortingOrder = (int)当前最上层模糊界面.Layer - 1`，介于下方 UI 组与对话框组之间
- **生命周期**：独立于 UIGroup 的暂停/遮挡逻辑（直挂 GRoot，不受组状态影响）

## 7. 动画与参数

- **渐入**：UniTask 循环逐帧驱动 `_BlurProgress` 从 0 → 1，时长 = 该界面 `UIConfig.TweenDuration`（默认 0.3s，与开屏 Fade 同步）；`TweenDuration <= 0` 时直接置 1
- **取消**：新动画启动或界面关闭时取消旧动画，防止残留驱动
- **压暗**：`_MaskPower = 0.7` 强压暗，随 `_BlurProgress` 同步加深

## 8. 错误处理与降级

| 场景 | 行为 |
|---|---|
| Shader 未找到 / 加载失败 | 模糊功能禁用，界面照常打开，`FuLogger` 警告 |
| StageCamera 不存在 | OnInit 跳过挂载，模糊禁用 |
| 分辨率变化（旋转/缩放） | 重建半分辨率 RT |
| 打开失败（CreateFuiWin 异常） | 已冻结 RT 无泄漏，下次打开重新截屏 |
| 叠加模糊界面 | 新顶层打开时重截冻结（背板反映其背后画面）；关闭时重定位复用冻结帧 |

所有降级路径均不影响 `Blur=false` 界面。

## 9. 测试

1. **Play 冒烟（unity-cli）**：打开 `WinDialogGuide` → 验证 0.3s 渐入、背景模糊 + 压暗、点击穿透、关闭恢复
2. **对照组**：打开 `WinBag`（`Blur=false`）→ 无模糊，完全不受影响
3. **旋转**：屏幕旋转后验证 RT 重建、模糊仍正常
4. **叠加**：手动构造两个模糊界面叠加 → 顶层打开时验证重截冻结、关闭时验证重定位复用冻结帧
5. **降级**：移除 Shader 后打开模糊界面 → 界面正常、无崩溃、有警告日志
6. **Shader 防剥离**：打一个包验证 `UIBlurBackground` 可用（Always Included Shaders 配置生效）

## 10. 实现期风险

1. **HybridCLR `OnRenderImage`**：Hotfix MonoBehaviour 消息是否可靠。退路：AOT 薄组件。
2. **覆盖层 sortingOrder**：GRoot 子 GImage 的 `sortingOrder` 在 FairyGUI 中是否按预期排序（UIGroup 同机制）。实现时验证。
3. **Always Included Shaders**：需手动在 GraphicsSettings 添加，遗漏则热更包 Shader 不可用。

## 11. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`feat:` Shader 资源（`UIBlurBackground.shader`）
- **Commit 2**：`feat:` BlurCapture + UIModule.Blur + UIModule 三处挂点 + Always Included Shaders 配置
- **Commit 3**：`docs:` 本设计文档

每个 commit 前征得用户同意；提交时只 add 本任务相关文件，不波及工作区其他未提交改动。
