using System;
using System.Collections.Generic;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public interface ISessionContainer
{
	IAppSession GetSessionByID(string sessionID);

	int GetSessionCount();

	IEnumerable<IAppSession> GetSessions(Predicate<IAppSession> criteria = null);

	IEnumerable<TAppSession> GetSessions<TAppSession>(Predicate<TAppSession> criteria = null) where TAppSession : IAppSession;
}
