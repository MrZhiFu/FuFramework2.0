﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;

namespace GameFrameX.Network.Runtime
{
    /// <summary>
    /// 网络消息包处理器接口。
    /// </summary>
    public interface IPacketSendBodyHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageBodyBuffer"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        bool Handler(byte[] messageBodyBuffer, MemoryStream destination);
    }
}