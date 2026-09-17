using System.Buffers;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket.FramePartReader;

internal abstract class PackagePartReader : IPackagePartReader<WebSocketPackage>
{
	public static IPackagePartReader<WebSocketPackage> NewReader => FixPartReader;

	protected static IPackagePartReader<WebSocketPackage> FixPartReader { get; private set; }

	protected static IPackagePartReader<WebSocketPackage> ExtendedLengthReader { get; private set; }

	protected static IPackagePartReader<WebSocketPackage> MaskKeyReader { get; private set; }

	protected static IPackagePartReader<WebSocketPackage> PayloadDataReader { get; private set; }

	static PackagePartReader()
	{
		FixPartReader = new FixPartReader();
		ExtendedLengthReader = new ExtendedLengthReader();
		MaskKeyReader = new MaskKeyReader();
		PayloadDataReader = new PayloadDataReader();
	}

	public abstract bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData);

	protected bool TryInitIfEmptyMessage(WebSocketPackage package)
	{
		if (package.PayloadLength != 0L)
		{
			return false;
		}
		if (package.Head != null)
		{
			return false;
		}
		if (package.OpCode == OpCode.Text)
		{
			package.Message = string.Empty;
		}
		return true;
	}
}
