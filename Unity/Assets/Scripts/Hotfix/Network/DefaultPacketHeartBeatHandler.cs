using FuFramework.Core.Runtime;
using FuFramework.Network.Runtime;
using Hotfix.Proto;

namespace Hotfix.Network
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