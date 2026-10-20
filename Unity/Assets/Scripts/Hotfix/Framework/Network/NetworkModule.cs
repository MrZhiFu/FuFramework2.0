using System.Net.Sockets;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络管理模块。
	/// 功能：
	///     1. 管理网络频道。
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
		/// 所有网络频道的字典，Key为网络频道名称，Value为网络频道对象。
		/// </summary>
		private readonly Dictionary<string, NetworkChannelBase> m_networkChannelDict = new();

		/// <summary>
		/// 遍历频道字典时使用的快照缓冲，避免遍历过程中字典被修改。
		/// </summary>
		private readonly List<NetworkChannelBase> m_networkChannelSnapshot = new();

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

			// 初始化协议注册表（原 ProtoMessageIdHandler.Init 职责并入，模块生命周期保证先于一切查询）：
			// 先 Reset 保证「注册失败后重试」不残留半成品，再由生成物写入全部静态注册数据。
			ProtoMessageIdRegistry.Reset();
			ProtoMessageRegistry.RegisterAll();
		}

		/// <summary>
		/// 帧更新。
		/// </summary>
		/// <param name="deltaTime">帧间隔时间。</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
		protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
		{
			// 先快照再遍历：Update 内部会触达用户消息处理器，
			// 用户代码可能调用 DestroyNetworkChannel/CreateNetworkChannel 修改字典，
			// 直接遍历字典会抛 InvalidOperationException 中断整帧。
			m_networkChannelSnapshot.Clear();
			foreach (var networkChannel in m_networkChannelDict.Values)
			{
				m_networkChannelSnapshot.Add(networkChannel);
			}

			foreach (var channel in m_networkChannelSnapshot)
			{
				channel.Update(deltaTime, unscaledDeltaTime);
			}

			m_networkChannelSnapshot.Clear();
		}

		/// <summary>
		/// 释放。
		/// </summary>
		protected internal override void OnDispose()
		{
			// 同样先快照：Shutdown 会广播关闭事件并触达用户处理器，
			// 用户代码可能在此期间销毁/创建频道。
			m_networkChannelSnapshot.Clear();
			foreach (var networkChannel in m_networkChannelDict.Values)
			{
				m_networkChannelSnapshot.Add(networkChannel);
			}

			foreach (var networkChannelBase in m_networkChannelSnapshot)
			{
				networkChannelBase.m_NetworkChannelConnected     -= OnNetworkChannelConnected;
				networkChannelBase.m_NetworkChannelClosed        -= OnNetworkChannelClosed;
				networkChannelBase.m_NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
				networkChannelBase.m_NetworkChannelError         -= OnNetworkChannelError;
				networkChannelBase.Shutdown();
			}

			m_networkChannelSnapshot.Clear();
			m_networkChannelDict.Clear();
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
