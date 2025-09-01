using YooAsset;
using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 加载场景失败事件。
    /// </summary>
    public sealed class LoadSceneFailureEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载场景失败事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载场景失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadSceneFailureEventArgs).FullName;

        /// <summary>
        /// 场景名称。
        /// </summary>
        public string SceneName { get; private set; }

        /// <summary>
        /// 错误信息。
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 加载场景状态
        /// </summary>
        public EOperationStatus Status { get; private set; }

        /// <summary>
        /// 创建加载场景失败事件。
        /// </summary>
        /// <param name="sceneName">场景名称。</param>
        /// <param name="status">加载场景状态。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的加载场景失败事件。</returns>
        public static LoadSceneFailureEventArgs Create(string sceneName, EOperationStatus status, string errorMessage, object userData)
        {
            var loadSceneFailureEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<LoadSceneFailureEventArgs>();
            loadSceneFailureEventArgs.SceneName    = sceneName;
            loadSceneFailureEventArgs.ErrorMessage = errorMessage;
            loadSceneFailureEventArgs.UserData     = userData;
            loadSceneFailureEventArgs.Status       = status;
            return loadSceneFailureEventArgs;
        }

        /// <summary>
        /// 清理加载场景失败事件。
        /// </summary>
        public override void Clear()
        {
            SceneName    = null;
            ErrorMessage = null;
            UserData     = null;
            Status       = EOperationStatus.None;
        }
    }
}