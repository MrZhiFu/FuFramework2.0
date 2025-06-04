﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFrameX.Network.Runtime
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 心跳状态
        /// </summary>
        public sealed class HeartBeatState
        {
            private float _heartBeatElapseSeconds;
            private int _missHeartBeatCount;

            [UnityEngine.Scripting.Preserve]
            public HeartBeatState()
            {
                _heartBeatElapseSeconds = 0f;
                _missHeartBeatCount = 0;
            }

            /// <summary>
            /// 心跳间隔时长
            /// </summary>
            public float HeartBeatElapseSeconds
            {
                get { return _heartBeatElapseSeconds; }
                set { _heartBeatElapseSeconds = value; }
            }

            /// <summary>
            /// 心跳丢失次数
            /// </summary>
            public int MissHeartBeatCount
            {
                get { return _missHeartBeatCount; }
                set { _missHeartBeatCount = value; }
            }

            /// <summary>
            /// 重置心跳数据=>保活
            /// </summary>
            /// <param name="resetHeartBeatElapseSeconds">是否重置心跳流逝时长</param>
            public void Reset(bool resetHeartBeatElapseSeconds)
            {
                if (resetHeartBeatElapseSeconds)
                {
                    _heartBeatElapseSeconds = 0f;
                }

                _missHeartBeatCount = 0;
            }
        }
    }
}