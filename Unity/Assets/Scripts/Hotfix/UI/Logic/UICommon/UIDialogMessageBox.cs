using System;
using UIManager = FuFramework.UI.Runtime.UIManager;

namespace Hotfix.UI
{
    public partial class UIDialogMessageBox
    {
        protected override void OnOpen()
        {
            base.OnOpen();

            var data = (UIDialogMessageBoxData)UserData;
            if (data == null) return;

            m_content.text = data.Message;

            m_enter_button.onClick.Set(() =>
            {
                UIManager.Instance.CloseUI<UIDialogMessageBox>();
                data.OnEnter?.Invoke();
            });

            m_cancel_button.onClick.Set(() =>
            {
                UIManager.Instance.CloseUI<UIDialogMessageBox>();
                data.OnCancel?.Invoke();
            });
        }

        /// <summary>
        /// 对话框弹窗数据
        /// </summary>
        public sealed class UIDialogMessageBoxData
        {
            public string Message  { get; }
            public Action OnEnter  { get; }
            public Action OnCancel { get; }

            public UIDialogMessageBoxData(string message, Action onEnter, Action onCancel = null)
            {
                Message  = message;
                OnEnter  = onEnter;
                OnCancel = onCancel;
            }
        }
    }
}