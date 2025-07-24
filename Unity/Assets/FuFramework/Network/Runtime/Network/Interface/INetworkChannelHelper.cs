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
    /// 网络频道辅助器接口。
    /// </summary>
    public interface INetworkChannelHelper
    {
        /// <summary>
        /// 初始化网络频道辅助器。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        void Initialize(INetworkChannel networkChannel);

        /// <summary>
        /// 关闭并清理网络频道辅助器。
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 准备进行连接。
        /// </summary>
        void PrepareForConnecting();

        /// <summary>
        /// 发送心跳消息包。
        /// </summary>
        /// <returns>是否发送心跳消息包成功。</returns>
        bool SendHeartBeat();

        /// <summary>
        /// 序列化消息头
        /// </summary>
        /// <param name="messageObject">消息对象</param>
        /// <param name="destination">消息流目标对象</param>
        /// <param name="messageBodyBuffer">消息体缓冲区,用于存储序列化后的消息</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool SerializePacketHeader<T>(T messageObject, MemoryStream destination, out byte[] messageBodyBuffer) where T : MessageObject;

        /// <summary>
        /// 序列化消息包。
        /// </summary>
        /// <param name="messageBodyBuffer">要序列化的消息包。</param>
        /// <param name="destination">要序列化的目标流。</param>
        /// <returns>是否序列化成功。</returns>
        bool SerializePacketBody(byte[] messageBodyBuffer, MemoryStream destination);

        /// <summary>
        /// 反序列化消息包头。
        /// </summary>
        /// <param name="source">要反序列化的来源流。</param>
        /// <returns>反序列化后的消息包头。</returns>
        bool DeserializePacketHeader(byte[] source);

        /// <summary>
        /// 反序列化消息包。
        /// </summary>
        /// <param name="source">要反序列化的来源流。</param>
        /// <param name="messageId">消息ID</param>
        /// <param name="messageObject">消息结果对象</param>
        /// <returns>反序列化后的消息包。</returns>
        bool DeserializePacketBody(byte[] source, int messageId, out MessageObject messageObject);
    }
}