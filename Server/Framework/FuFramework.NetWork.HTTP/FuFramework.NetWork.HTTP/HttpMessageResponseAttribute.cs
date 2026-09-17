using System;
using FuFramework.Utility.Extensions;

namespace FuFramework.NetWork.HTTP;

/// <summary>
/// HTTP响应消息特性，用于标记HTTP响应消息类型
/// 此特性用于标记HTTP处理器的响应消息类型，确保响应消息类型继承自HttpMessageResponseBase
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class HttpMessageResponseAttribute : Attribute
{
	/// <summary>
	/// 获取响应消息的类型
	/// 该属性存储HTTP响应消息的具体类型，用于运行时的消息处理和序列化
	/// </summary>
	public Type MessageType { get; }

	/// <summary>
	/// 初始化 <see cref="T:FuFramework.NetWork.HTTP.HttpMessageResponseAttribute" /> 的新实例
	/// 构造函数会验证传入的类型是否为有效的HTTP响应消息类型
	/// </summary>
	/// <param name="classType">响应消息的类型，必须继承自HttpMessageResponseBase</param>
	/// <exception cref="T:System.ArgumentNullException">当 classType 为 null 时抛出此异常</exception>
	/// <exception cref="T:System.InvalidCastException">当 classType 未继承自HttpMessageResponseBase时抛出此异常</exception>
	public HttpMessageResponseAttribute(Type classType)
	{
		classType.CheckNotNull("classType");
		if (!classType.IsSubclassOf(typeof(HttpMessageResponseBase)))
		{
			throw new InvalidCastException("消息类型 " + classType.Name + " 必须继承自 HttpMessageResponseBase");
		}
		MessageType = classType;
	}
}
