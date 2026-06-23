namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public interface IHandshakeRequiredSession
{
	bool Handshaked { get; }
}
