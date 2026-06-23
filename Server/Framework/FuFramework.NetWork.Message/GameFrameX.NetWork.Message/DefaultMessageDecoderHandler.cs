using System;
using System.Buffers;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.Messages;
using FuFramework.ProtoBuf.Net;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Message;

/// <summary>
/// 基础消息解码处理器
/// </summary>
public class DefaultMessageDecoderHandler : BaseMessageDecoderHandler
{
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
			reader.TryReadBigEndian(out int value);
			reader.TryReadBigEndian(out ushort value2);
			reader.TryReadBytes(value2, out var value3);
			INetworkMessageHeader networkMessageHeader = (INetworkMessageHeader)ProtoBufSerializerHelper.Deserialize(value3, typeof(MessageObjectHeader));
			reader.TryReadBytes(value - value2 - PackageHeaderLength, out var value4);
			if (networkMessageHeader.ZipFlag > 0)
			{
				base.DecompressHandler.CheckNotNull("DecompressHandler");
				value4 = base.DecompressHandler.Handler(value4);
			}
			Type messageTypeById = MessageProtoHelper.GetMessageTypeById(networkMessageHeader.MessageId);
			if (networkMessageHeader.MessageId >= 0)
			{
				return OuterNetworkMessage.Create(networkMessageHeader, value4, messageTypeById);
			}
			return InnerNetworkMessage.Create(networkMessageHeader, value4, messageTypeById);
		}
		catch (Exception exception)
		{
			LogHelper.Fatal(exception);
			return null;
		}
	}
}
