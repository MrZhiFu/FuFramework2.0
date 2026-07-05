using System;
using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.UI.Runtime;
using Launcher.Procedure;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.UI
{
    public partial class WinLauncher : ViewBase
    {
        #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override UILayer Layer         => UILayer.Normal;   // 界面所属的层级。
         protected override UITweenType TweenType => UITweenType.Fade; // 界面打开/关闭时的动画效果。
         protected override bool AdjustNotch      => false;            // 是否适配刘海/打孔区域（全屏覆盖）。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
        //@formatter:on

        #endregion

        /// <summary>
        /// 确认按钮点击事件回调
        /// </summary>
        private EventCallback0 _btnOkClick;

        /// <summary>
        /// 初始化
        /// </summary>  
        protected override void OnInit()
        {
            InitUIComp();
            InitUIEvent();
            InitEvent();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            Subscribe(AssetDownloadProgressEventArgs.EventId, OnAssetDownloadProgressUpdate);
        }

        /// <summary>
        /// 界面打开
        /// </summary>
        protected override void OnOpen() { }

        /// <summary>
        /// 界面关闭
        /// </summary>
        protected override void OnClose() { }

        /// <summary>
        /// 界面销毁
        /// </summary>
        protected override void OnDispose() { }

        /// <summary>
        /// 设置下载时的提示文本
        /// </summary>
        /// <param name="text"></param>
        public void SetTipText(string text) => txtTips.text = text;

        /// <summary>
        /// 设置是否显示更新提示框
        /// </summary>
        /// <param name="needUpgrade">是否完成更新</param>
        public void SetUpdateSureUIState(bool needUpgrade) => SetController(needUpgrade ? EIsNeedUpgrade.Yes : EIsNeedUpgrade.No);

        /// <summary>
        /// 设置是否显示更新进度
        /// </summary>
        /// <param name="isFinish">是否完成更新</param>
        public void SetUpdateProgressState(bool isFinish) => SetController(isFinish ? EIsDownloading.No : EIsDownloading.Yes);

        /// <summary>
        /// 显示更新提示框
        /// </summary>
        /// <param name="content">更新内容</param>
        /// <param name="updateBtnClickCallback">更新按钮点击事件</param>
        public void ShowUpdateDialog(string content, Action updateBtnClickCallback)
        {
            // 打开更新提示框
            SetUpdateSureUIState(true);

            // 设置更新提示框
            var isChinese = GlobalModule.LocalizationModule.Language == ELanguage.ChineseSimplified ||
                            GlobalModule.LocalizationModule.Language == ELanguage.ChineseTraditional;

            btnOk.title     = isChinese ? "更新" : "Update";
            txtContent.text = content;

            // 设置更新内容文本点击回调
            txtContent.onClick.Set(context =>
            {
                if (context.data != null)
                    Utility.Application.OpenURL(context.data.ToString());
            });

            // 设置更新按钮点击回调
            _btnOkClick = () => { updateBtnClickCallback?.Invoke(); };
        }

        /// <summary>
        /// 资源下载进度更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        private void OnAssetDownloadProgressUpdate(object sender, GameEventArgs gameEventArgs)
        {
            SetUpdateProgressState(false);
            var message       = (AssetDownloadProgressEventArgs)gameEventArgs;
            var progress      = message.CurrentDownloadSizeBytes / (message.TotalDownloadSizeBytes * 1f);
            var currentSizeMb = Utility.File.GetBytesSizeWithUnit(message.CurrentDownloadSizeBytes);
            var totalSizeMb   = Utility.File.GetBytesSizeWithUnit(message.TotalDownloadSizeBytes);
            progressBar.value = progress * 100;
            SetTipText($"Downloading：{currentSizeMb}/{totalSizeMb}");
        }

        #region 交互事件与ListItem渲染回调处理

        private void OnBtnOkClick(EventContext ctx)
        {
            _btnOkClick?.Invoke();
        }

        #endregion
    }
}