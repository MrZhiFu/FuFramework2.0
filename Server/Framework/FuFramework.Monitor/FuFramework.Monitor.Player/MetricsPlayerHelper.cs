using Prometheus;

namespace FuFramework.Monitor.Player;

/// <summary>
/// 玩家监控帮助类
/// </summary>
public static class MetricsPlayerHelper
{
	private static Counter _getPlayerListCounterOptions;

	private static Counter _createCounterOptions;

	private static Counter _loginCounterOptions;

	private static Counter _heartBeatCounterOptions;

	private static Gauge _onlineCounterOptions;

	/// <summary>
	/// 获取玩家列表
	/// </summary>
	public static Counter GetPlayerListCounterOptions => _getPlayerListCounterOptions ?? (_getPlayerListCounterOptions = Metrics.CreateCounter("player_get_player_list", "获取玩家列表"));

	/// <summary>
	/// 玩家角色创建数量
	/// </summary>
	public static Counter CreateCounterOptions => _createCounterOptions ?? (_createCounterOptions = Metrics.CreateCounter("player_create", "玩家角色创建数量"));

	/// <summary>
	/// 玩家角色登录
	/// </summary>
	public static Counter LoginCounterOptions => _loginCounterOptions ?? (_loginCounterOptions = Metrics.CreateCounter("player_login", "玩家角色登录"));

	/// <summary>
	/// 玩家角色心跳
	/// </summary>
	public static Counter HeartBeatCounterOptions => _heartBeatCounterOptions ?? (_heartBeatCounterOptions = Metrics.CreateCounter("player_heart_beat", "玩家角色心跳"));

	/// <summary>
	/// 在线玩家数量
	/// </summary>
	public static Gauge OnlineCounterOptions => _onlineCounterOptions ?? (_onlineCounterOptions = Metrics.CreateGauge("player_online", "在线玩家数量"));
}
