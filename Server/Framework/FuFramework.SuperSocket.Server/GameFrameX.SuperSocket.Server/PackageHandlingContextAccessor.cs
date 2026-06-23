using System.Threading;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Provides access to the package handling context for a specific package type.
/// </summary>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class PackageHandlingContextAccessor<TPackageInfo> : IPackageHandlingContextAccessor<TPackageInfo>
{
	/// <summary>
	/// Holds the package handling context for the current asynchronous flow.
	/// </summary>
	private class PackageHandlingContextHolder
	{
		/// <summary>
		/// Gets or sets the package handling context.
		/// </summary>
		public PackageHandlingContext<IAppSession, TPackageInfo> Context { get; set; }
	}

	private static AsyncLocal<PackageHandlingContextHolder> AppSessionCurrent { get; set; } = new AsyncLocal<PackageHandlingContextHolder>();

	/// <summary>
	/// Gets or sets the package handling context for the current asynchronous flow.
	/// </summary>
	PackageHandlingContext<IAppSession, TPackageInfo> IPackageHandlingContextAccessor<TPackageInfo>.PackageHandlingContext
	{
		get
		{
			return AppSessionCurrent.Value?.Context;
		}
		set
		{
			PackageHandlingContextHolder value2 = AppSessionCurrent.Value;
			if (value2 != null)
			{
				value2.Context = null;
			}
			if (value != null)
			{
				AppSessionCurrent.Value = new PackageHandlingContextHolder
				{
					Context = value
				};
			}
		}
	}
}
