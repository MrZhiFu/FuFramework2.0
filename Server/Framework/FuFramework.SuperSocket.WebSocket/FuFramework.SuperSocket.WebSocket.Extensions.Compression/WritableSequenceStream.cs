using System;
using System.Buffers;
using System.IO;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket.Extensions.Compression;

internal class WritableSequenceStream : Stream
{
	private SequenceSegment _head;

	private SequenceSegment _tail;

	private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	/// <summary>
	/// Gets the length of the stream. Not supported.
	/// </summary>
	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Gets or sets the position within the stream. Not supported.
	/// </summary>
	/// <exception cref="T:System.NotSupportedException">Always thrown.</exception>
	public override long Position
	{
		get
		{
			throw new NotSupportedException();
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public override void Flush()
	{
		throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
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
		byte[] array = _arrayPool.Rent(count);
		Array.Copy(buffer, offset, array, 0, count);
		SequenceSegment sequenceSegment = new SequenceSegment(array, count);
		if (_head == null)
		{
			_tail = (_head = sequenceSegment);
		}
		else
		{
			_tail.SetNext(sequenceSegment);
		}
	}

	public ReadOnlySequence<byte> GetUnderlyingSequence()
	{
		return new ReadOnlySequence<byte>(_head, 0, _tail, _tail.Memory.Length);
	}
}
