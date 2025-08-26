using YooAsset;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 远程服务接口
    /// </summary>
    public partial class AssetManager
    {
        /// <summary>
        /// 远端资源服务器定义，用于提供远端资源的下载地址
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            /// <summary>
            /// 远端资源服务器地址
            /// </summary>
            private string HostServer { get; }

            /// <summary>
            /// 远端资源服务器备用地址
            /// </summary>
            private string FallbackHostServer { get; }

            public RemoteServices(string hostServer, string fallbackHostServer)
            {
                HostServer         = hostServer;
                FallbackHostServer = fallbackHostServer;
            }

            /// <summary>
            /// 获取远端资源的下载地址
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            public string GetRemoteMainURL(string fileName) => HostServer + fileName;

            /// <summary>
            /// 获取远端资源的备用下载地址
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            public string GetRemoteFallbackURL(string fileName) => FallbackHostServer + fileName;
        }
    }
}