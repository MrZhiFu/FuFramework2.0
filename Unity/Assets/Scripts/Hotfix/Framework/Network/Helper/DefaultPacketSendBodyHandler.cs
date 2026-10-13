using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 默认消息发送内容处理器
	/// </summary>
	public sealed class DefaultPacketSendBodyHandler : IPacketSendBodyHandler, IPacketHandler
	{
		/// <summary>
		/// 处理发送包体：将消息体字节写入发送流
		/// </summary>
		/// <param name="messageBodyBuffer">消息体缓冲区</param>
		/// <param name="destination">包体写入目标流</param>
		/// <returns>是否写入成功</returns>
		public bool Handler(byte[] messageBodyBuffer, MemoryStream destination)
		{
			destination.Write(messageBodyBuffer, 0, messageBodyBuffer.Length);
			return true;
		}
	}
}