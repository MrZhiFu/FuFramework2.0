using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.ProtoBase.ProxyProtocol;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Connection;

/// <summary>
/// Provides a base class for pipe-based connections, implementing common connection functionality.
/// </summary>
public abstract class PipeConnectionBase : ConnectionBase, IConnection, IPipeConnection
{
	internal struct BufferFilterResult<TPackageInfo>
	{
		public Exception Exception { get; set; }

		public TPackageInfo Package { get; set; }

		public long Consumed { get; set; }

		public BufferFilterResult(Exception exception)
		{
			Exception = exception;
			Package = default(TPackageInfo);
			Consumed = 0L;
		}

		public BufferFilterResult(TPackageInfo packageInfo, long consumed = 0L)
		{
			Package = packageInfo;
			Consumed = consumed;
			Exception = null;
		}
	}

	private CancellationTokenSource _cts = new CancellationTokenSource();

	private IPipelineFilter _pipelineFilter;

	private Task _connectionTask;

	private bool _isDetaching;

	/// <summary>
	/// Gets the semaphore used to synchronize send operations.
	/// </summary>
	protected SemaphoreSlim SendLock { get; } = new SemaphoreSlim(1, 1);

	/// <summary>
	/// Gets the pipe writer for output data.
	/// </summary>
	protected PipeWriter OutputWriter { get; }

	PipeWriter IPipeConnection.OutputWriter => OutputWriter;

	/// <summary>
	/// Gets the pipe reader for input data.
	/// </summary>
	protected PipeReader InputReader { get; }

	PipeReader IPipeConnection.InputReader => InputReader;

	IPipelineFilter IPipeConnection.PipelineFilter => _pipelineFilter;

	/// <summary>
	/// Gets the logger used for logging connection events.
	/// </summary>
	protected ILogger Logger { get; }

	/// <summary>
	/// Gets the connection options.
	/// </summary>
	protected ConnectionOptions Options { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.PipeConnectionBase" /> class with the specified input and output pipes and connection options.
	/// </summary>
	/// <param name="inputReader">The pipe reader for input data.</param>
	/// <param name="outputWriter">The pipe writer for output data.</param>
	/// <param name="options">The connection options.</param>
	protected PipeConnectionBase(PipeReader inputReader, PipeWriter outputWriter, ConnectionOptions options)
	{
		Options = options;
		Logger = options.Logger;
		InputReader = inputReader;
		OutputWriter = outputWriter;
		base.ConnectionToken = _cts.Token;
	}

	/// <summary>
	/// Updates the last active time of the connection to the current time.
	/// </summary>
	protected void UpdateLastActiveTime()
	{
		base.LastActiveTime = DateTimeOffset.Now;
	}

	/// <summary>
	/// Gets a task which represents the connection.
	/// </summary>
	/// <param name="readTask">The read task.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	protected virtual async Task GetConnectionTask(Task readTask, CancellationToken cancellationToken)
	{
		await readTask.ConfigureAwait(continueOnCapturedContext: false);
		FireClose();
	}

	/// <summary>
	/// Runs the connection asynchronously with the specified pipeline filter.
	/// </summary>
	/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
	/// <param name="pipelineFilter">The pipeline filter to use for processing data.</param>
	/// <returns>An asynchronous enumerable of package information.</returns>
	public override async IAsyncEnumerable<TPackageInfo> RunAsync<TPackageInfo>(IPipelineFilter<TPackageInfo> pipelineFilter)
	{
		_pipelineFilter = pipelineFilter;
		TaskCompletionSource readTaskCompletionSource = new TaskCompletionSource();
		_cts.Token.Register(delegate
		{
			readTaskCompletionSource.TrySetResult();
		});
		_connectionTask = GetConnectionTask(readTaskCompletionSource.Task, _cts.Token);
		IAsyncEnumerator<TPackageInfo> packagePipeEnumerator = ReadPipeAsync<TPackageInfo>(InputReader, _cts.Token).GetAsyncEnumerator(_cts.Token);
		while (true)
		{
			bool flag;
			try
			{
				flag = await packagePipeEnumerator.MoveNextAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception e)
			{
				OnError("Unhandled exception in the method PipeConnection.Run.", e);
				break;
			}
			if (!flag)
			{
				break;
			}
			yield return packagePipeEnumerator.Current;
		}
		readTaskCompletionSource.TrySetResult();
	}

	private void FireClose()
	{
		if (_isDetaching || base.IsClosed)
		{
			return;
		}
		try
		{
			Close();
			OnClosed();
		}
		catch (Exception e)
		{
			if (!IsIgnorableException(e))
			{
				OnError("Unhandled exception in the method PipeConnection.Close.", e);
			}
		}
	}

	/// <summary>
	/// Closes the connection and releases associated resources.
	/// </summary>
	protected abstract void Close();

	/// <summary>
	/// Closes the connection asynchronously with the specified reason.
	/// </summary>
	/// <param name="closeReason">The reason for closing the connection.</param>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	public override async ValueTask CloseAsync(CloseReason closeReason)
	{
		base.CloseReason = closeReason;
		await CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
		Task connectionTask = _connectionTask;
		if (connectionTask != null)
		{
			await connectionTask.ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	/// <summary>
	/// Cancels all the operations on the connection.
	/// </summary>
	protected async Task CancelAsync()
	{
		if (!_cts.IsCancellationRequested)
		{
			_cts.Cancel();
			await CompleteWriterAsync(OutputWriter, _isDetaching).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	/// <summary>
	/// Checks if the specified exception is ignorable.
	/// </summary>
	/// <param name="e">The exception.</param>
	protected virtual bool IsIgnorableException(Exception e)
	{
		if (e is ObjectDisposedException || e is NullReferenceException)
		{
			return true;
		}
		if (e.InnerException != null)
		{
			return IsIgnorableException(e.InnerException);
		}
		return false;
	}

	private void CheckConnectionSendAllowed()
	{
		if (base.IsClosed)
		{
			throw new Exception("Connection is closed now, send is not allowed.");
		}
		if (_cts.IsCancellationRequested)
		{
			throw new Exception("The communication over this connection is being closed, send is not allowed.");
		}
	}

	/// <summary>
	/// Sends data over the connection asynchronously using the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		bool sendLockAcquired = false;
		try
		{
			await SendLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			sendLockAcquired = true;
			WriteBuffer(OutputWriter, buffer);
			await OutputWriter.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (sendLockAcquired)
			{
				SendLock.Release();
			}
		}
	}

	private void WriteBuffer(PipeWriter writer, ReadOnlyMemory<byte> buffer)
	{
		CheckConnectionSendAllowed();
		writer.Write(buffer.Span);
	}

	/// <summary>
	/// Sends a package over the connection asynchronously using the specified encoder and package.
	/// </summary>
	/// <typeparam name="TPackage">The type of the package to send.</typeparam>
	/// <param name="packageEncoder">The encoder to use for the package.</param>
	/// <param name="package">The package to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public override async ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default(CancellationToken))
	{
		bool sendLockAcquired = false;
		try
		{
			await SendLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			sendLockAcquired = true;
			WritePackageWithEncoder(OutputWriter, packageEncoder, package);
			await OutputWriter.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (sendLockAcquired)
			{
				SendLock.Release();
			}
		}
	}

	/// <summary>
	/// Sends data over the connection asynchronously using a custom write action.
	/// </summary>
	/// <param name="write">The action to write data to the pipe writer.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public override async ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken)
	{
		CheckConnectionSendAllowed();
		bool sendLockAcquired = false;
		try
		{
			await SendLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			sendLockAcquired = true;
			write(OutputWriter);
			await OutputWriter.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (sendLockAcquired)
			{
				SendLock.Release();
			}
		}
	}

	/// <summary>
	/// Writes a package to the output writer using the specified encoder.
	/// </summary>
	/// <typeparam name="TPackage">The package type.</typeparam>
	/// <param name="writer">The buffer writer.</param>
	/// <param name="packageEncoder">The package encoder.</param>
	/// <param name="package">The package.</param>
	protected void WritePackageWithEncoder<TPackage>(IBufferWriter<byte> writer, IPackageEncoder<TPackage> packageEncoder, TPackage package)
	{
		CheckConnectionSendAllowed();
		packageEncoder.Encode(writer, package);
	}

	/// <summary>
	/// Invoked when data is read from the input pipe.
	/// </summary>
	/// <param name="result">The read result.</param>
	protected virtual void OnInputPipeRead(ReadResult result)
	{
	}

	/// <summary>
	/// Reads data from the input pipe asynchronously and processes it using the specified pipeline filter.
	/// </summary>
	/// <typeparam name="TPackageInfo">The package type.</typeparam>
	/// <param name="reader">The pipe reader.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	protected async IAsyncEnumerable<TPackageInfo> ReadPipeAsync<TPackageInfo>(PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		IPipelineFilter<TPackageInfo> pipelineFilter = _pipelineFilter as IPipelineFilter<TPackageInfo>;
		while (!cancellationToken.IsCancellationRequested)
		{
			ReadResult result;
			try
			{
				result = await reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				OnInputPipeRead(result);
			}
			catch (Exception ex)
			{
				if (!IsIgnorableException(ex) && !(ex is OperationCanceledException))
				{
					OnError("Failed to read from the pipe", ex);
				}
				break;
			}
			ReadOnlySequence<byte> buffer = result.Buffer;
			_ = buffer.End;
			bool completedOrCancelled = result.IsCompleted || result.IsCanceled;
			if (buffer.Length > 0)
			{
				BufferFilterResult<TPackageInfo> lastFilterResult = default(BufferFilterResult<TPackageInfo>);
				foreach (BufferFilterResult<TPackageInfo> bufferFilterResult in ReadBuffer(buffer, pipelineFilter))
				{
					lastFilterResult = bufferFilterResult;
					if (bufferFilterResult.Package != null)
					{
						yield return bufferFilterResult.Package;
					}
					if (bufferFilterResult.Exception != null)
					{
						OnError("Protocol error", bufferFilterResult.Exception);
						base.CloseReason = FuFramework.SuperSocket.Connection.CloseReason.ProtocolError;
						Close();
						completedOrCancelled = true;
						break;
					}
				}
				pipelineFilter = _pipelineFilter as IPipelineFilter<TPackageInfo>;
				if (lastFilterResult.Consumed > 0)
				{
					SequencePosition position = buffer.GetPosition(lastFilterResult.Consumed);
					reader.AdvanceTo(position, buffer.End);
				}
				else
				{
					reader.AdvanceTo(buffer.Start, buffer.End);
				}
			}
			if (completedOrCancelled)
			{
				break;
			}
		}
		await CompleteReaderAsync(reader, _isDetaching).ConfigureAwait(continueOnCapturedContext: false);
	}

	private IEnumerable<BufferFilterResult<TPackageInfo>> ReadBuffer<TPackageInfo>(ReadOnlySequence<byte> buffer, IPipelineFilter<TPackageInfo> pipelineFilter)
	{
		long bytesConsumedTotal = 0L;
		int maxPackageLength = Options.MaxPackageLength;
		while (true)
		{
			IPipelineFilter<TPackageInfo> pipelineFilter2 = pipelineFilter;
			bool flag = false;
			TPackageInfo val = default(TPackageInfo);
			Exception ex = null;
			long num = 0L;
			bool flag2 = false;
			try
			{
				SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
				val = pipelineFilter.Filter(ref reader);
				num = reader.Consumed;
				flag2 = reader.End;
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
			if (ex != null)
			{
				yield return new BufferFilterResult<TPackageInfo>(ex);
				break;
			}
			IPipelineFilter<TPackageInfo> nextFilter = pipelineFilter.NextFilter;
			if (nextFilter != null)
			{
				if (bytesConsumedTotal == 0L && pipelineFilter is IProxyProtocolPipelineFilter proxyProtocolPipelineFilter)
				{
					base.ProxyInfo = proxyProtocolPipelineFilter.ProxyInfo;
				}
				nextFilter.Context = pipelineFilter.Context;
				PipeConnectionBase pipeConnectionBase = this;
				IPipelineFilter<TPackageInfo> pipelineFilter3;
				pipelineFilter = (pipelineFilter3 = nextFilter);
				pipeConnectionBase._pipelineFilter = pipelineFilter3;
				flag = true;
			}
			bytesConsumedTotal += num;
			long num2 = num;
			if (num2 == 0L)
			{
				num2 = buffer.Length;
			}
			if (maxPackageLength > 0 && num2 > maxPackageLength)
			{
				yield return new BufferFilterResult<TPackageInfo>(new Exception($"Package cannot be larger than {maxPackageLength}."));
				break;
			}
			if (val != null || flag)
			{
				pipelineFilter2.Reset();
			}
			bool needReadMore = flag2 || (val == null && !flag);
			if (!flag2 && num > 0)
			{
				buffer = buffer.Slice(num);
			}
			if (val != null || needReadMore)
			{
				yield return new BufferFilterResult<TPackageInfo>(val, bytesConsumedTotal);
				if (needReadMore)
				{
					break;
				}
			}
		}
	}

	/// <summary>
	/// Detaches the connection asynchronously.
	/// </summary>
	/// <returns>A task that represents the asynchronous detach operation.</returns>
	public override async ValueTask DetachAsync()
	{
		_isDetaching = true;
		await CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _connectionTask.ConfigureAwait(continueOnCapturedContext: false);
		_isDetaching = false;
	}

	/// <summary>
	/// Handles errors that occur during connection operations.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="e">The exception that occurred, if any.</param>
	protected void OnError(string message, Exception e = null)
	{
		if (e != null)
		{
			Logger?.LogError(e, message);
		}
		else
		{
			Logger?.LogError(message);
		}
	}

	/// <summary>
	/// Completes the reader asynchronously.
	/// </summary>
	/// <param name="reader">The pipe reader.</param>
	/// <param name="isDetaching">Indicates if this operation is a part of detaching action.</param>
	protected virtual async ValueTask CompleteReaderAsync(PipeReader reader, bool isDetaching)
	{
		await reader.CompleteAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	/// <summary>
	/// Completes the writer asynchronously.
	/// </summary>
	/// <param name="writer">The pipe writer.</param>
	/// <param name="isDetaching">Indicates if this operation is a part of detaching action.</param>
	protected virtual async ValueTask CompleteWriterAsync(PipeWriter writer, bool isDetaching)
	{
		await writer.CompleteAsync().ConfigureAwait(continueOnCapturedContext: false);
	}
}
