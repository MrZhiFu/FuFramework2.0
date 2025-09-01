using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 加载全局配置失败事件。
    /// </summary>
    public sealed class LoadConfigFailureEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载全局配置失败事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载全局配置失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadConfigFailureEventArgs).FullName;

        /// <summary>
        /// 获取全局配置资源名称。
        /// </summary>
        public string ConfigAssetName { get; private set; }

        /// <summary>
        /// 获取错误信息。
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建加载全局配置失败事件。
        /// </summary>
        /// <param name="dataAssetName"></param>
        /// <param name="errorMessage"></param>
        /// <param name="userData"></param>
        /// <returns>创建的加载全局配置失败事件。</returns>
        public static LoadConfigFailureEventArgs Create(string dataAssetName, string errorMessage, object userData)
        {
            var loadConfigFailureEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<LoadConfigFailureEventArgs>();
            loadConfigFailureEventArgs.ConfigAssetName = dataAssetName;
            loadConfigFailureEventArgs.ErrorMessage    = errorMessage;
            loadConfigFailureEventArgs.UserData        = userData;
            return loadConfigFailureEventArgs;
        }

        /// <summary>
        /// 清理加载全局配置失败事件。
        /// </summary>
        public override void Clear()
        {
            ConfigAssetName = null;
            ErrorMessage    = null;
            UserData        = null;
        }
    }
}