using System.Buffers;

namespace FuFramework.SuperSocket.ProtoBase;

/// <summary>
/// A pipeline filter that transparently processes packages without modifying the data.
/// </summary>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class TransparentPipelineFilter<TPackageInfo> : PipelineFilterBase<TPackageInfo> where TPackageInfo : class
{
	/// <summary>
	/// Filters the data and extracts a package from the sequence reader.
	/// </summary>
	/// <param name="reader">The sequence reader containing the data.</param>
	/// <returns>The extracted package.</returns>
	public override TPackageInfo Filter(ref SequenceReader<byte> reader)
	{
		ReadOnlySequence<byte> buffer = reader.Sequence;
		long remaining = reader.Remaining;
		TPackageInfo result = DecodePackage(ref buffer);
		reader.Advance(remaining);
		return result;
	}
}
