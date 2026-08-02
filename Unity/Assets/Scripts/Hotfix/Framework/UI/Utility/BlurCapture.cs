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
