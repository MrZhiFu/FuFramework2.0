using System.IO.Pipelines;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.Connection;

public interface IPipeConnection
{
	IPipelineFilter PipelineFilter { get; }

	PipeReader InputReader { get; }

	PipeWriter OutputWriter { get; }
}
