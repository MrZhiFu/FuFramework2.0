using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Game.Proto;

namespace Hotfix.Game.Network
{
	/// <summary>
	/// 心跳包构造器（游戏侧实现，心跳机制的实际生效点）。
	/// 存在的必要性：心跳消息 ReqHeartBeat 是 proto 生成物，位于 Game 层——
	/// Framework 层不引用 Game，框架无法构造心跳包，必须由本类提供构造
	/// （基类 PacketHeartBeatHandlerBase 为 abstract，漏实现活不过编译；
	/// 基类以 Base 后缀命名，为本类让出 PacketHeartBeatHandler 名称并消除全限定消歧）。
	/// 注册：由生成物 MessageRegistry.gen.cs 的 RegisterExternalPacketHandler 自动装配，无需手动注册。
	/// 复用单个 ReqHeartBeat 实例：心跳包内容每次仅覆盖 Timestamp/UniqueId，避免重复分配。
	/// </summary>
	public sealed class PacketHeartBeatHandler : PacketHeartBeatHandlerBase
	{
		private readonly ReqHeartBeat m_reqHeartBeat = new();

		public override MessageObject Handler()
		{
			m_reqHeartBeat.Timestamp = Utility.Time.ClientNow();
			m_reqHeartBeat.UpdateUniqueId();
			return m_reqHeartBeat;
		}
	}
}