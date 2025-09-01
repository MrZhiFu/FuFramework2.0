using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public sealed partial class DownloadManager
    {
        private sealed partial class DownloadCounter
        {
            /// <summary>
            /// 下载计数器节点
            /// </summary>
            private sealed class DownloadCounterNode : IReference
            {
                /// 差值大小
                public long DeltaLength { get; private set; }

                /// 下载逻辑流逝时间，无时间缩放效果，以秒为单位
                public float ElapseSeconds { get; private set; }

                public static DownloadCounterNode Create()
                {
                    return ReferencePool.Acquire<DownloadCounterNode>();
                }

                /// <summary>
                /// 帧更新
                /// </summary>
                /// <param name="elapseSeconds">逻辑帧间隔流逝时间，以秒为单位。</param>
                /// <param name="realElapseSeconds">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
                // ReSharper disable once UnusedParameter.Local
                public void Update(float elapseSeconds, float realElapseSeconds) => ElapseSeconds += realElapseSeconds;

                /// <summary>
                /// 累加差值大小
                /// </summary>
                /// <param name="deltaLength">差值大小</param>
                public void AddDeltaLength(int deltaLength) => DeltaLength += deltaLength;

                /// <summary>
                /// 清理
                /// </summary>
                public void Clear()
                {
                    DeltaLength   = 0L;
                    ElapseSeconds = 0f;
                }
            }
        }
    }
}