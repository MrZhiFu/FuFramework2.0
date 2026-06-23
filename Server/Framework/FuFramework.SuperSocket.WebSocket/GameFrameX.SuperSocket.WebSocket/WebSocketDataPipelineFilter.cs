using System.Buffers;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.WebSocket.FramePartReader;

namespace FuFramework.SuperSocket.WebSocket;

public class WebSocketDataPipelineFilter : PackagePartsPipelineFilter<WebSocketPackage>
{
	private readonly HttpHeader _httpHeader;

	private readonly bool _requireMask = true;

	/// <summary>
	/// -1: default value
	/// 0: ready to preserve bytes
	/// N: the bytes we preserved
	/// </summary>
	private long _consumed = -1L;

	public WebSocketDataPipelineFilter(HttpHeader httpHeader, bool requireMask = true)
	{
		_httpHeader = httpHeader;
		_requireMask = requireMask;
	}

	protected override WebSocketPackage CreatePackage()
	{
		return new WebSocketPackage
		{
			HttpHeader = _httpHeader
		};
	}

	public override WebSocketPackage Filter(ref SequenceReader<byte> reader)
	{
		WebSocketPackage webSocketPackage = null;
		long consumed = _consumed;
		if (consumed > 0)
		{
			SequenceReader<byte> reader2 = new SequenceReader<byte>(reader.Sequence);
			reader2.Advance(consumed);
			webSocketPackage = base.Filter(ref reader2);
			consumed = reader2.Consumed;
		}
		else
		{
			webSocketPackage = base.Filter(ref reader);
			if (_consumed == 0L)
			{
				consumed = reader.Consumed;
				reader.Rewind(consumed);
			}
		}
		if (consumed > 0)
		{
			if (_consumed < 0)
			{
				reader.Advance(consumed);
			}
			else
			{
				_consumed = consumed;
			}
		}
		return webSocketPackage;
	}

	protected override IPackagePartReader<WebSocketPackage> GetFirstPartReader()
	{
		return PackagePartReader.NewReader;
	}

	protected override void OnPartReaderSwitched(IPackagePartReader<WebSocketPackage> currentPartReader, IPackagePartReader<WebSocketPackage> nextPartReader)
	{
		if (currentPartReader is FixPartReader)
		{
			if (_requireMask && !base.CurrentPackage.HasMask)
			{
				throw new ProtocolException("Mask is required for this websocket package.");
			}
			if (!base.CurrentPackage.FIN || base.CurrentPackage.Head != null)
			{
				_consumed = 0L;
			}
		}
	}

	public override void Reset()
	{
		_consumed = -1L;
		base.Reset();
	}
}
