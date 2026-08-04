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
    ///     1. 截屏冻结：利用 StageCamera 的 OnRenderImage 捕获"UI界面出现前"的完整合成帧。
    ///     2. 模糊覆盖层：全屏 GImage 采样冻结帧，渲染在对话框层级下方。
    ///     3. 渐入动画：驱动 _BlurProgress 0→1，与界面开屏 Fade 同步。
    /// 原理：
    ///     1. StageCamera 的 clearFlags=Depth（只清深度保留颜色），其 OnRenderImage 的 source 即
    ///        "3D 场景 + UI"完整合成帧；BlurCapture 在捕获帧将其截取到半分辨率 RT 后冻结。
    ///     2. 冻结帧采集于对话框上屏前，不含对话框自身，避免自反馈。
    ///     3. 覆盖层使用三级星型模糊 Shader 采样冻结帧，_BlurSize/_MaskPower/_BlurProgress 控制
    ///        模糊强度、压暗与渐变进度。
    /// 流程：
    ///     1. UIModule.OnInit → InitBlur：挂载 BlurCapture 到 StageCamera + 异步加载 Shader 创建材质。
    ///     2. 打开 Blur=true 界面 → _OpenAsync 调 OnWinOpeningAsync：捕获一帧并冻结。
    ///     3. CreateFuiWin → OnWinOpened：定位覆盖层到界面层级下方、注入冻结帧、播放渐入。
    ///     4. 关闭界面 → Close/CloseNow 调 OnWinClosed：隐藏覆盖层；叠加时重定位复用冻结帧。
    ///     5. UIModule.OnDispose → ReleaseBlur：释放 RT/材质/覆盖层/Shader 句柄。
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>
        /// 模糊 Shader 资源名称。
        /// </summary>
        private const string BlurShaderName = "UIBlurBackground";

        /// <summary>
        /// 模糊采样步长（BRP 半分辨率纹素翻倍 → URP 0.8 × 2）。
        /// </summary>
        private const float BlurSize = 1.6f;

        /// <summary>
        /// 压暗强度（0=不压暗，1=全黑）。参考文档 Blur 类型：强模糊 + 强压暗。
        /// </summary>
        private const float MaskPower = 0.35f;

        /// <summary>
        /// 截屏组件（挂在 FairyGUI StageCamera 上）。
        /// </summary>
        private BlurCapture m_BlurCapture;

        /// <summary>
        /// 模糊 Shader 资源句柄（模块生命周期内保持引用，释放时 Release）。
        /// </summary>
        private AssetHandle m_BlurShaderHandle;

        /// <summary>
        /// 模糊 Shader（加载完成后有效）。
        /// </summary>
        private Shader m_BlurShader;

        /// <summary>
        /// Shader 在途加载任务（OnWinOpeningAsync 惰性等待，防冷启动竞态）。
        /// </summary>
        private UniTaskCompletionSource<Shader> m_ShaderLoadTask;

        /// <summary>
        /// 模糊材质单例（HideFlags.HideAndDontSave，不随场景保存）。
        /// </summary>
        private Material m_BlurMaterial;

        /// <summary>
        /// 半分辨率截屏 RT。
        /// </summary>
        private RenderTexture m_BlurRT;

        /// <summary>
        /// 全屏模糊覆盖层单例。
        /// </summary>
        private GImage m_BlurOverlay;

        /// <summary>
        /// 当前可见模糊界面的层级集合（判断叠加 + 定位最上层）。
        /// </summary>
        private readonly List<EUILayer> m_ActiveBlurLayers = new();

        /// <summary>
        /// 渐入动画取消令牌。
        /// </summary>
        private CancellationTokenSource m_BlurAnimCts;

        /// <summary>
        /// 是否已销毁（热更重载防护：在途 Shader 加载完成后不写回状态，防句柄/材质泄漏）。
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        /// 模糊 Shader 资源路径（Assets/Bundles/Shader/UIBlurBackground.shader）。
        /// </summary>
        private static string BlurShaderPath => UtilityAOT.AssetPath.GetShaderPath($"{BlurShaderName}.shader");

        /// <summary>
        /// 初始化模糊功能（UIModule.OnInit 调用）。
        /// 挂载截屏组件 + 异步预热 Shader 与材质。
        /// </summary>
        private void InitBlur()
        {
            m_IsDisposed = false;

            if (StageCamera.main != null)
            {
                // 检查 StageCamera 是否已挂载截屏组件，未挂载则挂载一个。
                var go = StageCamera.main.gameObject;
                m_BlurCapture         = go.GetComponent<BlurCapture>() ?? go.AddComponent<BlurCapture>();
                m_BlurCapture.enabled = false; // 默认禁用
            }

            m_ShaderLoadTask = new UniTaskCompletionSource<Shader>();

            // 异步加载模糊 Shader 并创建材质。
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
                m_BlurShader       = m_BlurShaderHandle.GetAssetObject<Shader>();
                if (m_BlurShader == null)
                    throw new InvalidOperationException($"[UIModule] Shader '{BlurShaderPath}' 类型不匹配。");

                // 模块销毁后（热更重载）：释放句柄并放弃写入，防止旧任务覆盖新任务状态导致泄漏
                if (m_IsDisposed)
                {
                    m_BlurShaderHandle.Release();
                    m_BlurShaderHandle = null;
                    m_BlurShader       = null;
                    return;
                }

                m_BlurMaterial = new Material(m_BlurShader) { hideFlags = HideFlags.HideAndDontSave };
                m_ShaderLoadTask.TrySetResult(m_BlurShader);
                FuLogger.LogInfo("[UIModule] UI背景模糊 Shader 加载完成。");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[UIModule] UI背景模糊 Shader 加载失败，模糊功能禁用：'{e.Message}'。");

                // 模块已销毁时不污染新任务状态
                if (m_IsDisposed) return;
                m_ShaderLoadTask.TrySetException(e);
            }
        }

        /// <summary>
        /// 截屏冻结：捕获"UI界面出现前"的完整合成帧到半分辨率 RT。
        /// 在 _OpenAsync 创建 Fui 前调用（仅 Blur=true 时）。
        /// </summary>
        private async UniTask OnWinOpeningAsync()
        {
            if (m_BlurCapture == null) return;

            // Shader 未就绪时等待在途加载（失败则本次模糊禁用）
            if (m_BlurShader == null && m_ShaderLoadTask != null)
            {
                try
                {
                    await m_ShaderLoadTask.Task;
                }
                catch
                {
                    return;
                }
            }

            if (m_BlurShader == null || m_BlurMaterial == null) return;

            // 叠加场景：先隐藏覆盖层，保证重截帧不含旧覆盖层
            if (m_ActiveBlurLayers.Count > 0 && m_BlurOverlay != null)
                m_BlurOverlay.visible = false;

            // 确保半分辨率截屏 RT 存在且尺寸匹配当前屏幕。
            EnsureBlurRT();
            if (m_BlurRT == null) return;

            // 捕获一帧：OnRenderImage 把当前帧（对话框未上屏）合成画面截到 RT，随后冻结
            m_BlurCapture.m_CaptureRT = m_BlurRT;
            m_BlurCapture.m_Capture   = true;
            m_BlurCapture.enabled     = true;

            // 等待本帧渲染完成（LastPostLateUpdate 阶段），确保 OnRenderImage 已把画面截到 RT，不受调用阶段影响
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            m_BlurCapture.enabled   = false;
            m_BlurCapture.m_Capture = false;
        }

        /// <summary>
        /// 界面打开完成：显示模糊覆盖层并播放渐入动画。
        /// 在 CreateFuiWin 中 win._OnOpen() 之后调用（win.UIConfig.Blur == true）。
        /// </summary>
        /// <param name="win">已打开的模糊界面。</param>
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
            m_BlurMaterial.SetFloat("_BlurSize",  BlurSize);
            m_BlurMaterial.SetFloat("_MaskPower", MaskPower);

            m_BlurOverlay.visible = true;

            // 渐入动画：驱动 _BlurProgress 0→1，与界面开屏 Fade 同步。
            AnimateBlurIn(win.UIConfig?.TweenDuration ?? 0.3f);
        }

        /// <summary>
        /// 界面关闭：隐藏/重定位覆盖层。
        /// 在 Close/CloseNow 中 win._OnClose() 之后调用（win.UIConfig.Blur == true）。
        /// </summary>
        /// <param name="win">已关闭的模糊界面。</param>
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
            {
                m_BlurOverlay.sortingOrder = MaxActiveBlurLayer() - 1;

                // 叠加打开时 OnWinOpeningAsync 可能隐藏了覆盖层，这里恢复显示，防止下层模糊界面背板丢失
                m_BlurOverlay.visible = true;
            }
        }

        /// <summary>
        /// 释放模糊资源（UIModule.OnDispose 调用）。
        /// </summary>
        private void ReleaseBlur()
        {
            // 标记已销毁：在途 Shader 加载完成后不写回状态（见 LoadBlurShaderAsync）
            m_IsDisposed = true;

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

            // 销毁截屏组件，避免残留（否则第三方误启用会使 StageCamera 每帧多两次 Blit）
            if (m_BlurCapture != null)
            {
                UnityEngine.Object.Destroy(m_BlurCapture);
                m_BlurCapture = null;
            }

            m_BlurShader     = null;
            m_ShaderLoadTask = null;
            m_ActiveBlurLayers.Clear();
        }

        /// <summary>
        /// 确保半分辨率截屏 RT 存在且尺寸匹配当前屏幕。
        /// </summary>
        private void EnsureBlurRT()
        {
            var w = Screen.width  / 2;
            var h = Screen.height / 2;
            if (w <= 0 || h <= 0) return;
            
            // IsCreated() 检测：app 切后台后 RenderTexture 可能设备丢失（width/height 保留原值但已释放），需重建
            if (m_BlurRT != null && m_BlurRT.IsCreated() && m_BlurRT.width == w && m_BlurRT.height == h) 
                return;

            if (m_BlurRT != null)
                m_BlurRT.Release();
            
            m_BlurRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags  = HideFlags.HideAndDontSave,
            };
            
            // 显式创建，使 IsCreated() 立即为 true，避免"未使用就重建"的边界分配
            m_BlurRT.Create(); 
        }

        /// <summary>
        /// 确保全屏模糊覆盖层存在（懒创建，GRoot 级单例）。
        /// </summary>
        private void EnsureBlurOverlay()
        {
            if (m_BlurOverlay != null) return;

            m_BlurOverlay      = new GImage();
            m_BlurOverlay.name = m_BlurOverlay.gameObjectName = "__UIBlurBackground";

            // 不拦截触摸，点击穿透到下层
            m_BlurOverlay.touchable = false;
            m_BlurOverlay.visible   = false;
            m_BlurOverlay.texture   = new NTexture(Texture2D.whiteTexture) { destroyMethod = DestroyMethod.None };
            m_BlurOverlay.material  = m_BlurMaterial;
            GRoot.inst.AddChild(m_BlurOverlay);

            // 全屏，忽略刘海
            m_BlurOverlay.IgnoreSafeArea();
        }

        /// <summary>
        /// 播放模糊渐入动画（0→1，时长取界面 TweenDuration）。
        /// </summary>
        /// <param name="duration">渐入动画时长（秒），&lt;=0 时直接置满。</param>
        private void AnimateBlurIn(float duration)
        {
            CancelBlurAnim();
            m_BlurAnimCts = new CancellationTokenSource();
            RunBlurAnimAsync(duration, m_BlurAnimCts.Token).Forget();
        }

        /// <summary>
        /// 模糊渐入动画实现：逐帧驱动 _BlurProgress。
        /// </summary>
        /// <param name="duration">渐入动画时长（秒）。</param>
        /// <param name="ct">取消令牌，动画被取消时终止。</param>
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
        /// <returns>当前可见模糊界面中的最大层级数值；无模糊界面时返回 0。</returns>
        private int MaxActiveBlurLayer()
        {
            if (m_ActiveBlurLayers.Count == 0) return 0;

            var max = (int)m_ActiveBlurLayers[0];
            for (var i = 1; i < m_ActiveBlurLayers.Count; i++)
            {
                var value = (int)m_ActiveBlurLayers[i];
                if (value > max)
                    max = value;
            }

            return max;
        }
    }
}