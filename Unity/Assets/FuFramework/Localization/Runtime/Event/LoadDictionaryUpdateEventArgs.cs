using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Runtime
{
    /// <summary>
    /// 加载字典更新事件。
    /// </summary>
    public sealed class LoadDictionaryUpdateEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取加载字典更新事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 加载字典更新事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LoadDictionaryUpdateEventArgs).FullName;

        /// <summary>
        /// 获取字典资源名称。
        /// </summary>
        public string DictionaryAssetName { get; private set; }

        /// <summary>
        /// 获取加载字典进度。
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化加载字典更新事件的新实例。
        /// </summary>
        public LoadDictionaryUpdateEventArgs()
        {
            DictionaryAssetName = null;
            Progress = 0f;
            UserData = null;
        }

        /// <summary>
        /// 创建加载字典更新事件。
        /// </summary>
        /// <param name="dataAssetName">字典资源名称。</param>
        /// <param name="progress">加载字典进度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的加载字典更新事件。</returns>
        public static LoadDictionaryUpdateEventArgs Create(string dataAssetName, float progress, object userData)
        {
            var loadDictionaryUpdateEventArgs = ReferencePool.Acquire<LoadDictionaryUpdateEventArgs>();
            loadDictionaryUpdateEventArgs.DictionaryAssetName = dataAssetName;
            loadDictionaryUpdateEventArgs.Progress = progress;
            loadDictionaryUpdateEventArgs.UserData = userData;
            return loadDictionaryUpdateEventArgs;
        }

        /// <summary>
        /// 清理加载字典更新事件。
        /// </summary>
        public override void Clear()
        {
            DictionaryAssetName = null;
            Progress = 0f;
            UserData = null;
        }
    }
}