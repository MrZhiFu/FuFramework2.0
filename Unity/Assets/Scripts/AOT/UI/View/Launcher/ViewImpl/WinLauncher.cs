using FairyGUI;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace AOT.UI.View.Launcher
{
    public partial class WinLauncher : ViewBase
    {
        #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override UILayer Layer         => UILayer.Normal;   // 界面所属的层级。
         protected override UITweenType TweenType => UITweenType.Fade; // 界面打开/关闭时的动画效果。
         protected override bool IsFullScreen     => true;             // 是否是全屏界面。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
        //@formatter:on

        #endregion

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
            // Example:Subscribe(XxxEventArgs.EventId, XxxEventArgs.Create(xxx));
        }

        /// <summary>
        /// 界面打开
        /// </summary>
        protected override void OnOpen()
        {
            Refresh();
        }

        /// <summary>
        /// 界面关闭
        /// </summary>
        protected override void OnClose() { }

        /// <summary>
        /// 界面销毁
        /// </summary>
        protected override void OnDispose() { }

        /// <summary>
        /// 刷新界面
        /// </summary>
        private void Refresh()
        {
            // TODO：刷新逻辑
        }

        /// <summary>
        /// 设置下载时的提示文本
        /// </summary>
        /// <param name="text"></param>
        public void SetTipText(string text) => txtTips.text = text;

        /// <summary>
        /// 设置更新完成状态
        /// </summary>
        /// <param name="isFinish">是否完成更新</param>
        public void SetUpdateState(bool isFinish) => SetController(isFinish ? EIsDownloading.No : EIsDownloading.Yes);

        /// <summary>
        /// 设置更新确认弹框状态
        /// </summary>
        /// <param name="isNeedUpgrade">是否需要更新</param>
        public void SetUpdateSureUIState(bool isNeedUpgrade) => SetController(isNeedUpgrade ? EIsNeedUpgrade.Yes : EIsNeedUpgrade.No);

        /// <summary>
        /// 设置更新进度
        /// </summary>
        /// <param name="progress"></param>
        public void SetUpdateProgress(float progress) => progressBar.value = progress;

        /// <summary>
        /// 设置更新按钮文本
        /// </summary>
        /// <param name="text"></param>
        public void SetUpdateBtnTitle(string text) => btnOk.title = text;

        /// <summary>
        /// 设置更新提示文本
        /// </summary>
        /// <param name="text"></param>
        public void SetUpdateTipText(string text) => txtContent.text = text;

        /// <summary>
        /// 设置更新提示文本点击事件
        /// </summary>
        /// <param name="onClick">点击事件</param>
        public void SetUpdateTipTextOnClick(EventCallback1 onClick) => txtContent.onClick.Set(onClick);

        /// <summary>
        /// 设置更新按钮点击事件
        /// </summary>
        /// <param name="onClick">点击事件</param>
        public void SetUpdateBtnOnClick(EventCallback0 onClick) => btnOk.onClick.Set(onClick);

        #region 交互事件与ListItem渲染回调处理

        private void OnBtnOkClick(EventContext ctx)
        {
            // todo
        }

        #endregion
    }
}