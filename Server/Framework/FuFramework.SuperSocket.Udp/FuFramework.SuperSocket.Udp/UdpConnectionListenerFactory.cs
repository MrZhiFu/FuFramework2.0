using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Udp;

internal class UdpConnectionListenerFactory : IConnectionListenerFactory
{
	private readonly IConnectionFactoryBuilder _connectionFactoryBuilder;

	private readonly IUdpSessionIdentifierProvider _udpSessionIdentifierProvider;

	private readonly IAsyncSessionContainer _sessionContainer;

	public UdpConnectionListenerFactory(IConnectionFactoryBuilder connectionFactoryBuilder, IUdpSessionIdentifierProvider udpSessionIdentifierProvider, IAsyncSessionContainer sessionContainer)
	{
		_connectionFactoryBuilder = connectionFactoryBuilder;
		_udpSessionIdentifierProvider = udpSessionIdentifierProvider;
		_sessionContainer = sessionContainer;
	}

	public IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory)
	{
		connectionOptions.Logger = loggerFactory.CreateLogger("IConnection");
		ILogger logger = loggerFactory.CreateLogger("UdpConnectionFactory");
		IConnectionFactory connectionFactory = _connectionFactoryBuilder.Build(options, connectionOptions);
		return new UdpConnectionListener(options, connectionOptions, connectionFactory, logger, _udpSessionIdentifierProvider, _sessionContainer);
	}
}
