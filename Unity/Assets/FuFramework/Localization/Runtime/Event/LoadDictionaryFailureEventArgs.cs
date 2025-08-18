using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Runtime
{
    /// <summary>
    /// 加载字典失败事件。
    /// </summary>
    public sealed class LoadDictionaryFailureEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载字典失败事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载字典失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadDictionaryFailureEventArgs).FullName;

        /// <summary>
        /// 获取字典资源名称。
        /// </summary>
        public string DictionaryAssetName { get; private set; }

        /// <summary>
        /// 获取错误信息。
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化加载字典失败事件的新实例。
        /// </summary>
        public LoadDictionaryFailureEventArgs()
        {
            DictionaryAssetName = null;
            ErrorMessage = null;
            UserData = null;
        }

        /// <summary>
        /// 创建加载字典失败事件。
        /// </summary>
        /// <param name="dataAssetName">字典资源名称。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的加载字典失败事件。</returns>
        public static LoadDictionaryFailureEventArgs Create(string dataAssetName, string errorMessage, object userData)
        {
            var loadDictionaryFailureEventArgs = ReferencePool.Acquire<LoadDictionaryFailureEventArgs>();
            loadDictionaryFailureEventArgs.DictionaryAssetName = dataAssetName;
            loadDictionaryFailureEventArgs.ErrorMessage = errorMessage;
            loadDictionaryFailureEventArgs.UserData = userData;
            return loadDictionaryFailureEventArgs;
        }

        /// <summary>
        /// 清理加载字典失败事件。
        /// </summary>
        public override void Clear()
        {
            DictionaryAssetName = null;
            ErrorMessage = null;
            UserData = null;
        }
    }
}