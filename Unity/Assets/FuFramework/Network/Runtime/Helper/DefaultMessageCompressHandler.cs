using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 默认消息压缩处理
    /// </summary>
    public sealed class DefaultMessageCompressHandler : IMessageCompressHandler, IPacketHandler
    {
        /// <summary>
        /// 压缩处理
        /// </summary>
        /// <param name="message">消息未压缩内容</param>
        /// <returns></returns>
        public byte[] Handler(byte[] message) => ZipHelper.Compress(message);
    }
}