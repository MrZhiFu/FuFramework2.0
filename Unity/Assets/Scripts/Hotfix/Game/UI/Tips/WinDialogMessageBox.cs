using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;
using System;
using FairyGUI;
using Hotfix.Framework.UI;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class WinDialogMessageBox : ViewBase
    {
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
            InitRedDot();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);
        }

        /// <summary>
        /// 注册界面相关红点
        /// </summary>
        private void InitRedDot()
        {
            // Example: RedDotRegister.RegisterRedDot(this, ERedDotKey.Bag_Item, btnLogin);
        }

        /// <summary>
        /// 界面打开
        /// </summary>
        protected override void OnOpen()
        {
            if (UserData is not DialogMessageBoxData userData) return;
            m_data          = userData;
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
