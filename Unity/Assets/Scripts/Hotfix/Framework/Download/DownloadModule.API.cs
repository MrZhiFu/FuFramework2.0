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
            var removed = m_TaskPool.RemoveTask(serialId);

            if (m_DownloadingTaskDict.TryRemove(serialId, out var downloadData))
            {
                // 条目被丢弃即完成其 Tcs（false = 未成功，与模块既有失败语义一致），
                // 否则 await AddDownloadAsync 的调用方将永久挂起。
                downloadData.Tcs.TrySetResult(false);
            }

            return removed;
        }

        /// <summary>
        /// 根据下载任务的标签移除下载任务。
        /// </summary>
        /// <param name="taskTag">要移除下载任务的标签。</param>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveDownloads(string taskTag)
        {
            // 先从任务池移除全部同标签任务再处理字典条目：顺序反了的话，Tcs 完成所唤醒的续体若立刻重试
            // AddDownload（同标签），新任务会被随后的 RemoveTasks 一并移除而其回调永不触发 → 新条目 Tcs 永久挂起。
            var count = m_TaskPool.RemoveTasks(taskTag);

            // 先枚举收集「全部」匹配项再逐个处理：原实现只摘除首个匹配项却调用 RemoveTasks 移除全部同标签任务，
            // 其余 DownloadData 会永久滞留在字典中，且其 Tcs 永不完成（await 永久挂起）。
            List<int> serialIds = null;
            foreach (var downloadData in m_DownloadingTaskDict.Values)
            {
                if (downloadData.Tag != taskTag) continue;
                (serialIds ??= new List<int>()).Add(downloadData.SerialId);
            }

            if (serialIds != null)
            {
                foreach (var serialId in serialIds)
                {
                    if (!m_DownloadingTaskDict.TryRemove(serialId, out var downloadData)) continue;
                    downloadData.Tcs.TrySetResult(false); // 与 RemoveDownload 一致：丢弃条目即完成 Tcs
                }
            }

            return count;
        }

        /// <summary>
        /// 移除所有下载任务。
        /// </summary>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveAllDownloads()
        {
            // 先移除任务池中的全部任务，再完成并清空字典条目：
            // 顺序反了的话，Tcs 完成所唤醒的续体若立刻重试 AddDownload，新任务会落在「池中任务已被移除、
            // 成功/失败回调永不触发」的空档，其 Tcs 将永久挂起（CompleteAndClearAllDownloads 的前置条件即此顺序）。
            var count = m_TaskPool.RemoveAllTasks();
            CompleteAndClearAllDownloads();
            return count;
        }

        #endregion
    }
}
