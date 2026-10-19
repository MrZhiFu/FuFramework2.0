using Newtonsoft.Json;
using ProtoBuf;
using AOT.Framework.Core.Utility;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// HTTP消息包装基类
	/// </summary>
	[ProtoContract]
	public class MessageHttpObject
	{
		/// <summary>
		/// 消息ID
		/// </summary>
		[ProtoMember(1)]
		public int Id { get; set; }

		/// <summary>
		/// 消息序列号
		/// </summary>
		[ProtoMember(2)]
		public int UniqueId { get; set; }

		/// <summary>
		/// 消息体字节（JSON 序列化时忽略）
		/// </summary>
		[JsonIgnore] [ProtoMember(3)] public byte[] Body { get; set; }

		/// <summary>
		/// 序列化为 JSON 字符串
		/// </summary>
		/// <returns>消息的 JSON 表示</returns>
		public override string ToString() => UtilityAOT.Json.ToJson(this);
	}
}