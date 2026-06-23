using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server;

/// <summary>
/// Defines a method to configure a container builder in the context of a host.
/// </summary>
internal interface IConfigureContainerAdapter
{
	/// <summary>
	/// Configures the container builder with the specified host context.
	/// </summary>
	/// <param name="hostContext">The context of the host.</param>
	/// <param name="containerBuilder">The container builder to configure.</param>
	void ConfigureContainer(HostBuilderContext hostContext, object containerBuilder);
}
