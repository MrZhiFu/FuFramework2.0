using System.Threading.Tasks;
using System;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 异步打开全屏界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <typeparam name="T">UI的具体类型。</typeparam>
        /// <param name="userData">传递给UI的用户数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>返回打开的UI实例。</returns>
        public async Task<T> OpenFullScreenAsync<T>(string uiAssetPath, object userData = null, bool isMultiple = false) where T : class, IUIBase
        {
            return await OpenUIAsync<T>(uiAssetPath, true, userData, true, isMultiple);
        }

        /// <summary>
        /// 异步打开全屏界面。
        /// </summary>
        /// <typeparam name="T">UI的具体类型。</typeparam>
        /// <param name="userData">传递给UI的用户数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>返回打开的UI实例。</returns>
        public async Task<T> OpenFullScreenAsync<T>(object userData = null, bool isMultiple = false) where T : class, IUIBase
        {
            var uiAssetName = typeof(T).Name;
            var uiAssetPath = Utility.Asset.Path.GetUIPath(uiAssetName);
            return await OpenFullScreenAsync<T>(uiAssetPath, userData, isMultiple);
        }

        /// <summary>
        /// 异步打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="uiType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<IUIBase> OpenUIAsync(string uiAssetPath, Type uiType, bool pauseCoveredUI, object userData = null, bool isFullScreen = false, bool isMultiple = false)
        {
            return await m_UIManager.OpenUIAsync(uiAssetPath, uiType, pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 异步打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        private async Task<T> OpenUIAsync<T>(string uiAssetPath, bool pauseCoveredUI, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIBase
        {
            var ui = await m_UIManager.OpenUIAsync<T>(uiAssetPath, pauseCoveredUI, userData, isFullScreen, isMultiple);
            return ui as T;
        }

        /// <summary>
        /// 异步打开界面。
        /// </summary>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenUIAsync<T>(bool pauseCoveredUI, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIBase
        {
            var uiAssetName = typeof(T).Name;
            var uiAssetPath = Utility.Asset.Path.GetUIPath(uiAssetName);
            return await OpenUIAsync<T>(uiAssetPath, pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 异步打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenAsync<T>(string uiAssetPath, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIBase
        {
            return await OpenUIAsync<T>(uiAssetPath, false, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUI">是否暂停覆盖的UI</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenAsync<T>(string uiAssetPath, bool pauseCoveredUI, object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIBase
        {
            return await OpenUIAsync<T>(uiAssetPath, pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<T> OpenAsync<T>(object userData = null, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIBase
        {
            var uiAssetName = typeof(T).Name;
            var uiAssetPath = Utility.Asset.Path.GetUIPath(uiAssetName);
            return await OpenAsync<T>(uiAssetPath, userData, isFullScreen, isMultiple);
        }
    }
}