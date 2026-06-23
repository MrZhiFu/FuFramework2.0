using System;

namespace ProtoBuf;

internal sealed class BufferPool
{
	private class CachedBuffer
	{
		private readonly WeakReference _reference;

		public int Size { get; }

		public bool IsAlive => _reference.IsAlive;

		public byte[] Buffer => (byte[])_reference.Target;

		public CachedBuffer(byte[] buffer)
		{
			Size = buffer.Length;
			_reference = new WeakReference(buffer);
		}
	}

	private const int POOL_SIZE = 20;

	internal const int BUFFER_LENGTH = 1024;

	/// <remarks>
	/// https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/gcallowverylargeobjects-element
	/// </remarks>
	private const int MaxByteArraySize = 2147483591;

	private static readonly CachedBuffer[] Pool = new CachedBuffer[20];

	private BufferPool()
	{
	}

	internal static void Flush()
	{
		lock (Pool)
		{
			for (int i = 0; i < Pool.Length; i++)
			{
				Pool[i] = null;
			}
		}
	}

	internal static byte[] GetBuffer()
	{
		return GetBuffer(1024);
	}

	internal static byte[] GetBuffer(int minSize)
	{
		return GetCachedBuffer(minSize) ?? new byte[minSize];
	}

	internal static byte[] GetCachedBuffer(int minSize)
	{
		lock (Pool)
		{
			int num = -1;
			byte[] array = null;
			for (int i = 0; i < Pool.Length; i++)
			{
				CachedBuffer cachedBuffer = Pool[i];
				if (cachedBuffer != null && cachedBuffer.Size >= minSize && (array == null || array.Length >= cachedBuffer.Size))
				{
					byte[] buffer = cachedBuffer.Buffer;
					if (buffer == null)
					{
						Pool[i] = null;
						continue;
					}
					array = buffer;
					num = i;
				}
			}
			if (num >= 0)
			{
				Pool[num] = null;
			}
			return array;
		}
	}

	internal static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
	{
		int num = buffer.Length * 2;
		if (num < 0)
		{
			num = 2147483591;
		}
		if (num < toFitAtLeastBytes)
		{
			num = toFitAtLeastBytes;
		}
		if (copyBytes == 0)
		{
			ReleaseBufferToPool(ref buffer);
		}
		byte[] array = GetCachedBuffer(toFitAtLeastBytes) ?? new byte[num];
		if (copyBytes > 0)
		{
			Buffer.BlockCopy(buffer, copyFromIndex, array, 0, copyBytes);
			ReleaseBufferToPool(ref buffer);
		}
		buffer = array;
	}

	internal static void ReleaseBufferToPool(ref byte[] buffer)
	{
		if (buffer == null)
		{
			return;
		}
		lock (Pool)
		{
			int num = 0;
			int num2 = int.MaxValue;
			for (int i = 0; i < Pool.Length; i++)
			{
				CachedBuffer cachedBuffer = Pool[i];
				if (cachedBuffer == null || !cachedBuffer.IsAlive)
				{
					num = 0;
					break;
				}
				if (cachedBuffer.Size < num2)
				{
					num = i;
					num2 = cachedBuffer.Size;
				}
			}
			Pool[num] = new CachedBuffer(buffer);
		}
		buffer = null;
	}
}
