using System.Buffers;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket.FramePartReader;

internal class FixPartReader : PackagePartReader
{
	public override bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData)
	{
		if (reader.Length < 2)
		{
			nextPartReader = null;
			needMoreData = true;
			return false;
		}
		needMoreData = false;
		reader.TryRead(out var value);
		OpCode opCode = (OpCode)(value & 0xF);
		if (opCode != 0)
		{
			package.OpCode = opCode;
		}
		package.OpCodeByte = value;
		reader.TryRead(out var value2);
		package.PayloadLength = value2 & 0x7F;
		package.HasMask = (value2 & 0x80) == 128;
		if (package.PayloadLength >= 126)
		{
			nextPartReader = PackagePartReader.ExtendedLengthReader;
		}
		else if (package.HasMask)
		{
			nextPartReader = PackagePartReader.MaskKeyReader;
		}
		else
		{
			if (TryInitIfEmptyMessage(package))
			{
				nextPartReader = null;
				return true;
			}
			nextPartReader = PackagePartReader.PayloadDataReader;
		}
		return false;
	}
}
