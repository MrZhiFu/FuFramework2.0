// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 心跳请求创建接口（编译期强制游戏侧实现）。
	///
	/// 说明（依赖方向）：心跳消息 ReqHeartBeat 是 proto 生成物，位于 Game 层——
	/// Framework 不引用 Game，框架无法构造具体心跳包，因此实现类必须由游戏侧提供
	/// （接口成员编译期强制，漏实现活不过编译，而不是运行时抛异常）。
	///
	/// 本接口只承担两件事：
	///     1. 实现类的类型锚点：生成器沿继承链识别游戏侧实现并注册；
	///     2. 心跳包构造的职责边界：ReqHeartBeat 是 proto 生成物位于 Game 层，Framework 不引用 Game
	///        无法构造，必须由本接口实现（心跳参数等运行时配置统一见 <see cref="NetworkModule"/>）。
	///
	/// 装配：框架默认装配**不包含**心跳（没有默认实现可装），
	/// 由生成物 MessageRegistry.gen.cs 的 RegisterExtPacketHandler 注册游戏侧实现。
	/// </summary>
	public interface IHeartBeatReqCreator
	{
		/// <summary>
		/// 创建心跳消息。由游戏侧实现类实现（ReqHeartBeat 为 proto 生成物，框架不引用）
		/// </summary>
		/// <returns>心跳消息对象</returns>
		MessageObject CreateHeartBeatReq();
	}
}