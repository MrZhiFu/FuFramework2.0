using System;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public static class InProcSessionContainerMiddlewareExtensions
{
	public static ISuperSocketHostBuilder UseInProcSessionContainer(this ISuperSocketHostBuilder builder)
	{
		return builder.UseMiddleware((IServiceProvider s) => s.GetRequiredService<InProcSessionContainerMiddleware>()).ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<InProcSessionContainerMiddleware>();
			services.AddSingleton((Func<IServiceProvider, ISessionContainer>)((IServiceProvider s) => s.GetRequiredService<InProcSessionContainerMiddleware>()));
			services.AddSingleton((IServiceProvider s) => s.GetRequiredService<ISessionContainer>().ToAsyncSessionContainer());
		}) as ISuperSocketHostBuilder;
	}
}
