// ReSharper disable once CheckNamespace

namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源更新状态
    /// </summary>
    public enum EUpdateStates
    {
        /// <summary>
        /// 获取资源版本
        /// </summary>
        GetVersion,

        /// <summary>
        /// 更新资源清单
        /// </summary>
        UpdateManifest,

        /// <summary>
        /// 创建资源下载器
        /// </summary>
        CreateDownloader,

        /// <summary>
        /// 下载远端资源文件
        /// </summary>
        Download,

        /// <summary>
        /// 更新资源完毕
        /// </summary>
        UpdateDone,
    }
}