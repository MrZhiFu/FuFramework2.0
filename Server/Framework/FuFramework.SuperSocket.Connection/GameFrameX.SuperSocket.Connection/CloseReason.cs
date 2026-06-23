namespace FuFramework.SuperSocket.Connection;

/// <summary>
/// 关闭原因
/// </summary>
public enum CloseReason
{
	/// <summary>
	/// The socket is closed for unknown reason
	/// </summary>
	Unknown,
	/// <summary>
	/// Close for server shutdown
	/// </summary>
	ServerShutdown,
	/// <summary>
	/// The close behavior is initiated from the remote endpoint
	/// </summary>
	RemoteClosing,
	/// <summary>
	/// The close behavior is initiated from the local endpoint
	/// </summary>
	LocalClosing,
	/// <summary>
	/// Application error
	/// </summary>
	ApplicationError,
	/// <summary>
	/// The socket is closed for a socket error
	/// </summary>
	SocketError,
	/// <summary>
	/// The socket is closed by server for timeout
	/// </summary>
	TimeOut,
	/// <summary>
	/// Protocol error 
	/// </summary>
	ProtocolError,
	/// <summary>
	/// SuperSocket internal error
	/// </summary>
	InternalError
}
