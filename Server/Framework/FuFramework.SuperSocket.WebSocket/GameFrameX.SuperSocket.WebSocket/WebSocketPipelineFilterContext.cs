using System.Collections.Generic;
using FuFramework.SuperSocket.WebSocket.Extensions;

namespace FuFramework.SuperSocket.WebSocket;

public class WebSocketPipelineFilterContext
{
	public IReadOnlyList<IWebSocketExtension> Extensions { get; set; }
}
