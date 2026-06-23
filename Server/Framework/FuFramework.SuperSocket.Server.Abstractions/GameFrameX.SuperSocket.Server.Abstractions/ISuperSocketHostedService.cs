using System;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Abstractions;

public interface ISuperSocketHostedService : IHostedService, IServer, IServerInfo, IDisposable, IAsyncDisposable, IConnectionRegister, ILoggerAccessor, ISessionEventHost
{
}
