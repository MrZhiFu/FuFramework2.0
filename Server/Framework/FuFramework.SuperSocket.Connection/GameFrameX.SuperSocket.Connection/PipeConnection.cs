using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Connection;

/// <summary>
/// Represents a pipe-based connection with input and output capabilities.
/// </summary>
public abstract class PipeConnection : PipeConnectionBase
{
	private readonly TimeSpan sendTimeout = TimeSpan.FromSeconds(30.0);

	/// <summary>
	/// Gets the input pipe for the connection.
	/// </summary>
	protected Pipe Input { get; }

	/// <summary>
	/// Gets the output pipe for the connection.
	/// </summary>
	protected Pipe Output { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.PipeConnection" /> class with the specified connection options.
	/// </summary>
	/// <param name="options">The connection options.</param>
	public PipeConnection(ConnectionOptions options)
		: this(GetInputPipe(options), GetOutputPipe(options), options)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.PipeConnection" /> class with the specified input and output pipes and connection options.
	/// </summary>
	/// <param name="input">The input pipe.</param>
	/// <param name="output">The output pipe.</param>
	/// <param name="options">The connection options.</param>
	public PipeConnection(Pipe input, Pipe output, ConnectionOptions options)
		: base(input.Reader, output.Writer, options)
	{
		Input = input;
		Output = output;
		if (options.SendTimeout > 0)
		{
			sendTimeout = TimeSpan.FromMilliseconds(options.SendTimeout);
		}
	}

	private static Pipe GetInputPipe(ConnectionOptions connectionOptions)
	{
		return connectionOptions.Input ?? new Pipe();
	}

	private static Pipe GetOutputPipe(ConnectionOptions connectionOptions)
	{
		return connectionOptions.Output ?? new Pipe();
	}

	/// <inheritdoc />
	protected override async Task GetConnectionTask(Task readTask, CancellationToken cancellationToken)
	{
		await Task.WhenAll(FillPipeAsync(Input.Writer, cancellationToken), ProcessSends()).ConfigureAwait(continueOnCapturedContext: false);
		await base.GetConnectionTask(readTask, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	/// <summary>
	/// Processes send operations for the connection.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected virtual async Task ProcessSends()
	{
		PipeReader output = Output.Reader;
		while (!(await ProcessOutputRead(output).ConfigureAwait(continueOnCapturedContext: false)))
		{
		}
		output.Complete();
	}

	/// <summary>
	/// Fills the pipe with data asynchronously.
	/// </summary>
	/// <param name="writer">The pipe writer to write data to.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	internal virtual async Task FillPipeAsync(PipeWriter writer, CancellationToken cancellationToken)
	{
		ConnectionOptions options = base.Options;
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				int num = options.ReceiveBufferSize;
				_ = options.MaxPackageLength;
				if (num <= 0)
				{
					num = 4096;
				}
				Memory<byte> memory = writer.GetMemory(num);
				int num2 = await FillPipeWithDataAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num2 == 0)
				{
					if (!base.CloseReason.HasValue)
					{
						base.CloseReason = FuFramework.SuperSocket.Connection.CloseReason.RemoteClosing;
					}
					break;
				}
				UpdateLastActiveTime();
				writer.Advance(num2);
			}
			catch (Exception ex)
			{
				if (!IsIgnorableException(ex))
				{
					if (!(ex is OperationCanceledException))
					{
						OnError("Exception happened in ReceiveAsync", ex);
					}
					if (!base.CloseReason.HasValue)
					{
						base.CloseReason = (cancellationToken.IsCancellationRequested ? FuFramework.SuperSocket.Connection.CloseReason.LocalClosing : FuFramework.SuperSocket.Connection.CloseReason.SocketError);
					}
				}
				else if (!base.CloseReason.HasValue)
				{
					base.CloseReason = FuFramework.SuperSocket.Connection.CloseReason.RemoteClosing;
				}
				break;
			}
			if ((await writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false)).IsCompleted)
			{
				break;
			}
		}
		await writer.CompleteAsync().ConfigureAwait(continueOnCapturedContext: false);
		await Output.Writer.CompleteAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	/// <summary>
	/// Processes output data read from the pipe.
	/// </summary>
	/// <param name="reader">The pipe reader to read data from.</param>
	/// <returns>A value task that represents the asynchronous operation. The result indicates whether the operation is completed or cancelled.</returns>
	protected async ValueTask<bool> ProcessOutputRead(PipeReader reader)
	{
		ReadResult readResult = await reader.ReadAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		if (readResult.IsCanceled)
		{
			return true;
		}
		bool completedOrCancelled = readResult.IsCompleted || readResult.IsCanceled;
		ReadOnlySequence<byte> buffer = readResult.Buffer;
		SequencePosition end = buffer.End;
		if (!buffer.IsEmpty)
		{
			try
			{
				ValueTask<int> valueTask = SendOverIoAsync(buffer, CancellationToken.None);
				if (!valueTask.IsCompleted)
				{
					await valueTask.AsTask().WaitAsync(sendTimeout).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await valueTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				UpdateLastActiveTime();
			}
			catch (Exception e)
			{
				await CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (!IsIgnorableException(e))
				{
					OnError("Exception happened in SendAsync", e);
				}
				return true;
			}
		}
		reader.AdvanceTo(end);
		return completedOrCancelled;
	}

	/// <summary>
	/// Sends data over the connection asynchronously using the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The total number of bytes sent.</returns>
	protected abstract ValueTask<int> SendOverIoAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken);

	/// <summary>
	/// Gets an array segment from the specified memory buffer.
	/// </summary>
	/// <param name="memory">The memory buffer to extract the array segment from.</param>
	/// <returns>The array segment representing the memory buffer.</returns>
	/// <exception cref="T:System.InvalidOperationException">Thrown if the memory buffer is not backed by an array.</exception>
	protected internal ArraySegment<byte> GetArrayByMemory(ReadOnlyMemory<byte> memory)
	{
		if (!MemoryMarshal.TryGetArray(memory, out var segment))
		{
			throw new InvalidOperationException("Buffer backed by array was expected");
		}
		return segment;
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

	/// <summary>
	/// Fills the pipe with data asynchronously.
	/// </summary>
	/// <param name="memory">The memory buffer to fill.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The total number of bytes read.</returns>
	protected abstract ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken);
}
