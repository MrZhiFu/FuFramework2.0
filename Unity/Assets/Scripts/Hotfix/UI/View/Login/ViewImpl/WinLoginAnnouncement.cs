using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Login
{
    public partial class WinLoginAnnouncement : ViewBase
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

        #region 交互事件与ListItem渲染回调处理
        
        #endregion
    }
}