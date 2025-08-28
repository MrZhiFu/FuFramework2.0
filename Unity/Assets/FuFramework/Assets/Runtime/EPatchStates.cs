// ReSharper disable once CheckNamespace

namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 补丁系统更新状态
    /// </summary>
    public enum EPatchStates
    {
        /// <summary>
        /// 更新资源版本
        /// </summary>
        UpdateVersion,

        /// <summary>
        /// 更新补丁清单
        /// </summary>
        UpdateManifest,

        /// <summary>
        /// 创建下载器
        /// </summary>
        CreateDownloader,

        /// <summary>
        /// 下载远端文件
        /// </summary>
        Download,

        /// <summary>
        /// 更新流程完毕
        /// </summary>
        UpdateDone,
    }
}