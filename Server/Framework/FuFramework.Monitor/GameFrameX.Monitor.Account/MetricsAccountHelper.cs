using Prometheus;

namespace FuFramework.Monitor.Account;

/// <summary>
/// 账号监控帮助类
/// </summary>
public static class MetricsAccountHelper
{
	private static Counter _loginCounterOptions;

	private static Counter _registerCounterOptions;

	/// <summary>
	/// 账号登录次数
	/// </summary>
	public static Counter LoginCounterOptions => _loginCounterOptions ?? (_loginCounterOptions = Metrics.CreateCounter("account_login", "账号登录次数"));

	/// <summary>
	/// 账号注册次数
	/// </summary>
	public static Counter RegisterCounterOptions => _registerCounterOptions ?? (_registerCounterOptions = Metrics.CreateCounter("account_register", "账号注册次数"));
}
