using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 默认消息接收内容处理器
	/// </summary>
	public sealed class DefaultPacketReceiveBodyHandler : IPacketReceiveBodyHandler, IPacketHandler
	{
		/// <summary>
		/// 处理接收包体：按消息 ID 反序列化出消息对象
		/// </summary>
		/// <param name="source">包体原始字节</param>
		/// <param name="messageId">消息 ID</param>
		/// <param name="messageObject">输出：反序列化得到的消息对象</param>
		/// <typeparam name="T">目标消息类型</typeparam>
		/// <returns>是否解析成功</returns>
		public bool Handler<T>(byte[] source, int messageId, out T messageObject) where T : MessageObject
		{
			var messageType = ProtoMessageIdHandler.GetRespTypeById(messageId);
			messageObject = (T)SerializerHelper.Deserialize(source, messageType);
			return true;
		}
	}
}