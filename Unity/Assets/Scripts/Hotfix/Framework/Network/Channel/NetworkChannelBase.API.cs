using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——对外 API 分部：处理器属性与注册方法、RPC 调用。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// RPC 状态
		/// </summary>
		private readonly RpcState m_rpcState;

		/// <summary>
		/// 消息发送包头处理器
		/// </summary>
		public IPacketSendHeaderHandler PacketSendHeaderHandler { get; private set; }

		/// <summary>
		/// 消息发送内容处理器
		/// </summary>
		public IPacketSendBodyHandler PacketSendBodyHandler { get; private set; }

		/// <summary>
		/// 消息接收包头处理器
		/// </summary>
		public IPacketReceiveHeaderHandler PacketReceiveHeaderHandler { get; private set; }

		/// <summary>
		/// 心跳消息处理器
		/// </summary>
		public IPacketHeartBeatHandler PacketHeartBeatHandler { get; private set; }

		/// <summary>
		/// 消息接收内容处理器
		/// </summary>
		public IPacketReceiveBodyHandler PacketReceiveBodyHandler { get; private set; }

		/// <summary>
		/// 消息压缩处理器
		/// </summary>
		public IMessageCompressHandler MessageCompressHandler { get; private set; }

		/// <summary>
		/// 消息解压处理器（仅子类解压时使用，不对外暴露）
		/// </summary>
		protected IMessageDecompressHandler MessageDecompressHandler { get; private set; }

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
			PacketSendHeaderHandler = handler;
		}


		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketSendBodyHandler handler)
		{
			handler.NotNull(nameof(handler));
			PacketSendBodyHandler = handler;
		}

		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketReceiveHeaderHandler handler)
		{
			handler.NotNull(nameof(handler));
			PacketReceiveHeaderHandler = handler;
		}

		/// <summary>
		/// 注册网络消息包处理函数。
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数。</param>
		public void RegisterHandler(IPacketReceiveBodyHandler handler)
		{
			handler.NotNull(nameof(handler));
			PacketReceiveBodyHandler = handler;
		}

		/// <summary>
		/// 注册网络消息心跳处理函数，用于处理心跳消息
		/// </summary>
		/// <param name="handler">要注册的网络消息包处理函数</param>
		public void RegisterHeartBeatHandler(IPacketHeartBeatHandler handler)
		{
			handler.NotNull(nameof(handler));
			PacketHeartBeatHandler = handler;
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
		/// 向远程主机发起 RPC 调用：发送请求并挂起等待服务器响应。
		/// 说明：
		///     1. 第一步即调用 Send 完成物理发送（复用同一条发送管线）；
		///     2. 随后按 UniqueId 向 RpcState 登记等待，收到配对回包时唤醒并返回结果；
		///        超时或断线时以错误/取消结束。
		/// 详细说明与示例见 <see cref="INetworkChannel.Call{TResponse}"/>。
		/// </summary>
		/// <typeparam name="TResponse">响应消息类型，从请求实现的 IRequestMessage&lt;TResponse&gt; 自动推断。</typeparam>
		/// <param name="messageObject">请求消息对象（须为生成器产出的、实现映射接口的 Req 消息）。</param>
		/// <returns>服务器响应消息；响应类型与 TResponse 不符时抛出异常（而非返回 null）。</returns>
		public async UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> messageObject)
			where TResponse : MessageObject, IResponseMessage
		{
			messageObject.NotNull(nameof(messageObject));
			// 生成器产出的 Req 消息均为 MessageObject 子类；显式转换以便复用发送管线与 RPC 登记。
			var request = (MessageObject)messageObject;
			Send(request);
			var result = await m_rpcState.Call(request);
			if (result is TResponse response) return response;

			// 编译期已锁定 Req→Resp 配对，此处只可能因服务器回包类型异常触发——显式失败优于原 as 的静默 null。
			throw new InvalidOperationException($"RPC 响应类型不匹配。请求:{request.GetType().Name}, 期望响应:{typeof(TResponse).Name}, 实际:{result?.GetType().Name ?? "null"}");
		}

		/// <summary>
		/// 设置忽略的消息打印列表（用于过滤高频消息——如心跳、移动同步——的调试日志，
		/// 需开启 ENABLE_NETWORK_REQ_LOG / ENABLE_NETWORK_RSP_LOG 宏后生效）。
		/// </summary>
		/// <param name="sendIds">发送消息ID忽略列表。传 null 清空。</param>
		/// <param name="receiveIds">接收消息ID忽略列表。传 null 清空。</param>
		public void SetIgnoreLogNetworkIds(List<int> sendIds, List<int> receiveIds)
		{
			m_IgnoreSendIds    = sendIds    ?? new List<int>();
			m_IgnoreReceiveIds = receiveIds ?? new List<int>();
		}
	}
}