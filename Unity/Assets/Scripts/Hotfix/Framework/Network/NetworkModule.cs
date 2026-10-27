using System.Net.Sockets;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络管理模块。
	/// 功能：
	///     1. 持有唯一的网络频道（单连接模型；传输实现由编译宏在创建时决定 TCP/WS）。
	///     2. 网络事件广播。
	///     3. 驱动网络频道Update轮询。
	/// </summary>
	public sealed partial class NetworkModule : ModuleBase
	{
		/// <summary>
		/// 模块单例
		/// </summary>
		public static NetworkModule Instance { get; private set; }

		/// <summary>
		/// 当前网络频道
		/// </summary>
		private NetworkChannelBase m_networkChannel;

		/// <summary>
		/// 事件组件。
		/// </summary>
		private EventModule m_eventModule;

		/// <summary>
		/// 初始化。
		/// </summary>
		protected internal override void OnInit()
		{
			Instance      = this;
			m_eventModule = ModuleManager.GetModule<EventModule>();
			if (m_eventModule == null)
			{
				FuLogger.LogFatal("[NetworkModule] 事件模块不存在!");
			}

			// 初始化协议注册表(先 Reset 保证「注册失败后重试」不残留半成品，再由生成物写入全部静态注册数据。)
			MessageIdRegistry.Reset();
			MessageRegistry.RegisterAll();
		}

		/// <summary>
		/// 帧更新。
		/// </summary>
		/// <param name="deltaTime">帧间隔时间。</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
		protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
		{
			m_networkChannel?.Update(deltaTime);
		}

		/// <summary>
		/// 释放。
		/// </summary>
		protected internal override void OnDispose()
		{
			DestroyChannel();
			Instance = null;
		}

		/// <summary>
		/// 频道连接成功回调，广播连接成功事件
		/// </summary>
		/// <param name="networkChannel">网络频道</param>
		/// <param name="userData">用户自定义数据</param>
		private void OnNetworkChannelConnected(NetworkChannelBase networkChannel, object userData)
		{
			var networkConnectedEventArgs = NetworkConnectedEventArgs.Create(networkChannel, userData);
			m_eventModule.Broadcast(this, networkConnectedEventArgs);
		}

		/// <summary>
		/// 频道关闭回调，广播连接关闭事件
		/// </summary>
		/// <param name="networkChannel">网络频道</param>
		private void OnNetworkChannelClosed(NetworkChannelBase networkChannel)
		{
			var networkClosedEventArgs = NetworkClosedEventArgs.Create(networkChannel);
			m_eventModule.Broadcast(this, networkClosedEventArgs);
		}

		/// <summary>
		/// 频道心跳丢失回调，广播心跳丢失事件
		/// </summary>
		/// <param name="networkChannel">网络频道</param>
		/// <param name="missHeartBeatCount">连续丢失次数</param>
		private void OnNetworkChannelMissHeartBeat(NetworkChannelBase networkChannel, int missHeartBeatCount)
		{
			var networkMissHeartBeatEventArgs = NetworkMissHeartBeatEventArgs.Create(networkChannel, missHeartBeatCount);
			m_eventModule.Broadcast(this, networkMissHeartBeatEventArgs);
		}

		/// <summary>
		/// 频道错误回调，广播网络错误事件
		/// </summary>
		/// <param name="networkChannel">网络频道</param>
		/// <param name="errorCode">网络错误码</param>
		/// <param name="socketErrorCode">套接字错误码</param>
		/// <param name="errorMessage">错误消息</param>
		private void OnNetworkChannelError(NetworkChannelBase networkChannel, ENetworkErrorCode errorCode, SocketError socketErrorCode,
		                                   string errorMessage)
		{
			var networkErrorEventArgs = NetworkErrorEventArgs.Create(networkChannel, errorCode, socketErrorCode, errorMessage);
			m_eventModule.Broadcast(this, networkErrorEventArgs);
		}
	}
}