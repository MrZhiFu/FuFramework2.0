namespace FuFramework.SuperSocket.Server.Abstractions;

public enum ServerState
{
	None,
	Starting,
	Started,
	Stopping,
	Stopped,
	Failed
}
