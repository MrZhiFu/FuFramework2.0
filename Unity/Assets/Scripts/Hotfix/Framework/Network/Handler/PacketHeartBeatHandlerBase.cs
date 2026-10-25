// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 心跳包处理器基类（abstract，编译期强制游戏侧实现）。
	///
	/// 说明（依赖方向）：心跳消息 ReqHeartBeat 是 proto 生成物，位于 Game 层——
	/// Framework 不引用 Game，框架无法构造具体心跳包，因此 Handler 必须由游戏侧派生实现
	/// （抽象成员编译期强制，漏实现活不过编译，而不是运行时抛异常）。
	///
	/// 此基类只承担两件事：
	///     1. 心跳参数默认值：HeartBeatInterval / MissHeartBeatCountByClose
	///        （RegisterHeartBeatHandler 时读取，覆盖频道默认配置）；
	///     2. 派生类的类型锚点：生成器沿继承链识别游戏侧实现并注册。
	///
	/// 装配：框架默认装配**不包含**心跳（没有默认实现可装），
	/// 由生成物 MessageRegistry.gen.cs 的 RegisterExternalPacketHandler 注册游戏侧实现。
	/// </summary>
	public abstract class PacketHeartBeatHandlerBase
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
		/// 生成心跳消息。由游戏侧派生类实现（ReqHeartBeat 为 proto 生成物，框架不引用）
		/// </summary>
		/// <returns>心跳消息对象</returns>
		public abstract MessageObject Handler();
	}
}
