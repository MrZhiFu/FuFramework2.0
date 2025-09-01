using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 卸载场景失败事件。
    /// </summary>
    public sealed class UnloadSceneFailureEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载场景失败事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载场景失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(UnloadSceneFailureEventArgs).FullName;

        /// <summary>
        /// 获取场景资源名称。
        /// </summary>
        public string SceneName { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建卸载场景失败事件。
        /// </summary>
        /// <param name="sceneName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的卸载场景失败事件。</returns>
        public static UnloadSceneFailureEventArgs Create(string sceneName, object userData)
        {
            var unloadSceneFailureEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<UnloadSceneFailureEventArgs>();
            unloadSceneFailureEventArgs.SceneName = sceneName;
            unloadSceneFailureEventArgs.UserData  = userData;
            return unloadSceneFailureEventArgs;
        }

        /// <summary>
        /// 清理卸载场景失败事件。
        /// </summary>
        public override void Clear()
        {
            SceneName = null;
            UserData  = null;
        }
    }
}