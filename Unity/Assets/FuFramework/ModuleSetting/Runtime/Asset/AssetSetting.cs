using UnityEngine;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 资源管理模块配置
    /// </summary>
    public class AssetSetting : ScriptableObject
    {
        /// <summary>
        /// 资源运行模式
        /// </summary>
        [SerializeField] private EPlayMode m_PlayMode = EPlayMode.EditorSimulateMode; // 资源运行模式

        /// <summary>
        /// 默认资源包名称
        /// </summary>
        [SerializeField] private string m_DefaultPackageName = "DefaultPackage";

        /// <summary>
        /// 资源下载最大并发数量
        /// </summary>
        [SerializeField] private int m_DownloadingMaxNum = 10;

        /// <summary>
        /// 资源下载失败重试次数
        /// </summary>
        [SerializeField] private int m_FailedTryAgainNum = 3;

        /// <summary>
        /// YooAsset异步系统参数-每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        [SerializeField] private int m_AsyncSystemMaxSlicePerFrame = 30;

        /// <summary>
        /// 资源服务器URL根地址
        /// </summary>
        [SerializeField] private string m_ResCdnRootURL = "http://localhost:8080/CDN/";

        /// <summary>
        /// 获取资源运行模式
        /// </summary>
        public EPlayMode PlayMode => m_PlayMode;

        /// <summary>
        /// 获取默认资源包名称
        /// </summary>
        public string DefaultPackageName => m_DefaultPackageName;

        /// <summary>
        /// 获取资源下载最大并发数量
        /// </summary>
        public int DownloadingMaxNum => m_DownloadingMaxNum;

        /// <summary>
        /// 获取资源下载失败重试次数
        /// </summary>
        public int FailedTryAgainNum => m_FailedTryAgainNum;

        /// <summary>
        /// 获取YooAsset异步系统参数-每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        public int AsyncSystemMaxSlicePerFrame => m_AsyncSystemMaxSlicePerFrame;

        /// <summary>
        /// 获取资源服务器URL根地址
        /// </summary>
        public string ResCdnRootRootURL => m_ResCdnRootURL;

        /// <summary>
        /// 重置配置
        /// </summary>
        public void Reset()
        {
            m_PlayMode                    = EPlayMode.EditorSimulateMode;
            m_DefaultPackageName          = "DefaultPackage";
            m_DownloadingMaxNum           = 10;
            m_FailedTryAgainNum           = 3;
            m_AsyncSystemMaxSlicePerFrame = 30;
        }
    }
}