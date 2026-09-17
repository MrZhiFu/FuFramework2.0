using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Udp;

internal class UdpConnectionListener : IConnectionListener, IDisposable
{
	private ILogger _logger;

	private Socket _listenSocket;

	private IPEndPoint _acceptRemoteEndPoint;

	private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

	private CancellationTokenSource _cancellationTokenSource;

	private TaskCompletionSource<bool> _stopTaskCompletionSource;

	private IUdpSessionIdentifierProvider _udpSessionIdentifierProvider;

	private IAsyncSessionContainer _sessionContainer;

	public IConnectionFactory ConnectionFactory { get; }

	public ListenOptions Options { get; }

	public ConnectionOptions ConnectionOptions { get; }

	public bool IsRunning { get; private set; }

	public event NewConnectionAcceptHandler NewConnectionAccept;

	public UdpConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, IConnectionFactory connectionFactory, ILogger logger, IUdpSessionIdentifierProvider udpSessionIdentifierProvider, IAsyncSessionContainer sessionContainer)
	{
		Options = options;
		ConnectionOptions = connectionOptions;
		ConnectionFactory = connectionFactory;
		_logger = logger;
		_udpSessionIdentifierProvider = udpSessionIdentifierProvider;
		_sessionContainer = sessionContainer;
	}

	public bool Start()
	{
		ListenOptions options = Options;
		try
		{
			IPEndPoint iPEndPoint = options.ToEndPoint();
			Socket socket = (_listenSocket = new Socket(iPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp));
			if (options.NoDelay)
			{
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Debug, optionValue: true);
			}
			socket.ExclusiveAddressUse = options.UdpExclusiveAddressUse;
			socket.Bind(iPEndPoint);
			_acceptRemoteEndPoint = ((iPEndPoint.AddressFamily == AddressFamily.InterNetworkV6) ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0));
			uint num = 402653184u;
			uint ioControlCode = 0x80000000u | num | 0xC;
			byte[] optionInValue = new byte[1] { Convert.ToByte(value: false) };
			byte[] optionOutValue = new byte[4];
			try
			{
				socket.IOControl((int)ioControlCode, optionInValue, optionOutValue);
			}
			catch (PlatformNotSupportedException)
			{
				_logger.LogWarning("Failed to set socket option SIO_UDP_CONNRESET because the platform doesn't support it.");
			}
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
			byte[] buffer = null;
			try
			{
				int maxPackageLength = ConnectionOptions.MaxPackageLength;
				buffer = _bufferPool.Rent(maxPackageLength);
				SocketReceiveFromResult socketReceiveFromResult = await listenSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, _acceptRemoteEndPoint).ConfigureAwait(continueOnCapturedContext: false);
				ArraySegment<byte> packageData = new ArraySegment<byte>(buffer, 0, socketReceiveFromResult.ReceivedBytes);
				IPEndPoint remoteEndPoint = socketReceiveFromResult.RemoteEndPoint as IPEndPoint;
				string sessionID = _udpSessionIdentifierProvider.GetSessionIdentifier(remoteEndPoint, packageData);
				IAppSession appSession = await _sessionContainer.GetSessionByIDAsync(sessionID);
				IVirtualConnection virtualConnection;
				if (appSession != null)
				{
					virtualConnection = appSession.Connection as IVirtualConnection;
				}
				else
				{
					virtualConnection = (await CreateConnection(_listenSocket, remoteEndPoint, sessionID)) as IVirtualConnection;
					if (virtualConnection == null)
					{
						return;
					}
					OnNewConnectionAccept(virtualConnection);
				}
				await virtualConnection.WritePipeDataAsync(packageData.AsMemory(), _cancellationTokenSource.Token);
			}
			catch (Exception ex)
			{
				if (ex is ObjectDisposedException || ex is NullReferenceException || (ex is SocketException { ErrorCode: var errorCode } && (errorCode == 125 || errorCode == 89 || errorCode == 995 || errorCode == 10004 || errorCode == 10038)))
				{
					break;
				}
				_logger.LogError(ex, "Listener[" + ToString() + "] failed to receive udp data");
			}
			finally
			{
				_bufferPool.Return(buffer);
			}
		}
		_stopTaskCompletionSource.TrySetResult(result: true);
	}

	private void OnNewConnectionAccept(IConnection connection)
	{
		this.NewConnectionAccept?.Invoke(Options, connection);
	}

	private async ValueTask<IConnection> CreateConnection(Socket socket, IPEndPoint remoteEndPoint, string sessionIdentifier)
	{
		try
		{
			using CancellationTokenSourcePool.PooledCancellationTokenSource cts = CancellationTokenSourcePool.Shared.Rent(Options.ConnectionAcceptTimeOut);
			return await ConnectionFactory.CreateConnection(new UdpConnectionInfo
			{
				Socket = socket,
				SessionIdentifier = sessionIdentifier,
				RemoteEndPoint = remoteEndPoint,
				ConnectionOptions = ConnectionOptions
			}, cts.Token);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, $"Failed to create connection for {socket.RemoteEndPoint}.");
			return null;
		}
	}

	public async Task<IConnection> CreateConnection(object connection)
	{
		Socket socket = (Socket)connection;
		IPEndPoint remoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
		return await CreateConnection(socket, remoteEndPoint, _udpSessionIdentifierProvider.GetSessionIdentifier(remoteEndPoint, null));
	}

	public Task StopAsync()
	{
		Socket listenSocket = _listenSocket;
		if (listenSocket == null)
		{
			return Task.Delay(0);
		}
		_stopTaskCompletionSource = new TaskCompletionSource<bool>();
		_cancellationTokenSource.Cancel();
		listenSocket.Close();
		return _stopTaskCompletionSource.Task;
	}

	public override string ToString()
	{
		return Options?.ToString();
	}

	public void Dispose()
	{
		Socket listenSocket = _listenSocket;
		if (listenSocket != null && Interlocked.CompareExchange(ref _listenSocket, null, listenSocket) == listenSocket)
		{
			listenSocket.Dispose();
		}
	}
}
