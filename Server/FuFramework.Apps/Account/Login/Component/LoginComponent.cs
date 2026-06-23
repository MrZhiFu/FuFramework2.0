/*using System.Threading.Tasks;
using FuFramework.Apps.Account.Login.Entity;
using FuFramework.Apps.Player.Player.Entity;
using FuFramework.Core.Abstractions.Attribute;
using FuFramework.Core.Components;
using FuFramework.Monitor.Account;
using FuFramework.Monitor.Player;
using FuFramework.Setting;
using Random = FuFramework.Utility.Random;

namespace FuFramework.Apps.Account.Login.Component;

[ComponentType((ushort)ActorType.Account)]
public sealed class LoginComponent : StateComponent<LoginState>
{
    public async Task<LoginState> OnLogin(ReqLogin reqLogin)
    {
        MetricsAccountRegister.LoginCounterOptions.Inc();
        return await GameDb.FindAsync<LoginState>(m => m.UserName == reqLogin.UserName && m.Password == reqLogin.Password);
    }

    public async Task<LoginState> Register(long accountId, ReqLogin reqLogin)
    {
        MetricsAccountRegister.RegisterCounterOptions.Inc();
        var loginState = new LoginState { Id = accountId, UserName = reqLogin.UserName, Password = reqLogin.Password, };
        await GameDb.SaveOneAsync(loginState);
        return loginState;
    }

    public async Task<List<PlayerState>> GetPlayerList(ReqPlayerList reqPlayerList)
    {
        MetricsPlayerRegister.GetPlayerListCounterOptions.Inc();
        return await GameDb.FindListAsync<PlayerState>(m => m.AccountId == reqPlayerList.Id);
    }

    public async Task<PlayerState> OnPlayerCreate(ReqPlayerCreate reqPlayerCreate)
    {
        var playerState = new PlayerState
        {
            Id = ActorIdGenerator.GetActorId(GlobalConst.ActorTypePlayer),
            AccountId = reqPlayerCreate.Id,
            Name = reqPlayerCreate.Name,
            Level = (uint)Random.Next(1, 50),
            State = 0,
            Avatar = (uint)Random.Next(1, 50),
        };
        MetricsPlayerRegister.CreateCounterOptions.Inc();
        await GameDb.SaveOneAsync(playerState);
        return playerState;
    }
}*/