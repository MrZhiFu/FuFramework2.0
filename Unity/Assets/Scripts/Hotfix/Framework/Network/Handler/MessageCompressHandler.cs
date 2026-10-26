using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 消息压缩处理
	/// </summary>
	public sealed class MessageCompressHandler
	{
		/// <summary>
		/// 压缩处理
		/// </summary>
		/// <param name="message">消息未压缩内容</param>
		/// <returns></returns>
		public byte[] Compress(byte[] message) => Utility.Zip.Compress(message);
	}
}