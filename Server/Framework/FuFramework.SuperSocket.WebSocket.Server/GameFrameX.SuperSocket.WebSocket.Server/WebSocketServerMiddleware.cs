using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.WebSocket.Server;

/// <summary>
/// Represents middleware for managing WebSocket server sessions.
/// </summary>
internal class WebSocketServerMiddleware : MiddlewareBase, IWebSocketServerMiddleware
{
	private ConcurrentQueue<WebSocketSession> _openHandshakePendingQueue = new ConcurrentQueue<WebSocketSession>();

	private ConcurrentQueue<WebSocketSession> _closeHandshakePendingQueue = new ConcurrentQueue<WebSocketSession>();

	private Timer _checkingTimer;

	private readonly HandshakeOptions _options;

	private IMiddleware _sessionContainerMiddleware;

	private ISessionEventHost _sessionEventHost;

	public int OpenHandshakePendingQueueLength => _openHandshakePendingQueue.Count;

	public int CloseHandshakePendingQueueLength => _closeHandshakePendingQueue.Count;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.WebSocket.Server.WebSocketServerMiddleware" /> class.
	/// </summary>
	/// <param name="handshakeOptions">The handshake options.</param>
	public WebSocketServerMiddleware(IOptions<HandshakeOptions> handshakeOptions)
	{
		HandshakeOptions handshakeOptions2 = handshakeOptions.Value;
		if (handshakeOptions2 == null)
		{
			handshakeOptions2 = new HandshakeOptions();
		}
		_options = handshakeOptions2;
	}

	/// <summary>
	/// Starts the middleware with the specified server.
	/// </summary>
	/// <param name="server">The server instance.</param>
	public override void Start(IServer server)
	{
		_sessionContainerMiddleware = server.GetSessionContainer() as IMiddleware;
		_sessionEventHost = server as ISessionEventHost;
		_checkingTimer = new Timer(HandshakePendingQueueCheckingCallback, null, _options.CheckingInterval * 1000, _options.CheckingInterval * 1000);
	}

	/// <summary>
	/// Shuts down the middleware with the specified server.
	/// </summary>
	/// <param name="server">The server instance.</param>
	public override void Shutdown(IServer server)
	{
		_sessionContainerMiddleware = null;
		Timer checkingTimer = _checkingTimer;
		if (checkingTimer != null && Interlocked.CompareExchange(ref _checkingTimer, null, checkingTimer) == checkingTimer)
		{
			checkingTimer.Change(-1, -1);
			checkingTimer.Dispose();
		}
	}

	/// <summary>
	/// Registers a session with the middleware asynchronously.
	/// </summary>
	/// <param name="session">The session to register.</param>
	/// <returns>A task that represents the asynchronous registration operation.</returns>
	public override ValueTask<bool> RegisterSession(IAppSession session)
	{
		WebSocketSession item = session as WebSocketSession;
		_openHandshakePendingQueue.Enqueue(item);
		return new ValueTask<bool>(result: true);
	}

	private void OnCloseHandshakeStarted(object sender, EventArgs e)
	{
		WebSocketSession webSocketSession = sender as WebSocketSession;
		webSocketSession.CloseHandshakeStarted -= OnCloseHandshakeStarted;
		_closeHandshakePendingQueue.Enqueue(webSocketSession);
	}

	private void HandshakePendingQueueCheckingCallback(object state)
	{
		_checkingTimer.Change(-1, -1);
		Task task = Task.Run(delegate
		{
			WebSocketSession result;
			while (_openHandshakePendingQueue.TryPeek(out result))
			{
				if (!result.Handshaked && result.State != SessionState.Closed)
				{
					IAppSession appSession = result;
					if (appSession == null || !appSession.Connection.IsClosed)
					{
						if (DateTime.Now < result.StartTime.AddSeconds(_options.OpenHandshakeTimeOut))
						{
							break;
						}
						_openHandshakePendingQueue.TryDequeue(out result);
						result.CloseWithoutHandshake();
						continue;
					}
				}
				_openHandshakePendingQueue.TryDequeue(out result);
			}
		});
		Task task2 = Task.Run(delegate
		{
			WebSocketSession result2;
			while (_closeHandshakePendingQueue.TryPeek(out result2))
			{
				if (result2.State == SessionState.Closed)
				{
					_closeHandshakePendingQueue.TryDequeue(out result2);
				}
				else
				{
					if (DateTime.Now < result2.CloseHandshakeStartTime.AddSeconds(_options.CloseHandshakeTimeOut))
					{
						break;
					}
					_closeHandshakePendingQueue.TryDequeue(out result2);
					result2.CloseWithoutHandshake();
				}
			}
		});
		Task.WhenAll(task, task2);
		_checkingTimer?.Change(_options.CheckingInterval * 1000, _options.CheckingInterval * 1000);
	}

	/// <summary>
	/// Handles the completion of a session handshake.
	/// </summary>
	/// <param name="session">The WebSocket session.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public ValueTask HandleSessionHandshakeCompleted(WebSocketSession session)
	{
		session.CloseHandshakeStarted += OnCloseHandshakeStarted;
		_sessionContainerMiddleware?.RegisterSession(session);
		return _sessionEventHost.HandleSessionConnectedEvent(session);
	}
}
