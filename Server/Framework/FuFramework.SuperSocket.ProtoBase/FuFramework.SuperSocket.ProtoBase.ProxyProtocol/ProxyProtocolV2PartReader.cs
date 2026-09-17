using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace FuFramework.SuperSocket.ProtoBase.ProxyProtocol;

/// <summary>
/// A part reader for processing proxy protocol version 2 headers.
/// </summary>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
internal class ProxyProtocolV2PartReader<TPackageInfo> : ProxyProtocolPackagePartReader<TPackageInfo>
{
	/// <summary>
	/// The fixed length of the part after the signature.
	/// </summary>
	private const int FIXPART_LEN_AFTER_SIGNATURE = 4;

	/// <summary>
	/// The length of an IPv6 address.
	/// </summary>
	private const int IPV6_ADDRESS_LEN = 16;

	/// <summary>
	/// The total length of source and destination IPv6 addresses.
	/// </summary>
	private const int IPV6_ADDRESS_ALL_LEN = 32;

	/// <summary>
	/// A shared pool for renting and returning byte arrays.
	/// </summary>
	private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

	/// <summary>
	/// Processes the proxy protocol version 2 header and extracts connection information.
	/// </summary>
	/// <param name="package">The package being processed.</param>
	/// <param name="filterContext">The context for the filter.</param>
	/// <param name="reader">The sequence reader containing the data.</param>
	/// <param name="nextPartReader">The next part reader to use.</param>
	/// <param name="needMoreData">Indicates whether more data is needed to complete processing.</param>
	/// <returns><c>true</c> if processing was successful; otherwise, <c>false</c>.</returns>
	public override bool Process(TPackageInfo package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<TPackageInfo> nextPartReader, out bool needMoreData)
	{
		nextPartReader = null;
		ProxyInfo proxyInfo = filterContext as ProxyInfo;
		if (proxyInfo.AddressLength == 0)
		{
			if (reader.Length < 4)
			{
				needMoreData = true;
				return false;
			}
			reader.TryRead(out var value);
			proxyInfo.Version = value / 16;
			proxyInfo.Command = ((value % 16 != 0) ? ProxyCommand.PROXY : ProxyCommand.LOCAL);
			reader.TryRead(out var value2);
			ProxyInfo proxyInfo2 = proxyInfo;
			proxyInfo2.AddressFamily = (value2 / 16) switch
			{
				0 => AddressFamily.Unspecified, 
				1 => AddressFamily.InterNetwork, 
				2 => AddressFamily.InterNetworkV6, 
				3 => AddressFamily.Unix, 
				_ => throw new NotSupportedException(), 
			};
			proxyInfo2 = proxyInfo;
			proxyInfo2.ProtocolType = (value2 % 16) switch
			{
				0 => ProtocolType.IP, 
				1 => ProtocolType.Tcp, 
				2 => ProtocolType.Udp, 
				_ => throw new NotSupportedException(), 
			};
			reader.TryRead(out var value3);
			reader.TryRead(out var value4);
			proxyInfo.AddressLength = value3 * 256 + value4;
			needMoreData = false;
			return false;
		}
		if (reader.Length < proxyInfo.AddressLength)
		{
			needMoreData = true;
			return false;
		}
		needMoreData = false;
		if (proxyInfo.AddressFamily == AddressFamily.InterNetwork)
		{
			reader.TryReadBigEndian(out uint value5);
			if (BitConverter.IsLittleEndian)
			{
				value5 = BinaryPrimitives.ReverseEndianness(value5);
			}
			proxyInfo.SourceIPAddress = new IPAddress(value5);
			reader.TryReadBigEndian(out uint value6);
			if (BitConverter.IsLittleEndian)
			{
				value6 = BinaryPrimitives.ReverseEndianness(value6);
			}
			proxyInfo.DestinationIPAddress = new IPAddress(value6);
			reader.TryReadBigEndian(out ushort value7);
			proxyInfo.SourcePort = value7;
			reader.TryReadBigEndian(out ushort value8);
			proxyInfo.DestinationPort = value8;
		}
		else if (proxyInfo.AddressFamily == AddressFamily.InterNetworkV6)
		{
			byte[] array = _bufferPool.Rent(32);
			try
			{
				Span<byte> destination = array.AsSpan().Slice(0, 32);
				reader.TryCopyTo(destination);
				proxyInfo.SourceIPAddress = new IPAddress(destination.Slice(0, 16));
				proxyInfo.DestinationIPAddress = new IPAddress(destination.Slice(16));
				reader.Advance(32L);
			}
			finally
			{
				_bufferPool.Return(array);
			}
			reader.TryReadBigEndian(out ushort value9);
			proxyInfo.SourcePort = value9;
			reader.TryReadBigEndian(out ushort value10);
			proxyInfo.DestinationPort = value10;
		}
		return true;
	}
}
