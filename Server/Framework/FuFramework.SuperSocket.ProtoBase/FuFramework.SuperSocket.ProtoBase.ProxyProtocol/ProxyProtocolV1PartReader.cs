using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FuFramework.SuperSocket.ProtoBase.ProxyProtocol;

/// <summary>
/// Processes proxy protocol version 1 headers.
/// </summary>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
internal class ProxyProtocolV1PartReader<TPackageInfo> : ProxyProtocolPackagePartReader<TPackageInfo>
{
	/// <summary>
	/// Defines a processor for handling segments of the proxy line.
	/// </summary>
	private interface IProxySgementProcessor
	{
		/// <summary>
		/// Processes a segment of the proxy line.
		/// </summary>
		/// <param name="segment">The segment to process.</param>
		/// <param name="proxyInfo">The proxy information object to update.</param>
		void Process(ReadOnlySpan<char> segment, ProxyInfo proxyInfo);
	}

	private class SourceIPAddressProcessor : IProxySgementProcessor
	{
		public void Process(ReadOnlySpan<char> segment, ProxyInfo proxyInfo)
		{
			proxyInfo.SourceIPAddress = IPAddress.Parse(segment);
		}
	}

	private class DestinationIPAddressProcessor : IProxySgementProcessor
	{
		public void Process(ReadOnlySpan<char> segment, ProxyInfo proxyInfo)
		{
			proxyInfo.DestinationIPAddress = IPAddress.Parse(segment);
		}
	}

	private class SourcePortProcessor : IProxySgementProcessor
	{
		public void Process(ReadOnlySpan<char> segment, ProxyInfo proxyInfo)
		{
			proxyInfo.SourcePort = int.Parse(segment);
		}
	}

	private class DestinationPortProcessor : IProxySgementProcessor
	{
		public void Process(ReadOnlySpan<char> segment, ProxyInfo proxyInfo)
		{
			proxyInfo.DestinationPort = int.Parse(segment);
		}
	}

	private static readonly byte[] PROXY_DELIMITER = Encoding.ASCII.GetBytes("\r\n");

	private static readonly IProxySgementProcessor[] PROXY_SEGMENT_PARSERS = new IProxySgementProcessor[4]
	{
		new SourceIPAddressProcessor(),
		new DestinationIPAddressProcessor(),
		new SourcePortProcessor(),
		new DestinationPortProcessor()
	};

	/// <summary>
	/// Processes the proxy protocol version 1 header and extracts connection information.
	/// </summary>
	/// <param name="package">The package being processed.</param>
	/// <param name="filterContext">The context for the filter.</param>
	/// <param name="reader">The sequence reader containing the data.</param>
	/// <param name="nextPartReader">The next part reader to use.</param>
	/// <param name="needMoreData">Indicates whether more data is needed to complete processing.</param>
	/// <returns><c>true</c> if processing was successful; otherwise, <c>false</c>.</returns>
	public override bool Process(TPackageInfo package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<TPackageInfo> nextPartReader, out bool needMoreData)
	{
		if (!reader.TryReadTo(out ReadOnlySequence<byte> sequence, (ReadOnlySpan<byte>)PROXY_DELIMITER, advancePastDelimiter: true))
		{
			needMoreData = true;
			nextPartReader = null;
			return false;
		}
		needMoreData = false;
		nextPartReader = null;
		SequenceReader<byte> reader2 = new SequenceReader<byte>(sequence);
		string line = reader2.ReadString(0L);
		ProxyInfo proxyInfo = filterContext as ProxyInfo;
		LoadProxyInfo(proxyInfo, line, 11, 12);
		proxyInfo.Version = 1;
		proxyInfo.Command = ProxyCommand.PROXY;
		proxyInfo.ProtocolType = ProtocolType.Tcp;
		return true;
	}

	/// <summary>
	/// Loads proxy information from the proxy line.
	/// </summary>
	/// <param name="proxyInfo">The proxy information object to update.</param>
	/// <param name="line">The proxy line containing connection details.</param>
	/// <param name="startPos">The starting position for parsing.</param>
	/// <param name="lookForOffet">The offset for looking for the next segment.</param>
	private void LoadProxyInfo(ProxyInfo proxyInfo, string line, int startPos, int lookForOffet)
	{
		ReadOnlySpan<char> readOnlySpan = line.AsSpan();
		int num = 0;
		while (lookForOffet < line.Length)
		{
			int num2 = line.IndexOf(' ', lookForOffet);
			ReadOnlySpan<char> segment;
			if (num2 >= 0)
			{
				segment = readOnlySpan.Slice(startPos, num2 - startPos);
				startPos = num2 + 1;
				lookForOffet = startPos + 1;
			}
			else
			{
				segment = readOnlySpan.Slice(startPos);
				lookForOffet = line.Length;
			}
			PROXY_SEGMENT_PARSERS[num++].Process(segment, proxyInfo);
		}
	}
}
