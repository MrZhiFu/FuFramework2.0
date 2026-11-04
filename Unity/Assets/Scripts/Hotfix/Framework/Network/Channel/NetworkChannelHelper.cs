using System;
using System.Collections.Generic;
using System.IO;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using ProtoBuf;


// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 线协议包头解析结果（readonly 结构，显式传递取代隐式字段状态）。
	/// </summary>
	public readonly struct PacketHeader
	{
		/// <summary>包总长度（含包头）</summary>
		public uint PacketLength { get; }

		/// <summary>压缩标记（1 = 包体已压缩）</summary>
		public byte ZipFlag { get; }

		/// <summary>消息唯一号（RPC 配对单号）</summary>
		public int UniqueId { get; }

		/// <summary>消息 ID</summary>
		public int MessageId { get; }

		public PacketHeader(uint packetLength, byte zipFlag, int uniqueId, int messageId)
		{
			PacketLength = packetLength;
			ZipFlag      = zipFlag;
			UniqueId     = uniqueId;
			MessageId    = messageId;
		}
	}

	/// <summary>
	/// 网络频道编解码器（无状态静态工具类）：信封（包头/包体）的序列化写入与解析读取。
	/// 全部方法无共享可写状态——解析结果以返回值/出参显式传递，天然线程安全；
	/// 包头缓冲使用局部数组（14 字节，JIT 栈分配），复用 BufferEx 扩展写入。
	/// </summary>
	public static class NetworkChannelHelper
	{
		/// <summary>
		/// 外部装配委托集合（显式注册入口；项目铁律 4：不做反射扫描自动发现）。
		/// 委托在频道构造时调用，实例按频道创建、不可跨频道共享。
		/// 当前唯一使用者：生成物注册游戏侧心跳请求创建器（<see cref="RegisterExternalPacketHandler"/>）。
		/// 仅限启动期调用，之后不再变更。
		/// </summary>
		private static readonly List<Action<INetworkChannel>> sr_externalPacketHandlerList = new();

		/// <summary>
		/// 外部装配委托集合的锁对象
		/// </summary>
		private static readonly object sr_externalPacketHandlerRegistrarsLock = new();

		/// <summary>
		/// 执行外部装配委托（频道构造时调用）：注入频道引用，含生成物注册的游戏心跳。
		/// </summary>
		public static void RunExternalPacketHandlers(INetworkChannel channel)
		{
			lock (sr_externalPacketHandlerRegistrarsLock)
			{
				foreach (var handler in sr_externalPacketHandlerList)
				{
					handler?.Invoke(channel);
				}
			}
		}

		/// <summary>
		/// 显式注册一个外部装配委托。委托会在频道构造时被调用。
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
		/// 序列化消息并把包头写入缓存流（消息体字节经 out 返回由调用方续写）。
		/// 布局：包总长(4) + 操作类型(1) + 压缩标记(1) + UniqueId(4) + 消息ID(4)。
		/// </summary>
		/// <param name="msgObj">待发送的消息对象</param>
		/// <param name="cacheStream">包头写入目标流</param>
		/// <param name="msgBodyBuffer">输出：待发送的消息体缓冲区（超过压缩阈值时已压缩）</param>
		/// <returns>本次发送的消息 ID（发送日志过滤/监控埋点用）</returns>
		public static int SerializeAndWriteToCacheStream(MessageObject msgObj, MemoryStream cacheStream, out byte[] msgBodyBuffer)
		{
			msgObj.NotNull(nameof(msgObj));
			cacheStream.NotNull(nameof(cacheStream));

			var header       = new byte[NetworkModule.PACKET_HEADER_LENGTH];
			var offset       = 0;
			var messageId    = MessageIdRegistry.GetIdByType(msgObj.GetType());

			msgBodyBuffer = SerializerHelper.Serialize(msgObj);

#if UNITY_EDITOR
			// 网络调试面板发送埋点：抓压缩前的原始包体字节（与接收侧解压后字节对称，Hex 视图可与 JSON 对照）
			NetworkDebug.RecordRequest(msgObj, messageId, msgBodyBuffer);
#endif

			byte isZip;
			if (msgBodyBuffer.Length > NetworkModule.LIMIT_COMPRESS_LENGTH)
			{
				isZip         = 1;
				msgBodyBuffer = Utility.Zip.Compress(msgBodyBuffer);
			}
			else
			{
				isZip = 0;
			}

			var packetLength = (uint)(NetworkModule.PACKET_HEADER_LENGTH + msgBodyBuffer.Length);

			header.WriteUInt(packetLength, ref offset);                                // 数据包总大小
			header.WriteByte((byte)(msgObj is IHeartBeatMessage ? 1 : 4), ref offset); // 消息操作类型
			header.WriteByte(isZip,                                       ref offset); // 消息压缩标记
			header.WriteInt(msgObj.UniqueId,   ref offset);                            // 消息编号
			header.WriteInt(messageId,         ref offset);                            // 消息ID
			cacheStream.Write(header, 0, NetworkModule.PACKET_HEADER_LENGTH);
			return messageId;
		}

		/// <summary>
		/// 将消息体写入缓存流
		/// </summary>
		/// <param name="msgBodyBuffer">消息体缓冲区</param>
		/// <param name="cacheStream">包体写入目标流</param>
		/// <returns>是否写入成功</returns>
		public static bool WritePacketBodyToCacheStream(byte[] msgBodyBuffer, MemoryStream cacheStream)
		{
			msgBodyBuffer.NotNull(nameof(msgBodyBuffer));
			cacheStream.NotNull(nameof(cacheStream));

			cacheStream.Write(msgBodyBuffer, 0, msgBodyBuffer.Length);
			return true;
		}

		/// <summary>
		/// 解析接收包头并校验（解析即校验：包长度非法或超出包体上限时直接抛异常）。
		/// 操作类型仅推进读取偏移（零消费者），不进入结果。
		/// </summary>
		/// <param name="source">包头原始字节（14 字节）</param>
		/// <returns>包头解析结果（包总长含包头；包体长度 = PacketLength - 包头长度，由调用方换算）</returns>
		public static PacketHeader ParseHeader(byte[] source)
		{
			source.NotNull(nameof(source));

			var offset = 0;

			// 消息长度 4
			var packetLength = source.ReadUInt(ref offset);

			// 操作类型 1（无消费者，仅推进偏移）
			offset += sizeof(byte);

			// 压缩标记 1
			var zipFlag = source.ReadByte(ref offset);

			// 唯一ID 4
			var uniqueId = source.ReadInt(ref offset);

			// 消息ID 4
			var messageId = source.ReadInt(ref offset);

			if (packetLength < NetworkModule.PACKET_HEADER_LENGTH)
			{
				throw new InvalidOperationException($"包长度非法。packetLength:{packetLength}, headerLength:{NetworkModule.PACKET_HEADER_LENGTH}");
			}

			var bodyLength = (long)packetLength - NetworkModule.PACKET_HEADER_LENGTH;
			if (bodyLength > NetworkModule.MAX_PACKET_BODY_LENGTH)
			{
				throw new InvalidOperationException($"包体长度超出上限。bodyLength:{bodyLength}, limit:{NetworkModule.MAX_PACKET_BODY_LENGTH}");
			}

			return new PacketHeader(packetLength, zipFlag, uniqueId, messageId);
		}

		/// <summary>
		/// 反序列化接收包体
		/// </summary>
		/// <param name="source">包体原始字节</param>
		/// <param name="messageId">消息 ID</param>
		/// <param name="msgObj">输出：反序列化得到的消息对象</param>
		/// <returns>是否解析成功</returns>
		public static bool DeserializePacketBody(byte[] source, int messageId, out MessageObject msgObj)
		{
			source.NotNull(nameof(source));
			var messageType = MessageIdRegistry.GetTypeById(messageId);
			msgObj = (MessageObject)SerializerHelper.Deserialize(source, messageType);
			return true;
		}
	}
}
