using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.Messages;
using FuFramework.ProtoBuf.Net;
using Microsoft.Extensions.ObjectPool;

namespace FuFramework.NetWork.Message;

/// <summary>
/// 基础消息编码处理器
/// </summary>
public sealed class DefaultMessageEncoderHandler : BaseMessageEncoderHandler
{
	private readonly ObjectPool<MessageObjectHeader> _messageObjectHeaderObjectPool;

	/// <summary>
	/// 默认消息编码处理器
	/// </summary>
	public DefaultMessageEncoderHandler()
	{
		_messageObjectHeaderObjectPool = new DefaultObjectPoolProvider().Create<MessageObjectHeader>();
	}

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
			byte zipFlag = 0;
			BytesCompressHandler(ref bytes, ref zipFlag);
			MessageObjectHeader messageObjectHeader = _messageObjectHeaderObjectPool.Get();
			messageObjectHeader.OperationType = messageObject.OperationType;
			messageObjectHeader.UniqueId = messageObject.UniqueId;
			messageObjectHeader.MessageId = messageObject.MessageId;
			messageObjectHeader.ZipFlag = zipFlag;
			byte[] messageHeaderData = ProtoBufSerializerHelper.Serialize(messageObjectHeader);
			_messageObjectHeaderObjectPool.Return(messageObjectHeader);
			return InnerBufferHandler(bytes, ref messageHeaderData);
		}
		LogHelper.Error("消息对象为空，编码异常");
		return null;
	}
}
