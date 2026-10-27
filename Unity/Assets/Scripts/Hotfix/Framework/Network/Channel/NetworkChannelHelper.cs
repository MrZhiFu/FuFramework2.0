using System;
using System.Collections.Generic;
using System.IO;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Event;
using ProtoBuf;


// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道帮助器。
	/// 功能：
	///     1. 初始化网络频道帮助器。
	///     2. 信封读写：包头的序列化写入（含压缩判定）与解析读取（含接收状态缓存）。
	///     3. 显式注册外部包处理器装配委托（含生成物注册的游戏心跳）。
	/// </summary>
	public class NetworkChannelHelper
	{
		/// <summary>
		/// 包头长度：4(总长) + 1(操作类型) + 1(压缩标记) + 4(UniqueId) + 4(消息ID) = 14 字节
		/// </summary>
		private const int PACKET_HEADER_LENGTH = 14;

		/// <summary>
		/// 消息体超过该长度启用压缩
		/// </summary>
		private const int LIMIT_COMPRESS_LENGTH = 512;

		/// <summary>
		/// 包头缓存字节（避免每次发包重新分配）
		/// </summary>
		private readonly byte[] m_cachedByte = new byte[PACKET_HEADER_LENGTH];

		/// <summary>
		/// 包头缓存的写入偏移
		/// </summary>
		private int m_offset;

		/// <summary>
		/// 当前发送消息的 ID（供发送调试日志读取）
		/// </summary>
		private int m_currentSentId;

		/// <summary>
		/// 当前接收消息的包长（含 14 字节包头）
		/// </summary>
		private uint m_receivedPacketLength;

		/// <summary>
		/// 当前接收消息的 ID（供接收调试日志与包体反序列化读取）
		/// </summary>
		private int m_receivedId;

		/// <summary>
		/// 当前接收消息的 UniqueId（供收包侧还原 RPC 配对单号）
		/// </summary>
		private int m_receivedUniqueId;

		/// <summary>
		/// 当前接收消息的压缩标记
		/// </summary>
		private byte m_receivedZipFlag;

		/// <summary>
		/// 单个数据包的包体长度上限（默认 1 MiB，<b>可按协议需要调整</b>）。
		/// 包头中的 PacketLength 直接来自网络报文，若不设上限，畸形包会导致超大数组分配/内存耗尽。
		/// </summary>
		public static int MaxPacketBodyLength { get; set; } = 1024 * 1024;

		/// <summary>
		/// 网络频道
		/// </summary>
		private INetworkChannel m_networkChannel;

		/// <summary>
		/// 当前发送消息的 ID（发送调试日志过滤用）
		/// </summary>
		public int LastSentMessageId => m_currentSentId;

		/// <summary>
		/// 当前接收消息的 ID（接收调试日志过滤与包体反序列化用）
		/// </summary>
		public int LastReceivedMessageId => m_receivedId;

		/// <summary>
		/// 当前接收消息的 UniqueId（收包侧还原 RPC 配对单号用）
		/// </summary>
		public int LastReceivedUniqueId => m_receivedUniqueId;

		/// <summary>
		/// 当前接收消息的压缩标记（包体期解压判定用，跨收包批次存活）
		/// </summary>
		public byte LastReceivedZipFlag => m_receivedZipFlag;

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
		///     「显式装配框架自带的 6 个处理器」（见 <see cref="RegisterHandlers"/>）。
		///     因此，**新增自定义处理器必须显式注册**（调用 <see cref="RegisterExternalPacketHandler"/>），
		///     不再靠反射扫描自动发现。
		///     处理器实例按频道创建（处理器内部各自持有解析状态，不能跨频道共享）。
		///
		/// 仅限启动期调用，之后不再变更。
		/// </summary>
		private static readonly List<Action<INetworkChannel>> sr_externalPacketHandlerList = new();

		/// <summary>
		/// 自定义处理器注册表锁对象
		/// </summary>
		private static readonly object sr_externalPacketHandlerRegistrarsLock = new();

		/// <summary>
		/// 初始化网络频道帮助器。
		/// </summary>
		/// <param name="netChannel"></param>
		public void Initialize(INetworkChannel netChannel)
		{
			m_networkChannel = netChannel;

			// 显式注册的自定义委托（含生成物注册的游戏心跳）。
			// 新增自定义处理器必须显式注册，不再依赖反射扫描程序集。
			lock (sr_externalPacketHandlerRegistrarsLock)
			{
				foreach (var handler in sr_externalPacketHandlerList)
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
		/// 显式注册一个自定义包处理器装配委托。
		/// 委托会在每个频道 Initialize 时、框架默认处理器装配完成之后被调用。
		/// </summary>
		/// <param name="registrar">装配委托，参数为待装配的网络频道</param>
		public static void RegisterExternalPacketHandler(Action<INetworkChannel> registrar)
		{
			registrar.NotNull(nameof(registrar));
			lock (sr_externalPacketHandlerRegistrarsLock)
			{
				sr_externalPacketHandlerList.Add(registrar);
			}
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
			var message = m_networkChannel.HeartBeatProvider.CreateHeartBeatReq();
			m_networkChannel.Send(message);
			return true;
		}

		/// <summary>
		/// 序列化消息并写入缓存流（写 14 字节包头到缓存流，消息体字节经 out 返回由调用方续写）
		/// </summary>
		/// <param name="msgObj">待发送的消息对象</param>
		/// <param name="cacheStream">包头写入目标流</param>
		/// <param name="msgBodyBuffer">输出：待发送的消息体缓冲区（超过 512 字节时已压缩）</param>
		/// <returns>是否序列化成功</returns>
		public bool SerializeAndWriteToCacheStream<T>(T msgObj, MemoryStream cacheStream, out byte[] msgBodyBuffer) where T : MessageObject
		{
			m_networkChannel.NotNull(nameof(m_networkChannel));
			msgObj.NotNull(nameof(msgObj));
			cacheStream.NotNull(nameof(cacheStream));

			m_offset = 0;
			var messageType = msgObj.GetType();

			m_currentSentId = MessageIdRegistry.GetIdByType(messageType);
			msgBodyBuffer   = SerializerHelper.Serialize(msgObj);

			byte isZip;
			if (msgBodyBuffer.Length > LIMIT_COMPRESS_LENGTH)
			{
				isZip         = 1;
				msgBodyBuffer = Utility.Zip.Compress(msgBodyBuffer);
			}
			else
			{
				isZip = 0;
			}

			var packetLength = (uint)(PACKET_HEADER_LENGTH + msgBodyBuffer.Length);

			m_cachedByte.WriteUInt(packetLength, ref m_offset);                                // 数据包总大小
			m_cachedByte.WriteByte((byte)(msgObj is IHeartBeatMessage ? 1 : 4), ref m_offset); // 消息操作类型
			m_cachedByte.WriteByte(isZip,                                       ref m_offset); // 消息压缩标记
			m_cachedByte.WriteInt(msgObj.UniqueId, ref m_offset);                              // 消息编号
			m_cachedByte.WriteInt(m_currentSentId, ref m_offset);                              // 消息ID
			cacheStream.Write(m_cachedByte, 0, PACKET_HEADER_LENGTH);
			return true;
		}

		/// <summary>
		/// 将消息体写入缓存流
		/// </summary>
		/// <param name="msgBodyBuffer">消息体缓冲区</param>
		/// <param name="cacheStream">包体写入目标流</param>
		/// <returns>是否写入成功</returns>
		public bool WritePacketBodyToCacheStream(byte[] msgBodyBuffer, MemoryStream cacheStream)
		{
			msgBodyBuffer.NotNull(nameof(msgBodyBuffer));
			cacheStream.NotNull(nameof(cacheStream));

			cacheStream.Write(msgBodyBuffer, 0, msgBodyBuffer.Length);
			return true;
		}

		/// <summary>
		/// 反序列化接收包头：解析五字段（包长/操作类型/压缩标记/UniqueId/消息ID）并缓存接收状态。
		/// 操作类型仅推进读取偏移（零消费者），不保留为状态。
		/// </summary>
		/// <param name="source">包头原始字节（14 字节）</param>
		/// <returns>是否解析成功</returns>
		public bool DeserializePacketHeader(byte[] source)
		{
			source.NotNull(nameof(source));
			if (source is not byte[] reader) return false;

			// packetLength
			var offset = 0;
			m_receivedPacketLength = reader.ReadUInt(ref offset); //4

			// operationType（无消费者，仅推进偏移）
			offset += sizeof(byte); //1

			// zipFlag
			m_receivedZipFlag = reader.ReadByte(ref offset); //1

			// uniqueId
			m_receivedUniqueId = reader.ReadInt(ref offset); //4

			// messageId
			m_receivedId = reader.ReadInt(ref offset); //4
			return true;
		}

		/// <summary>
		/// 校验包头中的包长度并换算包体长度（接收侧，依据最近一次解析的包头）。
		/// </summary>
		/// <returns>包体长度</returns>
		/// <exception cref="InvalidOperationException">包长度非法或超出上限时抛出</exception>
		public int ValidateAndGetPacketBodyLength()
		{
			if (m_receivedPacketLength < PACKET_HEADER_LENGTH)
			{
				throw new InvalidOperationException($"包长度非法。packetLength:{m_receivedPacketLength}, headerLength:{PACKET_HEADER_LENGTH}");
			}

			var bodyLength = (long)m_receivedPacketLength - PACKET_HEADER_LENGTH;
			if (bodyLength > MaxPacketBodyLength)
			{
				throw new InvalidOperationException($"包体长度超出上限。bodyLength:{bodyLength}, limit:{MaxPacketBodyLength}");
			}

			return (int)bodyLength;
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
			var messageType = MessageIdRegistry.GetTypeById(messageId);
			msgObj = (MessageObject)SerializerHelper.Deserialize(source, messageType);
			return true;
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