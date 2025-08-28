using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 加载场景更新事件。
    /// </summary>
    public sealed class LoadSceneUpdateEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载场景更新事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载场景更新事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadSceneUpdateEventArgs).FullName;

        /// <summary>
        /// 获取场景资源名称。
        /// </summary>
        public string SceneAssetName { get; private set; }

        /// <summary>
        /// 获取加载场景进度。
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建加载场景更新事件。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="progress">加载场景进度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的加载场景更新事件。</returns>
        public static LoadSceneUpdateEventArgs Create(string sceneAssetName, float progress, object userData)
        {
            var loadSceneUpdateEventArgs = ReferencePool.Acquire<LoadSceneUpdateEventArgs>();
            loadSceneUpdateEventArgs.SceneAssetName = sceneAssetName;
            loadSceneUpdateEventArgs.Progress = progress;
            loadSceneUpdateEventArgs.UserData = userData;
            return loadSceneUpdateEventArgs;
        }

        /// <summary>
        /// 清理加载场景更新事件。
        /// </summary>
        public override void Clear()
        {
            SceneAssetName = null;
            Progress = 0f;
            UserData = null;
        }
    }
}