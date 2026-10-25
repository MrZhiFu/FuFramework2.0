/*using FuFramework.Core.BaseHandler;
using FuFramework.NetWork.Abstractions;

namespace FuFramework.Hotfix.Logic.Account.Login;

[MessageMapping(typeof(ReqUserLogin))]
internal class ReqUserLoginHandler : GlobalComponentHandler<LoginComponentAgent>
{
    protected override async Task ActionAsync()
    {
        await ComponentAgent.OnUserLogin(NetWorkChannel, Message as ReqUserLogin);
    }
}*/