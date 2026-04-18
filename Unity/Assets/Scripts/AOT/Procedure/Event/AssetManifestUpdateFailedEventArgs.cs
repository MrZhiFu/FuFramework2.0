using FuFramework.Event.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace Launcher.Procedure
{
    /// <summary>
    /// 资源清单更新失败事件
    /// </summary>
    public sealed class AssetManifestUpdateFailedEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(AssetManifestUpdateFailedEventArgs).FullName;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; private set; }

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
        /// 创建补丁清单更新失败
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="error">错误信息</param>
        /// <returns></returns>
        public static AssetManifestUpdateFailedEventArgs Create(string packageName, string error)
        {
            var assetPatchManifestUpdateFailed = ReferencePool.Acquire<AssetManifestUpdateFailedEventArgs>();
            assetPatchManifestUpdateFailed.PackageName = packageName;
            assetPatchManifestUpdateFailed.Error       = error;
            return assetPatchManifestUpdateFailed;
        }
    }
}