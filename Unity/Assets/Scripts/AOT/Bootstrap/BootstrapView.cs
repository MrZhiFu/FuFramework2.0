using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace Launcher
{
    /// <summary>
    /// AOT 启动加载界面。
    /// 功能：从 Resources 加载 "Launcher" FUI 包并显示进度/提示/更新确认框，脱离 UIModule/EventModule 自包含运行。
    /// </summary>
    public sealed class BootstrapView : IBootstrapView
    {
        private GComponent     m_View;
        private Controller     m_IsNeedUpgrade;
        private Controller     m_IsDownloading;
        private GTextField     m_TxtTips;
        private GProgressBar   m_ProgressBar;
        private GButton        m_BtnOk;
        private GRichTextField m_TxtContent;
        private Action         m_OnConfirm;

        /// <summary>
        /// 创建并显示加载界面。
        /// </summary>
        public static UniTask<BootstrapView> CreateAsync()
        {
            var view = new BootstrapView();
            view.Init();
            return UniTask.FromResult(view);
        }

        private void Init()
        {
            // 从 Resources 加载 Launcher 包（不依赖 YooAsset）
            UIPackage.AddPackage("UI/Launcher/Launcher");

            m_View = UIPackage.CreateObject("Launcher", "WinLauncher").asCom;
            m_View.MakeFullScreen();
            GRoot.inst.AddChild(m_View);

            m_IsNeedUpgrade = m_View.GetController("IsNeedUpgrade");
            m_IsDownloading = m_View.GetController("IsDownloading");
            m_TxtTips       = (GTextField)m_View.GetChild("_txtTips");
            m_ProgressBar   = (GProgressBar)m_View.GetChild("_progressBar");
            m_BtnOk         = (GButton)m_View.GetChild("_btnOk");
            m_TxtContent    = (GRichTextField)m_View.GetChild("_txtContent");

            m_BtnOk.onClick.Set(OnBtnOkClick);
            SetNeedUpgrade(false);
            SetDownloading(false);
        }

        /// <summary>
        /// 设置提示文本。
        /// </summary>
        public void SetTip(string text)
        {
            if (m_TxtTips != null) m_TxtTips.text = text;
        }

        /// <summary>
        /// 设置下载进度（0~1）与提示。
        /// </summary>
        public void SetProgress(float value01, string tip)
        {
            SetDownloading(true);
            if (m_ProgressBar != null) m_ProgressBar.value = value01 * 100f;
            SetTip(tip);
        }

        /// <summary>
        /// 显示更新确认框。
        /// </summary>
        public void ShowUpdateDialog(string content, Action onConfirm)
        {
            SetNeedUpgrade(true);
            m_BtnOk.title     = "更新";
            m_TxtContent.text = content;
            m_TxtContent.onClick.Set(ctx =>
            {
                if (ctx.data != null) Utility.Application.OpenURL(ctx.data.ToString());
            });
            m_OnConfirm = onConfirm;
        }

        /// <summary>
        /// 设置是否显示更新确认框。
        /// </summary>
        public void SetNeedUpgrade(bool need) => m_IsNeedUpgrade.SetSelectedIndex(need ? 1 : 0);

        /// <summary>
        /// 设置是否处于下载中状态。
        /// </summary>
        public void SetDownloading(bool downloading) => m_IsDownloading.SetSelectedIndex(downloading ? 1 : 0);

        /// <summary>
        /// 关闭并销毁加载界面。
        /// </summary>
        public void Close()
        {
            if (m_View == null) return;
            GRoot.inst.RemoveChild(m_View, true);
            m_View = null;
        }

        private void OnBtnOkClick(EventContext ctx) => m_OnConfirm?.Invoke();
    }
}