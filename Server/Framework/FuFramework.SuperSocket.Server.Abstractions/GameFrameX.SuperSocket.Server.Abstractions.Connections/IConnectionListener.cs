using System;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public interface IConnectionListener : IDisposable
{
	ListenOptions Options { get; }

	bool IsRunning { get; }

	IConnectionFactory ConnectionFactory { get; }

	event NewConnectionAcceptHandler NewConnectionAccept;

	bool Start();

	Task StopAsync();
}
