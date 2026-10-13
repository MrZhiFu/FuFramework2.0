// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 默认心跳包处理
	/// </summary>
	public class DefaultPacketHeartBeatHandler : IPacketHeartBeatHandler, IPacketHandler
	{
		/// <summary>
		/// 每次心跳的间隔
		/// </summary>
		public virtual float HeartBeatInterval => 5;

		/// <summary>
		/// 几次心跳丢失。触发断开网络
		/// </summary>
		public virtual int MissHeartBeatCountByClose => 5;

		/// <summary>
		/// 生成心跳消息。基类兜底实现不携带具体协议，必须由派生类覆盖
		/// </summary>
		/// <returns>心跳消息对象</returns>
		public virtual MessageObject Handler()
		{
			throw new System.NotImplementedException();
		}
	}
}