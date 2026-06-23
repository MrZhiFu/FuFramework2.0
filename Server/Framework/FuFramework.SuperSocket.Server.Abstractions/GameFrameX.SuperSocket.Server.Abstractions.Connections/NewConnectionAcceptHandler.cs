using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public delegate ValueTask NewConnectionAcceptHandler(ListenOptions listenOptions, IConnection connection);
