using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;

namespace FuFramework.SuperSocket.Server.Connection;

/// <summary>
/// Factory for creating default connection stream initializers.
/// </summary>
public class DefaultConnectionStreamInitializersFactory : IConnectionStreamInitializersFactory
{
	private IEnumerable<IConnectionStreamInitializer> _empty = new IConnectionStreamInitializer[0];

	/// <summary>
	/// Gets the compression level used for the connection streams.
	/// </summary>
	public CompressionLevel CompressionLevel { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Connection.DefaultConnectionStreamInitializersFactory" /> class with no compression.
	/// </summary>
	public DefaultConnectionStreamInitializersFactory()
		: this(CompressionLevel.NoCompression)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Connection.DefaultConnectionStreamInitializersFactory" /> class with the specified compression level.
	/// </summary>
	/// <param name="compressionLevel">The compression level to use for the connection streams.</param>
	public DefaultConnectionStreamInitializersFactory(CompressionLevel compressionLevel)
	{
		CompressionLevel = compressionLevel;
	}

	/// <summary>
	/// Creates a collection of connection stream initializers based on the specified listen options.
	/// </summary>
	/// <param name="listenOptions">The options for the listener.</param>
	/// <returns>A collection of connection stream initializers.</returns>
	public virtual IEnumerable<IConnectionStreamInitializer> Create(ListenOptions listenOptions)
	{
		List<IConnectionStreamInitializer> list = new List<IConnectionStreamInitializer>();
		if (listenOptions.AuthenticationOptions != null && listenOptions.AuthenticationOptions.EnabledSslProtocols != 0)
		{
			list.Add(new NetworkStreamInitializer());
			list.Add(new SslStreamInitializer());
		}
		if (CompressionLevel != CompressionLevel.NoCompression)
		{
			if (!list.Any())
			{
				list.Add(new NetworkStreamInitializer());
			}
			list.Add(new GZipStreamInitializer());
		}
		list.ForEach(delegate(IConnectionStreamInitializer initializer)
		{
			initializer.Setup(listenOptions);
		});
		return list;
	}
}
