namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUI(int serialId) => m_UIManager.CloseUI(serialId);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(int serialId, object userData) => m_UIManager.CloseUI(serialId, userData);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUI(IUIForm uiForm) => m_UIManager.CloseUI(uiForm);

        /// <summary>
        /// 关闭界面。
        /// 该函数只适用于界面只有一个的情况.因为当找到一个目标对象之后就会立即终止
        /// </summary>
        /// <typeparam name="T">关闭界面的类型</typeparam>
        public void CloseUI<T>(object userData = null) where T : IUIForm => m_UIManager.CloseUI<T>(userData);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(IUIForm uiForm, object userData) => m_UIManager.CloseUI(uiForm, userData);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUINow(int serialId) => m_UIManager.CloseUINow(serialId);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(int serialId, object userData) => m_UIManager.CloseUINow(serialId, userData);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUINow(IUIForm uiForm) => m_UIManager.CloseUINow(uiForm);

        /// <summary>
        /// 立即关闭界面。
        /// 该函数只适用于界面只有一个的情况.因为当找到一个目标对象之后就会立即终止
        /// </summary>
        /// <typeparam name="T">关闭界面的类型</typeparam>
        public void CloseUINow<T>(object userData = null) where T : IUIForm => m_UIManager.CloseUINow<T>(userData);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(IUIForm uiForm, object userData) => m_UIManager.CloseUINow(uiForm, userData);

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIs() => m_UIManager.CloseAllLoadedUIs();

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIs(object userData) => m_UIManager.CloseAllLoadedUIs(userData);

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIForms() => m_UIManager.CloseAllLoadingUIs();
    }
}