namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Represents the context for handling a package in the server.
/// </summary>
/// <typeparam name="TAppSession">The type of the application session.</typeparam>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class PackageHandlingContext<TAppSession, TPackageInfo>
{
	/// <summary>
	/// Gets the application session associated with the package.
	/// </summary>
	public TAppSession AppSession { get; }

	/// <summary>
	/// Gets the package information.
	/// </summary>
	public TPackageInfo PackageInfo { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.PackageHandlingContext`2" /> class.
	/// </summary>
	/// <param name="appSession">The application session associated with the package.</param>
	/// <param name="packageInfo">The package information.</param>
	public PackageHandlingContext(TAppSession appSession, TPackageInfo packageInfo)
	{
		AppSession = appSession;
		PackageInfo = packageInfo;
	}
}
