using System.Collections.Generic;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public interface IConnectionStreamInitializersFactory
{
	IEnumerable<IConnectionStreamInitializer> Create(ListenOptions listenOptions);
}
