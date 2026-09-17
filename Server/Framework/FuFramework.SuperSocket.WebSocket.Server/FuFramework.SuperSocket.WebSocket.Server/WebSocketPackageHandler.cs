using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.WebSocket.Extensions;
using FuFramework.SuperSocket.WebSocket.Server.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.WebSocket.Server;

/// <summary>
/// Handles WebSocket packages, including handshake and protocol management.
/// </summary>
public class WebSocketPackageHandler : IPackageHandler<WebSocketPackage>
{
	private const string _magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

	private static Encoding _textEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private IServiceProvider _serviceProvider;

	private IPackageHandler<WebSocketPackage> _websocketCommandMiddleware;

	private Func<WebSocketSession, WebSocketPackage, ValueTask> _packageHandlerDelegate;

	private Dictionary<string, ISubProtocolHandler> _subProtocolHandlers;

	private ILogger _logger;

	private readonly HandshakeOptions _handshakeOptions;

	private readonly IWebSocketServerMiddleware _websocketServerMiddleware;

	private Dictionary<string, IEnumerable<IWebSocketExtensionFactory>> _extensionFactories;

	private readonly Func<WebSocketEncoder> _websocketEncoderFactory;

	private readonly Lazy<WebSocketEncoder> _defaultMessageEncoder;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.WebSocket.Server.WebSocketPackageHandler" /> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="loggerFactory">The logger factory.</param>
	/// <param name="handshakeOptions">The handshake options.</param>
	public WebSocketPackageHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IOptions<HandshakeOptions> handshakeOptions)
	{
		_serviceProvider = serviceProvider;
		_websocketServerMiddleware = serviceProvider.GetService<IWebSocketServerMiddleware>();
		_websocketCommandMiddleware = serviceProvider.GetService<IWebSocketCommandMiddleware>() as IPackageHandler<WebSocketPackage>;
		_subProtocolHandlers = serviceProvider.GetServices<ISubProtocolHandler>().ToDictionary<ISubProtocolHandler, string>((ISubProtocolHandler h) => h.Name, StringComparer.OrdinalIgnoreCase);
		_extensionFactories = (from f in serviceProvider.GetServices<IWebSocketExtensionFactory>()
			group f by f.Name).ToDictionary<IGrouping<string, IWebSocketExtensionFactory>, string, IEnumerable<IWebSocketExtensionFactory>>((IGrouping<string, IWebSocketExtensionFactory> g) => g.Key, (IGrouping<string, IWebSocketExtensionFactory> g) => g.AsEnumerable(), StringComparer.OrdinalIgnoreCase);
		_packageHandlerDelegate = serviceProvider.GetService<Func<WebSocketSession, WebSocketPackage, ValueTask>>();
		_logger = loggerFactory.CreateLogger<WebSocketPackageHandler>();
		_handshakeOptions = handshakeOptions.Value;
		_websocketEncoderFactory = _serviceProvider.GetService<Func<WebSocketEncoder>>() ?? ((Func<WebSocketEncoder>)(() => new WebSocketEncoder()));
		_defaultMessageEncoder = new Lazy<WebSocketEncoder>(_websocketEncoderFactory);
	}

	private CloseStatus GetCloseStatusFromPackage(WebSocketPackage package)
	{
		if (package.Data.Length < 2)
		{
			return new CloseStatus
			{
				Reason = CloseReason.NormalClosure
			};
		}
		SequenceReader<byte> reader = new SequenceReader<byte>(package.Data);
		reader.TryReadBigEndian(out short value);
		CloseStatus closeStatus = new CloseStatus
		{
			Reason = (CloseReason)value
		};
		if (reader.Remaining > 0)
		{
			closeStatus.ReasonText = package.Data.Slice(2L).GetString(Encoding.UTF8);
		}
		return closeStatus;
	}

	/// <summary>
	/// Handles a WebSocket package asynchronously.
	/// </summary>
	/// <param name="session">The session associated with the package.</param>
	/// <param name="package">The WebSocket package.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous handling operation.</returns>
	public async ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken)
	{
		WebSocketSession websocketSession = session as WebSocketSession;
		if (package.OpCode == OpCode.Handshake)
		{
			websocketSession.HttpHeader = package.HttpHeader;
			if (!(await HandleHandshake(websocketSession, package)))
			{
				websocketSession.CloseWithoutHandshake();
				return;
			}
			websocketSession.Handshaked = true;
			await _websocketServerMiddleware.HandleSessionHandshakeCompleted(websocketSession);
		}
		else
		{
			if (!websocketSession.Handshaked)
			{
				return;
			}
			if (package.OpCode == OpCode.Close)
			{
				if (websocketSession.CloseStatus == null)
				{
					CloseStatus closeStatusFromPackage = GetCloseStatusFromPackage(package);
					websocketSession.CloseStatus = closeStatusFromPackage;
					try
					{
						await websocketSession.SendAsync(package, cancellationToken);
						return;
					}
					catch (InvalidOperationException)
					{
						return;
					}
				}
				websocketSession.CloseWithoutHandshake();
			}
			else if (package.OpCode == OpCode.Ping)
			{
				package.OpCode = OpCode.Pong;
				await websocketSession.SendAsync(package, cancellationToken);
			}
			else
			{
				if (package.OpCode == OpCode.Pong)
				{
					return;
				}
				ISubProtocolHandler subProtocolHandler = websocketSession.SubProtocolHandler;
				if (subProtocolHandler != null)
				{
					await subProtocolHandler.Handle(session, package, cancellationToken);
					return;
				}
				IPackageHandler<WebSocketPackage> websocketCommandMiddleware = _websocketCommandMiddleware;
				if (websocketCommandMiddleware != null)
				{
					await websocketCommandMiddleware.Handle(session, package, cancellationToken);
					return;
				}
				Func<WebSocketSession, WebSocketPackage, ValueTask> packageHandlerDelegate = _packageHandlerDelegate;
				if (packageHandlerDelegate != null)
				{
					await packageHandlerDelegate(websocketSession, package);
				}
			}
		}
	}

	private bool SelectSubProtocol(string requestedProtocols, out string selectedProtocol, out ISubProtocolHandler selectedProtocolHandler)
	{
		string[] array = requestedProtocols.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
		}
		if (_subProtocolHandlers.Any())
		{
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (_subProtocolHandlers.TryGetValue(text, out var value))
				{
					selectedProtocol = text;
					selectedProtocolHandler = value;
					return true;
				}
			}
		}
		selectedProtocol = string.Empty;
		selectedProtocolHandler = null;
		return false;
	}

	private List<string> SelectExtensions(string requestedExtensions, out List<IWebSocketExtension> extensions)
	{
		extensions = null;
		if (string.IsNullOrEmpty(requestedExtensions) || _extensionFactories.Count == 0)
		{
			return null;
		}
		extensions = new List<IWebSocketExtension>();
		List<string> list = new List<string>();
		string[] array = requestedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			int num = text.IndexOf(';');
			string key = ((num < 0) ? text : text.Substring(0, num));
			NameValueCollection nameValueCollection = null;
			if (num >= 0)
			{
				nameValueCollection = new NameValueCollection();
				string[] array2 = text.Substring(num + 1).Split(';');
				foreach (string text2 in array2)
				{
					int num2 = text2.IndexOf('=');
					if (num2 < 0)
					{
						nameValueCollection.Add(text2, string.Empty);
					}
					else
					{
						nameValueCollection.Add(text2.Substring(0, num2), text2.Substring(num2 + 1));
					}
				}
			}
			if (!_extensionFactories.TryGetValue(key, out var value))
			{
				continue;
			}
			foreach (IWebSocketExtensionFactory item in value)
			{
				NameValueCollection supportedOptions;
				IWebSocketExtension webSocketExtension = item.Create(nameValueCollection, out supportedOptions);
				if (webSocketExtension != null)
				{
					text = ((supportedOptions != null && supportedOptions.Count != 0) ? CreateExtensionResponseItem(webSocketExtension.Name, supportedOptions) : webSocketExtension.Name);
					list.Add(text);
					extensions.Add(webSocketExtension);
				}
			}
		}
		return list;
	}

	private string CreateExtensionResponseItem(string name, NameValueCollection options)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(name);
		string[] allKeys = options.AllKeys;
		foreach (string text in allKeys)
		{
			string value = options.Get(text);
			stringBuilder.Append("; ");
			if (string.IsNullOrEmpty(value))
			{
				stringBuilder.Append(text);
				continue;
			}
			stringBuilder.Append("; ");
			stringBuilder.Append(text);
			stringBuilder.Append("=");
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	private WebSocketEncoder GetWebSocketEncoder(IReadOnlyList<IWebSocketExtension> extensions)
	{
		if (extensions == null || !extensions.Any())
		{
			return _defaultMessageEncoder.Value;
		}
		WebSocketEncoder webSocketEncoder = _websocketEncoderFactory();
		webSocketEncoder.Extensions = extensions;
		return webSocketEncoder;
	}

	private async ValueTask<bool> HandleHandshake(IAppSession session, WebSocketPackage p)
	{
		string value = p.HttpHeader.Items["Sec-WebSocket-Version"];
		if (!"13".Equals(value))
		{
			return false;
		}
		string secWebSocketKey = p.HttpHeader.Items["Sec-WebSocket-Key"];
		if (string.IsNullOrEmpty(secWebSocketKey))
		{
			return false;
		}
		Func<WebSocketSession, WebSocketPackage, ValueTask<bool>> func = _handshakeOptions?.HandshakeValidator;
		if (func != null && !(await func(session as WebSocketSession, p)))
		{
			return false;
		}
		WebSocketSession webSocketSession = session as WebSocketSession;
		string text = p.HttpHeader.Items["Sec-WebSocket-Protocol"];
		string selectedProtocol = string.Empty;
		if (!string.IsNullOrEmpty(text) && SelectSubProtocol(text, out var selectedProtocol2, out var selectedProtocolHandler))
		{
			webSocketSession.SubProtocol = selectedProtocol2;
			webSocketSession.SubProtocolHandler = selectedProtocolHandler;
			selectedProtocol = selectedProtocol2;
		}
		List<IWebSocketExtension> extensions;
		List<string> selectedExtensionHeadItems = SelectExtensions(p.HttpHeader.Items["Sec-WebSocket-Extensions"], out extensions);
		if (selectedExtensionHeadItems != null && selectedExtensionHeadItems.Count > 0)
		{
			(session.Connection as IPipeConnection).PipelineFilter.Context = new WebSocketPipelineFilterContext
			{
				Extensions = extensions
			};
		}
		webSocketSession.MessageEncoder = GetWebSocketEncoder(extensions);
		string secKeyAccept = string.Empty;
		try
		{
			secKeyAccept = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
		}
		catch (Exception)
		{
			return false;
		}
		Encoding encoding = _textEncoding;
		await session.Connection.SendAsync(delegate(PipeWriter writer)
		{
			writer.Write("HTTP/1.1 101 Switching Protocols\r\n", encoding);
			writer.Write("Upgrade: WebSocket\r\n", encoding);
			writer.Write("Connection: Upgrade\r\n", encoding);
			writer.Write($"Sec-WebSocket-Accept: {secKeyAccept}\r\n", encoding);
			if (!string.IsNullOrEmpty(selectedProtocol))
			{
				writer.Write($"Sec-WebSocket-Protocol: {selectedProtocol}\r\n", encoding);
			}
			WriteExtensions(writer, encoding, selectedExtensionHeadItems);
			writer.Write("\r\n", encoding);
			writer.FlushAsync().GetAwaiter().GetResult();
		});
		return true;
	}

	private void WriteExtensions(PipeWriter writer, Encoding encoding, IReadOnlyList<string> selectedExtensionHeadItems)
	{
		if (selectedExtensionHeadItems == null || selectedExtensionHeadItems.Count <= 0)
		{
			return;
		}
		writer.Write("Sec-WebSocket-Extensions:", encoding);
		for (int i = 0; i < selectedExtensionHeadItems.Count; i++)
		{
			string text = selectedExtensionHeadItems[i];
			if (i % 3 == 0)
			{
				if (i != 0)
				{
					writer.Write(",\r\n\t", encoding);
				}
			}
			else
			{
				writer.Write(", ", encoding);
			}
			writer.Write(text, encoding);
		}
		writer.Write("\r\n", encoding);
	}
}
