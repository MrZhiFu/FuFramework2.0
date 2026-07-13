using FuFramework.Core.Runtime;
using Hotfix.Proto;

namespace Hotfix.Manager
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