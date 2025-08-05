using System.Collections.Generic;
using FuFramework.Core.Runtime;
using GameFrameX.Runtime;
using Hotfix.Proto;

namespace Hotfix.Manager
{
    public sealed class AccountManager : Singleton<AccountManager>
    {
        public List<PlayerInfo> PlayerList { get; set; }
    }
}