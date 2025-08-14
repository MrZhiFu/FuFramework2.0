using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Download")]
    public sealed class DownloadComponent : FuComponent
    {
        /// 下载默认优先级
        private const int DefaultPriority = 0;

        /// 1 兆字节: 1M(Megabyte）= 1024KB = 1024*1024byte
        private const int OneMegaBytes = 1024 * 1024;


        /// GF下载管理器
        private IDownloadManager m_DownloadManager;

        /// GF事件管理器
        private EventComponent m_EventComponent;


        [Header("下载代理实例对象根节点")]
        [SerializeField] private Transform m_InstanceRoot;

        /// 下载代理帮助类(默认使用WebRequest)
        [SerializeField] private string m_DownloadAgentHelperTypeName = "UnityGameFramework.Runtime.UnityWebRequestDownloadAgentHelper";

        /// 自定义下载代理帮助类
        [SerializeField] private DownloadAgentHelperBase m_CustomDownloadAgentHelper = null;

        /// 下载代理帮助类个数
        [SerializeField] private int m_DownloadAgentHelperCount = 3;

        /// 下载超时时长(秒)
        [SerializeField] private float m_Timeout = 30f;

        /// 将缓冲区写入磁盘的临界大小(仅当开启断点续传时有效)
        [SerializeField] private int m_FlushSize = OneMegaBytes;


        /// <summary>
        /// 获取或设置下载是否被暂停。
        /// </summary>
        public bool Paused
        {
            get => m_DownloadManager.Paused;
            set => m_DownloadManager.Paused = value;
        }

        /// <summary>
        /// 获取下载代理总数量。
        /// </summary>
        public int TotalAgentCount => m_DownloadManager.TotalAgentCount;

        /// <summary>
        /// 获取可用下载代理数量。
        /// </summary>
        public int FreeAgentCount => m_DownloadManager.FreeAgentCount;

        /// <summary>
        /// 获取工作中下载代理数量。
        /// </summary>
        public int WorkingAgentCount => m_DownloadManager.WorkingAgentCount;

        /// <summary>
        /// 获取等待下载任务数量。
        /// </summary>
        public int WaitingTaskCount => m_DownloadManager.WaitingTaskCount;

        /// <summary>
        /// 获取或设置下载超时时长，以秒为单位。
        /// </summary>
        public float Timeout
        {
            get => m_DownloadManager.Timeout;
            set => m_DownloadManager.Timeout = m_Timeout = value;
        }

        /// <summary>
        /// 获取或设置将缓冲区写入磁盘的临界大小，仅当开启断点续传时有效。
        /// </summary>
        public int FlushSize
        {
            get => m_DownloadManager.FlushSize;
            set => m_DownloadManager.FlushSize = m_FlushSize = value;
        }

        /// <summary>
        /// 获取当前下载速度。
        /// </summary>
        public float CurrentSpeed => m_DownloadManager.CurrentSpeed;


        /// <summary>
        /// 获取下载任务的字典。
        /// </summary>
        private readonly ConcurrentDictionary<int, DownloadData> m_Downloads = new();

        /// <summary>
        /// 下载数据。
        /// </summary>
        private sealed class DownloadData
        {
            public TaskCompletionSource<bool> Tcs { get; private set; }

            public object UserData { get; private set; }
            public string Tag      { get; private set; }
            public string Url      { get; private set; }
            public int    SerialId { get; private set; }


            /// <summary>
            /// 初始化下载数据的新实例。
            /// </summary>
            public DownloadData(string url, string tag, int serialId, object userData)
            {
                Url      = url;
                Tag      = tag;
                SerialId = serialId;
                UserData = userData;
                Tcs      = new TaskCompletionSource<bool>();
            }
        }


        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(IDownloadManager);

            base.Awake();

            m_DownloadManager = FuEntry.GetModule<IDownloadManager>();
            if (m_DownloadManager == null)
            {
                Log.Fatal("Download manager is invalid.");
                return;
            }

            m_DownloadManager.DownloadStart   += OnDownloadStart;
            m_DownloadManager.DownloadUpdate  += OnDownloadUpdate;
            m_DownloadManager.DownloadSuccess += OnDownloadSuccess;
            m_DownloadManager.DownloadFailure += OnDownloadFailure;
            m_DownloadManager.FlushSize       =  m_FlushSize;
            m_DownloadManager.Timeout         =  m_Timeout;
        }

        private void Start()
        {
            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (!m_EventComponent)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (!m_InstanceRoot)
            {
                m_InstanceRoot = new GameObject("Download Agent Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            for (var i = 0; i < m_DownloadAgentHelperCount; i++)
            {
                AddDownloadAgentHelper(i);
            }
        }


        #region 获取下载任务信息

        /// <summary>
        /// 根据下载任务的序列编号获取下载任务的信息。
        /// </summary>
        /// <param name="serialId">要获取信息的下载任务的序列编号。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo GetDownloadInfo(int serialId) => m_DownloadManager.GetDownloadInfo(serialId);

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="taskTag">要获取信息的下载任务的标签。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo[] GetDownloadInfos(string taskTag) => m_DownloadManager.GetDownloadInfos(taskTag);

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="taskTag">要获取信息的下载任务的标签。</param>
        /// <param name="results">下载任务的信息。</param>
        public void GetDownloadInfos(string taskTag, List<TaskInfo> results) => m_DownloadManager.GetDownloadInfos(taskTag, results);

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <returns>所有下载任务的信息。</returns>
        public TaskInfo[] GetAllDownloadInfos() => m_DownloadManager.GetAllDownloadInfos();

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <param name="results">所有下载任务的信息。</param>
        public void GetAllDownloadInfos(List<TaskInfo> results) => m_DownloadManager.GetAllDownloadInfos(results);

        #endregion

        #region 添加下载任务

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri)
            => AddDownload(downloadPath, downloadUri, null, DefaultPriority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string taskTag)
            => AddDownload(downloadPath, downloadUri, taskTag, DefaultPriority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority)
            => AddDownload(downloadPath, downloadUri, null, priority, null);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">存储路径</param>
        /// <param name="downloadUri">下载地址</param>
        /// <returns>返回是否下载成功</returns>
        public Task<bool> Download(string downloadPath, string downloadUri)
        {
            var serialId = AddDownload(downloadPath, downloadUri, null, DefaultPriority, null);
            return m_Downloads.TryGetValue(serialId, out var value) ? value.Tcs.Task : null;
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, object userData)
            => AddDownload(downloadPath, downloadUri, null, DefaultPriority, userData);

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="taskTag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string taskTag, int priority)
            => AddDownload(downloadPath, downloadUri, taskTag, priority, null);

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
            var serialId     = m_DownloadManager.AddDownload(downloadPath, downloadUri, taskTag, priority, userData);
            var downloadData = new DownloadData(downloadUri, taskTag, serialId, userData);
            m_Downloads.TryAdd(serialId, downloadData);
            return serialId;
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
            m_Downloads.TryRemove(serialId, out _);
            return m_DownloadManager.RemoveDownload(serialId);
        }

        /// <summary>
        /// 根据下载任务的标签移除下载任务。
        /// </summary>
        /// <param name="taskTag">要移除下载任务的标签。</param>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveDownloads(string taskTag)
        {
            int serialId = -1;
            foreach (var downloadData in m_Downloads.Values)
            {
                if (downloadData.Tag == taskTag)
                {
                    serialId = downloadData.SerialId;
                    break;
                }
            }

            m_Downloads.TryRemove(serialId, out _);
            return m_DownloadManager.RemoveDownloads(taskTag);
        }

        /// <summary>
        /// 移除所有下载任务。
        /// </summary>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveAllDownloads()
        {
            m_Downloads.Clear();
            return m_DownloadManager.RemoveAllDownloads();
        }

        #endregion

        #region private methods

        /// <summary>
        /// 增加下载代理辅助器。
        /// </summary>
        /// <param name="index">下载代理辅助器索引。</param>
        private void AddDownloadAgentHelper(int index)
        {
            DownloadAgentHelperBase downloadAgentHelper = Helper.CreateHelper(m_DownloadAgentHelperTypeName, m_CustomDownloadAgentHelper, index);
            if (downloadAgentHelper == null)
            {
                Log.Error("Can not create download agent helper.");
                return;
            }

            downloadAgentHelper.name = Utility.Text.Format("Download Agent Helper - {0}", index);
            var trs = downloadAgentHelper.transform;
            trs.SetParent(m_InstanceRoot);
            trs.localScale = Vector3.one;

            m_DownloadManager.AddDownloadAgentHelper(downloadAgentHelper);
        }

        /// <summary>
        /// 下载开始事件回调
        /// </summary>
        private void OnDownloadStart(object sender, DownloadStartEventArgs e)
        {
            m_EventComponent.Fire(this, DownloadStartEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, e.CurrentLength, e.UserData));
        }

        /// <summary>
        /// 下载更新中事件回调
        /// </summary>
        private void OnDownloadUpdate(object sender, DownloadUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, DownloadUpdateEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, e.CurrentLength, e.UserData));
        }

        /// <summary>
        /// 下载成功事件回调
        /// </summary>
        private void OnDownloadSuccess(object sender, DownloadSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, DownloadSuccessEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, e.CurrentLength, e.UserData));
            if (m_Downloads.TryRemove(e.SerialId, out var value))
            {
                value.Tcs.TrySetResult(true);
            }
        }

        /// <summary>
        /// 下载失败事件回调
        /// </summary>
        private void OnDownloadFailure(object sender, DownloadFailureEventArgs e)
        {
            Log.Warning("Download failure, download serial id '{0}', download path '{1}', download uri '{2}', error message '{3}'.", e.SerialId, e.DownloadPath, e.DownloadUri, e.ErrorMessage);
            m_EventComponent.Fire(this, DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, e.ErrorMessage, e.UserData));
            if (m_Downloads.TryRemove(e.SerialId, out var value))
            {
                value.Tcs.TrySetResult(false);
            }
        }

        #endregion
    }

    /*-----------------------------Example--------------------------------------------------------------
    public class DownloadDemo : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEntry.Event.Subscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
            GameEntry.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
            GameEntry.Event.Subscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
            GameEntry.Event.Subscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
        }

        private void OnDisable()
        {
            GameEntry.Event.Unsubscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
            GameEntry.Event.Unsubscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
            GameEntry.Event.Unsubscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
            GameEntry.Event.Unsubscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
        }

        private void Start()
        {
            DownloadTest("TestImg.png", "http://xxxx.TestImg.png");
            DownloadTest("TestAudio.mp3", "http://xxxx.TestAudio.mp3");
            DownloadTest("TestDat.dat", "http://xxxx.TestDat.dat");
        }

        protected void DownloadTest(string url, string fileName)
        {
            GameEntry.Download.AddDownload(Application.persistentDataPath + "/" + fileName, url);
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
}