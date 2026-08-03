using UnityEngine;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI 背景模糊截屏组件。
    /// 目标：在 StageCamera 渲染完成后，将"场景 + UI"完整合成帧截取到半分辨率 RT。
    /// 功能：
    ///     1. 运行时由 UIModule 动态挂载到 FairyGUI StageCamera 上。
    ///     2. 捕获一帧后关闭，实现"冻结帧"（不含对话框自身，无自反馈）。
    /// 原理：StageCamera 的 clearFlags=Depth（只清深度保留颜色），故 OnRenderImage 的 source 即完整合成帧。
    /// </summary>
    public sealed class BlurCapture : MonoBehaviour
    {
        /// <summary>
        /// 是否执行截屏。为 true 时将当前帧合成画面 Blit 到 m_CaptureRT。
        /// </summary>
        public bool m_Capture;

        /// <summary>
        /// 半分辨率截屏目标 RenderTexture（由 UIModule.Blur 注入）。
        /// </summary>
        public RenderTexture m_CaptureRT;

        /// <summary>
        /// StageCamera 渲染完成回调：source 为完整合成帧。
        /// </summary>
        /// <param name="source">StageCamera 渲染输出的完整合成帧（场景 + UI）。</param>
        /// <param name="destination">屏幕输出目标。</param>
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // 截屏：仅在捕获帧把当前合成帧复制到半分辨率 RT，供模糊覆盖层采样。
            if (m_Capture && m_CaptureRT != null)
                Graphics.Blit(source, m_CaptureRT);

            // 原样输出：把渲染画面写回屏幕输出目标，保证画面正常显示。
            Graphics.Blit(source, destination);
        }
    }
}