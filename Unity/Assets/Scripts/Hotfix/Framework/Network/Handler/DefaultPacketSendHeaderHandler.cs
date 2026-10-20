using ProtoBuf;
using System.IO;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 默认消息发送头部处理器
	/// </summary>
	public class DefaultPacketSendHeaderHandler : IPacketSendHeaderHandler, IPacketHandler
	{
		/// <summary>
		/// 网络包长度(4字节)
		/// </summary>
		private const int NET_PACKET_LENGTH = sizeof(uint);

		/// <summary>
		/// 消息操作类型长度(1字节)
		/// </summary>
		private const int NET_OPERATION_TYPE_LENGTH = sizeof(byte);

		/// <summary>
		/// 消息压缩标记长度(1字节)
		/// </summary>
		private const int NET_ZIP_FLAG_LENGTH = sizeof(byte);

		/// <summary>
		/// 消息编号长度(4字节)
		/// </summary>
		private const int NET_UNIQUE_ID_LENGTH = sizeof(int);

		/// <summary>
		/// 消息码出长度(4字节)
		/// </summary>
		private const int NET_CMD_ID_LENGTH = sizeof(int);


		/// <summary>
		/// 包头缓存字节（避免每次发包重新分配）
		/// </summary>
		private readonly byte[] m_cachedByte;

		/// <summary>
		/// 超过消息的长度超过该值的时候启用压缩.该值 必须在设置压缩器的时候才生效,默认512字节
		/// </summary>
		public virtual uint LimitCompressLength => 512;

		/// <summary>
		/// 当前缓存字节的写入偏移
		/// </summary>
		private int m_offset;


		/// <summary>
		/// 消息包头长度
		/// </summary>
		public ushort PacketHeaderLength { get; }

		/// <summary>
		/// 获取网络消息包协议编号。
		/// </summary>
		public int Id { get; private set; }

		/// <summary>
		/// 获取网络消息包长度。
		/// </summary>
		public uint PacketLength { get; private set; }

		/// <summary>
		/// 是否压缩消息内容
		/// </summary>
		public bool IsZip { get; private set; }


		/// <summary>
		/// 创建发送包头处理器，并计算包头总长度
		/// </summary>
		public DefaultPacketSendHeaderHandler()
		{
			// 4 + 1 + 1 + 4 + 4
			PacketHeaderLength = NET_PACKET_LENGTH + NET_OPERATION_TYPE_LENGTH + NET_ZIP_FLAG_LENGTH + NET_UNIQUE_ID_LENGTH + NET_CMD_ID_LENGTH;
			m_cachedByte       = new byte[PacketHeaderLength];
		}

		/// <summary>
		/// 处理消息
		/// </summary>
		/// <param name="messageObject">消息对象</param>
		/// <param name="messageCompressHandler">压缩消息内容处理器</param>
		/// <param name="destination">缓存流</param>
		/// <param name="messageBodyBuffer">消息序列化完的二进制数组</param>
		/// <typeparam name="T">消息类型</typeparam>
		/// <returns>是否序列化成功</returns>
		public bool Handler<T>(T messageObject, IMessageCompressHandler messageCompressHandler, MemoryStream destination,
		                       out byte[] messageBodyBuffer) where T : MessageObject
		{
			m_offset = 0;
			var messageType = messageObject.GetType();
			Id                = ProtoMessageIdRegistry.GetReqMessageIdByType(messageType);
			messageBodyBuffer = SerializerHelper.Serialize(messageObject);
			if (messageCompressHandler != null && messageBodyBuffer.Length > LimitCompressLength)
			{
				IsZip             = true;
				messageBodyBuffer = messageCompressHandler.Handler(messageBodyBuffer);
			}
			else
			{
				IsZip = false;
			}

			var messageLength = messageBodyBuffer.Length;
			PacketLength = (uint)(PacketHeaderLength + messageLength);

			m_cachedByte.WriteUInt(PacketLength, ref m_offset);                                                   // 数据包总大小
			m_cachedByte.WriteByte((byte)(ProtoMessageIdRegistry.IsHeartbeat(messageType) ? 1 : 4), ref m_offset); // 消息操作类型
			m_cachedByte.WriteByte((byte)(IsZip ? 1 : 0),                                          ref m_offset); // 消息压缩标记
			m_cachedByte.WriteInt(messageObject.UniqueId, ref m_offset);                                          // 消息编号
			m_cachedByte.WriteInt(Id,                     ref m_offset);                                          // 消息ID
			destination.Write(m_cachedByte, 0, PacketHeaderLength);
			return true;
		}
	}
}