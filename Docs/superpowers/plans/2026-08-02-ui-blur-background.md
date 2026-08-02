# UI 界面背景模糊功能实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `UIConfig.Blur == true` 的界面实现全屏背景模糊（3D 场景 + 下方 UI 的完整合成帧渐变为模糊），由 `BlurModule`→`UIModule.Blur` 协调。

**Architecture:** 在 FairyGUI StageCamera 上挂 `BlurCapture`（`OnRenderImage` 截取"对话框出现前"的完整合成帧到半分辨率 RT 并冻结）；`UIModule.Blur.cs`（partial）协调 RT/材质/覆盖层/渐入动画，在 `_OpenAsync`/`CreateFuiWin`/`Close` 三处挂点接入。

**Tech Stack:** Unity BRP、FairyGUI、YooAsset（异步加载 Shader）、UniTask（异步与渐入动画）、HybridCLR（Hotfix 层 MonoBehaviour）。

## Global Constraints

- **交流**：全程中文。
- **Git**：遵循 `Docs/Git提交规范.md` —— Conventional Commits 中文描述；**AI 提交 subject 前缀 `[AI]`**；**任何 git 操作前必须征得用户同意**；只 add 本任务相关文件，不波及工作区其他未提交改动。
- **代码风格**：遵循 `Docs/代码风格规范.md`（方法 PascalCase、成员用 XML `///` 注释、单文件 ≤500 行）。
- **Shader 防剥离**：`UIBlurBackground` 仅运行时经 YooAsset 加载，构建时无引用会被 Stripping 裁掉，**必须**加入 `ProjectSettings → Graphics → Always Included Shaders`（Task 1 提供 Editor 工具）。
- **验证方式**：本计划不使用 unity-cli 自动化。**每个任务完成后由用户手动编译**（Unity Editor 等待编译完成、Console 无错误）**与 Play 测试**。
- **已知限制**（见设计文档第 5.1 节）：叠加模糊界面时，顶层关闭后下层背板可能出现"自身被再次模糊"的视觉瑕疵，v1 接受。

---

### Task 1: UIBlurBackground Shader 资源 + Always Included 配置工具

**Files:**
- Create: `Unity/Assets/Bundles/Shader/UIBlurBackground.shader`
- Create: `Unity/Assets/Editor/UIBlurShaderSetup.cs`

**Interfaces:**
- Produces: `Assets/Bundles/Shader/UIBlurBackground.shader`（Shader 名 `UIBlurBackground`，材质属性 `_BlurBGTex` / `_BlurSize` / `_MaskPower` / `_BlurProgress`）。Task 3 通过 `UtilityAOT.AssetPath.GetShaderPath("UIBlurBackground.shader")` 加载。

- [ ] **Step 1: 创建 Shader 文件**

创建 `Unity/Assets/Bundles/Shader/UIBlurBackground.shader`，内容为参考文档（`C:\Users\Administrator\Desktop\UI模糊背景三级星型模糊实现方案.md` 章节 3.1）的 BRP 完整版，仅改 Shader 名为 `UIBlurBackground`：

```hlsl
Shader "UIBlurBackground"
{
    Properties
    {
        // ── 核心参数 ──
        [HideInInspector] _MainTex ("Main Texture", 2D) = "black" {}
        _BlurBGTex ("Background Texture", 2D) = "black" {}  // 截屏纹理，由 C# 在显示模糊层时注入
        _BlurSize ("Blur Scale", Float) = 1.6               // 模糊采样步长（BRP 半分辨率需 ×2）
        _MaskPower ("Mask Power", Range(0, 1)) = 0.7        // 压暗强度：0=不压暗，1=全黑
        _BlurProgress ("Blur Progress", Range(0, 1)) = 1.0  // 渐变进度：0=清晰，1=全模糊

        // ── 模板测试（与 FairyGUI 渲染管线配合）──
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        // ── 颜色掩码与混合 ──
        _ColorMask ("Color Mask", Float) = 15
        _BlendSrcFactor ("Blend SrcFactor", Float) = 5     // SrcAlpha
        _BlendDstFactor ("Blend DstFactor", Float) = 10    // OneMinusSrcAlpha
    }

    SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Fog { Mode Off }
        Blend [_BlendSrcFactor] [_BlendDstFactor], One One
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIBlurDisc"
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float4 color : COLOR;
                    float4 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float4 color : COLOR;
                    float4 texcoord : TEXCOORD0;
                };

                sampler2D _BlurBGTex;
                float4 _BlurBGTex_TexelSize;
                float _BlurSize;
                float _MaskPower;
                float _BlurProgress;

                /// 单级星型模糊：8 方向采样加权混合（正交权重 1，对角权重 2，总和 ÷12）
                half4 BlurSample8(float2 uv, float2 texelSize, float scale)
                {
                    float2 o = texelSize * scale;
                    half4 sum;
                    sum  = tex2D(_BlurBGTex, uv + float2(-o.x * 2.0, 0)) * 1.0;
                    sum += tex2D(_BlurBGTex, uv + float2(-o.x,  o.y)) * 2.0;
                    sum += tex2D(_BlurBGTex, uv + float2(0,  o.y * 2.0)) * 1.0;
                    sum += tex2D(_BlurBGTex, uv + float2( o.x,  o.y)) * 2.0;
                    sum += tex2D(_BlurBGTex, uv + float2( o.x * 2.0, 0)) * 1.0;
                    sum += tex2D(_BlurBGTex, uv + float2( o.x, -o.y)) * 2.0;
                    sum += tex2D(_BlurBGTex, uv + float2(0, -o.y * 2.0)) * 1.0;
                    sum += tex2D(_BlurBGTex, uv + float2(-o.x, -o.y)) * 2.0;
                    sum /= 12.0;
                    return sum;
                }

                v2f vert (appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = ComputeScreenPos(o.vertex);
                    o.color = v.color;
                    return o;
                }

                fixed4 frag (v2f i) : SV_Target
                {
                    float2 uv = i.texcoord.xy / i.texcoord.w;
                    float2 texelSize = _BlurBGTex_TexelSize.xy;
                    float s = max(_BlurSize, 0.1);

                    // 三级星型模糊
                    half4 c0 = BlurSample8(uv, texelSize, s * 0.9);
                    half4 c1 = BlurSample8(uv, texelSize, s * 2.2);
                    half4 c2 = BlurSample8(uv, texelSize, s * 4.2);
                    half4 blurred = c0 * 0.50;
                    blurred += c1 * 0.32;
                    blurred += c2 * 0.18;

                    // 渐变结冰效果
                    half4 original = tex2D(_BlurBGTex, uv);
                    half4 result = lerp(original, blurred, _BlurProgress);
                    result.rgb *= lerp(1.0, 1.0 - _MaskPower, _BlurProgress);
                    result.a = 1.0;
                    return result;
                }
            ENDCG
        }
    }
}
```

- [ ] **Step 2: 创建 Always Included 配置工具**

创建 `Unity/Assets/Editor/UIBlurShaderSetup.cs`（在 `Assets/Editor/Unity.Editor.asmdef` 程序集内，仅依赖 UnityEditor/UnityEngine）：

```csharp
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI 背景模糊 Shader 的 Always Included Shaders 配置工具。
/// Shader 仅运行时经 YooAsset 加载，构建时无材质/场景引用会被 Stripping 裁掉，
/// 必须先加入 GraphicsSettings 的 Always Included Shaders。
/// </summary>
public static class UIBlurShaderSetup
{
    private const string ShaderPath = "Assets/Bundles/Shader/UIBlurBackground.shader";

    /// <summary>
    /// 将 UIBlurBackground 加入 Always Included Shaders（已存在则跳过）。
    /// </summary>
    [MenuItem("Tools/UIBlur/Add Shader To Always Included")]
    public static void AddBlurShaderToAlwaysIncluded()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("UIBlur", $"Shader 不存在：{ShaderPath}", "OK");
            return;
        }

        var graphicsSettings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (graphicsSettings == null)
        {
            EditorUtility.DisplayDialog("UIBlur", "找不到 ProjectSettings/GraphicsSettings.asset", "OK");
            return;
        }

        var serializedObject = new SerializedObject(graphicsSettings);
        var array = serializedObject.FindProperty("m_AlwaysIncludedShaders");
        if (array == null)
        {
            EditorUtility.DisplayDialog("UIBlur", "找不到 m_AlwaysIncludedShaders 属性", "OK");
            return;
        }

        for (var i = 0; i < array.arraySize; i++)
        {
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == shader)
            {
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("UIBlur", "UIBlurBackground 已在 Always Included Shaders 中", "OK");
                return;
            }
        }

        array.InsertArrayElementAtIndex(array.arraySize);
        array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = shader;
        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("UIBlur", "已添加 UIBlurBackground 到 Always Included Shaders", "OK");
    }
}
```

- [ ] **Step 3: 用户手动编译验证**

用户在 Unity Editor 中等待编译完成，确认：
1. Console 无 Shader 编译错误（选中 `.shader` 文件 Inspector 正常显示）。
2. 菜单 `Tools → UIBlur → Add Shader To Always Included` 可点击，执行后弹出成功对话框。
3. `Edit → Project Settings → Graphics → Always Included Shaders` 列表中出现 `UIBlurBackground`。

Expected: 三项均通过。若失败则修复后重试。

- [ ] **Step 4: 提交（征得用户同意）**

```bash
git add Unity/Assets/Bundles/Shader/UIBlurBackground.shader Unity/Assets/Editor/UIBlurShaderSetup.cs
git commit -m "[AI]feat: 新增 UI 背景模糊 Shader 与 Always Included 配置工具"
```

---

### Task 2: BlurCapture 截屏组件

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/UI/Utility/BlurCapture.cs`

**Interfaces:**
- Produces: `Hotfix.Framework.UI.BlurCapture : MonoBehaviour`，实例字段 `Armed : bool`、`SinkRT : RenderTexture`。Task 3 的 `UIModule.Blur` 持有引用并控制 `enabled` / `Armed` / `SinkRT`。

> 注：与设计文档"静态字段"措辞的偏差——改用实例字段（UIModule 已持有组件引用，无需全局静态状态）。

- [ ] **Step 1: 创建 BlurCapture 组件**

创建 `Unity/Assets/Scripts/Hotfix/Framework/UI/Utility/BlurCapture.cs`：

```csharp
using UnityEngine;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI 背景模糊截屏组件。
    /// 挂在 FairyGUI StageCamera 上（运行时由 UIModule 动态挂载）。
    /// StageCamera 的 clearFlags=Depth（只清深度保留颜色），故 OnRenderImage 的 source
    /// 即"场景 + UI"的完整合成帧。武装一帧后关闭，实现"冻结帧"（不含对话框自身，无自反馈）。
    /// </summary>
    public sealed class BlurCapture : MonoBehaviour
    {
        /// <summary>
        /// 是否武装截屏。为 true 时将当前帧合成画面 Blit 到 SinkRT。
        /// </summary>
        public bool Armed;

        /// <summary>
        /// 半分辨率截屏目标 RenderTexture（由 UIModule.Blur 注入）。
        /// </summary>
        public RenderTexture SinkRT;

        /// <summary>
        /// StageCamera 渲染完成回调：source 为完整合成帧。
        /// </summary>
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Armed && SinkRT != null)
                Graphics.Blit(source, SinkRT);

            Graphics.Blit(source, destination); // 直通，保证画面正常显示
        }
    }
}
```

- [ ] **Step 2: 用户手动编译验证**

用户在 Unity Editor 中等待编译完成，确认 Console 无编译错误。

Expected: 编译通过。行为在 Task 5 的 Play 测试中统一验证。

- [ ] **Step 3: 提交（征得用户同意）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/Utility/BlurCapture.cs
git commit -m "[AI]feat: 新增 UI 背景模糊截屏组件 BlurCapture"
```

---

### Task 3: UIModule.Blur 资源层 + 开屏集成

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Blur.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs`（OnInit 末尾加 `InitBlur();`；OnDispose 末尾加 `ReleaseBlur();`）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Open.cs`（`_OpenAsync` 加 `needBlur` 计算 + 3 处 `CreateFuiWin` 前插 `if (needBlur) await OnWinOpening();`；`CreateFuiWin` 在 `uiGroup.Refresh();` 后加 `OnWinOpened` 调用）

**Interfaces:**
- Consumes: `BlurCapture`（Task 2）、`UIBlurBackground` Shader（Task 1）、`ConfigModule.Instance?.GetConfig<TbUIConfig>()?.Get(winName)`、`AssetModule.LoadAssetAsync<Shader>(path)`。
- Produces: `UIModule.OnWinOpening()`（async，截屏冻结）、`UIModule.OnWinOpened(WinBase)`（显示覆盖层+渐入）、`UIModule.OnWinClosed(WinBase)`（Task 4 接线）、`UIModule.InitBlur()` / `UIModule.ReleaseBlur()`。
- 本任务接线开屏路径（OnInit / _OpenAsync / CreateFuiWin）。`OnWinClosed` 方法在本任务文件中定义，Task 4 接线。

- [ ] **Step 1: 创建 UIModule.Blur.cs**

创建 `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Blur.cs`（partial，完整包含开屏/关屏/释放全部逻辑）：

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using FairyGUI;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Core;
using Hotfix.Game.Config;
using YooAsset;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI管理模块分部类之一。
    /// 目标：为 UIConfig.Blur == true 的界面提供全屏背景模糊（3D 场景 + 下方 UI）。
    /// 功能：
    ///     1. 截屏冻结：利用 StageCamera 的 OnRenderImage 捕获"对话框出现前"的完整合成帧。
    ///     2. 模糊覆盖层：全屏 GImage 采样冻结帧，渲染在对话框层级下方。
    ///     3. 渐入动画：驱动 _BlurProgress 0→1，与界面开屏 Fade 同步。
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>模糊 Shader 资源名称。</summary>
        private const string BlurShaderName = "UIBlurBackground";

        /// <summary>模糊采样步长（BRP 半分辨率纹素翻倍 → URP 0.8 × 2）。</summary>
        private const float BlurSize = 1.6f;

        /// <summary>压暗强度（0=不压暗，1=全黑）。参考文档 Blur 类型：强模糊 + 强压暗。</summary>
        private const float MaskPower = 0.7f;

        /// <summary>截屏组件（挂在 FairyGUI StageCamera 上）。</summary>
        private BlurCapture m_BlurCapture;

        /// <summary>模糊 Shader 资源句柄（模块生命周期内保持引用，释放时 Release）。</summary>
        private AssetHandle m_BlurShaderHandle;

        /// <summary>模糊 Shader（加载完成后有效）。</summary>
        private Shader m_BlurShader;

        /// <summary>Shader 在途加载任务（OnWinOpening 惰性等待，防冷启动竞态）。</summary>
        private UniTaskCompletionSource<Shader> m_ShaderLoadTask;

        /// <summary>模糊材质单例（HideFlags.HideAndDontSave，不随场景保存）。</summary>
        private Material m_BlurMaterial;

        /// <summary>半分辨率截屏 RT。</summary>
        private RenderTexture m_BlurRT;

        /// <summary>全屏模糊覆盖层单例。</summary>
        private GImage m_BlurOverlay;

        /// <summary>当前可见模糊界面的层级集合（判断叠加 + 定位最上层）。</summary>
        private readonly List<EUILayer> m_ActiveBlurLayers = new();

        /// <summary>渐入动画取消令牌。</summary>
        private CancellationTokenSource m_BlurAnimCts;

        /// <summary>模糊 Shader 资源路径（Assets/Bundles/Shader/UIBlurBackground.shader）。</summary>
        private static string BlurShaderPath => UtilityAOT.AssetPath.GetShaderPath($"{BlurShaderName}.shader");

        /// <summary>
        /// 初始化模糊功能（UIModule.OnInit 调用）。
        /// 挂载截屏组件 + 异步预热 Shader 与材质。
        /// </summary>
        private void InitBlur()
        {
            if (StageCamera.main != null)
            {
                var go = StageCamera.main.gameObject;
                m_BlurCapture = go.GetComponent<BlurCapture>() ?? go.AddComponent<BlurCapture>();
                m_BlurCapture.enabled = false; // 默认禁用，零开销
            }

            m_ShaderLoadTask = new UniTaskCompletionSource<Shader>();
            LoadBlurShaderAsync().Forget();
        }

        /// <summary>
        /// 异步加载模糊 Shader 并创建材质。
        /// </summary>
        private async UniTaskVoid LoadBlurShaderAsync()
        {
            try
            {
                var assetModule = ModuleManager.GetModule<AssetModule>();
                if (assetModule == null)
                    throw new InvalidOperationException("[UIModule] 资源模块不存在。");

                m_BlurShaderHandle = await assetModule.LoadAssetAsync<Shader>(BlurShaderPath);
                m_BlurShader = m_BlurShaderHandle.GetAssetObject<Shader>();
                if (m_BlurShader == null)
                    throw new InvalidOperationException($"[UIModule] Shader '{BlurShaderPath}' 类型不匹配。");

                m_BlurMaterial = new Material(m_BlurShader) { hideFlags = HideFlags.HideAndDontSave };
                m_ShaderLoadTask.TrySetResult(m_BlurShader);
                FuLogger.LogInfo("[UIModule] UI背景模糊 Shader 加载完成。");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[UIModule] UI背景模糊 Shader 加载失败，模糊功能禁用：'{e.Message}'。");
                m_ShaderLoadTask.TrySetException(e);
            }
        }

        /// <summary>
        /// 截屏冻结：捕获"对话框出现前"的完整合成帧到半分辨率 RT。
        /// 在 _OpenAsync 创建 Fui 前调用（仅 Blur=true 时）。
        /// </summary>
        private async UniTask OnWinOpening()
        {
            if (m_BlurCapture == null) return;

            // Shader 未就绪时等待在途加载（失败则本次模糊禁用）
            if (m_BlurShader == null && m_ShaderLoadTask != null)
            {
                try { await m_ShaderLoadTask.Task; }
                catch { return; }
            }
            if (m_BlurShader == null || m_BlurMaterial == null) return;

            // 叠加场景：先隐藏覆盖层，保证重截帧不含旧覆盖层
            if (m_ActiveBlurLayers.Count > 0 && m_BlurOverlay != null)
                m_BlurOverlay.visible = false;

            EnsureBlurRT();
            if (m_BlurRT == null) return;

            // 武装一帧：OnRenderImage 把当前帧（对话框未上屏）合成画面截到 RT，随后冻结
            m_BlurCapture.SinkRT = m_BlurRT;
            m_BlurCapture.Armed = true;
            m_BlurCapture.enabled = true;
            await UniTask.Yield();
            m_BlurCapture.enabled = false;
            m_BlurCapture.Armed = false;
        }

        /// <summary>
        /// 界面打开完成：显示模糊覆盖层并播放渐入动画。
        /// 在 CreateFuiWin 中 win._OnOpen() 之后调用（win.UIConfig.Blur == true）。
        /// </summary>
        private void OnWinOpened(WinBase win)
        {
            var layer = win.UIConfig?.Layer ?? EUILayer.Normal;
            m_ActiveBlurLayers.Add(layer);

            if (m_BlurMaterial == null || m_BlurRT == null) return;

            EnsureBlurOverlay();
            if (m_BlurOverlay == null) return;

            // 定位到最上层模糊界面层级下方
            m_BlurOverlay.sortingOrder = MaxActiveBlurLayer() - 1;

            // 注入冻结帧与参数
            m_BlurMaterial.SetTexture("_BlurBGTex", m_BlurRT);
            m_BlurMaterial.SetFloat("_BlurSize", BlurSize);
            m_BlurMaterial.SetFloat("_MaskPower", MaskPower);

            m_BlurOverlay.visible = true;
            AnimateBlurIn(win.UIConfig?.TweenDuration ?? 0.3f);
        }

        /// <summary>
        /// 界面关闭：隐藏/重定位覆盖层。
        /// 在 Close/CloseNow 中 win._OnClose() 之后调用（win.UIConfig.Blur == true）。
        /// </summary>
        private void OnWinClosed(WinBase win)
        {
            var layer = win.UIConfig?.Layer ?? EUILayer.Normal;
            m_ActiveBlurLayers.Remove(layer);

            if (m_ActiveBlurLayers.Count == 0)
            {
                CancelBlurAnim();
                if (m_BlurOverlay != null)
                    m_BlurOverlay.visible = false;
                return;
            }

            // 仍有模糊界面：覆盖层重定位到剩余最上层下方（复用冻结帧，不重截）
            if (m_BlurOverlay != null)
                m_BlurOverlay.sortingOrder = MaxActiveBlurLayer() - 1;
        }

        /// <summary>
        /// 释放模糊资源（UIModule.OnDispose 调用）。
        /// </summary>
        private void ReleaseBlur()
        {
            CancelBlurAnim();

            if (m_BlurOverlay != null)
            {
                m_BlurOverlay.Dispose();
                m_BlurOverlay = null;
            }
            if (m_BlurRT != null)
            {
                m_BlurRT.Release();
                m_BlurRT = null;
            }
            if (m_BlurMaterial != null)
            {
                UnityEngine.Object.Destroy(m_BlurMaterial);
                m_BlurMaterial = null;
            }
            if (m_BlurShaderHandle != null)
            {
                m_BlurShaderHandle.Release();
                m_BlurShaderHandle = null;
                ModuleManager.GetModule<AssetModule>()?.UnloadAsset(BlurShaderPath);
            }
            m_BlurShader = null;
            m_ShaderLoadTask = null;
            m_BlurCapture = null;
            m_ActiveBlurLayers.Clear();
        }

        /// <summary>
        /// 确保半分辨率截屏 RT 存在且尺寸匹配当前屏幕。
        /// </summary>
        private void EnsureBlurRT()
        {
            var w = Screen.width / 2;
            var h = Screen.height / 2;
            if (w <= 0 || h <= 0) return;
            if (m_BlurRT != null && m_BlurRT.width == w && m_BlurRT.height == h) return;

            if (m_BlurRT != null) m_BlurRT.Release();
            m_BlurRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        /// <summary>
        /// 确保全屏模糊覆盖层存在（懒创建，GRoot 级单例）。
        /// </summary>
        private void EnsureBlurOverlay()
        {
            if (m_BlurOverlay != null) return;

            m_BlurOverlay = new GImage();
            m_BlurOverlay.name = m_BlurOverlay.gameObjectName = "__UIBlurBackground";
            m_BlurOverlay.touchable = false;      // 不拦截触摸，点击穿透到下层
            m_BlurOverlay.visible = false;
            m_BlurOverlay.texture = new NTexture(Texture2D.whiteTexture) { destroyMethod = DestroyMethod.None };
            m_BlurOverlay.material = m_BlurMaterial;
            GRoot.inst.AddChild(m_BlurOverlay);
            m_BlurOverlay.IgnoreSafeArea(RelationType.Size); // 全屏含刘海，复用 GObjectSafeAreaExt
        }

        /// <summary>
        /// 播放模糊渐入动画（0→1，时长取界面 TweenDuration）。
        /// </summary>
        private void AnimateBlurIn(float duration)
        {
            CancelBlurAnim();
            m_BlurAnimCts = new CancellationTokenSource();
            RunBlurAnimAsync(duration, m_BlurAnimCts.Token).Forget();
        }

        /// <summary>
        /// 模糊渐入动画实现：逐帧驱动 _BlurProgress。
        /// </summary>
        private async UniTask RunBlurAnimAsync(float duration, CancellationToken ct)
        {
            m_BlurMaterial.SetFloat("_BlurProgress", 0f);
            await UniTask.NextFrame(); // 确保 progress=0 先渲染一帧，避免首帧闪现

            if (duration <= 0f)
            {
                m_BlurMaterial.SetFloat("_BlurProgress", 1f);
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_BlurMaterial.SetFloat("_BlurProgress", Mathf.Clamp01(elapsed / duration));
                await UniTask.NextFrame(PlayerLoopTiming.Update, ct);
            }
            m_BlurMaterial.SetFloat("_BlurProgress", 1f);
        }

        /// <summary>
        /// 取消渐入动画。
        /// </summary>
        private void CancelBlurAnim()
        {
            m_BlurAnimCts?.Cancel();
            m_BlurAnimCts?.Dispose();
            m_BlurAnimCts = null;
        }

        /// <summary>
        /// 获取当前最上层模糊界面的层级数值。
        /// </summary>
        private int MaxActiveBlurLayer()
        {
            var max = (int)m_ActiveBlurLayers[0];
            for (var i = 1; i < m_ActiveBlurLayers.Count; i++)
            {
                var value = (int)m_ActiveBlurLayers[i];
                if (value > max) max = value;
            }
            return max;
        }
    }
}
```

- [ ] **Step 2: 修改 UIModule.cs**

`Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs` 两处：

① `OnInit()` 末尾（`foreach (EUILayer layer ...)` 循环之后）追加：

```csharp
            // 初始化 UI 背景模糊功能（挂载截屏组件 + 预热 Shader）
            InitBlur();
```

② `OnDispose()` 末尾（`PkgManager.RemoveAllPkg();` 之后）追加：

```csharp
            ReleaseBlur();
```

- [ ] **Step 3: 修改 UIModule.Open.cs**

`Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Open.cs`：

① 顶部 using 追加：

```csharp
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
```

② `_OpenAsync<T>` 中，在 `var winName = typeof(T).Name;` 之后、`if (IsLoading(winName))` 之前插入配置查询：

```csharp
            var winName = typeof(T).Name;

            // 查询 UIConfig：是否带模糊背景（与 WinBase.Init 同款查表方式）
            var uiConfig = ConfigModule.Instance?.GetConfig<TbUIConfig>()?.Get(winName);
            var needBlur = uiConfig?.Blur == true;
```

③ 三处 `return CreateFuiWin(win, tempSerialId, isNewIns, userData);` 之前各插入一行（在 `await PkgManager.LoadPkgAsync` 之后的第三处插在 await 与 CreateFuiWin 之间）：

```csharp
                    // Blur=true：先截屏冻结"对话框出现前"的画面
                    if (needBlur) await OnWinOpening();
```

④ `CreateFuiWin<T>` 中，在 `win._OnOpen(); uiGroup.Refresh();` 之后、广播 `OpenUISuccessEventArgs` 之前插入：

```csharp
                win._OnOpen();     // 界面打开回调
                uiGroup.Refresh(); // 刷新界面组

                // 模糊界面：显示模糊覆盖层并播放渐入
                if (win.UIConfig?.Blur == true)
                    OnWinOpened(win);
```

- [ ] **Step 4: 用户手动编译 + Play 测试**

用户在 Unity Editor 中等待编译完成，确认 Console 无编译错误。

Play 测试（在 `Test.unity` 或可运行场景中）：
1. 打开 `WinDialogGuide`（`UIConfig.Blur == true`）。
2. 预期：其背后全屏画面在约 0.3s 内从清晰渐变为模糊 + 压暗；对话框本身清晰。
3. 打开 `WinBag`（`Blur == false`）→ 无模糊，完全不受影响。

Expected: 编译通过；Play 中 WinDialogGuide 显示模糊背景。若模糊未出现，检查 Console 的 Shader 加载失败日志或 `m_BlurOverlay` 覆盖层逻辑。

- [ ] **Step 5: 提交（征得用户同意）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Blur.cs Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Open.cs
git commit -m "[AI]feat: UI 背景模糊开屏集成（截屏冻结 + 覆盖层 + 渐入动画）"
```

---

### Task 4: 关屏集成 + 叠加处理 + 资源释放

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Close.cs`（`Close` 与 `CloseNow` 两处 `win._OnClose(); uiGroup.Refresh();` 之后加 `OnWinClosed` 调用）

**Interfaces:**
- Consumes: `UIModule.OnWinClosed(WinBase)`（Task 3 已定义）、`UIModule.ReleaseBlur()`（Task 3 已定义）。
- Produces: 关屏/叠加行为（Task 3 的 `OnWinClosed` 方法接线）。

- [ ] **Step 1: 修改 UIModule.Close.cs**

`Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Close.cs` 的 `Close(WinBase)` 与 `CloseNow(WinBase)` 两处，在 `win._OnClose(); uiGroup.Refresh();` 之后、广播 `CloseUICompleteEventArgs` 之前各插入：

```csharp
            win._OnClose();
            uiGroup.Refresh();

            // 模糊界面：隐藏/重定位模糊覆盖层
            if (win.UIConfig?.Blur == true)
                OnWinClosed(win);
```

（`Close` 与 `CloseNow` 各有一处，共两处插入。）

- [ ] **Step 2: 用户手动编译 + Play 测试**

用户在 Unity Editor 中等待编译完成，确认 Console 无编译错误。

Play 测试：
1. 打开 `WinDialogGuide` → 模糊出现 → 关闭 → 模糊覆盖层隐藏，底层恢复清晰。
2. 手动构造两个 `Blur=true` 界面叠加（如临时给某 `Tips` 层界面配置 Blur=true）：顶层打开时背板反映其背后画面（含下层模糊对话框）；顶层关闭后覆盖层重定位到剩余模糊界面的层级下方。
3. 屏幕旋转：模糊仍正常（RT 重建）。

Expected: 关闭恢复清晰；叠加时重定位正确；旋转无异常。

- [ ] **Step 3: 提交（征得用户同意）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Close.cs
git commit -m "[AI]feat: UI 背景模糊关屏集成（隐藏/重定位覆盖层）"
```

---

### Task 5: Play 全量验证

**Files:**
- 无代码改动（仅验证 + 按需修复）。

**Interfaces:**
- 无。

- [ ] **Step 1: 验证 spec 第 9 节全部场景**

用户按以下清单在 Play 中逐一验证：

1. **打开 `WinDialogGuide`**：约 0.3s 渐入（清晰→模糊+压暗）；背景覆盖全屏含刘海；对话框清晰；点击可穿透到对话框（覆盖层 touchable=false）。
2. **关闭**：模糊覆盖层消失，底层恢复清晰。
3. **对照组 `WinBag`（Blur=false）**：无模糊，零影响。
4. **旋转/缩放**：半分辨率 RT 重建，模糊正常。
5. **叠加**：两个 Blur 界面叠加 → 顶层背板重截、关闭后重定位（接受已知"下层自模糊"瑕疵）。
6. **降级**：临时删除 `UIBlurBackground.shader` 后打开 `WinDialogGuide` → 界面正常打开、无崩溃、Console 有 Shader 加载失败警告（验证后恢复 Shader）。
7. **3D 场景验证（关键）**：确认模糊背板确实包含 3D 场景内容（而非仅 UI）。若场景未模糊（只糊了 UI），说明 StageCamera OnRenderImage 的 source 未含主相机画面——实现期风险点，需调整截屏时机或合成方式。

- [ ] **Step 2: 打包验证 Shader 防剥离（可选，用户有包环境时）**

打一个包，运行后打开 `WinDialogGuide`，确认模糊正常（`Always Included Shaders` 配置生效，Shader 未被剥离）。同时确认 YooAsset 构建收集了 `Bundles/Shader/UIBlurBackground.shader`。

- [ ] **Step 3: 全量通过后提交收尾**

若 Step 1/2 发现需修复的问题，修复后重新验证；全部通过后（如需）提交修复：

```bash
git add <修改的文件>
git commit -m "[AI]fix: 修复 UI 背景模糊<具体问题>"
```

---

## Self-Review 结论

- **Spec 覆盖**：Spec 第 3 节（Shader）→ Task 1；第 4 节（BlurCapture）→ Task 2；第 2/5/6/7 节（UIModule.Blur、覆盖层、动画、集成挂点）→ Task 3/4；第 8 节（降级）→ Task 3/5；第 9 节（测试）→ Task 5；Always Included Shaders → Task 1。
- **已知偏差**：① `BlurCapture` 的 `Armed`/`SinkRT` 由 spec 的"静态"改为实例字段（UIModule 持有引用，无需全局状态）；② `OnWinOpening` 去掉 spec 中未使用的 `(EUILayer layer)` 参数；③ spec 的 Commit 2 按评审粒度拆为 Task 2/3/4 三个 `feat` 提交。
