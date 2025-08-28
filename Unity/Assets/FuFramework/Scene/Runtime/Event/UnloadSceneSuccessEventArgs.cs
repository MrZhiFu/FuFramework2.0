using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 卸载场景成功事件。
    /// </summary>
    public sealed class UnloadSceneSuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载场景成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载场景成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(UnloadSceneSuccessEventArgs).FullName;

        /// <summary>
        /// 获取场景资源名称。
        /// </summary>
        public string SceneAssetName { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建卸载场景成功事件。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的卸载场景成功事件。</returns>
        public static UnloadSceneSuccessEventArgs Create(string sceneAssetName, object userData)
        {
            var unloadSceneSuccessEventArgs = ReferencePool.Acquire<UnloadSceneSuccessEventArgs>();
            unloadSceneSuccessEventArgs.SceneAssetName = sceneAssetName;
            unloadSceneSuccessEventArgs.UserData = userData;
            return unloadSceneSuccessEventArgs;
        }

        /// <summary>
        /// 清理卸载场景成功事件。
        /// </summary>
        public override void Clear()
        {
            SceneAssetName = null;
            UserData = null;
        }
    }
}