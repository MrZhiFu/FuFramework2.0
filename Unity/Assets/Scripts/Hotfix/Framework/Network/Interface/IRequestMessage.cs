// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 客户端发送给服务器的消息的基类接口
	/// </summary>
	public interface IRequestMessage { }

	/// <summary>
	/// 请求-响应映射标记接口：声明该请求对应的响应类型，供 <see cref="INetworkChannel.Call{TResponse}"/>
	/// 在编译期校验 Req→Resp 配对（TResponse 从本接口推断，传错请求或配错响应类型直接编译不过）。
	/// 由 ProtoExport 生成器为有配对响应的 Req 消息自动实现；无配对响应的请求（如心跳）仅实现 IRequestMessage。
	/// </summary>
	/// <typeparam name="TResponse">该请求对应的响应消息类型。</typeparam>
	public interface IRequestMessage<TResponse> : IRequestMessage where TResponse : IResponseMessage { }
}