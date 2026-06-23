/*using FuFramework.Core.BaseHandler;
using FuFramework.NetWork.Abstractions;

namespace FuFramework.Hotfix.Logic.Account.Login;

[MessageMapping(typeof(ReqPlayerCreate))]
internal class ReqPlayerCreateHandler : GlobalComponentHandler<LoginComponentAgent>
{
    protected override async Task ActionAsync()
    {
        await ComponentAgent.OnPlayerCreate(NetWorkChannel, Message as ReqPlayerCreate);
    }
}*/