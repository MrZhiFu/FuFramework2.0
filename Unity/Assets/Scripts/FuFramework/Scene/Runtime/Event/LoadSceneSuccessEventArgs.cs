using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 加载场景成功事件。
    /// </summary>
    public sealed class LoadSceneSuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载场景成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载场景成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadSceneSuccessEventArgs).FullName;

        /// <summary>
        /// 获取场景资源名称。
        /// </summary>
        public string SceneName { get; private set; }


        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建加载场景成功事件。
        /// </summary>
        /// <param name="sceneName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的加载场景成功事件。</returns>
        public static LoadSceneSuccessEventArgs Create(string sceneName, object userData)
        {
            var loadSceneSuccessEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<LoadSceneSuccessEventArgs>();
            loadSceneSuccessEventArgs.SceneName = sceneName;
            loadSceneSuccessEventArgs.UserData  = userData;
            return loadSceneSuccessEventArgs;
        }

        /// <summary>
        /// 清理加载场景成功事件。
        /// </summary>
        public override void Clear()
        {
            SceneName = null;
            UserData  = null;
        }
    }
}