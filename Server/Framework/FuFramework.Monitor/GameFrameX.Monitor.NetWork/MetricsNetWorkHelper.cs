using Prometheus;

namespace FuFramework.Monitor.NetWork;

public static class MetricsNetWorkHelper
{
	private static Counter _totalBytesReceivedCounter;

	private static Counter _totalBytesSentCounter;

	private static Gauge _currentConnectionsGauge;

	private static Gauge _bytesReceivedPerSecondGauge;

	private static Gauge _bytesSentPerSecondGauge;

	private static Gauge _networkLatencyGauge;

	private static Counter _totalPacketsReceivedCounter;

	private static Counter _totalPacketsSentCounter;

	private static Gauge _packetsReceivedPerMinuteGauge;

	private static Gauge _packetsSentPerMinuteGauge;

	private static Counter _connectionFailuresCounter;

	private static Counter _connectionTimeoutsCounter;

	private static Counter _packetErrorsCounter;

	private static Counter _packetDroppedCounter;

	private static Gauge _connectionQualityGauge;

	private static Gauge _bandwidthUtilizationGauge;

	private static Histogram _packetSizeHistogram;

	private static Gauge _retransmissionRateGauge;

	private static Counter _tcpResetCounter;

	private static Counter _dnsFailuresCounter;

	private static Gauge _socketBacklogGauge;

	private static Counter _tlsHandshakeFailuresCounter;

	private static Gauge _ipv4ConnectionsGauge;

	private static Gauge _ipv6ConnectionsGauge;

	private static Counter _udpErrorsCounter;

	private static Counter _totalConnectionsCounter;

	private static Gauge _averageTrafficGauge;

	private static Gauge _peakTrafficGauge;

	private static Counter _totalTrafficCounter;

	/// <summary>
	/// 总连接数
	/// </summary>
	public static Counter TotalConnectionsCounter => _totalConnectionsCounter ?? (_totalConnectionsCounter = Metrics.CreateCounter("total_connections", "总连接数"));

	/// <summary>
	/// 流量平均值(bytes/s)
	/// </summary>
	public static Gauge AverageTrafficGauge => _averageTrafficGauge ?? (_averageTrafficGauge = Metrics.CreateGauge("average_traffic", "流量平均值"));

	/// <summary>
	/// 流量峰值(bytes/s)
	/// </summary>
	public static Gauge PeakTrafficGauge => _peakTrafficGauge ?? (_peakTrafficGauge = Metrics.CreateGauge("peak_traffic", "流量峰值"));

	/// <summary>
	/// 总流量(bytes)
	/// </summary>
	public static Counter TotalTrafficCounter => _totalTrafficCounter ?? (_totalTrafficCounter = Metrics.CreateCounter("total_traffic", "总流量"));

	/// <summary>
	/// 连接失败次数
	/// </summary>
	public static Counter ConnectionFailuresCounter => _connectionFailuresCounter ?? (_connectionFailuresCounter = Metrics.CreateCounter("connection_failures", "连接失败次数"));

	/// <summary>
	/// 连接超时次数
	/// </summary>
	public static Counter ConnectionTimeoutsCounter => _connectionTimeoutsCounter ?? (_connectionTimeoutsCounter = Metrics.CreateCounter("connection_timeouts", "连接超时次数"));

	/// <summary>
	/// 数据包错误数
	/// </summary>
	public static Counter PacketErrorsCounter => _packetErrorsCounter ?? (_packetErrorsCounter = Metrics.CreateCounter("packet_errors", "数据包错误数"));

	/// <summary>
	/// 丢包数
	/// </summary>
	public static Counter PacketDroppedCounter => _packetDroppedCounter ?? (_packetDroppedCounter = Metrics.CreateCounter("packet_dropped", "丢包数"));

	/// <summary>
	/// 连接质量指标(0-100)
	/// </summary>
	public static Gauge ConnectionQualityGauge => _connectionQualityGauge ?? (_connectionQualityGauge = Metrics.CreateGauge("connection_quality", "连接质量指标"));

	/// <summary>
	/// 带宽利用率(%)
	/// </summary>
	public static Gauge BandwidthUtilizationGauge => _bandwidthUtilizationGauge ?? (_bandwidthUtilizationGauge = Metrics.CreateGauge("bandwidth_utilization", "带宽利用率"));

	/// <summary>
	/// 数据包大小分布
	/// </summary>
	public static Histogram PacketSizeHistogram => _packetSizeHistogram ?? (_packetSizeHistogram = Metrics.CreateHistogram("packet_size", "数据包大小分布"));

	/// <summary>
	/// 重传率(%)
	/// </summary>
	public static Gauge RetransmissionRateGauge => _retransmissionRateGauge ?? (_retransmissionRateGauge = Metrics.CreateGauge("retransmission_rate", "重传率"));

	/// <summary>
	/// TCP重置次数
	/// </summary>
	public static Counter TcpResetCounter => _tcpResetCounter ?? (_tcpResetCounter = Metrics.CreateCounter("tcp_reset", "TCP重置次数"));

	/// <summary>
	/// DNS解析失败次数
	/// </summary>
	public static Counter DnsFailuresCounter => _dnsFailuresCounter ?? (_dnsFailuresCounter = Metrics.CreateCounter("dns_failures", "DNS解析失败次数"));

	/// <summary>
	/// Socket积压队列大小
	/// </summary>
	public static Gauge SocketBacklogGauge => _socketBacklogGauge ?? (_socketBacklogGauge = Metrics.CreateGauge("socket_backlog", "Socket积压队列大小"));

	/// <summary>
	/// TLS握手失败次数
	/// </summary>
	public static Counter TlsHandshakeFailuresCounter => _tlsHandshakeFailuresCounter ?? (_tlsHandshakeFailuresCounter = Metrics.CreateCounter("tls_handshake_failures", "TLS握手失败次数"));

	/// <summary>
	/// IPv4连接数
	/// </summary>
	public static Gauge Ipv4ConnectionsGauge => _ipv4ConnectionsGauge ?? (_ipv4ConnectionsGauge = Metrics.CreateGauge("ipv4_connections", "IPv4连接数"));

	/// <summary>
	/// IPv6连接数
	/// </summary>
	public static Gauge Ipv6ConnectionsGauge => _ipv6ConnectionsGauge ?? (_ipv6ConnectionsGauge = Metrics.CreateGauge("ipv6_connections", "IPv6连接数"));

	/// <summary>
	/// UDP错误数
	/// </summary>
	public static Counter UdpErrorsCounter => _udpErrorsCounter ?? (_udpErrorsCounter = Metrics.CreateCounter("udp_errors", "UDP错误数"));

	/// <summary>
	/// 总接收数据包数量
	/// </summary>
	public static Counter TotalPacketsReceivedCounter => _totalPacketsReceivedCounter ?? (_totalPacketsReceivedCounter = Metrics.CreateCounter("total_packets_received", "总接收数据包数量"));

	/// <summary>
	/// 总发送数据包数量
	/// </summary>
	public static Counter TotalPacketsSentCounter => _totalPacketsSentCounter ?? (_totalPacketsSentCounter = Metrics.CreateCounter("total_packets_sent", "总发送数据包数量"));

	/// <summary>
	/// 每分钟接收数据包数量
	/// </summary>
	public static Gauge PacketsReceivedPerMinuteGauge => _packetsReceivedPerMinuteGauge ?? (_packetsReceivedPerMinuteGauge = Metrics.CreateGauge("packets_received_per_minute", "每分钟接收数据包数量"));

	/// <summary>
	/// 每分钟发送数据包数量
	/// </summary>
	public static Gauge PacketsSentPerMinuteGauge => _packetsSentPerMinuteGauge ?? (_packetsSentPerMinuteGauge = Metrics.CreateGauge("packets_sent_per_minute", "每分钟发送数据包数量"));

	/// <summary>
	/// 总接收字节数
	/// </summary>
	public static Counter TotalBytesReceivedCounter => _totalBytesReceivedCounter ?? (_totalBytesReceivedCounter = Metrics.CreateCounter("total_bytes_received", "总接收字节数"));

	/// <summary>
	/// 总发送字节数
	/// </summary>
	public static Counter TotalBytesSentCounter => _totalBytesSentCounter ?? (_totalBytesSentCounter = Metrics.CreateCounter("total_bytes_sent", "总发送字节数"));

	/// <summary>
	/// 当前连接数
	/// </summary>
	public static Gauge CurrentConnectionsGauge => _currentConnectionsGauge ?? (_currentConnectionsGauge = Metrics.CreateGauge("current_connections", "当前连接数"));

	/// <summary>
	/// 每秒接收字节数
	/// </summary>
	public static Gauge BytesReceivedPerSecondGauge => _bytesReceivedPerSecondGauge ?? (_bytesReceivedPerSecondGauge = Metrics.CreateGauge("bytes_received_per_second", "每秒接收字节数"));

	/// <summary>
	/// 每秒发送字节数
	/// </summary>
	public static Gauge BytesSentPerSecondGauge => _bytesSentPerSecondGauge ?? (_bytesSentPerSecondGauge = Metrics.CreateGauge("bytes_sent_per_second", "每秒发送字节数"));

	/// <summary>
	/// 网络延迟
	/// </summary>
	public static Gauge NetworkLatencyGauge => _networkLatencyGauge ?? (_networkLatencyGauge = Metrics.CreateGauge("network_latency", "网络延迟(毫秒)"));
}
