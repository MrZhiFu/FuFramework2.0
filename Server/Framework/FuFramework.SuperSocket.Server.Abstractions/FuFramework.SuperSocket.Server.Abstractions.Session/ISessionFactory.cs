using System;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public interface ISessionFactory
{
	Type SessionType { get; }

	IAppSession Create();
}
