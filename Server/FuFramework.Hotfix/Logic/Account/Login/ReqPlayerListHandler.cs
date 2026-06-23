/*using FuFramework.Core.BaseHandler;
using FuFramework.NetWork.Abstractions;

namespace FuFramework.Hotfix.Logic.Account.Login;

[MessageMapping(typeof(ReqPlayerList))]
internal class ReqPlayerListHandler : GlobalComponentHandler<LoginComponentAgent>
{
    protected override async Task ActionAsync()
    {
        await ComponentAgent.OnGetPlayerList(NetWorkChannel, Message as ReqPlayerList);
    }
}*/