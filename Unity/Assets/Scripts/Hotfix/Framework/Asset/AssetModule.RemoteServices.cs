using System.Collections.Generic;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 远程服务接口
    /// </summary>
    public partial class AssetModule
    {
        /// <summary>
        /// 远端资源服务器定义，用于提供远端资源的下载地址
        /// </summary>
        private class RemoteServices : IRemoteService
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
            /// 获取指定文件的所有远端候选地址，按优先级排序。
            /// </summary>
            /// <param name="fileName">资源文件名。</param>
            /// <returns>按优先级排序的远端候选地址列表。</returns>
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                var urls = new List<string>(2);
                if (!string.IsNullOrEmpty(HostServer))
                    urls.Add(HostServer + fileName);
                if (!string.IsNullOrEmpty(FallbackHostServer))
                    urls.Add(FallbackHostServer + fileName);
                return urls;
            }
        }
    }
}
