using System;
using System.Buffers;
using System.IO;

namespace FuFramework.SuperSocket.WebSocket.Extensions.Compression;

internal class ReadOnlySequenceStream : Stream
{
	private ReadOnlySequence<byte> _sequence;

	private long _length;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => _length;

	/// <summary>
	/// Gets or sets the position within the stream. Not implemented.
	/// </summary>
	/// <exception cref="T:System.NotImplementedException">Always thrown.</exception>
	public override long Position
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public ReadOnlySequenceStream(ReadOnlySequence<byte> sequence)
	{
		_sequence = sequence;
		_length = sequence.Length;
	}

	public override void Flush()
	{
		throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ReadOnlySpan<byte> firstSpan = _sequence.FirstSpan;
		if (firstSpan.IsEmpty)
		{
			return 0;
		}
		int num = Math.Min(firstSpan.Length, count);
		Span<byte> destination = new Span<byte>(buffer, offset, num);
		firstSpan.CopyTo(destination);
		_sequence = _sequence.Slice(num);
		return num;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}
}
