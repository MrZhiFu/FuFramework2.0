using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Server.Connection;

/// <summary>
/// Represents a TCP connection listener that accepts and manages incoming connections.
/// </summary>
public class TcpConnectionListener : IConnectionListener, IDisposable
{
	private Socket _listenSocket;

	private CancellationTokenSource _cancellationTokenSource;

	private TaskCompletionSource<bool> _stopTaskCompletionSource;

	private ILogger _logger;

	/// <summary>
	/// Gets the connection factory used to create connections.
	/// </summary>
	public IConnectionFactory ConnectionFactory { get; }

	/// <summary>
	/// Gets the options for the listener.
	/// </summary>
	public ListenOptions Options { get; }

	/// <summary>
	/// Gets a value indicating whether the listener is running.
	/// </summary>
	public bool IsRunning { get; private set; }

	/// <summary>
	/// Occurs when a new connection is accepted.
	/// </summary>
	public event NewConnectionAcceptHandler NewConnectionAccept;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Connection.TcpConnectionListener" /> class.
	/// </summary>
	/// <param name="options">The options for the listener.</param>
	/// <param name="connectionFactory">The factory for creating connections.</param>
	/// <param name="logger">The logger for logging events.</param>
	public TcpConnectionListener(ListenOptions options, IConnectionFactory connectionFactory, ILogger logger)
	{
		Options = options;
		ConnectionFactory = connectionFactory;
		_logger = logger;
	}

	/// <summary>
	/// Starts the TCP connection listener.
	/// </summary>
	/// <returns>True if the listener started successfully; otherwise, false.</returns>
	public bool Start()
	{
		ListenOptions options = Options;
		try
		{
			IPEndPoint iPEndPoint = options.ToEndPoint();
			Socket socket = (_listenSocket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp));
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, optionValue: true);
			socket.LingerState = new LingerOption(enable: false, 0);
			if (options.NoDelay)
			{
				socket.NoDelay = true;
			}
			socket.Bind(iPEndPoint);
			socket.Listen(options.BackLog);
			IsRunning = true;
			_cancellationTokenSource = new CancellationTokenSource();
			KeepAccept(socket).DoNotAwait();
			return true;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "The listener[" + ToString() + "] failed to start.");
			return false;
		}
	}

	private async Task KeepAccept(Socket listenSocket)
	{
		while (!_cancellationTokenSource.IsCancellationRequested)
		{
			try
			{
				OnNewClientAccept(await listenSocket.AcceptAsync().ConfigureAwait(continueOnCapturedContext: false));
			}
			catch (Exception ex)
			{
				if (ex is ObjectDisposedException || ex is NullReferenceException)
				{
					break;
				}
				if (ex is SocketException { ErrorCode: var errorCode } ex2 && (errorCode == 89 || errorCode == 125 || errorCode == 995 || errorCode == 10004))
				{
					_logger.LogDebug($"The listener[{ToString()}] was closed for the socket error: {errorCode}. {ex2.Message}");
					break;
				}
				_logger.LogError(ex, "Listener[" + ToString() + "] failed to do AcceptAsync");
			}
		}
		_stopTaskCompletionSource?.TrySetResult(result: true);
	}

	private async void OnNewClientAccept(Socket socket)
	{
		NewConnectionAcceptHandler handler = this.NewConnectionAccept;
		if (handler == null)
		{
			return;
		}
		IConnection connection = null;
		try
		{
			using CancellationTokenSourcePool.PooledCancellationTokenSource cts = CancellationTokenSourcePool.Shared.Rent(Options.ConnectionAcceptTimeOut);
			connection = await ConnectionFactory.CreateConnection(socket, cts.Token);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, $"Failed to create connection for {socket.RemoteEndPoint}.");
			return;
		}
		await handler(Options, connection);
	}

	/// <summary>
	/// Stops the TCP connection listener asynchronously.
	/// </summary>
	/// <returns>A task that represents the asynchronous stop operation.</returns>
	public Task StopAsync()
	{
		Socket listenSocket = _listenSocket;
		if (listenSocket == null)
		{
			return Task.CompletedTask;
		}
		_stopTaskCompletionSource = new TaskCompletionSource<bool>();
		_cancellationTokenSource.Cancel();
		listenSocket.Close();
		return _stopTaskCompletionSource.Task;
	}

	/// <summary>
	/// Releases the resources used by the listener.
	/// </summary>
	public void Dispose()
	{
		Socket listenSocket = _listenSocket;
		if (listenSocket != null && Interlocked.CompareExchange(ref _listenSocket, null, listenSocket) == listenSocket)
		{
			listenSocket.Dispose();
		}
	}

	public override string ToString()
	{
		return Options?.ToString();
	}
}
