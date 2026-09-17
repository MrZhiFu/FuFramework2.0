using System;

namespace FuFramework.NetWork.Abstractions;

/// <summary>
/// 消息处理帮助类，用于管理消息的编码和解码处理器
/// </summary>
public sealed class MessageHelper
{
	/// <summary>
	/// 消息编码处理器 - 用于将消息编码成二进制格式
	/// </summary>
	public static IMessageEncoderHandler EncoderHandler { get; private set; }

	/// <summary>
	/// 消息解码处理器 - 用于将二进制数据解码成消息对象
	/// </summary>
	public static IMessageDecoderHandler DecoderHandler { get; private set; }

	/// <summary>
	/// 设置消息解码处理器和解压缩处理器
	/// </summary>
	/// <param name="decoderHandler">消息解码处理器实例</param>
	/// <param name="decompressHandler">消息解压缩处理器实例</param>
	/// <exception cref="T:System.ArgumentNullException">当decoderHandler为null时抛出</exception>
	public static void SetMessageDecoderHandler(IMessageDecoderHandler decoderHandler, IMessageDecompressHandler decompressHandler)
	{
		ArgumentNullException.ThrowIfNull(decoderHandler, "decoderHandler");
		DecoderHandler = decoderHandler;
		DecoderHandler.SetDecompressionHandler(decompressHandler);
	}

	/// <summary>
	/// 设置消息编码处理器和压缩处理器
	/// </summary>
	/// <param name="encoderHandler">消息编码处理器实例</param>
	/// <param name="compressHandler">消息压缩处理器实例</param>
	/// <exception cref="T:System.ArgumentNullException">当encoderHandler为null时抛出</exception>
	public static void SetMessageEncoderHandler(IMessageEncoderHandler encoderHandler, IMessageCompressHandler compressHandler)
	{
		ArgumentNullException.ThrowIfNull(encoderHandler, "encoderHandler");
		EncoderHandler = encoderHandler;
		EncoderHandler.SetCompressionHandler(compressHandler);
	}
}
