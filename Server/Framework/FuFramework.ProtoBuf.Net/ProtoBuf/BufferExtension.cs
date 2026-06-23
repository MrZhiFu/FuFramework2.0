using System;
using System.IO;

namespace ProtoBuf;

/// <summary>
/// Provides a simple buffer-based implementation of an <see cref="T:ProtoBuf.IExtension">extension</see> object.
/// </summary>
public sealed class BufferExtension : IExtension, IExtensionResettable
{
	private byte[] buffer;

	int IExtension.GetLength()
	{
		if (buffer != null)
		{
			return buffer.Length;
		}
		return 0;
	}

	Stream IExtension.BeginAppend()
	{
		return new MemoryStream();
	}

	void IExtension.EndAppend(Stream stream, bool commit)
	{
		using (stream)
		{
			int num;
			if (commit && (num = (int)stream.Length) > 0)
			{
				MemoryStream memoryStream = (MemoryStream)stream;
				if (buffer == null)
				{
					buffer = memoryStream.ToArray();
					return;
				}
				int num2 = buffer.Length;
				byte[] dst = new byte[num2 + num];
				Buffer.BlockCopy(buffer, 0, dst, 0, num2);
				Buffer.BlockCopy(Helpers.GetBuffer(memoryStream), 0, dst, num2, num);
				buffer = dst;
			}
		}
	}

	Stream IExtension.BeginQuery()
	{
		if (buffer != null)
		{
			return new MemoryStream(buffer);
		}
		return Stream.Null;
	}

	void IExtension.EndQuery(Stream stream)
	{
		using (stream)
		{
		}
	}

	void IExtensionResettable.Reset()
	{
		buffer = null;
	}
}
