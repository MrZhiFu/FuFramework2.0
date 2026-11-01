using Newtonsoft.Json;
using ProtoBuf;
using AOT.Framework.Core.Utility;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// HTTP 版"消息包头/信封"：通过 HTTP 传输 protobuf 协议消息时，用本类把消息包成一封固定格式的信，
	/// 让服务器收到字节后能认出"这是什么类型的消息"。
	/// </summary>
	/// <remarks>
	/// 为什么需要这一层信封？
	/// 1. HTTP 协议没有 TCP 那样的自定义包头。TCP 长连接靠字节流最前端的"包头"（INetworkMessageHeader，
	///    含 MessageId/UniqueId）标识消息；HTTP 没有这一区段，于是用本类承载同样的信息。
	/// 2. 服务器收到一坨字节后，必须先知道消息类型才能反序列化；而"消息 ↔ 类型编号"的映射在外部注册表
	///    （MessageIdRegistry / MessageProtoHelper）里，类型编号没法内嵌进消息体字节中——这是"先有鸡
	///    还是先有蛋"的死局。
	/// 3. 信封格式固定（永远只有 Id/UniqueId/Body 三字段），服务器必然第一步就能解析成功拿到 Id，
	///    再用 Id 查出真实类型，最后才按该类型反序列化 Body。
	///
	/// 收发链路（客户端与服务器各持一份同构类，即同一份协议）：
	///   发送：WebModule.PostPbReq → 套信封(Id/UniqueId/Body) → 整体 protobuf 序列化 → POST 出去；
	///   接收：服务器 HttpHandler 先解信封 → 拿 Id 查类型 → 反序列化 Body → SetMessageId/SetUniqueId；
	///   响应：服务器处理完同样套一封新信封返回（Id + 原请求 UniqueId 原样带回 + 响应 Body），
	///         客户端靠原样返回的 UniqueId 对上自己发出的请求（RPC 配对）。
	/// </remarks>
	[ProtoContract]
	public class MessageHttpObject
	{
		/// <summary>
		/// 消息类型编号（协议号）。发送侧由 MessageIdRegistry.GetIdByType(msgObj.GetType()) 反查得到，
		/// 服务器靠它在注册表中查出 Body 对应的真实消息类型，之后才能反序列化 Body。
		/// </summary>
		[ProtoMember(1)]
		public int Id { get; set; }

		/// <summary>
		/// 消息序列号（RPC 配对单号）。响应时服务器会按请求值原样返回，客户端据此对上"这封回信对应哪条请求"。
		/// </summary>
		[ProtoMember(2)]
		public int UniqueId { get; set; }

		/// <summary>
		/// 真正的 protobuf 消息体字节（SerializerHelper.Serialize 序列化业务消息的产物）。
		/// [JsonIgnore]：Body 是二进制，直接进 JSON 会变成乱码，调试用 ToString() 输出 JSON 预览时跳过它。
		/// </summary>
		[JsonIgnore]
		[ProtoMember(3)]
		public byte[] Body { get; set; }

		/// <summary>
		/// 序列化为 JSON 字符串
		/// </summary>
		/// <returns>消息的 JSON 表示</returns>
		public override string ToString() => UtilityAOT.Json.ToJson(this);
	}
}