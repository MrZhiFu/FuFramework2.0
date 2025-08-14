using GameFrameX.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源版本号更新失败
    /// </summary>
    public sealed class AssetStaticVersionUpdateFailedEventArgs : GameEventArgs
    {
        public static readonly string EventId = typeof(AssetStaticVersionUpdateFailedEventArgs).FullName;
        public override string Id => EventId;

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
            Error = null;
        }

        /// <summary>
        /// 创建资源版本号更新失败
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static AssetStaticVersionUpdateFailedEventArgs Create(string packageName, string error)
        {
            var assetStaticVersionUpdateFailed = ReferencePool.Acquire<AssetStaticVersionUpdateFailedEventArgs>();
            assetStaticVersionUpdateFailed.PackageName = packageName;
            assetStaticVersionUpdateFailed.Error = error;
            return assetStaticVersionUpdateFailed;
        }
    }
}