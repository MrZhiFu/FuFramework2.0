using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.ProtoBase.ProxyProtocol;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Represents a SuperSocket service that handles connections and sessions.
/// </summary>
/// <typeparam name="TReceivePackageInfo">The type of the package information received.</typeparam>
public class SuperSocketService<TReceivePackageInfo> : ISuperSocketHostedService, IHostedService, IServer, IServerInfo, IDisposable, IAsyncDisposable, IConnectionRegister, ILoggerAccessor, ISessionEventHost
{
	private readonly IServiceProvider _serviceProvider;

	private readonly ILoggerFactory _loggerFactory;

	private readonly ILogger _logger;

	private IPipelineFilterFactory<TReceivePackageInfo> _pipelineFilterFactory;

	private IConnectionListenerFactory _connectionListenerFactory;

	private List<IConnectionListener> _connectionListeners;

	private IPackageHandlingScheduler<TReceivePackageInfo> _packageHandlingScheduler;

	private IPackageHandlingContextAccessor<TReceivePackageInfo> _packageHandlingContextAccessor;

	private int _sessionCount;

	private ISessionFactory _sessionFactory;

	private IMiddleware[] _middlewares;

	private ServerState _state;

	private SessionHandlers _sessionHandlers;

	private bool disposedValue;

	/// <summary>
	/// Gets the service provider for dependency injection.
	/// </summary>
	public IServiceProvider ServiceProvider => _serviceProvider;

	/// <summary>
	/// Gets the server options for configuration.
	/// </summary>
	public ServerOptions Options { get; }

	protected internal ILogger Logger => _logger;

	ILogger ILoggerAccessor.Logger => _logger;

	/// <summary>
	/// Gets the name of the server.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the current session count.
	/// </summary>
	public int SessionCount => _sessionCount;

	protected IMiddleware[] Middlewares => _middlewares;

	public ServerState State => _state;

	public object DataContext { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.SuperSocketService`1" /> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider for dependency injection.</param>
	/// <param name="serverOptions">The server options for configuration.</param>
	public SuperSocketService(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions)
	{
		if (serviceProvider == null)
		{
			throw new ArgumentNullException("serviceProvider");
		}
		if (serverOptions == null)
		{
			throw new ArgumentNullException("serverOptions");
		}
		Name = serverOptions.Value.Name;
		Options = serverOptions.Value;
		_serviceProvider = serviceProvider;
		_pipelineFilterFactory = GetPipelineFilterFactory();
		_loggerFactory = serviceProvider.GetService<ILoggerFactory>();
		_logger = _loggerFactory.CreateLogger("SuperSocketService");
		_connectionListenerFactory = serviceProvider.GetService<IConnectionListenerFactory>();
		_sessionHandlers = serviceProvider.GetService<SessionHandlers>();
		_sessionFactory = serviceProvider.GetService<ISessionFactory>();
		_packageHandlingContextAccessor = serviceProvider.GetService<IPackageHandlingContextAccessor<TReceivePackageInfo>>();
		InitializeMiddlewares();
		IPackageHandler<TReceivePackageInfo> packageHandler = serviceProvider.GetService<IPackageHandler<TReceivePackageInfo>>() ?? _middlewares.OfType<IPackageHandler<TReceivePackageInfo>>().FirstOrDefault();
		if (packageHandler == null)
		{
			Logger.LogWarning("The PackageHandler cannot be found.");
			return;
		}
		Func<IAppSession, PackageHandlingException<TReceivePackageInfo>, ValueTask<bool>> errorHandler = serviceProvider.GetService<Func<IAppSession, PackageHandlingException<TReceivePackageInfo>, ValueTask<bool>>>() ?? new Func<IAppSession, PackageHandlingException<TReceivePackageInfo>, ValueTask<bool>>(OnSessionErrorAsync);
		_packageHandlingScheduler = serviceProvider.GetService<IPackageHandlingScheduler<TReceivePackageInfo>>() ?? new SerialPackageHandlingScheduler<TReceivePackageInfo>();
		_packageHandlingScheduler.Initialize(packageHandler, errorHandler);
	}

	protected virtual IPipelineFilterFactory<TReceivePackageInfo> GetPipelineFilterFactory()
	{
		IPipelineFilterFactory<TReceivePackageInfo> pipelineFilterFactory = _serviceProvider.GetRequiredService<IPipelineFilterFactory<TReceivePackageInfo>>();
		if (Options.EnableProxyProtocol)
		{
			pipelineFilterFactory = new ProxyProtocolPipelineFilterFactory<TReceivePackageInfo>(pipelineFilterFactory);
		}
		return pipelineFilterFactory;
	}

	private bool AddConnectionListener(ListenOptions listenOptions, ServerOptions serverOptions)
	{
		IConnectionListener connectionListener = _connectionListenerFactory.CreateConnectionListener(listenOptions, serverOptions, _loggerFactory);
		connectionListener.NewConnectionAccept += OnNewConnectionAccept;
		if (!connectionListener.Start())
		{
			_logger.LogError($"Failed to listen {connectionListener}.");
			return false;
		}
		_logger.LogInformation($"The listener [{connectionListener}] has been started.");
		_connectionListeners.Add(connectionListener);
		return true;
	}

	private Task<bool> StartListenAsync(CancellationToken cancellationToken)
	{
		_connectionListeners = new List<IConnectionListener>();
		ServerOptions options = Options;
		if (options.Listeners != null && options.Listeners.Any())
		{
			foreach (ListenOptions listener in options.Listeners)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				AddConnectionListener(listener, options);
			}
		}
		else
		{
			_logger.LogWarning("No listener was defined, so this server only can accept connections from the ActiveConnect.");
			if (!AddConnectionListener(null, options))
			{
				_logger.LogError("Failed to add the connection creator.");
				return Task.FromResult(result: false);
			}
		}
		return Task.FromResult(_connectionListeners.Any());
	}

	protected virtual ValueTask OnNewConnectionAccept(ListenOptions listenOptions, IConnection connection)
	{
		return AcceptNewConnection(connection);
	}

	private ValueTask AcceptNewConnection(IConnection connection)
	{
		AppSession session = _sessionFactory.Create() as AppSession;
		return HandleSession(session, connection);
	}

	async Task IConnectionRegister.RegisterConnection(object connectionSource)
	{
		IConnectionListener connectionListener = _connectionListeners.FirstOrDefault();
		using CancellationTokenSourcePool.PooledCancellationTokenSource cts = CancellationTokenSourcePool.Shared.Rent(connectionListener.Options.ConnectionAcceptTimeOut);
		await AcceptNewConnection(await connectionListener.ConnectionFactory.CreateConnection(connectionSource, cts.Token));
	}

	protected virtual object CreatePipelineContext(IAppSession session)
	{
		return session;
	}

	private void InitializeMiddlewares()
	{
		_middlewares = (from m in _serviceProvider.GetServices<IMiddleware>()
			orderby m.Order
			select m).ToArray();
	}

	private void ShutdownMiddlewares()
	{
		IMiddleware[] middlewares = _middlewares;
		foreach (IMiddleware middleware in middlewares)
		{
			try
			{
				middleware.Shutdown(this);
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "The exception was thrown from the middleware " + middleware.GetType().Name + " when it is being shutdown.");
			}
		}
	}

	private async ValueTask<bool> RegisterSessionInMiddlewares(IAppSession session)
	{
		IMiddleware[] middlewares = _middlewares;
		if (middlewares != null && middlewares.Length != 0)
		{
			foreach (IMiddleware middleware in middlewares)
			{
				if (!(await middleware.RegisterSession(session)))
				{
					_logger.LogWarning($"A session from {session.RemoteEndPoint} was rejected by the middleware {middleware.GetType().Name}.");
					return false;
				}
			}
		}
		return true;
	}

	private async ValueTask UnRegisterSessionFromMiddlewares(IAppSession session)
	{
		IMiddleware[] middlewares = _middlewares;
		if (middlewares == null || middlewares.Length == 0)
		{
			return;
		}
		foreach (IMiddleware middleware in middlewares)
		{
			try
			{
				if (!(await middleware.UnRegisterSession(session)))
				{
					_logger.LogWarning($"The session from {session.RemoteEndPoint} was failed to be unregistered from the middleware {middleware.GetType().Name}.");
				}
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, $"An unhandled exception occured when the session from {session.RemoteEndPoint} was being unregistered from the middleware {"RegisterSessionInMiddlewares"}.");
			}
		}
	}

	private async ValueTask<bool> InitializeSession(IAppSession session, IConnection connection)
	{
		session.Initialize(this, connection);
		_ = _middlewares;
		try
		{
			if (!(await RegisterSessionInMiddlewares(session)))
			{
				return false;
			}
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "An unhandled exception occured in RegisterSessionInMiddlewares.");
			return false;
		}
		connection.Closed += delegate(object? s, CloseEventArgs e)
		{
			OnConnectionClosed(session, e);
		};
		return true;
	}

	protected virtual ValueTask OnSessionConnectedAsync(IAppSession session)
	{
		return (_sessionHandlers?.Connected)?.Invoke(session) ?? default(ValueTask);
	}

	private void OnConnectionClosed(IAppSession session, CloseEventArgs e)
	{
		FireSessionClosedEvent(session as AppSession, e.Reason).DoNotAwait();
	}

	protected virtual ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
	{
		return (_sessionHandlers?.Closed)?.Invoke(session, e) ?? ValueTask.CompletedTask;
	}

	protected virtual async ValueTask FireSessionConnectedEvent(AppSession session)
	{
		if (session is IHandshakeRequiredSession { Handshaked: false })
		{
			return;
		}
		_logger.LogInformation("A new session connected: " + session.SessionID);
		try
		{
			Interlocked.Increment(ref _sessionCount);
			await session.FireSessionConnectedAsync();
			await OnSessionConnectedAsync(session);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "There is one exception thrown from the event handler of SessionConnected.");
		}
	}

	protected virtual async ValueTask FireSessionClosedEvent(AppSession session, CloseReason reason)
	{
		if (session is IHandshakeRequiredSession { Handshaked: false })
		{
			return;
		}
		await UnRegisterSessionFromMiddlewares(session);
		_logger.LogInformation($"The session disconnected: {session.SessionID} ({reason})");
		try
		{
			Interlocked.Decrement(ref _sessionCount);
			CloseEventArgs closeEventArgs = new CloseEventArgs(reason);
			await session.FireSessionClosedAsync(closeEventArgs);
			await OnSessionClosedAsync(session, closeEventArgs);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "There is one exception thrown from the event of OnSessionClosed.");
		}
	}

	ValueTask ISessionEventHost.HandleSessionConnectedEvent(IAppSession session)
	{
		return FireSessionConnectedEvent((AppSession)session);
	}

	ValueTask ISessionEventHost.HandleSessionClosedEvent(IAppSession session, CloseReason reason)
	{
		return FireSessionClosedEvent((AppSession)session, reason);
	}

	private async ValueTask HandleSession(AppSession session, IConnection connection)
	{
		if (!(await InitializeSession(session, connection)))
		{
			return;
		}
		try
		{
			IPipelineFilter<TReceivePackageInfo> pipelineFilter = _pipelineFilterFactory.Create(connection);
			pipelineFilter.Context = CreatePipelineContext(session);
			IAsyncEnumerable<TReceivePackageInfo> packageStream = connection.RunAsync(pipelineFilter);
			await FireSessionConnectedEvent(session);
			IPackageHandlingScheduler<TReceivePackageInfo> packageHandlingScheduler = _packageHandlingScheduler;
			using CancellationTokenSource cancellationTokenSource = GetPackageHandlingCancellationTokenSource(connection.ConnectionToken);
			ValueTask prevPackageHandlingTask = ValueTask.CompletedTask;
			await foreach (TReceivePackageInfo p in packageStream)
			{
				if (_packageHandlingContextAccessor != null)
				{
					_packageHandlingContextAccessor.PackageHandlingContext = new PackageHandlingContext<IAppSession, TReceivePackageInfo>(session, p);
				}
				if (prevPackageHandlingTask != ValueTask.CompletedTask)
				{
					await prevPackageHandlingTask;
				}
				prevPackageHandlingTask = packageHandlingScheduler.HandlePackage(session, p, cancellationTokenSource.Token);
				cancellationTokenSource.TryReset();
			}
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to handle the session " + session.SessionID + ".");
		}
	}

	protected virtual CancellationTokenSource GetPackageHandlingCancellationTokenSource(CancellationToken cancellationToken)
	{
		CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Options.PackageHandlingTimeOut));
		return cancellationTokenSource;
	}

	protected virtual ValueTask<bool> OnSessionErrorAsync(IAppSession session, PackageHandlingException<TReceivePackageInfo> exception)
	{
		_logger.LogError(exception, "Session[" + session.SessionID + "]: session exception.");
		return new ValueTask<bool>(result: true);
	}

	/// <summary>
	/// Starts the SuperSocket service asynchronously.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task<bool> StartAsync(CancellationToken cancellationToken)
	{
		ServerState state = _state;
		if (state != 0 && state != ServerState.Stopped && state != ServerState.Failed)
		{
			throw new InvalidOperationException($"The server cannot be started right now, because its state is {state}.");
		}
		_state = ServerState.Starting;
		IMiddleware[] middlewares = _middlewares;
		for (int i = 0; i < middlewares.Length; i++)
		{
			middlewares[i].Start(this);
		}
		if (!(await StartListenAsync(cancellationToken)))
		{
			_state = ServerState.Failed;
			_logger.LogError("Failed to start any listener.");
			return false;
		}
		_state = ServerState.Started;
		try
		{
			await OnStartedAsync();
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "There is one exception thrown from the method OnStartedAsync().");
		}
		return true;
	}

	protected virtual ValueTask OnStartedAsync()
	{
		return ValueTask.CompletedTask;
	}

	protected virtual ValueTask OnStopAsync()
	{
		return ValueTask.CompletedTask;
	}

	private async Task StopListener(IConnectionListener listener)
	{
		await listener.StopAsync().ConfigureAwait(continueOnCapturedContext: false);
		_logger.LogInformation($"The listener [{listener}] has been stopped.");
	}

	/// <summary>
	/// Stops the SuperSocket service asynchronously.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		ServerState state = _state;
		if (state != ServerState.Started)
		{
			throw new InvalidOperationException($"The server cannot be stopped right now, because its state is {state}.");
		}
		_state = ServerState.Stopping;
		await Task.WhenAll((from l in _connectionListeners
			where l.IsRunning
			select StopListener(l)).Union(new Task[1] { Task.Run((Action)ShutdownMiddlewares) })).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await OnStopAsync();
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "There is an exception thrown from the method OnStopAsync().");
		}
		_state = ServerState.Stopped;
	}

	async Task IHostedService.StartAsync(CancellationToken cancellationToken)
	{
		if (!(await StartAsync(cancellationToken)))
		{
			throw new Exception("Failed to start the server.");
		}
	}

	ValueTask IAsyncDisposable.DisposeAsync()
	{
		return DisposeAsync(disposing: true);
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if (disposedValue)
		{
			return;
		}
		if (disposing)
		{
			try
			{
				if (_state == ServerState.Started)
				{
					await StopAsync(CancellationToken.None);
				}
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Failed to stop the server");
			}
			List<IConnectionListener> connectionListeners = _connectionListeners;
			if (connectionListeners != null && connectionListeners.Any())
			{
				foreach (IConnectionListener item in connectionListeners)
				{
					item.Dispose();
				}
			}
		}
		disposedValue = true;
	}

	protected virtual void Dispose(bool disposing)
	{
		DisposeAsync(disposing).GetAwaiter().GetResult();
	}

	void IDisposable.Dispose()
	{
		DisposeAsync(disposing: true).GetAwaiter().GetResult();
	}
}
