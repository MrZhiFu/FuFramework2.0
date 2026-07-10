using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Network
{
    /// <summary>
    /// 默认消息发送内容处理器
    /// </summary>
    public sealed class DefaultPacketSendBodyHandler : IPacketSendBodyHandler, IPacketHandler
    {
        public bool Handler(byte[] messageBodyBuffer, MemoryStream destination)
        {
            destination.Write(messageBodyBuffer, 0, messageBodyBuffer.Length);
            return true;
        }
    }
}