using System.Threading.Tasks;
using System;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 异步打开全屏UI。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <typeparam name="T">UI的具体类型。</typeparam>
        /// <param name="userData">传递给UI的用户数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>返回打开的UI实例。</returns>
        public async Task<T> OpenFullScreenAsync<T>(string uiFormAssetPath, object userData = null, bool isMultiple = false) where T : class, IUIForm
        {
            return await OpenUIFormAsync<T>(uiFormAssetPath, true, userData, true, isMultiple);
        }

        /// <summary>
        /// 异步打开全屏UI。
        /// </summary>
        /// <typeparam name="T">UI的具体类型。</typeparam>
        /// <param name="userData">传递给UI的用户数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>返回打开的UI实例。</returns>
        public async Task<T> OpenFullScreenAsync<T>(object userData = null, bool isMultiple = false) where T : class, IUIForm
        {
            var uiFormAssetName = typeof(T).Name;
            var uiFormAssetPath = Utility.Asset.Path.GetUIPath(uiFormAssetName);
            return await OpenFullScreenAsync<T>(uiFormAssetPath, userData, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="uiFormType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<IUIForm> OpenUIAsync(string uiFormAssetPath, Type uiFormType, bool pauseCoveredUIForm, object userData = null, bool isFullScreen = false, bool isMultiple = false)
        {
            return await m_UIManager.OpenUIFormAsync(uiFormAssetPath, uiFormType, pauseCoveredUIForm, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        private async Task<T> OpenUIFormAsync<T>(string uiFormAssetPath, bool pauseCoveredUIForm, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIForm
        {
            var ui = await m_UIManager.OpenUIFormAsync<T>(uiFormAssetPath, pauseCoveredUIForm, userData, isFullScreen, isMultiple);
            return ui as T;
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenUIFormAsync<T>(bool pauseCoveredUIForm, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIForm
        {
            var uiFormAssetName = typeof(T).Name;
            var uiFormAssetPath = Utility.Asset.Path.GetUIPath(uiFormAssetName);
            return await OpenUIFormAsync<T>(uiFormAssetPath, pauseCoveredUIForm, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenAsync<T>(string uiFormAssetPath, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIForm
        {
            return await OpenUIFormAsync<T>(uiFormAssetPath, false, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUIForm">是否暂停覆盖的UI</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenAsync<T>(string uiFormAssetPath, bool pauseCoveredUIForm, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIForm
        {
            return await OpenUIFormAsync<T>(uiFormAssetPath, pauseCoveredUIForm, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenAsync<T>(object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIForm
        {
            var uiFormAssetName = typeof(T).Name;
            var uiFormAssetPath = Utility.Asset.Path.GetUIPath(uiFormAssetName);
            return await OpenAsync<T>(uiFormAssetPath, userData, isFullScreen, isMultiple);
        }
    }
}