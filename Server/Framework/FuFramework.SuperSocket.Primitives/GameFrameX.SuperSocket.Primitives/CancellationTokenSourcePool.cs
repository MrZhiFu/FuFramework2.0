using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FuFramework.SuperSocket.Primitives;

/// <summary>
/// Provides a pool for reusing <see cref="T:System.Threading.CancellationTokenSource" /> instances to reduce memory allocations.
/// </summary>
public sealed class CancellationTokenSourcePool
{
	/// <summary>
	/// Represents a <see cref="T:System.Threading.CancellationTokenSource" /> with a back pointer to the pool it came from.
	/// </summary>
	public sealed class PooledCancellationTokenSource : CancellationTokenSource
	{
		private readonly CancellationTokenSourcePool _pool;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool.PooledCancellationTokenSource" /> class with the specified pool.
		/// </summary>
		/// <param name="pool">The pool to which this instance belongs.</param>
		public PooledCancellationTokenSource(CancellationTokenSourcePool pool)
		{
			_pool = pool;
		}

		/// <summary>
		/// Disposes the <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool.PooledCancellationTokenSource" /> and returns it to the pool if possible.
		/// </summary>
		/// <param name="disposing">A value indicating whether the object is being disposed.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && !_pool.Return(this))
			{
				base.Dispose(disposing);
			}
		}
	}

	private const int MaxQueueSize = 1024;

	private readonly ConcurrentQueue<PooledCancellationTokenSource> _queue = new ConcurrentQueue<PooledCancellationTokenSource>();

	private int _count;

	/// <summary>
	/// Gets a shared instance of the <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool" />.
	/// </summary>
	public static readonly CancellationTokenSourcePool Shared = new CancellationTokenSourcePool();

	/// <summary>
	/// Rents a <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool.PooledCancellationTokenSource" /> from the pool.
	/// </summary>
	/// <returns>A pooled <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool.PooledCancellationTokenSource" />.</returns>
	public PooledCancellationTokenSource Rent()
	{
		if (_queue.TryDequeue(out var result))
		{
			Interlocked.Decrement(ref _count);
			result.CancelAfter(-1);
			return result;
		}
		return new PooledCancellationTokenSource(this);
	}

	/// <summary>
	/// Rents a <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool.PooledCancellationTokenSource" /> from the pool and sets a cancellation delay.
	/// </summary>
	/// <param name="delay">The delay after which the token will be canceled.</param>
	/// <returns>A pooled <see cref="T:FuFramework.SuperSocket.Primitives.CancellationTokenSourcePool.PooledCancellationTokenSource" />.</returns>
	public PooledCancellationTokenSource Rent(TimeSpan delay)
	{
		PooledCancellationTokenSource pooledCancellationTokenSource = Rent();
		pooledCancellationTokenSource.CancelAfter(delay);
		return pooledCancellationTokenSource;
	}

	private bool Return(PooledCancellationTokenSource cts)
	{
		if (Interlocked.Increment(ref _count) > 1024 || !cts.TryReset())
		{
			Interlocked.Decrement(ref _count);
			return false;
		}
		_queue.Enqueue(cts);
		return true;
	}
}
