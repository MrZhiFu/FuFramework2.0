using System;
using System.Buffers;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.Messages;
using FuFramework.ProtoBuf.Net;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Message;

/// <summary>
/// 基础消息编码处理器
/// </summary>
public sealed class ClientMessageEncoderHandler : BaseMessageEncoderHandler
{
	/// <summary>
	/// totalLength + operationType + zipFlag + uniqueId + messageId
	/// </summary>
	public override ushort PackageHeaderLength { get; } = 14;

	/// <summary>
	/// 和客户端之间的消息 数据长度(2)+消息唯一ID(4)+消息ID(4)+消息内容
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public override byte[] Handler(IMessage message)
	{
		if (message is MessageObject messageObject)
		{
			MessageProtoHelper.SetMessageId(messageObject);
			messageObject.SetOperationType(MessageProtoHelper.GetMessageOperationType(messageObject));
			byte[] bytes = ProtoBufSerializerHelper.Serialize(messageObject);
			bool flag = MessageProtoHelper.IsHeartbeat(messageObject.GetType());
			byte zipFlag = 0;
			BytesCompressHandler(ref bytes, ref zipFlag);
			ushort num = (ushort)(PackageHeaderLength + bytes.Length);
			byte[] array = ArrayPool<byte>.Shared.Rent(num);
			int offset = 0;
			array.WriteUInt(num, ref offset);
			array.WriteByte((byte)(flag ? 1 : 4), ref offset);
			array.WriteByte(zipFlag, ref offset);
			array.WriteInt(messageObject.UniqueId, ref offset);
			array.WriteInt(messageObject.MessageId, ref offset);
			array.WriteBytesWithoutLength(bytes, ref offset);
			byte[] result = array.AsSpan(0, num).ToArray();
			ArrayPool<byte>.Shared.Return(array);
			return result;
		}
		LogHelper.Error("消息对象为空，编码异常");
		return null;
	}
}
