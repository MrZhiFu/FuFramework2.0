using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 资源包信息
    /// </summary>
    [Serializable]
    public sealed class AssetPackageInfo
    {
        [SerializeField] private string m_PackageName;         // 包名
        [SerializeField] private bool   m_IsDefaultPackage;    // 是否为默认包
        [SerializeField] private string m_DownloadURL;         // 下载地址
        [SerializeField] private string m_FallbackDownloadURL; // 备用下载地址

        /// <summary>
        /// 包名
        /// </summary>
        public string PackageName
        {
            get => m_PackageName;
            set => m_PackageName = value;
        }

        /// <summary>
        /// 是否为默认包
        /// </summary>
        public bool IsDefaultPackage
        {
            get => m_IsDefaultPackage;
            set => m_IsDefaultPackage = value;
        }

        /// <summary>
        /// 下载地址
        /// </summary>
        public string DownloadURL
        {
            get => m_DownloadURL;
            set => m_DownloadURL = value;
        }

        /// <summary>
        /// 备用下载地址
        /// </summary>
        public string FallbackDownloadURL
        {
            get => m_FallbackDownloadURL;
            set => m_FallbackDownloadURL = value;
        }

        public AssetPackageInfo(string packageName = "New Package")
        {
            m_PackageName         = packageName;
            m_IsDefaultPackage    = false;
            m_DownloadURL         = "https://example.com/download";
            m_FallbackDownloadURL = "https://example.com/download";
            ;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            m_PackageName         = "New Package";
            m_IsDefaultPackage    = false;
            m_DownloadURL         = "https://example.com/download";
            m_FallbackDownloadURL = "https://example.com/download";
        }
    }
}