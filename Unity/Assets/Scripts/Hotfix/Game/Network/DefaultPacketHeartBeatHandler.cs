using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Proto;

namespace Hotfix.Game.Network
{
	public sealed class DefaultPacketHeartBeatHandler : BasePacketHeartBeatHandler
	{
		private readonly ReqHeartBeat m_reqHeartBeat;

		public DefaultPacketHeartBeatHandler()
		{
			m_reqHeartBeat = new ReqHeartBeat();
		}

		public override MessageObject Handler()
		{
			m_reqHeartBeat.Timestamp = Utility.Time.ClientNow();
			m_reqHeartBeat.UpdateUniqueId();
			return m_reqHeartBeat;
		}
	}
}
