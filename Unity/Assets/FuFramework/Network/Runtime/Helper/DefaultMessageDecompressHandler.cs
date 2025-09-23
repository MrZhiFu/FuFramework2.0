using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 默认消息解压处理
    /// </summary>
    public sealed class DefaultMessageDecompressHandler : IMessageDecompressHandler, IPacketHandler
    {
        /// <summary>
        /// 解压处理
        /// </summary>
        /// <param name="message">消息压缩内容</param>
        /// <returns></returns>
        public byte[] Handler(byte[] message) => ZipHelper.Decompress(message);
    }
}