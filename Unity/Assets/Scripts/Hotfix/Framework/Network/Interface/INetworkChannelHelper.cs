using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道辅助器接口。
	/// </summary>
	public interface INetworkChannelHelper
	{
		/// <summary>
		/// 初始化网络频道辅助器。
		/// </summary>
		/// <param name="networkChannel">网络频道。</param>
		void Initialize(INetworkChannel networkChannel);

		/// <summary>
		/// 关闭并清理网络频道辅助器。
		/// </summary>
		void Shutdown();

		/// <summary>
		/// 准备进行连接。
		/// </summary>
		void PrepareForConnecting();

		/// <summary>
		/// 发送心跳消息包。
		/// </summary>
		/// <returns>是否发送心跳消息包成功。</returns>
		bool SendHeartBeat();

		/// <summary>
		/// 序列化消息包头。
		/// </summary>
		/// <param name="messageObject">待序列化的消息对象。</param>
		/// <param name="destination">序列化后写入的目标流。</param>
		/// <param name="messageBodyBuffer">消息体缓冲区,用于存储序列化后的消息</param>
		/// <typeparam name="T"></typeparam>
		/// <returns>是否序列化成功</returns>
		bool SerializePacketHeader<T>(T messageObject, MemoryStream destination, out byte[] messageBodyBuffer) where T : MessageObject;

		/// <summary>
		/// 序列化消息包体。
		/// </summary>
		/// <param name="messageBodyBuffer">待序列化的消息对象。</param>
		/// <param name="destination">序列化后写入的目标流。</param>
		/// <returns>是否序列化成功。</returns>
		bool SerializePacketBody(byte[] messageBodyBuffer, MemoryStream destination);

		/// <summary>
		/// 反序列化消息包头。
		/// </summary>
		/// <param name="source">待反序列化的来源流。</param>
		/// <returns>是否反序列化成功。</returns>
		bool DeserializePacketHeader(byte[] source);

		/// <summary>
		/// 反序列化消息包。
		/// </summary>
		/// <param name="source">待反序列化的来源流。</param>
		/// <param name="messageId">消息ID</param>
		/// <param name="messageObject">消息结果对象</param>
		/// <returns>是否反序列化成功。</returns>
		bool DeserializePacketBody(byte[] source, int messageId, out MessageObject messageObject);
	}
}