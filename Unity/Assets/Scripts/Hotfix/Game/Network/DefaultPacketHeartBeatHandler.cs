using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Game.UI;
using Hotfix.Game.Tables;
using Hotfix.Game.Tables.Tables;
using Hotfix.Game.Proto;

namespace Hotfix.Game.Network
{
    public sealed class DefaultPacketHeartBeatHandler : BasePacketHeartBeatHandler
    {
        private readonly ReqHeartBeat m_ReqHeartBeat;

        public DefaultPacketHeartBeatHandler()
        {
            m_ReqHeartBeat = new ReqHeartBeat();
        }

        public override MessageObject Handler()
        {
            m_ReqHeartBeat.Timestamp = Utility.Time.ClientNow();
            m_ReqHeartBeat.UpdateUniqueId();
            return m_ReqHeartBeat;
        }
    }
}
