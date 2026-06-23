using System;
using System.Collections.Specialized;
using FuFramework.SuperSocket.WebSocket.Extensions;
using FuFramework.SuperSocket.WebSocket.Extensions.Compression;

namespace FuFramework.SuperSocket.WebSocket.Server.Extensions.Compression;

/// <summary>
///     WebSocket Per-Message Compression Extension
///     https://tools.ietf.org/html/rfc7692
/// </summary>
public class WebSocketPerMessageCompressionExtensionFactory : IWebSocketExtensionFactory
{
	private static readonly NameValueCollection _supportedOptions;

	public string Name => "permessage-deflate";

	static WebSocketPerMessageCompressionExtensionFactory()
	{
		_supportedOptions = new NameValueCollection();
		_supportedOptions.Add("client_no_context_takeover", string.Empty);
	}

	public IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions)
	{
		supportedOptions = _supportedOptions;
		if (options != null && options.Count > 0)
		{
			string[] allKeys = options.AllKeys;
			foreach (string text in allKeys)
			{
				if (text.StartsWith("server_", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(options.Get(text)))
				{
					return null;
				}
			}
		}
		return new WebSocketPerMessageCompressionExtension();
	}
}
