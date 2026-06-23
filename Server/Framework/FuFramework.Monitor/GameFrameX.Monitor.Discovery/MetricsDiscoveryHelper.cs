using Prometheus;

namespace FuFramework.Monitor.Discovery;

/// <summary>
/// 发现中心监控帮助类
/// </summary>
public static class MetricsDiscoveryHelper
{
	private static Gauge _serviceCounterOptions;

	/// <summary>
	/// 注册到发现中心的服务数量
	/// </summary>
	public static Gauge ServiceCounterOptions => _serviceCounterOptions ?? (_serviceCounterOptions = Metrics.CreateGauge("service_count", "注册到发现中心的服务数量"));
}
