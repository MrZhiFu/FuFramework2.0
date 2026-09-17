using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.ObjectPool;

namespace FuFramework.SuperSocket.Connection.Sockets;

/// <summary>
/// Represents a sender for asynchronous socket operations using <see cref="T:System.Net.Sockets.SocketAsyncEventArgs" />.
/// </summary>
public class SocketSender : SocketAsyncEventArgs, IValueTaskSource<int>, IResettable
{
	private Action<object> _continuation;

	private static readonly Action<object> _continuationCompleted = delegate
	{
	};

	private List<ArraySegment<byte>> _bufferList;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.Sockets.SocketSender" /> class.
	/// </summary>
	public SocketSender()
		: base(unsafeSuppressExecutionContextFlow: true)
	{
	}

	/// <summary>
	/// Sends data asynchronously over the specified socket.
	/// </summary>
	/// <param name="socket">The socket to send data over.</param>
	/// <param name="buffer">The data to send.</param>
	/// <returns>A <see cref="T:System.Threading.Tasks.ValueTask`1" /> representing the asynchronous send operation.</returns>
	internal ValueTask<int> SendAsync(Socket socket, in ReadOnlySequence<byte> buffer)
	{
		SetBuffer(in buffer);
		if (socket.SendAsync(this))
		{
			return new ValueTask<int>(this, 0);
		}
		if (base.SocketError == SocketError.Success)
		{
			return new ValueTask<int>(base.BytesTransferred);
		}
		return new ValueTask<int>(Task.FromException<int>(new SocketException((int)base.SocketError)));
	}

	private void SetBuffer(in ReadOnlySequence<byte> buffer)
	{
		if (buffer.IsSingleSegment)
		{
			ArraySegment<byte> arrayByMemory = GetArrayByMemory(buffer.First);
			SetBuffer(arrayByMemory.Array, arrayByMemory.Offset, arrayByMemory.Count);
			return;
		}
		List<ArraySegment<byte>> list = _bufferList;
		if (list == null)
		{
			list = (_bufferList = new List<ArraySegment<byte>>());
		}
		ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> current = enumerator.Current;
			list.Add(GetArrayByMemory(current));
		}
		base.BufferList = list;
	}

	/// <summary>
	/// Handles the completion of the asynchronous socket operation.
	/// </summary>
	/// <param name="e">The <see cref="T:System.Net.Sockets.SocketAsyncEventArgs" /> instance containing event data.</param>
	protected override void OnCompleted(SocketAsyncEventArgs e)
	{
		Action<object> continuation = _continuation;
		if (continuation != null || Interlocked.CompareExchange(ref _continuation, _continuationCompleted, null) != null)
		{
			object userToken = base.UserToken;
			base.UserToken = null;
			_continuation = _continuationCompleted;
			ThreadPool.UnsafeQueueUserWorkItem(continuation, userToken, preferLocal: false);
		}
	}

	/// <summary>
	/// Gets the result of the asynchronous operation.
	/// </summary>
	/// <param name="token">The token associated with the operation.</param>
	/// <returns>The number of bytes transferred.</returns>
	public int GetResult(short token)
	{
		_continuation = null;
		return base.BytesTransferred;
	}

	/// <summary>
	/// Gets the status of the asynchronous operation.
	/// </summary>
	/// <param name="token">The token associated with the operation.</param>
	/// <returns>The status of the operation.</returns>
	public ValueTaskSourceStatus GetStatus(short token)
	{
		if ((object)_continuation != _continuationCompleted)
		{
			return ValueTaskSourceStatus.Pending;
		}
		if (base.SocketError != 0)
		{
			return ValueTaskSourceStatus.Faulted;
		}
		return ValueTaskSourceStatus.Succeeded;
	}

	/// <summary>
	/// Schedules the continuation action for the asynchronous operation.
	/// </summary>
	/// <param name="continuation">The continuation action to invoke.</param>
	/// <param name="state">The state to pass to the continuation action.</param>
	/// <param name="token">The token associated with the operation.</param>
	/// <param name="flags">Flags that control the behavior of the continuation.</param>
	public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
	{
		base.UserToken = state;
		if ((object)Interlocked.CompareExchange(ref _continuation, continuation, null) == _continuationCompleted)
		{
			base.UserToken = null;
			ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
		}
	}

	/// <summary>
	/// Attempts to reset the state of the sender.
	/// </summary>
	/// <returns><c>true</c> if the state was successfully reset; otherwise, <c>false</c>.</returns>
	public bool TryReset()
	{
		if (base.BufferList != null)
		{
			base.BufferList = null;
			_bufferList?.Clear();
		}
		else
		{
			SetBuffer(null, 0, 0);
		}
		return true;
	}

	private ArraySegment<byte> GetArrayByMemory(ReadOnlyMemory<byte> memory)
	{
		if (!MemoryMarshal.TryGetArray(memory, out var segment))
		{
			throw new InvalidOperationException("Buffer backed by array was expected");
		}
		return segment;
	}
}
