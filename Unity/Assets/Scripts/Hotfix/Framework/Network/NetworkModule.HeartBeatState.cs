// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
    public sealed partial class NetworkModule
    {
        /// <summary>
        /// 心跳状态
        /// </summary>
        public sealed class HeartBeatState
        {
            /// <summary>
            /// 心跳间隔时长
            /// </summary>
            public float HeartBeatElapseSeconds { get; set; }

            /// <summary>
            /// 心跳丢失次数
            /// </summary>
            public int MissHeartBeatCount { get; set; }

            /// <summary>
            /// 重置心跳数据=>保活
            /// </summary>
            /// <param name="resetHeartBeatElapseSeconds">是否重置心跳流逝时长</param>
            public void Reset(bool resetHeartBeatElapseSeconds)
            {
                if (resetHeartBeatElapseSeconds) HeartBeatElapseSeconds = 0f;
                MissHeartBeatCount = 0;
            }
        }
    }
}
