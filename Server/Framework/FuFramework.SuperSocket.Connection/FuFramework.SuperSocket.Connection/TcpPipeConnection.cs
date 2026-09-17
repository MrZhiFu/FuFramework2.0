using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection.Sockets;
using Microsoft.Extensions.ObjectPool;

namespace FuFramework.SuperSocket.Connection;

/// <summary>
/// Represents a pipe connection for managing TCP-based connections.
/// </summary>
public class TcpPipeConnection : PipeConnection
{
	private Socket _socket;

	private readonly ObjectPool<SocketSender> _socketSenderPool;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.TcpPipeConnection" /> class with the specified socket, options, and socket sender pool.
	/// </summary>
	/// <param name="socket">The TCP socket.</param>
	/// <param name="options">The connection options.</param>
	/// <param name="socketSenderPool">The pool of socket senders, or <c>null</c> to create new senders as needed.</param>
	public TcpPipeConnection(Socket socket, ConnectionOptions options, ObjectPool<SocketSender> socketSenderPool = null)
		: base(options)
	{
		_socket = socket;
		base.RemoteEndPoint = socket.RemoteEndPoint;
		base.LocalEndPoint = socket.LocalEndPoint;
		_socketSenderPool = socketSenderPool;
	}

	/// <summary>
	/// Handles the closure of the connection.
	/// </summary>
	protected override void OnClosed()
	{
		_socket = null;
		base.OnClosed();
	}

	/// <summary>
	/// Fills the pipe with data received from the socket asynchronously.
	/// </summary>
	/// <param name="memory">The memory buffer to fill with data.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The total number of bytes read.</returns>
	protected override async ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
	{
		return await ReceiveAsync(_socket, memory, SocketFlags.None, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> memory, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		return await socket.ReceiveAsync(GetArrayByMemory(memory), socketFlags, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	/// <summary>
	/// Sends data over the connection asynchronously.
	/// </summary>
	/// <param name="buffer">The data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The total number of bytes sent.</returns>
	protected override async ValueTask<int> SendOverIoAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
	{
		ObjectPool<SocketSender> socketSenderPool = _socketSenderPool;
		SocketSender socketSender = socketSenderPool?.Get() ?? new SocketSender();
		try
		{
			int result = await socketSender.SendAsync(_socket, in buffer).ConfigureAwait(continueOnCapturedContext: false);
			if (socketSenderPool != null)
			{
				socketSenderPool.Return(socketSender);
				socketSender = null;
			}
			return result;
		}
		finally
		{
			socketSender?.Dispose();
		}
	}

	/// <summary>
	/// Closes the connection by shutting down and closing the socket.
	/// </summary>
	protected override void Close()
	{
		Socket socket = _socket;
		if (socket == null || Interlocked.CompareExchange(ref _socket, null, socket) != socket)
		{
			return;
		}
		try
		{
			socket.Shutdown(SocketShutdown.Both);
		}
		finally
		{
			socket.Close();
		}
	}

	/// <summary>
	/// Determines whether the specified exception is ignorable.
	/// </summary>
	/// <param name="e">The exception to check.</param>
	/// <returns><c>true</c> if the exception is ignorable; otherwise, <c>false</c>.</returns>
	protected override bool IsIgnorableException(Exception e)
	{
		if (base.IsIgnorableException(e))
		{
			return true;
		}
		if (e is SocketException se && se.IsIgnorableSocketException())
		{
			return true;
		}
		return false;
	}
}
