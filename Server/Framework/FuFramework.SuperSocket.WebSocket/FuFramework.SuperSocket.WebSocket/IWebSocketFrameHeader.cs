namespace FuFramework.SuperSocket.WebSocket;

public interface IWebSocketFrameHeader
{
	bool FIN { get; }

	bool RSV1 { get; }

	bool RSV2 { get; }

	bool RSV3 { get; }
}
