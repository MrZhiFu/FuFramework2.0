using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 默认消息发送内容处理器
	/// </summary>
	public sealed class PacketSendBodyHandler
	{
		/// <summary>
		/// 处理发送包体：将消息体字节写入发送流
		/// </summary>
		/// <param name="msgBodyBuffer">消息体缓冲区</param>
		/// <param name="cacheStream">包体写入目标流</param>
		/// <returns>是否写入成功</returns>
		public bool WriteToCacheStream(byte[] msgBodyBuffer, MemoryStream cacheStream)
		{
			cacheStream.Write(msgBodyBuffer, 0, msgBodyBuffer.Length);
			return true;
		}
	}
}