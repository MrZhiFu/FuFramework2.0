using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Prometheus;

namespace FuFramework.Monitor;

/// <summary>
/// 监控帮助类
/// </summary>
public static class MetricsHelper
{
	private static KestrelMetricServer _server;

	/// <summary>
	/// 停止监控
	/// </summary>
	public static void Stop()
	{
		_server.Stop();
	}

	/// <summary>
	/// 启动监控
	/// </summary>
	/// <param name="port">对外访问的端口</param>
	public static void Start(int port = 0)
	{
		if (port <= 0)
		{
			return;
		}
		Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions
		{
			SuppressEventCounters = true,
			SuppressMeters = true,
			SuppressProcessMetrics = true
		});
		_server = new KestrelMetricServer(port);
		_server.Start();
		Counter totalSleepTime = Metrics.CreateCounter("sample_sleep_seconds_total", "Total amount of time spent sleeping.");
		Task.Run(async delegate
		{
			while (true)
			{
				using (new Activity("Pausing before record processing").Start())
				{
					Stopwatch sleepStopwatch = Stopwatch.StartNew();
					await Task.Delay(TimeSpan.FromSeconds(1.0));
					totalSleepTime.Inc(sleepStopwatch.Elapsed.TotalSeconds);
				}
			}
		});
		Console.WriteLine($"Open http://localhost:{port}/metrics?accept=application/openmetrics-text in a web browser.");
	}
}
