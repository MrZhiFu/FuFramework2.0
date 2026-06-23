namespace FuFramework.NetWork.Abstractions;

/// <summary>
/// 响应消息
/// </summary>
public interface IResponseMessage : INetworkMessage, IMessage
{
	/// <summary>
	/// 错误码，非 0 表示错误
	/// </summary>
	int ErrorCode { get; set; }
}
