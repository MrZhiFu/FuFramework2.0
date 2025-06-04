﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.Network.Runtime
{
    /// <summary>
    /// 网络连接成功事件。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class NetworkConnectedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 网络连接成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(NetworkConnectedEventArgs).FullName;

        /// <summary>
        /// 获取网络连接成功事件编号。
        /// </summary>
        public override string Id
        {
            get { return EventId; }
        }

        /// <summary>
        /// 初始化网络连接成功事件的新实例。
        /// </summary>
        public NetworkConnectedEventArgs()
        {
            NetworkChannel = null;
            UserData = null;
        }

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        public INetworkChannel NetworkChannel { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建网络连接成功事件。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的网络连接成功事件。</returns>
        public static NetworkConnectedEventArgs Create(INetworkChannel networkChannel, object userData)
        {
            NetworkConnectedEventArgs networkConnectedEventArgs = ReferencePool.Acquire<NetworkConnectedEventArgs>();
            networkConnectedEventArgs.NetworkChannel = networkChannel;
            networkConnectedEventArgs.UserData = userData;
            return networkConnectedEventArgs;
        }

        /// <summary>
        /// 清理网络连接成功事件。
        /// </summary>
        public override void Clear()
        {
            NetworkChannel = null;
            UserData = null;
        }
    }
}