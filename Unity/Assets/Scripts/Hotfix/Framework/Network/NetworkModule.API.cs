using System;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络管理模块的公共 API。
	/// 功能：
	///     1. 单频道模型：模块直接持有唯一网络频道，传输实现由编译宏在创建时决定（TCP/WS）。
	///     2. 业务侧通过 Call/Send 门面收发消息，不感知频道对象与频道名。
	/// </summary>
	public sealed partial class NetworkModule : ModuleBase
	{
		/// <summary>
		/// 当前网络频道。未创建时为 null。
		/// 业务侧优先使用 Call/Send 门面，仅在需要频道级配置（如 SetIgnoreLogNetworkIds）时直接触碰频道对象。
		/// </summary>
		public INetworkChannel Channel => m_networkChannel;

		#region 频道生命周期

		/// <summary>
		/// 创建网络频道（模块内唯一，重复创建抛异常）。
		/// 传输实现由编译宏决定：WebGL + WebSocket 宏（或 FORCE_ENABLE_WEB_SOCKET）→ WebSocket，否则 TCP。
		/// </summary>
		/// <param name="networkChannelHelper">网络频道辅助器（负责装配包处理器）。</param>
		/// <param name="rpcTimeout">RPC超时时间，默认5000毫秒。</param>
		/// <returns>要创建的网络频道。</returns>
		public INetworkChannel CreateChannel(NetworkChannelHelper networkChannelHelper, int rpcTimeout = 5000)
		{
			networkChannelHelper.NotNull(nameof(networkChannelHelper));
			if (m_networkChannel != null)
			{
				throw new InvalidOperationException("[NetworkModule]网络频道已存在，请先 DestroyChannel。");
			}

#if UNITY_WEBGL || FORCE_ENABLE_WEB_SOCKET
			NetworkChannelBase networkChannel = new WebSocketNetworkChannel(networkChannelHelper, rpcTimeout);
#else
			NetworkChannelBase networkChannel = new SystemTcpNetworkChannel(networkChannelHelper, rpcTimeout);
#endif
			networkChannel.m_NetworkChannelConnected     += OnNetworkChannelConnected;
			networkChannel.m_NetworkChannelClosed        += OnNetworkChannelClosed;
			networkChannel.m_NetworkChannelMissHeartBeat += OnNetworkChannelMissHeartBeat;
			networkChannel.m_NetworkChannelError         += OnNetworkChannelError;
			m_networkChannel                             =  networkChannel;
			return networkChannel;
		}

		/// <summary>
		/// 销毁网络频道（Dispose 终态）。
		/// 内部先置空引用再 Dispose：Dispose 触发的 Closed 事件回调里若再 CreateChannel，模块状态依然一致（消除重入竞态）。
		/// </summary>
		/// <returns>是否销毁成功（无频道时返回 false）。</returns>
		public void DestroyChannel()
		{
			if (m_networkChannel == null) return;

			var networkChannel = m_networkChannel;
			m_networkChannel                             =  null;
			networkChannel.m_NetworkChannelConnected     -= OnNetworkChannelConnected;
			networkChannel.m_NetworkChannelClosed        -= OnNetworkChannelClosed;
			networkChannel.m_NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
			networkChannel.m_NetworkChannelError         -= OnNetworkChannelError;
			networkChannel.Dispose();
		}

		/// <summary>
		/// 获取当前频道；未创建时抛出明确异常（区别于空引用异常）。
		/// </summary>
		private NetworkChannelBase GetChannel()
		{
			if (m_networkChannel == null)
			{
				throw new InvalidOperationException("[NetworkModule]网络频道未创建，请先创建网络频道。");
			}

			return m_networkChannel;
		}

		/// <summary>
		/// 连接服务器（转发当前频道 Connect；频道须已创建）。
		/// </summary>
		/// <param name="address">服务器地址。</param>
		/// <param name="userData">用户自定义数据（随连接成功事件回传）。</param>
		public void Connect(Uri address, object userData = null) => GetChannel().Connect(address, userData);

		/// <summary>
		/// 关闭连接（可重连，转发当前频道 Close）。频道未创建时静默。
		/// </summary>
		public void Close() => m_networkChannel?.Close();

		#endregion

		#region 消息发送

		/// <summary>
		/// 向服务器发起 RPC 调用：发送请求并挂起等待响应（转发当前频道 Call；
		/// Req→Resp 编译期配对与响应类型推断同频道接口，业务侧无需感知频道）。
		/// </summary>
		/// <typeparam name="TResponse">响应消息类型，从请求实现的 IRequestMessage&lt;TResponse&gt; 自动推断。</typeparam>
		/// <param name="msgObj">请求消息对象。</param>
		/// <returns>服务器响应消息；响应类型与 TResponse 不符时抛出异常。</returns>
		public UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> msgObj) where TResponse : MessageObject, IResponseMessage
			=> GetChannel().Call(msgObj);

		/// <summary>
		/// 向服务器单向发送消息（发完即忘，转发当前频道 Send）。
		/// </summary>
		/// <typeparam name="T">消息包类型。</typeparam>
		/// <param name="msgObj">要发送的消息包。</param>
		public void Send<T>(T msgObj) where T : MessageObject => GetChannel().Send(msgObj);

		/// <summary>
		/// 挂起推送派发：此后到达的推送进入缓冲队列（保持到达顺序，上限 1024 条，超出丢弃最旧并警告）。
		/// 用于「连接已建立但业务基线未就绪」的窗口期——推送多为增量更新，
		/// 在登录/重连流程拉取全量数据前挂起，拉取完成后调 <see cref="ResumeNotifyDispatch"/> 按序回放。
		/// 幂等：已挂起时重复调用无副作用。
		/// </summary>
		public void SuspendNotifyDispatch() => NotifyMessageHandlerRegistry.SuspendDispatch();

		/// <summary>
		/// 恢复推送派发并按到达顺序回放缓冲队列。
		/// 幂等：未挂起时调用无副作用。回放过程中可再次挂起（此时剩余消息留在队列）。
		/// </summary>
		public void ResumeNotifyDispatch() => NotifyMessageHandlerRegistry.ResumeDispatch();

		/// <summary>
		/// 清空推送缓冲队列（不派发）。用于登出/切换账号时丢弃旧会话的推送，防止串号。
		/// </summary>
		public void ClearNotifyBuffer() => NotifyMessageHandlerRegistry.ClearBuffer();

		#endregion
	}
}