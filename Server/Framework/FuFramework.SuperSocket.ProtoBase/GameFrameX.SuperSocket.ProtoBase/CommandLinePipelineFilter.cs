using System;

namespace FuFramework.SuperSocket.ProtoBase;

/// <summary>
/// A pipeline filter that processes command-line-style input terminated by a line break (\r\n).
/// </summary>
public class CommandLinePipelineFilter : TerminatorPipelineFilter<StringPackageInfo>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.ProtoBase.CommandLinePipelineFilter" /> class.
	/// </summary>
	public CommandLinePipelineFilter()
		: base((ReadOnlyMemory<byte>)new byte[2] { 13, 10 })
	{
	}
}
