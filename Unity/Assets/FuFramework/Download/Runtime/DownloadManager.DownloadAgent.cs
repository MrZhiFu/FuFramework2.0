using System;
using System.IO;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.TaskPool.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public sealed partial class DownloadManager
    {
        /// <summary>
        /// 下载代理。
        /// 使用下载帮助类UnityWebRequest下载文件。
        /// </summary>
        private sealed class DownloadAgent : ITaskAgent<DownloadTask>, IDisposable
        {
            /// 下载代理辅助器
            private readonly UnityWebRequestDownloadAgentHelper m_Helper;

            /// 下载文件流
            private FileStream m_FileStream;

            /// 等待刷新的大小(将缓冲区写入磁盘的临界大小)
            private int m_WaitFlushSize;

            /// 是否已销毁
            private bool m_Disposed;

            /// 事件管理器
            private readonly EventManager m_EventManager = ModuleManager.GetModule<EventManager>();
            

            /// 下载开始委托
            public Action<DownloadAgent> DownloadAgentStart;

            /// 下载更新委托
            public Action<DownloadAgent, int> DownloadAgentUpdate;

            /// 下载成功委托
            public Action<DownloadAgent, long> DownloadAgentSuccess;

            /// 下载失败委托
            public Action<DownloadAgent, string> DownloadAgentFailure;


            /// <summary>
            /// 构造下载代理的新实例。
            /// </summary>
            /// <param name="downloadAgentHelper">下载代理辅助器。</param>
            public DownloadAgent(UnityWebRequestDownloadAgentHelper downloadAgentHelper)
            {
                m_Helper = downloadAgentHelper ?? throw new FuException("[DownloadAgent]下载代理辅助器为空!");

                Task             = null;
                m_FileStream     = null;
                m_WaitFlushSize  = 0;
                WaitTime         = 0f;
                StartLength      = 0L;
                DownloadedLength = 0L;
                SavedLength      = 0L;
                m_Disposed       = false;

                DownloadAgentStart   = null;
                DownloadAgentUpdate  = null;
                DownloadAgentSuccess = null;
                DownloadAgentFailure = null;
            }

            /// <summary>
            /// 获取下载任务。
            /// </summary>
            public DownloadTask Task { get; private set; }

            /// <summary>
            /// 获取已经等待时间。
            /// </summary>
            public float WaitTime { get; private set; }

            /// <summary>
            /// 获取开始下载时已经存在的大小。
            /// </summary>
            public long StartLength { get; private set; }

            /// <summary>
            /// 获取本次已经下载的大小。
            /// </summary>
            public long DownloadedLength { get; private set; }

            /// <summary>
            /// 获取当前的大小。
            /// </summary>
            public long CurrentLength => StartLength + DownloadedLength;

            /// <summary>
            /// 获取已经存盘的大小。
            /// </summary>
            public long SavedLength { get; private set; }

            /// <summary>
            /// 初始化下载代理。
            /// </summary>
            public void Initialize()
            {
                m_EventManager.Subscribe(DownloadAgentHelperUpdateBytesEventArgs.EventId, _OnDownloadAgentHelperUpdateBytes);
                m_EventManager.Subscribe(DownloadAgentHelperUpdateLengthEventArgs.EventId, _OnDownloadAgentHelperUpdateLength);
                m_EventManager.Subscribe(DownloadAgentHelperCompleteEventArgs.EventId, _OnDownloadAgentHelperComplete);
                m_EventManager.Subscribe(DownloadAgentHelperErrorEventArgs.EventId, _OnDownloadAgentHelperError);
            }

            /// <summary>
            /// 下载代理轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑帧间隔流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                m_Helper.OnUpdate();
                
                if (Task.Status != DownloadTaskStatus.Doing) return;

                WaitTime += realElapseSeconds;
                if (WaitTime < Task.Timeout) return;

                // 调用下载代理辅助器错误事件
                var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, "Timeout");
                _OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                ReferencePool.Runtime.ReferencePool.Release(downloadAgentHelperErrorEventArgs);
            }

            /// <summary>
            /// 关闭并清理下载代理。
            /// </summary>
            public void Shutdown()
            {
                Dispose();

                m_EventManager.Unsubscribe(DownloadAgentHelperUpdateBytesEventArgs.EventId, _OnDownloadAgentHelperUpdateBytes);
                m_EventManager.Unsubscribe(DownloadAgentHelperUpdateLengthEventArgs.EventId, _OnDownloadAgentHelperUpdateLength);
                m_EventManager.Unsubscribe(DownloadAgentHelperCompleteEventArgs.EventId, _OnDownloadAgentHelperComplete);
                m_EventManager.Unsubscribe(DownloadAgentHelperErrorEventArgs.EventId, _OnDownloadAgentHelperError);
            }

            /// <summary>
            /// 开始处理下载任务。
            /// </summary>
            /// <param name="task">要处理的下载任务。</param>
            /// <returns>开始处理任务的状态。</returns>
            public StartTaskStatus Start(DownloadTask task)
            {
                Task = task ?? throw new FuException("[DownloadManager.DownloadAgent] 任务不能为空.");

                Task.Status = DownloadTaskStatus.Doing;
                var downloadFile = Utility.Text.Format("{0}.download", Task.DownloadedPath);

                try
                {
                    if (File.Exists(downloadFile))
                    {
                        m_FileStream = File.OpenWrite(downloadFile);
                        m_FileStream.Seek(0L, SeekOrigin.End);
                        StartLength      = SavedLength = m_FileStream.Length;
                        DownloadedLength = 0L;
                    }
                    else
                    {
                        var directory = Path.GetDirectoryName(Task.DownloadedPath);
                        if (directory != null && !Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        m_FileStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write);
                        StartLength  = SavedLength = DownloadedLength = 0L;
                    }

                    DownloadAgentStart?.Invoke(this);

                    // 使用帮助类开始下载
                    if (StartLength > 0L)
                        m_Helper.Download(Task.DownloadUri, StartLength); // 断点续传
                    else
                        m_Helper.Download(Task.DownloadUri); // 全新下载

                    return StartTaskStatus.CanResume;
                }
                catch (Exception exception)
                {
                    var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
                    _OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                    ReferencePool.Runtime.ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                    return StartTaskStatus.UnknownError;
                }
            }

            /// <summary>
            /// 重置下载代理。
            /// </summary>
            public void Reset()
            {
                m_Helper.Reset();

                if (m_FileStream != null)
                {
                    m_FileStream.Close();
                    m_FileStream = null;
                }

                Task             = null;
                m_WaitFlushSize  = 0;
                WaitTime         = 0f;
                StartLength      = 0L;
                DownloadedLength = 0L;
                SavedLength      = 0L;
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            public void Dispose()
            {
                if (m_Disposed) return;

                if (m_FileStream != null)
                {
                    m_FileStream.Dispose();
                    m_FileStream = null;
                }

                m_Disposed = true;
                
                // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// 下载代理辅助器更新数据流事件回调。
            /// </summary>
            private void _OnDownloadAgentHelperUpdateBytes(object sender, GameEventArgs eventArgs)
            {
                if (eventArgs is not DownloadAgentHelperUpdateBytesEventArgs e) return;
                
                WaitTime = 0f;

                try
                {
                    m_FileStream.Write(e.GetBytes(), e.Offset, e.Length);
                    m_WaitFlushSize += e.Length;
                    SavedLength     += e.Length;

                    if (m_WaitFlushSize >= Task.FlushSize)
                    {
                        m_FileStream.Flush();
                        m_WaitFlushSize = 0;
                    }
                }
                catch (Exception exception)
                {
                    var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
                    _OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                    ReferencePool.Runtime.ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                }
            }

            /// <summary>
            /// 下载代理辅助器更新数据大小事件回调。
            /// </summary>
            private void _OnDownloadAgentHelperUpdateLength(object sender, GameEventArgs gameEventArgs)
            {
                if (gameEventArgs is not DownloadAgentHelperUpdateLengthEventArgs e) return;
                WaitTime         =  0f;
                DownloadedLength += e.DeltaLength;
                DownloadAgentUpdate?.Invoke(this, e.DeltaLength);
            }

            /// <summary>
            /// 下载代理辅助器完成事件回调。
            /// </summary>
            private void _OnDownloadAgentHelperComplete(object sender, GameEventArgs gameEventArgs)
            {
                if (gameEventArgs is not DownloadAgentHelperCompleteEventArgs e) return;
                WaitTime         = 0f;
                DownloadedLength = e.Length;

                if (SavedLength != CurrentLength)
                    throw new FuException("Internal download error.");

                m_Helper.Reset();
                m_FileStream.Close();
                m_FileStream = null;

                if (File.Exists(Task.DownloadedPath))
                    File.Delete(Task.DownloadedPath);

                File.Move(Utility.Text.Format("{0}.download", Task.DownloadedPath), Task.DownloadedPath);

                Task.Status = DownloadTaskStatus.Done;
                DownloadAgentSuccess?.Invoke(this, e.Length);
                Task.Done = true;
            }

            /// <summary>
            /// 下载代理辅助器错误事件回调。
            /// </summary>
            private void _OnDownloadAgentHelperError(object sender, GameEventArgs gameEventArgs)
            {
                if (gameEventArgs is not DownloadAgentHelperErrorEventArgs e) return;
                
                m_Helper.Reset();
                if (m_FileStream != null)
                {
                    m_FileStream.Close();
                    m_FileStream = null;
                }

                if (e.DeleteDownloading)
                    File.Delete(Utility.Text.Format("{0}.download", Task.DownloadedPath));

                Task.Status = DownloadTaskStatus.Error;
                DownloadAgentFailure?.Invoke(this, e.ErrorMessage);
                Task.Done = true;
            }
        }
    }
}