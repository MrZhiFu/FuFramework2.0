using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Command;

public abstract class JsonAsyncCommand<TJsonObject> : JsonAsyncCommand<IAppSession, TJsonObject>
{
}
public abstract class JsonAsyncCommand<TAppSession, TJsonObject> : IAsyncCommand<TAppSession, IStringPackage>, ICommand where TAppSession : IAppSession
{
	public JsonSerializerOptions JsonSerializerOptions { get; }

	public JsonAsyncCommand()
	{
		JsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};
	}

	public virtual async ValueTask ExecuteAsync(TAppSession session, IStringPackage package, CancellationToken cancellationToken)
	{
		string body = package.Body;
		await ExecuteJsonAsync(session, string.IsNullOrEmpty(body) ? default(TJsonObject) : Deserialize(body), cancellationToken);
	}

	protected virtual TJsonObject Deserialize(string content)
	{
		return JsonSerializer.Deserialize<TJsonObject>(content, JsonSerializerOptions);
	}

	protected abstract ValueTask ExecuteJsonAsync(TAppSession session, TJsonObject jsonObject, CancellationToken cancellationToken);
}
