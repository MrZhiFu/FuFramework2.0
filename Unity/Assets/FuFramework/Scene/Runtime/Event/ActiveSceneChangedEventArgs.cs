using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 激活场景被改变事件。
    /// </summary>
    public sealed class ActiveSceneChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取激活场景被改变事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 激活场景被改变事件编号。
        /// </summary>
        public static readonly string EventId = typeof(ActiveSceneChangedEventArgs).FullName;

        /// <summary>
        /// 获取上一个被激活的场景。
        /// </summary>
        public UnityEngine.SceneManagement.Scene LastActiveScene { get; private set; }

        /// <summary>
        /// 获取被激活的场景。
        /// </summary>
        public UnityEngine.SceneManagement.Scene ActiveScene { get; private set; }

        /// <summary>
        /// 创建激活场景被改变事件。
        /// </summary>
        /// <param name="lastActiveScene">上一个被激活的场景。</param>
        /// <param name="activeScene">被激活的场景。</param>
        /// <returns>创建的激活场景被改变事件。</returns>
        public static ActiveSceneChangedEventArgs Create(UnityEngine.SceneManagement.Scene lastActiveScene, UnityEngine.SceneManagement.Scene activeScene)
        {
            var activeSceneChangedEventArgs = ReferencePool.Acquire<ActiveSceneChangedEventArgs>();
            activeSceneChangedEventArgs.LastActiveScene = lastActiveScene;
            activeSceneChangedEventArgs.ActiveScene     = activeScene;
            return activeSceneChangedEventArgs;
        }

        /// <summary>
        /// 清理激活场景被改变事件。
        /// </summary>
        public override void Clear()
        {
            LastActiveScene = default;
            ActiveScene     = default;
        }
    }
}