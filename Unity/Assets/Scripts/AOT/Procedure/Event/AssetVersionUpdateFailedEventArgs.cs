using FuFramework.Event.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace Launcher.Procedure
{
    /// <summary>
    /// 资源版本号更新失败事件
    /// </summary>
    public sealed class AssetVersionUpdateFailedEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(AssetVersionUpdateFailedEventArgs).FullName;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; private set; }

        public override void Clear()
        {
            PackageName = null;
            Error       = null;
        }

        /// <summary>
        /// 创建资源版本号更新失败
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static AssetVersionUpdateFailedEventArgs Create(string packageName, string error)
        {
            var assetStaticVersionUpdateFailed = ReferencePool.Acquire<AssetVersionUpdateFailedEventArgs>();
            assetStaticVersionUpdateFailed.PackageName = packageName;
            assetStaticVersionUpdateFailed.Error       = error;
            return assetStaticVersionUpdateFailed;
        }
    }
}