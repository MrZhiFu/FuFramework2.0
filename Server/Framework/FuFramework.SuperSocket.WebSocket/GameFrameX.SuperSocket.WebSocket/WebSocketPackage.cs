using System;
using System.Buffers;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket;

public class WebSocketPackage : IWebSocketFrameHeader
{
	public OpCode OpCode { get; set; }

	internal byte OpCodeByte { get; set; }

	public bool FIN
	{
		get
		{
			return (OpCodeByte & 0x80) == 128;
		}
		set
		{
			if (value)
			{
				OpCodeByte |= 128;
			}
			else
			{
				OpCodeByte ^= 128;
			}
		}
	}

	public bool RSV1
	{
		get
		{
			return (OpCodeByte & 0x40) == 64;
		}
		set
		{
			if (value)
			{
				OpCodeByte |= 64;
			}
			else
			{
				OpCodeByte ^= 64;
			}
		}
	}

	public bool RSV2
	{
		get
		{
			return (OpCodeByte & 0x20) == 32;
		}
		set
		{
			if (value)
			{
				OpCodeByte |= 32;
			}
			else
			{
				OpCodeByte ^= 32;
			}
		}
	}

	public bool RSV3
	{
		get
		{
			return (OpCodeByte & 0x10) == 16;
		}
		set
		{
			if (value)
			{
				OpCodeByte |= 16;
			}
			else
			{
				OpCodeByte ^= 16;
			}
		}
	}

	internal bool HasMask { get; set; }

	internal long PayloadLength { get; set; }

	internal byte[] MaskKey { get; set; }

	public string Message { get; set; }

	public HttpHeader HttpHeader { get; set; }

	public ReadOnlySequence<byte> Data { get; set; }

	internal SequenceSegment Head { get; set; }

	internal SequenceSegment Tail { get; set; }

	internal void SaveOpCodeByte()
	{
		OpCodeByte = (byte)((OpCodeByte & 0xF0) | (byte)OpCode);
	}

	internal void ConcatSequence(ref ReadOnlySequence<byte> second)
	{
		if (Head == null)
		{
			(Head, Tail) = second.DestructSequence();
		}
		else if (!second.IsEmpty)
		{
			ReadOnlySequence<byte>.Enumerator enumerator = second.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ReadOnlyMemory<byte> current = enumerator.Current;
				Tail = Tail.SetNext(new SequenceSegment(current));
			}
		}
	}

	internal void BuildData()
	{
		Data = new ReadOnlySequence<byte>(Head, 0, Tail, Tail.Memory.Length);
	}
}
