using System;
using System.Buffers;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.Messages;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Message;

/// <summary>
/// 对外部客户端的消息解码处理器
/// </summary>
public sealed class ClientMessageDecoderHandler : DefaultMessageDecoderHandler
{
	/// <summary>
	/// 消息头长度
	/// </summary>
	public override int PackageHeaderLength { get; } = 14;

	/// <summary>
	/// 消息解码
	/// </summary>
	/// <param name="sequence"></param>
	/// <returns></returns>
	public override IMessage Handler(ref ReadOnlySequence<byte> sequence)
	{
		SequenceReader<byte> reader = new SequenceReader<byte>(sequence);
		try
		{
			reader.TryReadBigEndian(out uint value);
			MessageObjectHeader messageObjectHeader = new MessageObjectHeader();
			// OperationType 按 1 字节读，与客户端头布局一致（14 字节：Length4 + Op1 + ZipFlag1 + UniqueId4 + MessageId4）
			reader.TryReadBigEndian(out byte value2);
			reader.TryReadBigEndian(out byte value3);
			reader.TryReadBigEndian(out int value4);
			reader.TryReadBigEndian(out int value5);
			messageObjectHeader.OperationType = (MessageOperationType)value2;
			messageObjectHeader.ZipFlag = value3;
			messageObjectHeader.UniqueId = value4;
			messageObjectHeader.MessageId = value5;
			reader.TryReadBytes((int)(value - PackageHeaderLength), out var value6);
			if (messageObjectHeader.ZipFlag > 0)
			{
				base.DecompressHandler.CheckNotNull("DecompressHandler");
				value6 = base.DecompressHandler.Handler(value6);
			}
			Type messageTypeById = MessageProtoHelper.GetMessageTypeById(messageObjectHeader.MessageId);
			if (messageObjectHeader.MessageId >= 0)
			{
				return OuterNetworkMessage.Create(messageObjectHeader, value6, messageTypeById);
			}
			throw new Exception("不支持的消息类型,消息ID:" + messageObjectHeader.MessageId);
		}
		catch (Exception exception)
		{
			LogHelper.Fatal(exception);
			return null;
		}
	}
}
