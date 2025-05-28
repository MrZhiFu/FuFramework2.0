using System;

namespace Hotfix.UI
{
    public partial class UIDialogMessageBox
    {
        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            var data = (UIDialogMessageBoxData)userData;
            if (data == null) return;

            m_content.text = data.Message;

            m_enter_button.onClick.Set(() =>
            {
                GameApp.UI.CloseUIForm<UIDialogMessageBox>();
                data.OnEnter?.Invoke();
            });

            m_cancel_button.onClick.Set(() =>
            {
                GameApp.UI.CloseUIForm<UIDialogMessageBox>();
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