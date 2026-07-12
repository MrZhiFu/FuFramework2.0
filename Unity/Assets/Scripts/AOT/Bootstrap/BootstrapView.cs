using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace Launcher
{
    /// <summary>
    /// AOT 启动加载界面。
    /// 功能：显示进度、提示文本、更新确认框，脱离 UIModule/EventModule 自包含运行。
    /// UI 组件绑定部分见 WinLauncher.Gen.cs。
    /// </summary>
    public sealed class BootstrapView : IBootstrapView
    {
        private WinLauncher m_WinLauncher;
        private Action m_OnConfirm;

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
            LoadUIPackage();
            InitWinLauncher();
            SetNeedUpgrade(false);
            SetDownloading(false);
        }

        /// <summary>
        /// 从 Resources 加载 Launcher FUI 包并创建 UI 组件（引导阶段不依赖 YooAsset）。
        /// </summary>
        private void LoadUIPackage()
        {
            UIPackage.AddPackage("UI/Launcher/Launcher");

            m_WinLauncher = new WinLauncher();
            m_WinLauncher.m_View = UIPackage.CreateObject("Launcher", "WinLauncher").asCom;
            m_WinLauncher.m_View.MakeFullScreen();
            GRoot.inst.AddChild(m_WinLauncher.m_View);
        }

        private void InitWinLauncher()
        {
            m_WinLauncher.InitUIComp();
            m_WinLauncher.btnOk.onClick.Set(OnBtnOkClick);
        }

        /// <summary>
        /// 设置提示文本。
        /// </summary>
        public void SetTip(string text)
        {
            if (m_WinLauncher.txtTips != null) m_WinLauncher.txtTips.text = text;
        }

        /// <summary>
        /// 设置下载进度（0~1）与提示。
        /// </summary>
        public void SetProgress(float value01, string tip)
        {
            SetDownloading(true);
            if (m_WinLauncher.progressBar != null) m_WinLauncher.progressBar.value = value01 * 100f;
            SetTip(tip);
        }

        /// <summary>
        /// 显示更新确认框。
        /// </summary>
        public void ShowUpdateDialog(string content, Action onConfirm)
        {
            SetNeedUpgrade(true);
            m_WinLauncher.btnOk.title     = "更新";
            m_WinLauncher.txtContent.text = content;
            m_WinLauncher.txtContent.onClick.Set(ctx =>
            {
                if (ctx.data != null) Utility.Application.OpenURL(ctx.data.ToString());
            });
            m_OnConfirm = onConfirm;
        }

        /// <summary>
        /// 设置是否显示更新确认框。
        /// </summary>
        public void SetNeedUpgrade(bool need) => m_WinLauncher.IsNeedUpgrade.SetSelectedIndex(need ? 1 : 0);

        /// <summary>
        /// 设置是否处于下载中状态。
        /// </summary>
        public void SetDownloading(bool downloading) => m_WinLauncher.IsDownloading.SetSelectedIndex(downloading ? 1 : 0);

        /// <summary>
        /// 关闭并销毁加载界面。
        /// </summary>
        public void Close()
        {
            if (m_WinLauncher?.m_View == null) return;
            GRoot.inst.RemoveChild(m_WinLauncher.m_View, true);
            m_WinLauncher = null;
        }

        private void OnBtnOkClick(EventContext ctx) => m_OnConfirm?.Invoke();
    }
}
