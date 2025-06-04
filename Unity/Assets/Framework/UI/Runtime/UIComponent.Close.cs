namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUIForm(int serialId)
        {
            m_UIManager.CloseUIForm(serialId);
        }


        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(int serialId, object userData)
        {
            m_UIManager.CloseUIForm(serialId, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUIForm(IUIForm uiForm)
        {
            m_UIManager.CloseUIForm(uiForm);
        }

        /// <summary>
        /// 关闭界面。
        /// 该函数只适用于界面只有一个的情况.因为当找到一个目标对象之后就会立即终止
        /// </summary>
        /// <typeparam name="T">关闭界面的类型</typeparam>
        public void CloseUIForm<T>(object userData = null) where T : IUIForm
        {
            m_UIManager.CloseUIForm<T>(userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(IUIForm uiForm, object userData)
        {
            m_UIManager.CloseUIForm(uiForm, userData);
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUIFormNow(int serialId)
        {
            m_UIManager.CloseUIFormNow(serialId);
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIFormNow(int serialId, object userData)
        {
            m_UIManager.CloseUIFormNow(serialId, userData);
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUIFormNow(IUIForm uiForm)
        {
            m_UIManager.CloseUIFormNow(uiForm);
        }

        /// <summary>
        /// 立即关闭界面。
        /// 该函数只适用于界面只有一个的情况.因为当找到一个目标对象之后就会立即终止
        /// </summary>
        /// <typeparam name="T">关闭界面的类型</typeparam>
        public void CloseUIFormNow<T>(object userData = null) where T : IUIForm
        {
            m_UIManager.CloseUIFormNow<T>(userData);
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIFormNow(IUIForm uiForm, object userData)
        {
            m_UIManager.CloseUIFormNow(uiForm, userData);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIForms()
        {
            m_UIManager.CloseAllLoadedUIForms();
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIForms(object userData)
        {
            m_UIManager.CloseAllLoadedUIForms(userData);
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIForms()
        {
            m_UIManager.CloseAllLoadingUIForms();
        }
    }
}