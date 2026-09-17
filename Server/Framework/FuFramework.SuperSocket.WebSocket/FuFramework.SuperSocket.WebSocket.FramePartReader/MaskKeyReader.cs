using System.Buffers;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket.FramePartReader;

internal class MaskKeyReader : PackagePartReader
{
	public override bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData)
	{
		int num = 4;
		if (reader.Remaining < num)
		{
			nextPartReader = null;
			needMoreData = true;
			return false;
		}
		needMoreData = false;
		package.MaskKey = reader.Sequence.Slice(reader.Consumed, 4L).ToArray<byte>();
		reader.Advance(4L);
		if (TryInitIfEmptyMessage(package))
		{
			nextPartReader = null;
			return true;
		}
		nextPartReader = PackagePartReader.PayloadDataReader;
		return false;
	}
}
