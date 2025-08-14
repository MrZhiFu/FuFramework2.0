using GameFrameX.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 加载全局配置更新事件。
    /// </summary>
    public sealed class LoadConfigUpdateEventArgs : GameEventArgs
    {
        /// <summary>
        /// 加载全局配置失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadConfigUpdateEventArgs).FullName;

        /// <summary>
        /// 获取加载全局配置失败事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 获取全局配置资源名称。
        /// </summary>
        public string ConfigAssetName { get; private set; }

        /// <summary>
        /// 获取加载全局配置进度。
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化加载全局配置更新事件的新实例。
        /// </summary>
        public LoadConfigUpdateEventArgs()
        {
            ConfigAssetName = null;
            Progress        = 0f;
            UserData        = null;
        }

        /// <summary>
        /// 创建加载全局配置更新事件。
        /// </summary>
        /// <param name="dataAssetName"></param>
        /// <param name="progress"></param>
        /// <param name="userData"></param>
        /// <returns>创建的加载全局配置更新事件。</returns>
        public static LoadConfigUpdateEventArgs Create(string dataAssetName, float progress, object userData)
        {
            var loadConfigUpdateEventArgs = ReferencePool.Acquire<LoadConfigUpdateEventArgs>();
            loadConfigUpdateEventArgs.ConfigAssetName = dataAssetName;
            loadConfigUpdateEventArgs.Progress        = progress;
            loadConfigUpdateEventArgs.UserData        = userData;
            return loadConfigUpdateEventArgs;
        }

        /// <summary>
        /// 清理加载全局配置更新事件。
        /// </summary>
        public override void Clear()
        {
            ConfigAssetName = null;
            Progress        = 0f;
            UserData        = null;
        }
    }
}