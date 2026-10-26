using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Game.Proto;

namespace Hotfix.Game.Network
{
	/// <summary>
	/// 心跳包构造器（游戏侧实现，心跳机制的实际生效点）。
	/// 存在的必要性：心跳消息 ReqHeartBeat 是 proto 生成物，位于 Game 层——
	/// Framework 层不引用 Game，框架无法构造心跳包，必须由本类提供构造。
	/// </summary>
	public sealed class PacketHeartBeatHandler : PacketHeartBeatHandlerBase
	{
		/// <summary>
		/// 心跳包请求
		/// </summary>
		private readonly ReqHeartBeat m_reqHeartBeat = new();

		public override MessageObject CreateHeartBeatReq()
		{
			m_reqHeartBeat.Timestamp = Utility.Time.ClientNow();
			m_reqHeartBeat.UpdateUniqueId();
			return m_reqHeartBeat;
		}
	}
}