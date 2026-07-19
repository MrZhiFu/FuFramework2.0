using Hotfix.Framework.Core;
using Hotfix.Game.UI;
using Hotfix.Game.Tables;
using Hotfix.Game.Tables.Tables;
using Hotfix.Game.Proto;

namespace Hotfix.Game.Manager_ToDelete
{
    public sealed class PlayerManager : Singleton<PlayerManager>
    {
        public PlayerManager()
        {
            PlayerInfo = new PlayerInfo();
        }

        public PlayerInfo PlayerInfo { get; set; }
    }
}
