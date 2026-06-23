using System;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.Abstractions;

/// <summary>
/// RPC 消息属性标记
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MessageRpcMappingAttribute : Attribute
{
	/// <summary>
	/// 请求消息
	/// </summary>
	public IRequestMessage RequestMessage { get; }

	/// <summary>
	/// 响应消息
	/// </summary>
	public IResponseMessage ResponseMessage { get; }

	/// <summary>
	/// 构建RPC 消息
	/// </summary>
	/// <param name="requestMessage">请求消息类型</param>
	/// <param name="responseMessage">返回消息类型</param>
	public MessageRpcMappingAttribute(IRequestMessage requestMessage, IResponseMessage responseMessage)
	{
		requestMessage.CheckNotNull("requestMessage");
		responseMessage.CheckNotNull("responseMessage");
		RequestMessage = requestMessage;
		ResponseMessage = responseMessage;
	}
}
