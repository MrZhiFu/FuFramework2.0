// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 消息压缩器接口定义，用于压缩消息数据
    /// </summary>
    public interface IMessageCompressHandler
    {
        /// <summary>
        /// 压缩处理
        /// </summary>
        /// <param name="message">消息未压缩内容</param>
        /// <returns></returns>
        byte[] Handler(byte[] message);
    }
}
