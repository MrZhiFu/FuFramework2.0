using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——对外 API 分部：处理器属性与注册方法、RPC 调用与回调设置、统计属性。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// RPC 状态
		/// </summary>
		protected readonly RpcState m_RpcState;

		/// <summary>
		/// 发送包头处理器
		/// </summary>
		private IPacketSendHeaderHandler m_packetSendHeaderHandler;

		/// <summary>
		/// 发送包体处理器
		/// </summary>
		private IPacketSendBodyHandler m_packetSendBodyHandler;

		/// <summary>
		/// 接收包头处理器
		/// </summary>
		private IPacketReceiveHeaderHandler m_packetReceiveHeaderHandler;

		/// <summary>
		/// 接收包体处理器
		/// </summary>
		private IPacketReceiveBodyHandler m_packetReceiveBodyHandler;

		/// <summary>
		/// 心跳处理器
		/// </summary>
		private IPacketHeartBeatHandler m_packetHeartBeatHandler;

		/// <summary>
		/// 消息发送包头处理器
		/// </summary>
		public IPacketSendHeaderHandler PacketSendHeaderHandler => m_packetSendHeaderHandler;

		/// <summary>
		/// 消息发送内容处理器
		/// </summary>
		public IPacketSendBodyHandler PacketSendBodyHandler => m_packetSendBodyHandler;

		/// <summary>
		/// 消息接收包头处理器
		/// </summary>
		public IPacketReceiveHeaderHandler PacketReceiveHeaderHandler => m_packetReceiveHeaderHandler;

		/// <summary>
		/// 心跳消息处理器
		/// </summary>
		public IPacketHeartBeatHandler PacketHeartBeatHandler => m_packetHeartBeatHandler;

		/// <summary>
		/// 消息接收内容处理器
		/// </summary>
		public IPacketReceiveBodyHandler PacketReceiveBodyHandler => m_packetReceiveBodyHandler;

		/// <summary>
		/// 消息压缩处理器
		/// </summary>
		public IMessageCompressHandler MessageCompressHandler { get; private set; }

		/// <summary>
		/// 消息解压处理器
		/// </summary>
		public IMessageDecompressHandler MessageDecompressHandler { get; private set; }

		/// <summary>
		/// 获取要发送的消息包数量。
		/// </summary>
		public int SendPacketCount
		{
			get
			{
				lock (m_SendPacketPool)
					return m_SendPacketPool.Count;
			}
		}

		/// <summary>
		/// 获取累计发送的消息包数量。
		/// </summary>
		public int SentPacketCount => m_SentPacketCount;

		/// <summary>
		/// 获取累计已接收的消息包数量。
		/// </summary>
		public int ReceivedPacketCount => m_ReceivedPacketCount;

		/// <summary>
		/// 注册消息压缩处理器
		/// </summary>
		/// <param name="handler">处理器对象,当设置为空的时候，不启用消息压缩</param>
		public void RegisterMessageCompressHandler(IMessageCompressHandler handler)
		{
			MessageCompressHandler = handler;
		}

		/// <summary>
		/// 注册消息解压处理器
		/// </summary>
		/// <param name="handler">处理器对象,当设置为空的时候，不启用消息解压</param>
		public void RegisterMessageDecompressHandler(IMessageDecompressHandler handler)
		{
			MessageDecompressHandler = handler;
		}

		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketSendHeaderHandler handler)
		{
			handler.NotNull(nameof(handler));
			m_packetSendHeaderHandler = handler;
		}


		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketSendBodyHandler handler)
		{
			handler.NotNull(nameof(handler));
			m_packetSendBodyHandler = handler;
		}

		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketReceiveHeaderHandler handler)
		{
			handler.NotNull(nameof(handler));
			m_packetReceiveHeaderHandler = handler;
		}

		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketReceiveBodyHandler handler)
		{
			handler.NotNull(nameof(handler));
			m_packetReceiveBodyHandler = handler;
		}

		/// <summary>
		/// 注册网络消息心跳处理函数，用于处理心跳消息
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数</param>
		[Obsolete("Use RegisterHeartBeatHandler instead")]
		public void RegisterHandler(IPacketHeartBeatHandler handler)
		{
			RegisterHeartBeatHandler(handler);
		}

		/// <summary>
		/// 注册网络消息心跳处理函数，用于处理心跳消息
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数</param>
		public void RegisterHeartBeatHandler(IPacketHeartBeatHandler handler)
		{
			handler.NotNull(nameof(handler));
			m_packetHeartBeatHandler = handler;
			if (handler.HeartBeatInterval > 0)
			{
				m_HeartBeatInterval = handler.HeartBeatInterval;
			}

			if (handler.MissHeartBeatCountByClose > 0)
			{
				m_MissHeartBeatCountByClose = handler.MissHeartBeatCountByClose;
			}
		}

		/// <summary>
		/// 设置RPC 的 ErrorCode 不为 0 的时候的处理函数
		/// </summary>
		/// <param name="handler"></param>
		public void SetRPCErrorCodeHandler(EventHandler<MessageObject> handler)
		{
			handler.NotNull(nameof(handler));
			m_RpcState.SetRPCErrorCodeHandler(handler);
		}

		/// <summary>
		/// 设置RPC错误的处理函数
		/// </summary>
		/// <param name="handler"></param>
		public void SetRPCErrorHandler(EventHandler<MessageObject> handler)
		{
			handler.NotNull(nameof(handler));
			m_RpcState.SetRPCErrorHandler(handler);
		}

		/// <summary>
		/// 设置RPC开始的处理函数
		/// </summary>
		/// <param name="handler"></param>
		public void SetRPCStartHandler(EventHandler<MessageObject> handler)
		{
			handler.NotNull(nameof(handler));
			m_RpcState.SetRPCStartHandler(handler);
		}

		/// <summary>
		/// 设置RPC结束的处理函数
		/// </summary>
		/// <param name="handler"></param>
		public void SetRPCEndHandler(EventHandler<MessageObject> handler)
		{
			handler.NotNull(nameof(handler));
			m_RpcState.SetRPCEndHandler(handler);
		}

		/// <summary>
		/// 向远程主机发送消息包
		/// </summary>
		/// <param name="messageObject"></param>
		/// <typeparam name="TResult"></typeparam>
		public async UniTask<TResult> Call<TResult>(MessageObject messageObject) where TResult : MessageObject, IResponseMessage
		{
			messageObject.NotNull(nameof(messageObject));
			Send(messageObject);
			var result = await m_RpcState.Call(messageObject);
			return result as TResult;
		}

		/// <summary>
		/// 设置忽略的消息打印列表（用于过滤高频消息——如心跳、移动同步——的调试日志，
		/// 需开启 ENABLE_NETWORK_REQ_LOG / ENABLE_NETWORK_RSP_LOG 宏后生效）。
		/// </summary>
		/// <param name="sendIds">发送消息ID忽略列表。传 null 清空。</param>
		/// <param name="receiveIds">接收消息ID忽略列表。传 null 清空。</param>
		public void SetIgnoreLogNetworkIds(List<int> sendIds, List<int> receiveIds)
		{
			m_IgnoreSendIds    = sendIds ?? new List<int>();
			m_IgnoreReceiveIds = receiveIds ?? new List<int>();
		}
	}
}
