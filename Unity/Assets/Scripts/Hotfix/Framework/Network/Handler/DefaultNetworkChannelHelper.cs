using System;
using System.Collections.Generic;
using System.IO;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Event;


// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 默认网络频道帮助器。
	/// 功能：
	///     1. 初始化网络频道帮助器。
	///     2. 显式装配包和包处理函数（不再反射扫描程序集，见 <see cref="RegisterDefaultHandlers"/>）。
	///     3. 发送默认的包和包处理函数。
	/// </summary>
	public class DefaultNetworkChannelHelper
	{
		/// <summary>
		/// 网络频道
		/// </summary>
		private INetworkChannel m_networkChannel;

		/// <summary>
		/// 获取事件组件。
		/// </summary>
		public static EventModule Event
		{
			get { return s_event ??= ModuleManager.GetModule<EventModule>(); }
		}

		/// <summary>
		/// 事件组件实例
		/// </summary>
		private static EventModule s_event;

		/// <summary>
		/// 自定义包处理器注册委托集合（显式注册入口）。
		///
		/// 说明（项目铁律 4：运行时杜绝反射）：
		///     原实现通过 <c>UtilityAOT.Assembly.GetTypes()</c> 扫描全部已加载程序集 + 接口判断 +
		///     <c>Activator.CreateInstance</c> 来发现并创建包处理器，现已改为
		///     「显式装配框架自带的 7 个处理器」（见 <see cref="RegisterDefaultHandlers"/>）。
		///     因此，**新增自定义处理器必须显式注册**，不再靠反射扫描自动发现：
		///         1) 调用 <see cref="AddCustomHandlerRegistrar"/> 注册装配委托；或
		///         2) 派生本类并重写 <see cref="RegisterCustomHandlers"/>。
		///     处理器实例按频道创建（处理器内部各自持有解析状态，不能跨频道共享）。
		///
		/// 仅限启动期调用，之后不再变更。
		/// </summary>
		private static readonly List<Action<INetworkChannel>> sr_customHandlerRegistrars = new();

		/// <summary>
		/// 自定义处理器注册表锁对象
		/// </summary>
		private static readonly object sr_customHandlerRegistrarsLock = new();

		/// <summary>
		/// 显式注册一个自定义包处理器装配委托。
		/// 委托会在每个频道 Initialize 时、框架默认处理器装配完成之后被调用。
		/// </summary>
		/// <param name="registrar">装配委托，参数为待装配的网络频道</param>
		public static void AddCustomHandlerRegistrar(Action<INetworkChannel> registrar)
		{
			registrar.NotNull(nameof(registrar));
			lock (sr_customHandlerRegistrarsLock)
			{
				sr_customHandlerRegistrars.Add(registrar);
			}
		}

		/// <summary>
		/// 装配框架自带的默认包处理器。
		///
		/// 说明（项目铁律 4）：原实现由反射扫描全部已加载程序集发现 IPacketHandler 实现并逐个实例化，
		/// 现改为显式列出。下表即原扫描结果中的框架侧实现类（框架外的实现由生成物追加注册并覆盖，见下）：
		///     接收包头 → DefaultPacketReceiveHeaderHandler
		///     接收包体 → DefaultPacketReceiveBodyHandler
		///     发送包头 → DefaultPacketSendHeaderHandler
		///     发送包体 → DefaultPacketSendBodyHandler
		///     心跳    → DefaultPacketHeartBeatHandler（兜底，可被框架外实现覆盖）
		///     压缩    → DefaultMessageCompressHandler
		///     解压    → DefaultMessageDecompressHandler
		///
		/// 心跳处理器：此处装配的是框架基类 DefaultPacketHeartBeatHandler，仅作为**兜底默认**。
		///     原实现靠反射扫描，会同时命中基类与该基类的游戏侧派生类
		///     Hotfix.Game.Network.PacketHeartBeatHandler；由于 RegisterHeartBeatHandler 是
		///     「后注册覆盖」，而编译产物中基类排在派生类之后，框架扫描的结果是**基类生效**
		///     （基类 Handler() 仅抛 NotImplementedException）。
		///     此前运行期之所以正常，是因为游戏侧 WinPlayerList.OnBtnLoginClick 在
		///     CreateChannel（内部即触发本方法）之后又手动注册了一次该派生类。
		///     现由生成物 Register/AutoGen/MessageRegistry.g.cs 登记装配委托，在本方法之后追加注册
		///     框架外的具体处理器（见 <see cref="AddCustomHandlerRegistrar"/>），
		///     使框架侧自洽、不再依赖游戏侧的手动补注册；最终生效者仍是游戏侧实现（后注册者生效）。
		/// </summary>
		protected virtual void RegisterDefaultHandlers()
		{
			var channel = m_networkChannel;

			channel.RegisterHandler(new DefaultPacketReceiveHeaderHandler());
			channel.RegisterHandler(new DefaultPacketReceiveBodyHandler());
			channel.RegisterHandler(new DefaultPacketSendHeaderHandler());
			channel.RegisterHandler(new DefaultPacketSendBodyHandler());
			channel.RegisterHeartBeatHandler(new DefaultPacketHeartBeatHandler());
			channel.RegisterMessageCompressHandler(new DefaultMessageCompressHandler());
			channel.RegisterMessageDecompressHandler(new DefaultMessageDecompressHandler());
		}

		/// <summary>
		/// 自定义处理器装配钩子。派生类可重写以补充或替换包处理器。
		/// 在框架默认处理器装配之后调用。
		/// </summary>
		protected virtual void RegisterCustomHandlers() { }

		/// <summary>
		/// 初始化网络频道帮助器。
		/// </summary>
		/// <param name="netChannel"></param>
		public void Initialize(INetworkChannel netChannel)
		{
			m_networkChannel = netChannel;

			// 显式装配包处理器：框架默认 7 个 → 派生类钩子 → 显式注册的自定义委托。
			// 新增自定义处理器必须显式注册，不再依赖反射扫描程序集。
			RegisterDefaultHandlers();
			RegisterCustomHandlers();

			lock (sr_customHandlerRegistrarsLock)
			{
				foreach (var handler in sr_customHandlerRegistrars)
				{
					handler?.Invoke(m_networkChannel);
				}
			}

			Event.Subscribe(NetworkConnectedEventArgs.EventId,     OnNetworkConnectedEventArgs);
			Event.Subscribe(NetworkClosedEventArgs.EventId,        OnNetworkClosedEventArgs);
			Event.Subscribe(NetworkMissHeartBeatEventArgs.EventId, OnNetworkMissHeartBeatEventArgs);
			Event.Subscribe(NetworkErrorEventArgs.EventId,         OnNetworkErrorEventArgs);
		}

		/// <summary>
		/// 关闭网络频道帮助器，退订网络事件
		/// </summary>
		public void Dispose()
		{
			Event.Unsubscribe(NetworkConnectedEventArgs.EventId,     OnNetworkConnectedEventArgs);
			Event.Unsubscribe(NetworkClosedEventArgs.EventId,        OnNetworkClosedEventArgs);
			Event.Unsubscribe(NetworkMissHeartBeatEventArgs.EventId, OnNetworkMissHeartBeatEventArgs);
			Event.Unsubscribe(NetworkErrorEventArgs.EventId,         OnNetworkErrorEventArgs);
			m_networkChannel = null;
		}

		/// <summary>
		/// 连接前的准备：设置 Socket 收发缓冲区大小
		/// </summary>
		public void PrepareForConnecting()
		{
			m_networkChannel.Socket.ReceiveBufferSize = 1024 * 64 - 1;
			m_networkChannel.Socket.SendBufferSize    = 1024 * 64 - 1;
		}

		/// <summary>
		/// 发送心跳包。由频道的定时心跳检查触发
		/// </summary>
		/// <returns>是否已发起发送</returns>
		public bool SendHeartBeat()
		{
			var message = m_networkChannel.PacketHeartBeatHandler.Handler();
			m_networkChannel.Send(message);
			return true;
		}

		/// <summary>
		/// 序列化发送包头
		/// </summary>
		/// <param name="msgObj">待发送的消息对象</param>
		/// <param name="destination">包头写入目标流</param>
		/// <param name="messageBodyBuffer">输出：待发送的消息体缓冲区</param>
		/// <returns>是否序列化成功</returns>
		public bool SerializePacketHeader<T>(T msgObj, MemoryStream destination, out byte[] messageBodyBuffer) where T : MessageObject
		{
			m_networkChannel.NotNull(nameof(m_networkChannel));
			m_networkChannel.PacketSendHeaderHandler.NotNull(nameof(m_networkChannel.PacketSendHeaderHandler));
			msgObj.NotNull(nameof(msgObj));
			destination.NotNull(nameof(destination));

			return m_networkChannel.PacketSendHeaderHandler.Handler(msgObj, m_networkChannel.MessageCompressHandler, destination, out messageBodyBuffer);
		}

		/// <summary>
		/// 序列化发送包体
		/// </summary>
		/// <param name="messageBodyBuffer">消息体缓冲区</param>
		/// <param name="destination">包体写入目标流</param>
		/// <returns>是否序列化成功</returns>
		public bool SerializePacketBody(byte[] messageBodyBuffer, MemoryStream destination)
		{
			m_networkChannel.NotNull(nameof(m_networkChannel));
			m_networkChannel.PacketSendHeaderHandler.NotNull(nameof(m_networkChannel.PacketSendHeaderHandler));
			m_networkChannel.PacketSendBodyHandler.NotNull(nameof(m_networkChannel.PacketSendBodyHandler));
			messageBodyBuffer.NotNull(nameof(messageBodyBuffer));
			destination.NotNull(nameof(destination));

			return m_networkChannel.PacketSendBodyHandler.Handler(messageBodyBuffer, destination);
		}

		/// <summary>
		/// 反序列化接收包头
		/// </summary>
		/// <param name="source">包头原始字节</param>
		/// <returns>是否解析成功</returns>
		public bool DeserializePacketHeader(byte[] source)
		{
			source.NotNull(nameof(source));
			return m_networkChannel.PacketReceiveHeaderHandler.Handler(source);
		}

		/// <summary>
		/// 反序列化接收包体
		/// </summary>
		/// <param name="source">包体原始字节</param>
		/// <param name="messageId">消息 ID</param>
		/// <param name="msgObj">输出：反序列化得到的消息对象</param>
		/// <returns>是否解析成功</returns>
		public bool DeserializePacketBody(byte[] source, int messageId, out MessageObject msgObj)
		{
			source.NotNull(nameof(source));
			return m_networkChannel.PacketReceiveBodyHandler.Handler(source, messageId, out msgObj);
		}

		/// <summary>
		/// 网络连接成功事件回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">事件参数</param>
		private void OnNetworkConnectedEventArgs(object sender, GameEventArgs e)
		{
			if (e is not NetworkConnectedEventArgs ne || ne.NetworkChannel != m_networkChannel) return;
			FuLogger.LogInfo("网络连接成功");
		}

		/// <summary>
		/// 网络连接关闭事件回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">事件参数</param>
		private void OnNetworkClosedEventArgs(object sender, GameEventArgs e)
		{
			if (e is not NetworkClosedEventArgs ne || ne.NetworkChannel != m_networkChannel) return;
			FuLogger.LogInfo("网络连接关闭");
		}

		/// <summary>
		/// 网络心跳丢失事件回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">事件参数</param>
		private void OnNetworkMissHeartBeatEventArgs(object sender, GameEventArgs e)
		{
			if (e is not NetworkMissHeartBeatEventArgs ne || ne.NetworkChannel != m_networkChannel) return;
			FuLogger.LogWarning($"网络心跳连续丢失 {ne.MissCount} 次。");
		}

		/// <summary>
		/// 网络错误事件回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">事件参数</param>
		private void OnNetworkErrorEventArgs(object sender, GameEventArgs e)
		{
			if (e is not NetworkErrorEventArgs ne || ne.NetworkChannel != m_networkChannel) return;
			FuLogger.LogError($"网络错误，错误码：'{ne.ErrorCode}'，错误消息：'{ne.ErrorMessage}'。");
			ne.NetworkChannel.Close();
		}
	}
}