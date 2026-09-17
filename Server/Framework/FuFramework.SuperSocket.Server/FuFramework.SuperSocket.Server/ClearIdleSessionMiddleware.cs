using System;
using System.Threading;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Middleware for clearing idle sessions based on their last active time.
/// </summary>
internal class ClearIdleSessionMiddleware : MiddlewareBase
{
	private ISessionContainer _sessionContainer;

	private Timer _timer;

	private ServerOptions _serverOptions;

	private ILogger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.ClearIdleSessionMiddleware" /> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider to resolve dependencies.</param>
	/// <param name="serverOptions">The server options containing configuration values.</param>
	/// <param name="loggerFactory">The logger factory to create loggers.</param>
	/// <exception cref="T:System.Exception">Thrown if the required <see cref="T:FuFramework.SuperSocket.Server.Abstractions.Session.ISessionContainer" /> middleware is not available.</exception>
	public ClearIdleSessionMiddleware(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions, ILoggerFactory loggerFactory)
	{
		_sessionContainer = serviceProvider.GetService<ISessionContainer>();
		if (_sessionContainer == null)
		{
			throw new Exception("ClearIdleSessionMiddleware needs a middleware of ISessionContainer");
		}
		_serverOptions = serverOptions.Value;
		_logger = loggerFactory.CreateLogger<ClearIdleSessionMiddleware>();
	}

	/// <summary>
	/// Starts the middleware and initializes the timer for clearing idle sessions.
	/// </summary>
	/// <param name="server">The server instance.</param>
	public override void Start(IServer server)
	{
		_timer = new Timer(OnTimerCallback, null, _serverOptions.ClearIdleSessionInterval * 1000, _serverOptions.ClearIdleSessionInterval * 1000);
	}

	private void OnTimerCallback(object state)
	{
		_timer.Change(-1, -1);
		try
		{
			DateTimeOffset dateTimeOffset = DateTimeOffset.Now.AddSeconds(-_serverOptions.IdleSessionTimeOut);
			foreach (IAppSession session in _sessionContainer.GetSessions())
			{
				if (session.LastActiveTime <= dateTimeOffset)
				{
					try
					{
						session.Connection.CloseAsync(CloseReason.TimeOut);
						_logger.LogWarning($"Close the idle session {session.SessionID}, it's LastActiveTime is {session.LastActiveTime}.");
					}
					catch (Exception exception)
					{
						_logger.LogError(exception, "Error happened when close the session " + session.SessionID + " for inactive for a while.");
					}
				}
			}
		}
		catch (Exception exception2)
		{
			_logger.LogError(exception2, "Error happened when clear idle session.");
		}
		_timer.Change(_serverOptions.ClearIdleSessionInterval * 1000, _serverOptions.ClearIdleSessionInterval * 1000);
	}

	/// <summary>
	/// Shuts down the middleware and disposes of the timer.
	/// </summary>
	/// <param name="server">The server instance.</param>
	public override void Shutdown(IServer server)
	{
		_timer.Change(-1, -1);
		_timer.Dispose();
		_timer = null;
	}
}
