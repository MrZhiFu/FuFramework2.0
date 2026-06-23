using Prometheus;

namespace FuFramework.Monitor.DataBase;

/// <summary>
/// 数据库监控帮助类
/// </summary>
public static class MetricsDataBaseHelper
{
	private static Counter _findCounterOptions;

	private static Counter _deleteCounterOptions;

	private static Counter _createCounterOptions;

	private static Counter _updateCounterOptions;

	private static Gauge _connectionPoolGauge;

	private static Gauge _activeConnectionsGauge;

	private static Histogram _queryDurationHistogram;

	private static Counter _queryErrorCounter;

	private static Counter _deadlockCounter;

	private static Gauge _transactionActiveGauge;

	/// <summary>
	/// 数据库查询次数
	/// </summary>
	public static Counter FindCounterOptions => _findCounterOptions ?? (_findCounterOptions = Metrics.CreateCounter("database_find_count", "数据库查询次数"));

	/// <summary>
	/// 数据库删除次数
	/// </summary>
	public static Counter DeleteCounterOptions => _deleteCounterOptions ?? (_deleteCounterOptions = Metrics.CreateCounter("database_delete_count", "数据库删除次数"));

	/// <summary>
	/// 数据库创建次数
	/// </summary>
	public static Counter CreateCounterOptions => _createCounterOptions ?? (_createCounterOptions = Metrics.CreateCounter("database_create_count", "数据库创建次数"));

	/// <summary>
	/// 数据库修改次数
	/// </summary>
	public static Counter UpdateCounterOptions => _updateCounterOptions ?? (_updateCounterOptions = Metrics.CreateCounter("database_update_count", "数据库修改次数"));

	/// <summary>
	/// 数据库连接池大小
	/// </summary>
	public static Gauge ConnectionPoolGauge => _connectionPoolGauge ?? (_connectionPoolGauge = Metrics.CreateGauge("database_connection_pool_size", "数据库连接池大小"));

	/// <summary>
	/// 活跃连接数
	/// </summary>
	public static Gauge ActiveConnectionsGauge => _activeConnectionsGauge ?? (_activeConnectionsGauge = Metrics.CreateGauge("database_active_connections", "当前活跃数据库连接数"));

	/// <summary>
	/// 查询执行时间分布
	/// </summary>
	public static Histogram QueryDurationHistogram => _queryDurationHistogram ?? (_queryDurationHistogram = Metrics.CreateHistogram("database_query_duration_seconds", "数据库查询执行时间", new HistogramConfiguration
	{
		Buckets = new double[14]
		{
			0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1.0,
			2.5, 5.0, 7.5, 10.0
		}
	}));

	/// <summary>
	/// 查询错误计数
	/// </summary>
	public static Counter QueryErrorCounter => _queryErrorCounter ?? (_queryErrorCounter = Metrics.CreateCounter("database_query_errors_total", "数据库查询错误总数"));

	/// <summary>
	/// 死锁次数
	/// </summary>
	public static Counter DeadlockCounter => _deadlockCounter ?? (_deadlockCounter = Metrics.CreateCounter("database_deadlocks_total", "数据库死锁发生次数"));

	/// <summary>
	/// 活跃事务数
	/// </summary>
	public static Gauge TransactionActiveGauge => _transactionActiveGauge ?? (_transactionActiveGauge = Metrics.CreateGauge("database_active_transactions", "当前活跃事务数"));
}
