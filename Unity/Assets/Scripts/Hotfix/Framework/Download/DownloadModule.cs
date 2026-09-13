using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    public sealed partial class DownloadModule : ModuleBase, ICancelAsync
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
        /// 下载任务的任务池。
        /// 类型全限定：本文件因 CancelAsync 需引用 Cysharp.Threading.Tasks，而其中也定义了 TaskPool&lt;T&gt;，
        /// 裸写 TaskPool&lt;DownloadTask&gt; 会与其产生歧义（CS0104）。
        /// </summary>
        private readonly Hotfix.Framework.Core.TaskPool<DownloadTask> m_TaskPool = new();

        /// <summary>
        /// 下载计数器，1秒更新一次，10秒记录一次，用于计算下载速度
        /// </summary>
        private readonly DownloadCounter m_DownloadCounter = new(1f, 10f);

        /// <summary>
        /// 正在下载的任务字典，key为任务编号，value为下载数据
        /// </summary>
        private readonly ConcurrentDictionary<int, DownloadData> m_DownloadingTaskDict = new();

        /// <summary>
        /// 取消范围：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
        /// 下载任务由任务池在主循环同步驱动（无 await 型在途操作），故在途计数恒为 0；
        /// OnDispose 时 Cancel，模块被 ModuleManager.CancelAllAsync 排水到时完成全部在途 Tcs。
        /// </summary>
        private CancellationScope m_Scope = new();

        /// <summary>
        /// 取消令牌：模块销毁（OnDispose）后触发。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并完成所有在途下载后才返回。供框架重启取消清理（ModuleManager.CancelAllAsync 排水）。
        /// </summary>
        public async UniTask CancelAsync()
        {
            await m_Scope.CancelAsync(); // 触发取消（下载任务无 await 型在途操作，计数为 0 时立即返回）

            // 完成任务池已移除后的全部在途 Tcs：否则 await AddDownloadAsync 的调用方永久挂起，
            // DownloadData 亦随字典跨生命周期泄漏。幂等：字典已空时为空操作。
            CompleteAndClearAllDownloads();
        }

        /// <summary>
        /// 完成并丢弃当前全部在途下载条目：对每个条目的 Tcs 置结果 false（与模块既有失败语义一致），
        /// 使 <c>await AddDownloadAsync(...)</c> 的调用方被唤醒而非永久挂起，并清空字典避免 DownloadData 泄漏。
        /// <b>调用方须先移除任务池中的对应任务</b>（OnDispose 的 Shutdown、RemoveAllDownloads 的 RemoveAllTasks）：
        /// 否则续体若立刻重试 AddDownload，新条目会落在「任务已被移除、回调永不触发」的空档而无人完成其 Tcs。
        /// </summary>
        private void CompleteAndClearAllDownloads()
        {
            // 先摘取再完成：TrySetResult 会同步执行等待方续体，续体可能再次 AddDownload/RemoveDownload。
            // 先把条目移出字典（先收拢到局部列表再 Clear），可保证续体新建的条目不被误清、其 Tcs 也不被漏完成。
            List<DownloadData> pending = null;
            foreach (var (_, downloadData) in m_DownloadingTaskDict)
            {
                (pending ??= new List<DownloadData>()).Add(downloadData);
            }

            if (pending == null) return;

            // 先清字典再完成 Tcs：TrySetResult 会同步执行等待方续体，续体若有重试逻辑会再次 AddDownload，
            // 先清空可保证续体新建的条目不会被本轮的 Clear 误摘走。
            m_DownloadingTaskDict.Clear();

            foreach (var downloadData in pending)
            {
                // 逐项隔离：单个续体抛出的异常不得中断整轮完成，否则其余在途 Tcs 将永久挂起。
                try
                {
                    downloadData.Tcs.TrySetResult(false);
                }
                catch (Exception e)
                {
                    FuLogger.LogError($"[DownloadModule] 完成下载任务 '{downloadData.SerialId}' 的等待方时发生异常: {e.Message}");
                }
            }
        }


        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            m_Scope  = new CancellationScope(); // 新生命周期 = 新 Token

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
            m_Scope.Cancel(); // 随模块销毁触发取消（排水等待由 CancelAsync 负责）

            // 先关任务池：其同步移除全部任务，此后不会再有任何成功/失败回调来完成任务 Tcs（代理 Reset 只中止
            // UWR，不派发事件）；且池内代理被全部 Shutdown 后 TotalAgentCount 为 0，销毁期间续体再调 AddDownload
            // 会被既有「可用下载代理个数为 0」守卫直接拒绝，不会产生无人完成的残留条目。
            // 放在 finally 前是为了保证即便任务清理抛异常，下面的 Tcs 收尾仍会执行（否则 await AddDownloadAsync 永久挂起）。
            try
            {
                m_TaskPool.Shutdown();
            }
            finally
            {
                // 显式完成并丢弃全部在途条目：避免 await AddDownloadAsync 永久挂起、DownloadData 跨生命周期泄漏。
                // （QuitGame 等不经过 CancelAllAsync 的销毁路径同样需要这一步。）
                CompleteAndClearAllDownloads();
            }

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