using System.Buffers;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket.FramePartReader;

internal class ExtendedLengthReader : PackagePartReader
{
	public override bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData)
	{
		int num = ((package.PayloadLength != 126) ? 8 : 2);
		if (reader.Remaining < num)
		{
			nextPartReader = null;
			needMoreData = true;
			return false;
		}
		needMoreData = false;
		if (num == 2)
		{
			reader.TryReadBigEndian(out ushort value);
			package.PayloadLength = value;
		}
		else
		{
			reader.TryReadBigEndian(out long value2);
			package.PayloadLength = value2;
		}
		if (package.HasMask)
		{
			nextPartReader = PackagePartReader.MaskKeyReader;
		}
		else
		{
			nextPartReader = PackagePartReader.PayloadDataReader;
		}
		return false;
	}
}
