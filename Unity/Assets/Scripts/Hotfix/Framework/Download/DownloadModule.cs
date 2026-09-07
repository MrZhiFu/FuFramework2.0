using System.IO;
using System.Collections.Concurrent;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

namespace Hotfix.Framework.Download
{
    /// <summary>
    /// 下载管理模块。
    /// 功能：
    ///     1. 负责管理下载任务，核心实现是任务池，用来存储执行下载任务。
    ///     2. 提供下载进度事件和下载完成事件。
    ///     3. 支持断点续传。
    ///     4. 提供下载速度的计算。
    ///     5. 添加/移除/获取下载任务。
    /// </summary>
    public sealed partial class DownloadModule : ModuleBase
    {
        /// <summary>
        /// 默认下载任务优先级。
        /// </summary>
        internal const int DefaultPriority = 0;

        /// <summary>
        ///  1 兆字节: 1M(Megabyte）= 1024KB = 1024*1024byte
        /// </summary>
        private const int OneMegaBytes = 1024 * 1024;

        /// <summary>
        /// 下载代理辅助器个数
        /// </summary>
        private const int DownloadAgentHelperCount = 3;

        /// <summary>
        /// 事件管理模块
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// 下载任务的任务池
        /// </summary>
        private readonly TaskPool.TaskPool<DownloadTask> m_TaskPool = new();

        /// <summary>
        /// 下载计数器，1秒更新一次，10秒记录一次，用于计算下载速度
        /// </summary>
        private readonly DownloadCounter m_DownloadCounter = new(1f, 10f);

        /// <summary>
        /// 正在下载的任务字典，key为任务编号，value为下载数据
        /// </summary>
        private readonly ConcurrentDictionary<int, DownloadData> m_DownloadingTaskDict = new();


        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;

            Timeout   = 30f;
            FlushSize = OneMegaBytes;

            m_EventModule = ModuleManager.GetModule<EventModule>();
            if (m_EventModule == null)
            {
                FuLogger.LogFatal("[DownloadModule] 事件管理模块为空!");
                return;
            }

            // 添加下载任务处理器
            for (var i = 0; i < DownloadAgentHelperCount; i++)
            {
                AddDownloadAgentHelper();
            }
        }

        /// <summary>
        /// 帧更新
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="unscaledDeltaTime"></param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            m_TaskPool.Update(deltaTime, unscaledDeltaTime);
            m_DownloadCounter.Update(deltaTime, unscaledDeltaTime);
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            m_TaskPool.Shutdown();
            m_DownloadCounter.Shutdown();
            Instance = null;
        }

        /// <summary>
        /// 增加下载代理辅助器。
        /// </summary>
        private void AddDownloadAgentHelper()
        {
            var downloadAgentHelper = new UnityWebRequestDownloadAgentHelper();
            var downloadAgent       = new DownloadAgent(downloadAgentHelper);

            downloadAgent.DownloadAgentStart   += OnDownloadAgentStart;
            downloadAgent.DownloadAgentUpdate  += OnDownloadAgentUpdate;
            downloadAgent.DownloadAgentSuccess += OnDownloadAgentSuccess;
            downloadAgent.DownloadAgentFailure += OnDownloadAgentFailure;

            // 向任务池中加入下载任务执行代理
            m_TaskPool.AddAgent(downloadAgent);
        }


        #region private methods

        /// <summary>
        /// 代理下载开始更新事件回调
        /// </summary>
        private void OnDownloadAgentStart(DownloadAgent sender)
        {
            // 检查sender.Task是否为null，避免空引用异常
            if (sender.Task == null)
            {
                FuLogger.LogWarning("[DownloadModule]下载开始事件被触发，但下载任务为null。");
                return;
            }

            var downloadStartEventArgs = DownloadStartEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedFullPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_EventModule.Broadcast(this, downloadStartEventArgs);
        }

        /// <summary>
        /// 代理下载数据更新事件回调
        /// </summary>
        private void OnDownloadAgentUpdate(DownloadAgent sender, int deltaLength)
        {
            // 检查sender.Task是否为null，避免空引用异常
            if (sender.Task == null)
            {
                FuLogger.LogWarning("[DownloadModule]下载更新事件被触发，但下载任务为null。");
                return;
            }

            m_DownloadCounter.RecordDeltaLength(deltaLength);
            var downloadUpdateEventArgs = DownloadUpdateEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedFullPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_EventModule.Broadcast(this, downloadUpdateEventArgs);
        }

        /// <summary>
        /// 代理下载成功事件回调
        /// </summary>
        private void OnDownloadAgentSuccess(DownloadAgent sender, long length)
        {
            // 检查sender.Task是否为null，避免空引用异常
            if (sender.Task == null)
            {
                FuLogger.LogWarning("[DownloadModule]下载成功事件被触发，但下载任务为null。");
                return;
            }

            var downloadSuccessEventArgs = DownloadSuccessEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedFullPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_EventModule.Broadcast(this, downloadSuccessEventArgs);
            if (m_DownloadingTaskDict.TryRemove(sender.Task.SerialId, out var downloadData))
            {
                downloadData.Tcs.TrySetResult(true);
            }
        }

        /// <summary>
        /// 代理下载失败事件回调
        /// </summary>
        private void OnDownloadAgentFailure(DownloadAgent sender, string errorMessage)
        {
            // 检查sender.Task是否为null，避免空引用异常
            if (sender.Task == null)
            {
                FuLogger.LogError($"[DownloadModule]下载失败! 下载任务为null，错误信息 '{errorMessage}'.");
                return;
            }

            // 检查是否为416 Range Not Satisfiable错误
            if (errorMessage.Contains("416") || errorMessage.Contains("Range Not Satisfiable"))
            {
                FuLogger.LogWarning($"[DownloadModule]检测到416 Range Not Satisfiable错误，将重新从头开始下载。下载任务序列编号 '{sender.Task.SerialId}', 下载后存放全路径 '{sender.Task.DownloadedFullPath}', 下载地址 '{sender.Task.DownloadUri}'.");

                // 删除损坏的下载文件
                var downloadFile = $"{sender.Task.DownloadedFullPath}.download";
                if (File.Exists(downloadFile))
                {
                    File.Delete(downloadFile);
                }

                // 从当前任务中移除，但保留任务信息以便重新添加
                if (m_DownloadingTaskDict.TryRemove(sender.Task.SerialId, out var downloadData01))
                {
                    // 重新添加下载任务，从头开始下载
                    var newSerialId = AddDownload(sender.Task.DownloadedFullPath, sender.Task.DownloadUri, sender.Task.Tag, sender.Task.Priority, sender.Task.UserData);
                    FuLogger.LogInfo($"[DownloadModule]已重新添加下载任务，新的序列编号为 '{newSerialId}'。");

                    // 完成原任务（返回false表示失败，但新任务会继续）
                    downloadData01.Tcs.TrySetResult(false);
                }

                return;
            }

            FuLogger.LogError($"[DownloadModule]下载失败! 下载任务序列编号 '{sender.Task.SerialId}', 下载路径 '{sender.Task.DownloadedFullPath}', 下载地址 '{sender.Task.DownloadUri}', 错误信息 '{errorMessage}'.");
            var downloadFailureEventArgs = DownloadFailureEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedFullPath, sender.Task.DownloadUri, errorMessage, sender.Task.UserData);
            m_EventModule.Broadcast(this, downloadFailureEventArgs);
            if (m_DownloadingTaskDict.TryRemove(sender.Task.SerialId, out var downloadData02))
            {
                downloadData02.Tcs.TrySetResult(false);
            }
        }

        #endregion
    }
}