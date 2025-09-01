using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;
using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载管理器。
    /// 负责管理下载任务，核心实现是任务池，用来存储执行下载任务
    /// </summary>
    public sealed partial class DownloadManager : FuComponent
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => 5;
        
        /// <summary>
        /// 默认下载任务优先级。
        /// </summary>
        internal const int DefaultPriority = 0;
        
        /// <summary>
        ///  1 兆字节: 1M(Megabyte）= 1024KB = 1024*1024byte
        /// </summary>
        private const int OneMegaBytes = 1024 * 1024;

        /// <summary>
        /// 事件管理器
        /// </summary>
        private EventManager m_EventManager;

        /// <summary>
        /// 下载任务的任务池
        /// </summary>
        private readonly Core.Runtime.TaskPool<DownloadTask> m_TaskPool = new();

        /// <summary>
        /// 下载计数器，1秒更新一次，10秒记录一次，用于计算下载速度
        /// </summary>
        private readonly DownloadCounter m_DownloadCounter = new(1f, 10f);

        /// <summary>
        /// 正在下载的任务字典，key为任务编号，value为下载数据
        /// </summary>
        private readonly ConcurrentDictionary<int, DownloadData> m_DownloadingTaskDict = new();

        [Header("下载代理实例对象根节点")]
        [SerializeField] private Transform m_InstanceRoot;

        [Header("下载代理辅助器个数")]
        [SerializeField] private int m_DownloadAgentHelperCount = 3;

        
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
        /// 下载管理器初始化。
        /// </summary>
        protected override void OnInit()
        {
            Timeout   = 30f;
            FlushSize = OneMegaBytes;

            m_EventManager = ModuleManager.GetModule<EventManager>();
            if (!m_EventManager)
            {
                Log.Fatal("[DownloadManager] 事件管理器为空!");
                return;
            }
            
            if (!m_InstanceRoot)
            {
                m_InstanceRoot = new GameObject("Download Agent Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }
            
            // 添加下载任务处理器
            for (var i = 0; i < m_DownloadAgentHelperCount; i++)
            {
                AddDownloadAgentHelper(i);
            }
        }

        /// <summary>
        /// 下载管理器轮询
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            m_TaskPool.Update(elapseSeconds, realElapseSeconds);
            m_DownloadCounter.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 下载管理器关闭。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            m_TaskPool.Shutdown();
            m_DownloadCounter.Shutdown();
        }
        
        /// <summary>
        /// 增加下载代理辅助器。
        /// </summary>
        /// <param name="index">索引</param>
        private void AddDownloadAgentHelper(int index)
        {
            var helpObject = new GameObject();
            var downloadAgentHelper = helpObject.AddComponent<UnityWebRequestDownloadAgentHelper>();
            helpObject.name = $"[DownloadAgentHelper]_{index}";
            if (downloadAgentHelper == null)
            {
                Log.Error("[DownloadManager]创建下载代理辅助器失败!");
                return;
            }

            var trs = downloadAgentHelper.transform;
            trs.SetParent(m_InstanceRoot);
            trs.localScale = Vector3.one;
            
            var agent = new DownloadAgent(downloadAgentHelper);
            agent.DownloadAgentStart   += _OnDownloadAgentStart;
            agent.DownloadAgentUpdate  += _OnDownloadAgentUpdate;
            agent.DownloadAgentSuccess += _OnDownloadAgentSuccess;
            agent.DownloadAgentFailure += _OnDownloadAgentFailure;
            
            // 向任务池中加入下载任务执行代理
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
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri)
        {
            return AddDownload(downloadPath, downloadUri, null, DefaultPriority, null);
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
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string taskTag)
        {
            return AddDownload(downloadPath, downloadUri, taskTag, DefaultPriority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, DefaultPriority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string taskTag, int priority)
        {
            return AddDownload(downloadPath, downloadUri, taskTag, priority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string taskTag, object userData)
        {
            return AddDownload(downloadPath, downloadUri, taskTag, DefaultPriority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string taskTag, int priority, object userData)
        {
            if (string.IsNullOrEmpty(downloadPath)) throw new FuException("下载路径不能为空.");
            if (string.IsNullOrEmpty(downloadUri)) throw new FuException("下载地址不能为空.");

            if (TotalAgentCount <= 0) throw new FuException("可用的下载代理个数为0.");

            // 创建下载任务
            var downloadTask = DownloadTask.Create(downloadPath, downloadUri, taskTag, priority, FlushSize, Timeout, userData);
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

        #region private methods

        /// <summary>
        /// 代理下载开始更新事件回调
        /// </summary>
        private void _OnDownloadAgentStart(DownloadAgent sender)
        {
            var downloadStartEventArgs = DownloadStartEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_EventManager.Fire(this, downloadStartEventArgs);
        }

        /// <summary>
        /// 代理下载数据更新事件回调
        /// </summary>
        private void _OnDownloadAgentUpdate(DownloadAgent sender, int deltaLength)
        {
            m_DownloadCounter.RecordDeltaLength(deltaLength);
            var downloadUpdateEventArgs = DownloadUpdateEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_EventManager.Fire(this, downloadUpdateEventArgs);
        }

        /// <summary>
        /// 代理下载成功事件回调
        /// </summary>
        private void _OnDownloadAgentSuccess(DownloadAgent sender, long length)
        {
            var downloadSuccessEventArgs = DownloadSuccessEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
            m_EventManager.Fire(this, downloadSuccessEventArgs);
            if (m_DownloadingTaskDict.TryRemove(sender.Task.SerialId, out var downloadData))
            {
                downloadData.Tcs.TrySetResult(true);
            }
        }

        /// <summary>
        /// 代理下载失败事件回调
        /// </summary>
        private void _OnDownloadAgentFailure(DownloadAgent sender, string errorMessage)
        {
            Log.Warning($"[DownloadManager]下载失败! 下载任务序列编号 '{sender.Task.SerialId}', 下载路径 '{sender.Task.DownloadedPath}', 下载地址 '{sender.Task.DownloadUri}', 错误信息 '{errorMessage}'.");
            var downloadFailureEventArgs = DownloadFailureEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadedPath, sender.Task.DownloadUri, errorMessage, sender.Task.UserData);
            m_EventManager.Fire(this, downloadFailureEventArgs);
            if (m_DownloadingTaskDict.TryRemove(sender.Task.SerialId, out var downloadData))
            {
                downloadData.Tcs.TrySetResult(false);
            }
        }

        #endregion
    }
}

 /*-----------------------------Example--------------------------------------------------------------
    public class DownloadDemo : MonoBehaviour
    {
        private void OnEnable()
        {
            GlobalModule.EventModule.Subscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
            GlobalModule.EventModule.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
            GlobalModule.EventModule.Subscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
            GlobalModule.EventModule.Subscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
        }

        private void OnDisable()
        {
            GlobalModule.EventModule.Unsubscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
            GlobalModule.EventModule.Unsubscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
            GlobalModule.EventModule.Unsubscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
            GlobalModule.EventModule.Unsubscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
        }

        private void Start()
        {
            DownloadTest("TestImg.png", "http://xxxx.TestImg.png");
            DownloadTest("TestAudio.mp3", "http://xxxx.TestAudio.mp3");
            DownloadTest("TestDat.dat", "http://xxxx.TestDat.dat");
        }

        protected void DownloadTest(string url, string fileName)
        {
            GlobalModule.DownloadModule.AddDownload(Application.persistentDataPath + "/" + fileName, url);
        }

        private void OnDownloadStart(object sender, GameEventArgs e)
        {
            Debug.Log("下载开始");
        }

        private void OnDownloadSuccess(object sender, GameEventArgs e)
        {
            Debug.Log("下载成功");
        }

        private void OnDownloadFailure(object sender, GameEventArgs e)
        {
            Debug.Log("下载失败");
        }

        private void OnDownloadUpdate(object sender, GameEventArgs e)
        {
            Debug.Log("下载更新进度");
        }
    }
    ----------------------------------------------------------------------------------------------------*/