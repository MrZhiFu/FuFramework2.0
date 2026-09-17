using System.Collections.Generic;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.Connection;

public static class Extensions
{
	public static IAsyncEnumerator<TPackageInfo> GetPackageStream<TPackageInfo>(this IConnection connection, IPipelineFilter<TPackageInfo> pipelineFilter) where TPackageInfo : class
	{
		return connection.RunAsync(pipelineFilter).GetAsyncEnumerator();
	}

	public static async ValueTask<TPackageInfo> ReceiveAsync<TPackageInfo>(this IAsyncEnumerator<TPackageInfo> packageStream)
	{
		if (await packageStream.MoveNextAsync())
		{
			return packageStream.Current;
		}
		return default(TPackageInfo);
	}
}
