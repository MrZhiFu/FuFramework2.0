using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Connection;

/// <summary>
/// Provides an abstract base class for virtual connections, extending pipe-based connection functionality.
/// </summary>
public abstract class VirtualConnection : PipeConnection, IVirtualConnection, IConnection
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.VirtualConnection" /> class with the specified connection options.
	/// </summary>
	/// <param name="options">The connection options.</param>
	public VirtualConnection(ConnectionOptions options)
		: base(options)
	{
	}

	/// <summary>
	/// Fills the pipe with data asynchronously. This implementation does nothing and completes immediately.
	/// </summary>
	/// <param name="writer">The pipe writer to write data to.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A completed task.</returns>
	internal override Task FillPipeAsync(PipeWriter writer, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Writes data to the pipe asynchronously.
	/// </summary>
	/// <param name="memory">The memory buffer containing the data to write.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation, including the flush result.</returns>
	public async ValueTask<FlushResult> WritePipeDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
	{
		return await base.Input.Writer.WriteAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}
}
