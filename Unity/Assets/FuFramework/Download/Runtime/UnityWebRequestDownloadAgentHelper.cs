using System;
using FuFramework.Core.Runtime;
using UnityEngine.Networking;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载证书验证处理器。目前不做任何处理，直接返回true。
    /// </summary>
    public sealed class DownloadCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // return base.ValidateCertificate(certificateData);
            return true;
        }
    }

    /// <summary>
    /// 使用 UnityWebRequest 实现的下载代理辅助器。
    /// </summary>
    public partial class UnityWebRequestDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        /// 缓存目标数据的字节数组的长度(0x1000 = 4096)
        private const int CachedBytesLength = 0x1000;

        /// 缓存目标数据的字节数组
        private readonly byte[] m_CachedBytes = new byte[CachedBytesLength];

        /// 记录是否已销毁
        private bool m_Disposed;

        /// Unity WebRequest
        private UnityWebRequest m_UnityWebRequest;

        private EventHandler<DownloadAgentHelperUpdateBytesEventArgs>  m_DownloadAgentHelperUpdateBytesEventHandler;  // 下载代理辅助器更新数据流事件
        private EventHandler<DownloadAgentHelperUpdateLengthEventArgs> m_DownloadAgentHelperUpdateLengthEventHandler; // 下载代理辅助器更新数据大小事件
        private EventHandler<DownloadAgentHelperCompleteEventArgs>     m_DownloadAgentHelperCompleteEventHandler;     // 下载代理辅助器完成事件
        private EventHandler<DownloadAgentHelperErrorEventArgs>        m_DownloadAgentHelperErrorEventHandler;        // 下载代理辅助器错误事件

        /// <summary>
        /// 下载代理辅助器更新数据流事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperUpdateBytesEventArgs> DownloadAgentHelperUpdateBytes
        {
            add => m_DownloadAgentHelperUpdateBytesEventHandler += value;
            remove => m_DownloadAgentHelperUpdateBytesEventHandler -= value;
        }

        /// <summary>
        /// 下载代理辅助器更新数据大小事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperUpdateLengthEventArgs> DownloadAgentHelperUpdateLength
        {
            add => m_DownloadAgentHelperUpdateLengthEventHandler += value;
            remove => m_DownloadAgentHelperUpdateLengthEventHandler -= value;
        }

        /// <summary>
        /// 下载代理辅助器完成事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperCompleteEventArgs> DownloadAgentHelperComplete
        {
            add => m_DownloadAgentHelperCompleteEventHandler += value;
            remove => m_DownloadAgentHelperCompleteEventHandler -= value;
        }

        /// <summary>
        /// 下载代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperErrorEventArgs> DownloadAgentHelperError
        {
            add => m_DownloadAgentHelperErrorEventHandler += value;
            remove => m_DownloadAgentHelperErrorEventHandler -= value;
        }

        private void Update()
        {
            if (m_UnityWebRequest == null) return;
            if (!m_UnityWebRequest.isDone) return;

            var isError = false;

#if UNITY_2020_2_OR_NEWER
            isError = m_UnityWebRequest.result != UnityWebRequest.Result.Success;
#else
            isError = m_UnityWebRequest.isError;
#endif
            if (isError)
            {
                var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(m_UnityWebRequest.responseCode == RangeNotSatisfiableErrorCode, m_UnityWebRequest.error);
                m_DownloadAgentHelperErrorEventHandler(this, downloadAgentHelperErrorEventArgs);
                ReferencePool.Release(downloadAgentHelperErrorEventArgs);
            }
            else
            {
                var downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create((long)m_UnityWebRequest.downloadedBytes);
                m_DownloadAgentHelperCompleteEventHandler(this, downloadAgentHelperCompleteEventArgs);
                ReferencePool.Release(downloadAgentHelperCompleteEventArgs);
            }
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null ||
                m_DownloadAgentHelperErrorEventHandler       == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest                    = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.certificateHandler = new DownloadCertificateHandler();
            m_UnityWebRequest.downloadHandler    = new DownloadHandler(this);
            m_UnityWebRequest.SendWebRequest();
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, long fromPosition, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null ||
                m_DownloadAgentHelperErrorEventHandler       == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest                    = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.certificateHandler = new DownloadCertificateHandler();
            m_UnityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-", fromPosition));
            m_UnityWebRequest.downloadHandler = new DownloadHandler(this);
            m_UnityWebRequest.SendWebRequest();
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        /// <param name="toPosition">下载数据结束位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, long fromPosition, long toPosition, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null ||
                m_DownloadAgentHelperErrorEventHandler       == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest                    = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.certificateHandler = new DownloadCertificateHandler();
            m_UnityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-{1}", fromPosition, toPosition));
            m_UnityWebRequest.downloadHandler = new DownloadHandler(this);
            m_UnityWebRequest.SendWebRequest();
        }

        /// <summary>
        /// 重置下载代理辅助器。
        /// </summary>
        public override void Reset()
        {
            if (m_UnityWebRequest != null)
            {
                m_UnityWebRequest.Abort();
                m_UnityWebRequest.Dispose();
                m_UnityWebRequest = null;
            }

            Array.Clear(m_CachedBytes, 0, CachedBytesLength);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">释放资源标记。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed) return;

            if (disposing && m_UnityWebRequest != null)
            {
                m_UnityWebRequest.Dispose();
                m_UnityWebRequest = null;
            }

            m_Disposed = true;
        }
    }
}