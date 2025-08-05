using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public sealed partial class DownloadManager
    {
        private sealed partial class DownloadCounter
        {
            private sealed class DownloadCounterNode : IReference
            {
                private long m_DeltaLength;
                private float m_ElapseSeconds;

                public DownloadCounterNode()
                {
                    m_DeltaLength = 0L;
                    m_ElapseSeconds = 0f;
                }

                public long DeltaLength
                {
                    get
                    {
                        return m_DeltaLength;
                    }
                }

                public float ElapseSeconds
                {
                    get
                    {
                        return m_ElapseSeconds;
                    }
                }

                public static DownloadCounterNode Create()
                {
                    return ReferencePool.Acquire<DownloadCounterNode>();
                }

                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                    m_ElapseSeconds += realElapseSeconds;
                }

                public void AddDeltaLength(int deltaLength)
                {
                    m_DeltaLength += deltaLength;
                }

                public void Clear()
                {
                    m_DeltaLength = 0L;
                    m_ElapseSeconds = 0f;
                }
            }
        }
    }
}
