using System.IO;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络消息包处理器接口。
    /// </summary>
    public interface IPacketSendBodyHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageBodyBuffer"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        bool Handler(byte[] messageBodyBuffer, MemoryStream destination);
    }
}