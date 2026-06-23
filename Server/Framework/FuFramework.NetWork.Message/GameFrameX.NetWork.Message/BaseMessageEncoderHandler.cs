using System;
using System.Buffers;
using FuFramework.NetWork.Abstractions;
using FuFramework.ProtoBuf.Net;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Message;

/// <summary>
/// 基础消息编码处理器
/// </summary>
public abstract class BaseMessageEncoderHandler : IMessageEncoderHandler
{
	/// <summary>
	/// 压缩消息处理器
	/// </summary>
	protected IMessageCompressHandler CompressHandler { get; private set; }

	/// <summary>
	/// 超过多少字节长度才启用压缩,默认512
	/// </summary>
	public virtual uint LimitCompressLength { get; } = 512u;

	/// <summary>
	/// totalLength + headerLength
	/// </summary>
	public virtual ushort PackageHeaderLength { get; } = 6;

	/// <summary>
	/// 和客户端之间的消息 数据长度(2)+消息唯一ID(4)+消息ID(4)+消息内容
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public abstract byte[] Handler(IMessage message);

	/// <summary>
	/// 内部消息
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public byte[] Handler(IInnerNetworkMessage message)
	{
		byte zipFlag = message.Header.ZipFlag;
		byte[] bytes = message.MessageData;
		BytesCompressHandler(ref bytes, ref zipFlag);
		message.Header.ZipFlag = zipFlag;
		message.SetMessageData(bytes);
		byte[] messageHeaderData = ProtoBufSerializerHelper.Serialize(message.Header);
		return InnerBufferHandler(message.MessageData, ref messageHeaderData);
	}

	/// <summary>
	/// 设置压缩消息处理器
	/// </summary>
	/// <param name="compressHandler">压缩消息处理器</param>
	public void SetCompressionHandler(IMessageCompressHandler compressHandler = null)
	{
		CompressHandler = compressHandler;
	}

	/// <summary>
	/// 内部消息结构写入
	/// 结构为 totalLength(uint) + headerLength(ushort) + header 数组 + body 数组
	/// </summary>
	/// <param name="messageHeaderData">消息头数组</param>
	/// <param name="messageBodyData">内容数组</param>
	/// <returns></returns>
	protected byte[] InnerBufferHandler(byte[] messageBodyData, ref byte[] messageHeaderData)
	{
		ushort num = (ushort)(PackageHeaderLength + messageBodyData.Length + messageHeaderData.Length);
		byte[] array = ArrayPool<byte>.Shared.Rent(num);
		int offset = 0;
		array.WriteUInt(num, ref offset);
		array.WriteUShort((ushort)messageHeaderData.Length, ref offset);
		array.WriteBytesWithoutLength(messageHeaderData, ref offset);
		array.WriteBytesWithoutLength(messageBodyData, ref offset);
		byte[] result = array.AsSpan(0, num).ToArray();
		ArrayPool<byte>.Shared.Return(array);
		return result;
	}

	/// <summary>
	/// 消息压缩处理
	/// </summary>
	/// <param name="bytes">压缩前的数据</param>
	/// <param name="zipFlag">压缩标记</param>
	/// <returns></returns>
	protected void BytesCompressHandler(ref byte[] bytes, ref byte zipFlag)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(zipFlag, "zipFlag");
		if (CompressHandler != null && bytes.Length > LimitCompressLength)
		{
			zipFlag = 1;
			bytes = CompressHandler.Handler(bytes);
		}
		else
		{
			zipFlag = 0;
		}
	}
}
