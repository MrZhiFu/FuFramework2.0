using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public class InProcSessionContainerMiddleware : MiddlewareBase, ISessionContainer
{
	private ConcurrentDictionary<string, IAppSession> _sessions;

	public InProcSessionContainerMiddleware(IServiceProvider serviceProvider)
	{
		base.Order = int.MaxValue;
		_sessions = new ConcurrentDictionary<string, IAppSession>(StringComparer.OrdinalIgnoreCase);
	}

	public override ValueTask<bool> RegisterSession(IAppSession session)
	{
		if (session is IHandshakeRequiredSession { Handshaked: false })
		{
			return new ValueTask<bool>(result: true);
		}
		_sessions.TryAdd(session.SessionID, session);
		return new ValueTask<bool>(result: true);
	}

	public override ValueTask<bool> UnRegisterSession(IAppSession session)
	{
		_sessions.TryRemove(session.SessionID, out var _);
		return new ValueTask<bool>(result: true);
	}

	public IAppSession GetSessionByID(string sessionID)
	{
		_sessions.TryGetValue(sessionID, out var value);
		return value;
	}

	public int GetSessionCount()
	{
		return _sessions.Count;
	}

	public IEnumerable<IAppSession> GetSessions(Predicate<IAppSession> criteria = null)
	{
		IEnumerator<KeyValuePair<string, IAppSession>> enumerator = _sessions.GetEnumerator();
		while (enumerator.MoveNext())
		{
			IAppSession value = enumerator.Current.Value;
			if (value.State == SessionState.Connected && (criteria == null || criteria(value)))
			{
				yield return value;
			}
		}
	}

	public IEnumerable<TAppSession> GetSessions<TAppSession>(Predicate<TAppSession> criteria = null) where TAppSession : IAppSession
	{
		IEnumerator<KeyValuePair<string, IAppSession>> enumerator = _sessions.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current.Value is TAppSession { State: SessionState.Connected } val && (criteria == null || criteria(val)))
			{
				yield return val;
			}
		}
	}
}
