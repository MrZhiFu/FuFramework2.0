using System;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Default implementation of the session factory.
/// </summary>
internal class DefaultSessionFactory : ISessionFactory
{
	/// <summary>
	/// Gets the type of the session created by this factory.
	/// </summary>
	public Type SessionType => typeof(AppSession);

	/// <summary>
	/// Creates a new instance of <see cref="T:FuFramework.SuperSocket.Server.Abstractions.Session.IAppSession" />.
	/// </summary>
	/// <returns>A new instance of <see cref="T:FuFramework.SuperSocket.Server.AppSession" />.</returns>
	public IAppSession Create()
	{
		return new AppSession();
	}
}
