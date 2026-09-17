using System.Text.Json;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Command;

public abstract class JsonCommand<TJsonObject> : JsonCommand<IAppSession, TJsonObject>
{
}
public abstract class JsonCommand<TAppSession, TJsonObject> : ICommand<TAppSession, IStringPackage>, ICommand where TAppSession : IAppSession
{
	public JsonSerializerOptions JsonSerializerOptions { get; }

	public JsonCommand()
	{
		JsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};
	}

	public virtual void Execute(TAppSession session, IStringPackage package)
	{
		string body = package.Body;
		ExecuteJson(session, string.IsNullOrEmpty(body) ? default(TJsonObject) : Deserialize(body));
	}

	protected abstract void ExecuteJson(TAppSession session, TJsonObject jsonObject);

	protected virtual TJsonObject Deserialize(string content)
	{
		return JsonSerializer.Deserialize<TJsonObject>(content, JsonSerializerOptions);
	}
}
