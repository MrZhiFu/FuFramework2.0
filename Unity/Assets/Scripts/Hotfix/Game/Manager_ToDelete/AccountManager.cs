using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;

namespace Hotfix.Game.Manager_ToDelete
{
    public sealed class AccountManager : Singleton<AccountManager>
    {
        public List<PlayerInfo> PlayerList { get; set; }
    }
}
