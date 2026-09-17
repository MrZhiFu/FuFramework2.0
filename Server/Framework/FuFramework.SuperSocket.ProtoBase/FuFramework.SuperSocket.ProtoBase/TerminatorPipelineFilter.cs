using System;
using System.Buffers;

namespace FuFramework.SuperSocket.ProtoBase;

/// <summary>
/// A pipeline filter that processes packages terminated by a specific byte sequence.
/// </summary>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class TerminatorPipelineFilter<TPackageInfo> : PipelineFilterBase<TPackageInfo> where TPackageInfo : class
{
	/// <summary>
	/// The byte sequence that marks the end of a package.
	/// </summary>
	private readonly ReadOnlyMemory<byte> _terminator;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.ProtoBase.TerminatorPipelineFilter`1" /> class with the specified terminator.
	/// </summary>
	/// <param name="terminator">The byte sequence that marks the end of a package.</param>
	public TerminatorPipelineFilter(ReadOnlyMemory<byte> terminator)
	{
		_terminator = terminator;
	}

	/// <summary>
	/// Filters the data and extracts a package if the terminator is found.
	/// </summary>
	/// <param name="reader">The sequence reader containing the data.</param>
	/// <returns>The decoded package, or <c>null</c> if the terminator is not found.</returns>
	public override TPackageInfo Filter(ref SequenceReader<byte> reader)
	{
		ReadOnlyMemory<byte> terminator = _terminator;
		ReadOnlySpan<byte> span = terminator.Span;
		if (!reader.TryReadTo(out ReadOnlySequence<byte> sequence, span, advancePastDelimiter: false))
		{
			return null;
		}
		try
		{
			return DecodePackage(ref sequence);
		}
		finally
		{
			reader.Advance(terminator.Length);
		}
	}
}
