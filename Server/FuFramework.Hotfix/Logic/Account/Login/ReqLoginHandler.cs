/*using FuFramework.Core.BaseHandler;
using FuFramework.NetWork.Abstractions;

namespace FuFramework.Hotfix.Logic.Account.Login;

[MessageMapping(typeof(ReqLogin))]
internal class ReqLoginHandler : GlobalComponentHandler<LoginComponentAgent>
{
    protected override async Task ActionAsync()
    {
        await ComponentAgent.OnLogin(NetWorkChannel, Message as ReqLogin);
    }
}*/