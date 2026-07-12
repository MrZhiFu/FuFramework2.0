using System;
using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace Launcher
{
    /// <summary>
    /// AOT 启动加载界面。
    /// 功能：显示进度、提示文本、更新确认框，脱离 UIModule/EventModule 自包含运行。
    /// UI 组件绑定部分见 WinLauncher.Gen.cs。
    /// </summary>
    public sealed partial class WinLauncher : IWinLauncher
    {
        private Action m_OnConfirm;

        /// <summary>
        /// 创建并显示加载界面。
        /// </summary>
        public static UniTask<WinLauncher> CreateAsync()
        {
            var view = new WinLauncher();
            view.Init();
            return UniTask.FromResult(view);
        }

        private void Init()
        {
            LoadUIPackage();
            InitUIComp();
            InitUIEvent();
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
