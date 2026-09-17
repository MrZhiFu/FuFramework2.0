using System;
using System.Buffers;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket;

public static class ExtensionMethods
{
	private static readonly char[] m_CrCf = new char[2] { '\r', '\n' };

	/// <summary>
	/// Appends in the format with CrCf as suffix.
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="format">The format.</param>
	/// <param name="arg">The arg.</param>
	public static void AppendFormatWithCrCf(this StringBuilder builder, string format, object arg)
	{
		builder.AppendFormat(format, arg);
		builder.Append(m_CrCf);
	}

	/// <summary>
	/// Appends in the format with CrCf as suffix.
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="format">The format.</param>
	/// <param name="args">The args.</param>
	public static void AppendFormatWithCrCf(this StringBuilder builder, string format, params object[] args)
	{
		builder.AppendFormat(format, args);
		builder.Append(m_CrCf);
	}

	/// <summary>
	/// Appends with CrCf as suffix.
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="content">The content.</param>
	public static void AppendWithCrCf(this StringBuilder builder, string content)
	{
		builder.Append(content);
		builder.Append(m_CrCf);
	}

	/// <summary>
	/// Appends with CrCf as suffix.
	/// </summary>
	/// <param name="builder">The builder.</param>
	public static void AppendWithCrCf(this StringBuilder builder)
	{
		builder.Append(m_CrCf);
	}

	internal static ReadOnlySequence<byte> CopySequence(this ref ReadOnlySequence<byte> seq)
	{
		SequenceSegment sequenceSegment = null;
		SequenceSegment sequenceSegment2 = null;
		ReadOnlySequence<byte>.Enumerator enumerator = seq.GetEnumerator();
		while (enumerator.MoveNext())
		{
			SequenceSegment sequenceSegment3 = SequenceSegment.CopyFrom(enumerator.Current);
			sequenceSegment2 = ((sequenceSegment != null) ? sequenceSegment2.SetNext(sequenceSegment3) : (sequenceSegment = sequenceSegment3));
		}
		return new ReadOnlySequence<byte>(sequenceSegment, 0, sequenceSegment2, sequenceSegment2.Memory.Length);
	}

	internal static (SequenceSegment, SequenceSegment) DestructSequence(this ref ReadOnlySequence<byte> first)
	{
		SequenceSegment sequenceSegment = first.Start.GetObject() as SequenceSegment;
		SequenceSegment sequenceSegment2 = first.End.GetObject() as SequenceSegment;
		if (sequenceSegment == null)
		{
			ReadOnlySequence<byte>.Enumerator enumerator = first.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ReadOnlyMemory<byte> current = enumerator.Current;
				sequenceSegment2 = ((sequenceSegment != null) ? sequenceSegment2.SetNext(new SequenceSegment(current)) : (sequenceSegment = new SequenceSegment(current)));
			}
		}
		return (sequenceSegment, sequenceSegment2);
	}

	internal static ReadOnlySequence<byte> ConcatSequence(this ref ReadOnlySequence<byte> first, ref ReadOnlySequence<byte> second)
	{
		var (startSegment, sequenceSegment) = first.DestructSequence();
		if (!second.IsEmpty)
		{
			ReadOnlySequence<byte>.Enumerator enumerator = second.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ReadOnlyMemory<byte> current = enumerator.Current;
				sequenceSegment = sequenceSegment.SetNext(new SequenceSegment(current));
			}
		}
		return new ReadOnlySequence<byte>(startSegment, 0, sequenceSegment, sequenceSegment.Memory.Length);
	}

	internal static ReadOnlySequence<byte> ConcatSequence(this ref ReadOnlySequence<byte> first, SequenceSegment segment)
	{
		(SequenceSegment, SequenceSegment) tuple = first.DestructSequence();
		SequenceSegment item = tuple.Item1;
		SequenceSegment item2 = tuple.Item2;
		item2 = item2.SetNext(segment);
		return new ReadOnlySequence<byte>(item, 0, item2, item2.Memory.Length);
	}
}
