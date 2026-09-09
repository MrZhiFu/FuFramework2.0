using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
    /// <summary>
    /// 下载管理模块的公共 API。
    /// 功能：
    ///     1. 提供配置属性：暂停(Paused)、写盘临界(FlushSize)、超时(Timeout)与代理/任务数量查询。
    ///     2. 提供下载任务添加（AddDownload/AddDownloadAsync）、移除（RemoveDownload(s)/RemoveAllDownloads）。
    ///     3. 提供下载任务信息查询（GetDownloadInfo(s)/GetAllDownloadInfos）与当前下载速度(CurrentSpeed)。
    /// </summary>
    public sealed partial class DownloadModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static DownloadModule Instance { get; private set; }

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
        /// <param name="taskTag">要获取信息的下载任务的标签。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo[] GetDownloadInfos(string taskTag) => m_TaskPool.GetTaskInfos(taskTag);

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="taskTag">要获取信息的下载任务的标签。</param>
        /// <param name="results">下载任务的信息。</param>
        public void GetDownloadInfos(string taskTag, List<TaskInfo> results) => m_TaskPool.GetTaskInfos(taskTag, results);

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
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri)
        {
            return AddDownload(downloadedFullPath, downloadUri, null, DefaultPriority, null);
        }

        /// <summary>
        /// 异步增加下载任务(await方式)。
        /// </summary>
        /// <param name="downloadPath">存储路径</param>
        /// <param name="downloadUri">下载地址</param>
        /// <returns>返回是否下载成功</returns>
        public UniTask<bool> AddDownloadAsync(string downloadPath, string downloadUri)
        {
            var serialId = AddDownload(downloadPath, downloadUri, null, DefaultPriority, null);
            return m_DownloadingTaskDict.TryGetValue(serialId, out var downloadData) ? downloadData.Tcs.Task : default;
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, string taskTag)
        {
            return AddDownload(downloadedFullPath, downloadUri, taskTag, DefaultPriority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, int priority)
        {
            return AddDownload(downloadedFullPath, downloadUri, null, priority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, object userData)
        {
            return AddDownload(downloadedFullPath, downloadUri, null, DefaultPriority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, string taskTag, int priority)
        {
            return AddDownload(downloadedFullPath, downloadUri, taskTag, priority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, string taskTag, object userData)
        {
            return AddDownload(downloadedFullPath, downloadUri, taskTag, DefaultPriority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, int priority, object userData)
        {
            return AddDownload(downloadedFullPath, downloadUri, null, priority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadedFullPath">下载后存放全路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadedFullPath, string downloadUri, string taskTag, int priority, object userData)
        {
            if (string.IsNullOrEmpty(downloadedFullPath)) throw new InvalidOperationException("下载路径不能为空.");
            if (string.IsNullOrEmpty(downloadUri)) throw new InvalidOperationException("下载地址不能为空.");

            if (TotalAgentCount <= 0) throw new InvalidOperationException("可用的下载代理个数为0.");

            // 创建下载任务
            var downloadTask = DownloadTask.Create(downloadedFullPath, downloadUri, taskTag, priority, FlushSize, Timeout, userData);
            m_TaskPool.AddTask(downloadTask);

            // 记录下载任务信息
            var downloadData = new DownloadData(downloadUri, taskTag, downloadTask.SerialId, userData);
            m_DownloadingTaskDict.TryAdd(downloadTask.SerialId, downloadData);
            return downloadTask.SerialId;
        }

        #endregion

        #region 移除下载任务

        /// <summary>
        /// 根据下载任务的序列编号移除下载任务。
        /// </summary>
        /// <param name="serialId">要移除下载任务的序列编号。</param>
        /// <returns>是否移除下载任务成功。</returns>
        public bool RemoveDownload(int serialId)
        {
            m_DownloadingTaskDict.TryRemove(serialId, out _);
            return m_TaskPool.RemoveTask(serialId);
        }

        /// <summary>
        /// 根据下载任务的标签移除下载任务。
        /// </summary>
        /// <param name="taskTag">要移除下载任务的标签。</param>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveDownloads(string taskTag)
        {
            var serialId = -1;
            foreach (var downloadData in m_DownloadingTaskDict.Values)
            {
                if (downloadData.Tag != taskTag) continue;
                serialId = downloadData.SerialId;
                break;
            }

            m_DownloadingTaskDict.TryRemove(serialId, out _);
            return m_TaskPool.RemoveTasks(taskTag);
        }

        /// <summary>
        /// 移除所有下载任务。
        /// </summary>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveAllDownloads()
        {
            m_DownloadingTaskDict.Clear();
            return m_TaskPool.RemoveAllTasks();
        }

        #endregion
    }
}
