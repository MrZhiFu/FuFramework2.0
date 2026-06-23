using System;
using System.Buffers;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket;

public class WebSocketPipelineFilter : IPipelineFilter<WebSocketPackage>, IPipelineFilter
{
	private static readonly char _TAB = '\t';

	private static readonly char _COLON = ':';

	private static readonly ReadOnlyMemory<byte> _headerTerminator = new byte[4] { 13, 10, 13, 10 };

	private readonly bool _requireMask = true;

	private static ReadOnlySpan<byte> _CRLF => "\r\n"u8;

	public IPackageDecoder<WebSocketPackage> Decoder { get; set; }

	public IPipelineFilter<WebSocketPackage> NextFilter { get; internal set; }

	public object Context { get; set; }

	public WebSocketPipelineFilter()
	{
	}

	public WebSocketPipelineFilter(bool requireMask)
	{
		_requireMask = requireMask;
	}

	public WebSocketPackage Filter(ref SequenceReader<byte> reader)
	{
		ReadOnlySpan<byte> span = _headerTerminator.Span;
		if (!reader.TryReadTo(out ReadOnlySequence<byte> sequence, span, advancePastDelimiter: false))
		{
			return null;
		}
		reader.Advance(span.Length);
		WebSocketPackage webSocketPackage = ParseHandshake(ref sequence);
		NextFilter = new WebSocketDataPipelineFilter(webSocketPackage.HttpHeader, _requireMask);
		return webSocketPackage;
	}

	private WebSocketPackage ParseHandshake(ref ReadOnlySequence<byte> pack)
	{
		HttpHeader httpHeader = ParseHttpHeaderItems(ref pack);
		return new WebSocketPackage
		{
			HttpHeader = httpHeader,
			OpCode = OpCode.Handshake
		};
	}

	private bool TryParseHttpHeaderItems(ref ReadOnlySequence<byte> header, out string firstLine, out NameValueCollection items)
	{
		StringReader stringReader = new StringReader(header.GetString(Encoding.UTF8));
		firstLine = stringReader.ReadLine();
		if (string.IsNullOrEmpty(firstLine))
		{
			items = null;
			return false;
		}
		items = new NameValueCollection();
		string text = string.Empty;
		string empty = string.Empty;
		while (!string.IsNullOrEmpty(empty = stringReader.ReadLine()))
		{
			if (empty.StartsWith(_TAB) && !string.IsNullOrEmpty(text))
			{
				string text2 = items.Get(text);
				items[text] = text2 + empty.Trim();
				continue;
			}
			int num = empty.IndexOf(_COLON);
			if (num <= 0)
			{
				continue;
			}
			string text3 = empty.Substring(0, num);
			if (!string.IsNullOrEmpty(text3))
			{
				text3 = text3.Trim();
			}
			if (string.IsNullOrEmpty(text3))
			{
				continue;
			}
			int num2 = num + 1;
			if (empty.Length > num2)
			{
				string text4 = empty.Substring(num2);
				if (!string.IsNullOrEmpty(text4) && text4.StartsWith(' ') && text4.Length > 1)
				{
					text4 = text4.Substring(1);
				}
				string text5 = items.Get(text3);
				if (string.IsNullOrEmpty(text5))
				{
					items.Add(text3, text4);
				}
				else
				{
					items[text3] = text5 + ", " + text4;
				}
				text = text3;
			}
		}
		return true;
	}

	protected virtual HttpHeader CreateHttpHeader(string verbItem1, string verbItem2, string verbItem3, NameValueCollection items)
	{
		return HttpHeader.CreateForRequest(verbItem1, verbItem2, verbItem3, items);
	}

	private HttpHeader ParseHttpHeaderItems(ref ReadOnlySequence<byte> header)
	{
		if (!TryParseHttpHeaderItems(ref header, out var firstLine, out var items))
		{
			return null;
		}
		string[] array = firstLine.Split(' ', 3);
		if (array.Length < 3)
		{
			return null;
		}
		return CreateHttpHeader(array[0], array[1], array[2], items);
	}

	public void Reset()
	{
	}
}
