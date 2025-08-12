using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using Runtime.FuFramework.Download;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载管理器。
    /// 负责管理下载任务，核心实现是任务池，用来存储执行下载任务
    /// </summary>
    public sealed partial class DownloadManager : FuModule, IDownloadManager
    {
        /// 1 兆字节: 1M(Megabyte）= 1024KB = 1024*1024byte
        private const int OneMegaBytes = 1024 * 1024;

        /// 下载任务的任务池
        private readonly TaskPool<DownloadTask> m_TaskPool;

        /// 下载计数器
        private readonly DownloadCounter m_DownloadCounter;


        /// 下载开始事件回调
        private EventHandler<DownloadStartEventArgs> m_DownloadStartEventHandler;

        /// 下载更新事件回调
        private EventHandler<DownloadUpdateEventArgs> m_DownloadUpdateEventHandler;

        /// 下载成功事件回调
        private EventHandler<DownloadSuccessEventArgs> m_DownloadSuccessEventHandler;

        /// 下载失败事件回调
        private EventHandler<DownloadFailureEventArgs> m_DownloadFailureEventHandler;
        

        /// <summary>
        /// 初始化下载管理器的新实例。
        /// </summary>
        public DownloadManager()
        {
            Timeout   = 30f;
            FlushSize = OneMegaBytes;

            m_TaskPool        = new TaskPool<DownloadTask>();
            m_DownloadCounter = new DownloadCounter(1f, 10f);

            m_DownloadStartEventHandler   = null;
            m_DownloadUpdateEventHandler  = null;
            m_DownloadSuccessEventHandler = null;
            m_DownloadFailureEventHandler = null;
        }


        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => 5;

        /// <summary>
        /// 获取或设置下载是否被暂停。
        /// </summary>
        public bool Paused
        {
            get => m_TaskPool.Paused;
            set => m_TaskPool.Paused = value;
        }

        /// <summary>
        /// 获取或设置将缓冲区写入磁盘的临界大小。(默认为1M)
        /// </summary>
        public int FlushSize { get; set; }

        /// <summary>
        /// 获取或设置下载超时时长，以秒为单位。
        /// </summary>
        public float Timeout { get; set; }

        /// <summary>
        /// 获取下载代理总数量。
        /// </summary>
        public int TotalAgentCount => m_TaskPool.TotalAgentCount;

        /// <summary>
        /// 获取可用下载代理数量。
        /// </summary>
        public int FreeAgentCount => m_TaskPool.FreeAgentCount;

        /// <summary>
        /// 获取工作中下载代理数量。
        /// </summary>
        public int WorkingAgentCount => m_TaskPool.WorkingAgentCount;

        /// <summary>
        /// 获取等待下载任务数量。
        /// </summary>
        public int WaitingTaskCount => m_TaskPool.WaitingTaskCount;

        /// <summary>
        /// 获取当前下载速度。
        /// </summary>
        public float CurrentSpeed => m_DownloadCounter.CurrentSpeed;


        /// <summary>
        /// 下载开始事件。
        /// </summary>
        public event EventHandler<DownloadStartEventArgs> DownloadStart
        {
            add => m_DownloadStartEventHandler += value;
            remove => m_DownloadStartEventHandler -= value;
        }

        /// <summary>
        /// 下载更新事件。
        /// </summary>
        public event EventHandler<DownloadUpdateEventArgs> DownloadUpdate
        {
            add => m_DownloadUpdateEventHandler += value;
            remove => m_DownloadUpdateEventHandler -= value;
        }

        /// <summary>
        /// 下载成功事件。
        /// </summary>
        public event EventHandler<DownloadSuccessEventArgs> DownloadSuccess
        {
            add => m_DownloadSuccessEventHandler += value;
            remove => m_DownloadSuccessEventHandler -= value;
        }

        /// <summary>
        /// 下载失败事件。
        /// </summary>
        public event EventHandler<DownloadFailureEventArgs> DownloadFailure
        {
            add => m_DownloadFailureEventHandler += value;
            remove => m_DownloadFailureEventHandler -= value;
        }


        /// <summary>
        /// 下载管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑帧间隔流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_TaskPool.Update(elapseSeconds, realElapseSeconds);
            m_DownloadCounter.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 关闭并清理下载管理器。
        /// </summary>
        protected override void Shutdown()
        {
            m_TaskPool.Shutdown();
            m_DownloadCounter.Shutdown();
        }

        /// <summary>
        /// 增加下载代理辅助器。
        /// </summary>
        /// <param name="downloadAgentHelper">要增加的下载代理辅助器。</param>
        public void AddDownloadAgentHelper(IDownloadAgentHelper downloadAgentHelper)
        {
            var agent = new DownloadAgent(downloadAgentHelper);
            agent.DownloadAgentStart   += _OnDownloadAgentStart;
            agent.DownloadAgentUpdate  += _OnDownloadAgentUpdate;
            agent.DownloadAgentSuccess += _OnDownloadAgentSuccess;
            agent.DownloadAgentFailure += _OnDownloadAgentFailure;
            m_TaskPool.AddAgent(agent);
        }

        #region 获取下载任务信息

        /// <summary>
        /// 根据下载任务的序列编号获取下载任务的信息。
        /// </summary>
        /// <param name="serialId">要获取信息的下载任务的序列编号。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo GetDownloadInfo(int serialId) => m_TaskPool.GetTaskInfo(serialId);

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的下载任务的标签。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo[] GetDownloadInfos(string tag) => m_TaskPool.GetTaskInfos(tag);

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的下载任务的标签。</param>
        /// <param name="results">下载任务的信息。</param>
        public void GetDownloadInfos(string tag, List<TaskInfo> results) => m_TaskPool.GetTaskInfos(tag, results);

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <returns>所有下载任务的信息。</returns>
        public TaskInfo[] GetAllDownloadInfos() => m_TaskPool.GetAllTaskInfos();

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <param name="results">所有下载任务的信息。</param>
        public void GetAllDownloadInfos(List<TaskInfo> results) => m_TaskPool.GetAllTaskInfos(results);

        #endregion

        #region 添加下载任务

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri) => AddDownload(downloadPath, downloadUri, null, Constant.DefaultPriority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag) => AddDownload(downloadPath, downloadUri, tag, Constant.DefaultPriority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority) => AddDownload(downloadPath, downloadUri, null, priority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, object userData)
            => AddDownload(downloadPath, downloadUri, null, Constant.DefaultPriority, userData);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority)
            => AddDownload(downloadPath, downloadUri, tag, priority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, object userData)
            => AddDownload(downloadPath, downloadUri, tag, Constant.DefaultPriority, userData);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority, object userData)
            => AddDownload(downloadPath, downloadUri, null, priority, userData);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority, object userData)
        {
            if (string.IsNullOrEmpty(downloadPath)) throw new FuException("Download path is invalid.");
            if (string.IsNullOrEmpty(downloadUri)) throw new FuException("Download uri is invalid.");

            if (TotalAgentCount <= 0) throw new FuException("You must add download agent first.");

            var downloadTask = DownloadTask.Create(downloadPath, downloadUri, tag, priority, FlushSize, Timeout, userData);
            m_TaskPool.AddTask(downloadTask);
            return downloadTask.SerialId;
        }

        #endregion

        #region 移除下载任务

        /// <summary>
        /// 根据下载任务的序列编号移除下载任务。
        /// </summary>
        /// <param name="serialId">要移除下载任务的序列编号。</param>
        /// <returns>是否移除下载任务成功。</returns>
        public bool RemoveDownload(int serialId) => m_TaskPool.RemoveTask(serialId);

        /// <summary>
        /// 根据下载任务的标签移除下载任务。
        /// </summary>
        /// <param name="tag">要移除下载任务的标签。</param>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveDownloads(string tag) => m_TaskPool.RemoveTasks(tag);

        /// <summary>
        /// 移除所有下载任务。
        /// </summary>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveAllDownloads() => m_TaskPool.RemoveAllTasks();

        #endregion

        #region private methods

        /// <summary>
        /// 代理下载开始更新事件回调
        /// </summary>
        private void _OnDownloadAgentStart(DownloadAgent sender)
        {
            if (m_DownloadStartEventHandler == null) return;
            var downloadStartEventArgs = DownloadStartEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_DownloadStartEventHandler(this, downloadStartEventArgs);
            ReferencePool.Release(downloadStartEventArgs);
        }

        /// <summary>
        /// 代理下载数据更新事件回调
        /// </summary>
        private void _OnDownloadAgentUpdate(DownloadAgent sender, int deltaLength)
        {
            m_DownloadCounter.RecordDeltaLength(deltaLength);
            if (m_DownloadUpdateEventHandler == null) return;
            var downloadUpdateEventArgs = DownloadUpdateEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_DownloadUpdateEventHandler(this, downloadUpdateEventArgs);
            ReferencePool.Release(downloadUpdateEventArgs);
        }

        /// <summary>
        /// 代理下载成功事件回调
        /// </summary>
        private void _OnDownloadAgentSuccess(DownloadAgent sender, long length)
        {
            if (m_DownloadSuccessEventHandler == null) return;
            var downloadSuccessEventArgs = DownloadSuccessEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_DownloadSuccessEventHandler(this, downloadSuccessEventArgs);
            ReferencePool.Release(downloadSuccessEventArgs);
        }

        /// <summary>
        /// 代理下载失败事件回调
        /// </summary>
        private void _OnDownloadAgentFailure(DownloadAgent sender, string errorMessage)
        {
            if (m_DownloadFailureEventHandler == null) return;
            var downloadFailureEventArgs = DownloadFailureEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, errorMessage, sender.Task.UserData);
            m_DownloadFailureEventHandler(this, downloadFailureEventArgs);
            ReferencePool.Release(downloadFailureEventArgs);
        }

        #endregion
    }
}