using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public sealed partial class DownloadManager
    {
        /// <summary>
        /// 下载计数器
        /// </summary>
        private sealed partial class DownloadCounter
        {
            /// 下载计数器链表容器
            private readonly FuLinkedList<DownloadCounterNode> m_DownloadCounterNodeList;

            /// 更新间隔(秒)
            private float m_UpdateInterval;

            /// 记录间隔(秒)
            private float m_RecordInterval;

            /// 计数累加器
            private float m_Accumulator;

            /// 剩余时间(秒)
            private float m_TimeLeft;

            /// 当前下载速度
            public float CurrentSpeed { get; private set; }

            /// 更新间隔(秒)
            // ReSharper disable once UnusedMember.Local
            public float UpdateInterval
            {
                get => m_UpdateInterval;
                set
                {
                    if (value <= 0f) throw new FuException("Update interval is invalid.");
                    m_UpdateInterval = value;
                    Reset();
                }
            }

            /// 记录间隔(秒)
            // ReSharper disable once UnusedMember.Local
            public float RecordInterval
            {
                get => m_RecordInterval;
                set
                {
                    if (value <= 0f) throw new FuException("Record interval is invalid.");
                    m_RecordInterval = value;
                    Reset();
                }
            }

            /// <summary>
            /// 构造一个下载计数器
            /// </summary>
            /// <param name="updateInterval">更新间隔(秒，默认为1秒1次)</param>
            /// <param name="recordInterval">记录间隔(秒，默认为10秒1次)</param>
            public DownloadCounter(float updateInterval, float recordInterval)
            {
                if (updateInterval <= 0f) throw new FuException("Update interval is invalid.");
                if (recordInterval <= 0f) throw new FuException("Record interval is invalid.");

                m_DownloadCounterNodeList = new FuLinkedList<DownloadCounterNode>();

                m_UpdateInterval = updateInterval;
                m_RecordInterval = recordInterval;

                Reset();
            }

            /// <summary>
            /// 关闭清理
            /// </summary>
            public void Shutdown() => Reset();

            /// <summary>
            /// 下载计数器轮询
            /// </summary>
            /// <param name="elapseSeconds">逻辑帧间隔流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (m_DownloadCounterNodeList.Count <= 0) return;

                m_Accumulator += realElapseSeconds;
                if (m_Accumulator > m_RecordInterval)
                    m_Accumulator = m_RecordInterval;

                m_TimeLeft -= realElapseSeconds;
                foreach (var downloadCounterNode in m_DownloadCounterNodeList)
                {
                    downloadCounterNode.Update(elapseSeconds, realElapseSeconds);
                }

                while (m_DownloadCounterNodeList.Count > 0)
                {
                    var downloadCounterNode = m_DownloadCounterNodeList.First.Value;
                    if (downloadCounterNode.ElapseSeconds < m_RecordInterval) break;

                    ReferencePool.Runtime.ReferencePool.Release(downloadCounterNode);
                    m_DownloadCounterNodeList.RemoveFirst();
                }

                if (m_DownloadCounterNodeList.Count <= 0)
                {
                    Reset();
                    return;
                }

                if (m_TimeLeft <= 0f)
                {
                    var totalDeltaLength = 0L;
                    foreach (var downloadCounterNode in m_DownloadCounterNodeList)
                    {
                        totalDeltaLength += downloadCounterNode.DeltaLength;
                    }

                    CurrentSpeed =  m_Accumulator > 0f ? totalDeltaLength / m_Accumulator : 0f;
                    m_TimeLeft   += m_UpdateInterval;
                }
            }

            /// <summary>
            /// 记录差值大小
            /// </summary>
            /// <param name="deltaLength">差值大小</param>
            public void RecordDeltaLength(int deltaLength)
            {
                if (deltaLength <= 0) return;

                DownloadCounterNode downloadCounterNode;
                if (m_DownloadCounterNodeList.Count > 0)
                {
                    downloadCounterNode = m_DownloadCounterNodeList.Last.Value;
                    if (downloadCounterNode.ElapseSeconds < m_UpdateInterval)
                    {
                        downloadCounterNode.AddDeltaLength(deltaLength);
                        return;
                    }
                }

                downloadCounterNode = DownloadCounterNode.Create();
                downloadCounterNode.AddDeltaLength(deltaLength);
                m_DownloadCounterNodeList.AddLast(downloadCounterNode);
            }

            private void Reset()
            {
                m_DownloadCounterNodeList.Clear();
                CurrentSpeed  = 0f;
                m_Accumulator = 0f;
                m_TimeLeft    = 0f;
            }
        }
    }
}
