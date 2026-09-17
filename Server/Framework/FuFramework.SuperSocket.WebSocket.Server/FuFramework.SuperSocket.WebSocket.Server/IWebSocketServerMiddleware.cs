using System.Threading.Tasks;

namespace FuFramework.SuperSocket.WebSocket.Server;

internal interface IWebSocketServerMiddleware
{
	int OpenHandshakePendingQueueLength { get; }

	int CloseHandshakePendingQueueLength { get; }

	ValueTask HandleSessionHandshakeCompleted(WebSocketSession session);
}
