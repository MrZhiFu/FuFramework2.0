using System;
using FairyGUI;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.PopWin
{
    public partial class WinDialogMessageBox : ViewBase
    {
         #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override UILayer Layer         => UILayer.Window;   // 界面所属的层级。
         protected override UITweenType TweenType => UITweenType.Fade; // 界面打开/关闭时的动画效果。
         protected override bool IsFullScreen     => true;             // 是否是全屏界面。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
         //@formatter:on
         
         #endregion
        
         /// <summary>
         /// 对话框弹窗数据
         /// </summary>
         public sealed class DialogMessageBoxData
         {
	         public string Message  { get; }
	         public Action OnEnter  { get; }
	         public Action OnCancel { get; }

	         public DialogMessageBoxData(string message, Action onEnter, Action onCancel = null)
	         {
		         Message  = message;
		         OnEnter  = onEnter;
		         OnCancel = onCancel;
	         }
         }
         
          private DialogMessageBoxData m_data;
         
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
	        if (UserData is not DialogMessageBoxData userData) return;
	        m_data = userData;
	        txtContent.text = m_data.Message;
	        
            Refresh();
        }

        /// <summary>
        /// 界面关闭
        /// </summary>
        protected override void OnClose()
        {
	        m_data = null;
        }

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
        
		private void OnBtnOkClick(EventContext ctx)
		{
			CloseSelf();
			m_data?.OnEnter?.Invoke();
		}

		private void OnBtnNoClick(EventContext ctx)
		{
			CloseSelf();
			m_data?.OnCancel?.Invoke();
		}

        #endregion
    }
}