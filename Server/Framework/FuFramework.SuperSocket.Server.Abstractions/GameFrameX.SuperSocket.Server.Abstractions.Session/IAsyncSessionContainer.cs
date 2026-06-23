using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public interface IAsyncSessionContainer
{
	ValueTask<IAppSession> GetSessionByIDAsync(string sessionID);

	ValueTask<int> GetSessionCountAsync();

	ValueTask<IEnumerable<IAppSession>> GetSessionsAsync(Predicate<IAppSession> criteria = null);

	ValueTask<IEnumerable<TAppSession>> GetSessionsAsync<TAppSession>(Predicate<TAppSession> criteria = null) where TAppSession : IAppSession;
}
