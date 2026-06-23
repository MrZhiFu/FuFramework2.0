using System;
using CommandLine;
using FuFramework.Utility.Extensions;

namespace FuFramework.StartUp.Options;

/// <summary>
/// 启动参数
/// </summary>
public sealed class LauncherOptions
{
	/// <summary>
	/// 服务器类型
	/// </summary>
	[Option("ServerType", Required = true, HelpText = "服务器类型,当该值无效时，默认为后续所有参数无效")]
	public string ServerType { get; set; }

	/// <summary>
	/// APM监控端口
	/// </summary>
	[Option("APMPort", HelpText = "APM监控端口")]
	public ushort APMPort { get; set; }

	/// <summary>
	/// 是否启用指标收集功能,需要IsOpenTelemetry为true时有效
	/// <para>用于收集和监控应用程序的性能指标数据</para>
	/// <para>默认值为false</para>
	/// </summary>
	[Option("IsOpenTelemetryMetrics", Default = false, HelpText = "是否启用分布式追踪功能,需要 IsOpenTelemetry 为true时有效,默认值为false")]
	public bool IsOpenTelemetryMetrics { get; set; }

	/// <summary>
	/// 是否启用分布式追踪功能,需要IsOpenTelemetry为true时有效
	/// <para>用于跟踪和分析分布式系统中的请求流程</para>
	/// <para>默认值为false</para>
	/// </summary>
	[Option("IsOpenTelemetryTracing", Default = false, HelpText = "是否启用分布式追踪功能,需要 IsOpenTelemetry为true时有效,默认值为false")]
	public bool IsOpenTelemetryTracing { get; set; }

	/// <summary>
	/// 是否启用OpenTelemetry遥测功能
	/// <para>OpenTelemetry是一个开源的可观测性框架</para>
	/// <para>启用后可以统一管理指标、追踪和日志等可观测性数据</para>
	/// <para>默认值为false</para>
	/// </summary>
	[Option("IsOpenTelemetry", Default = false, HelpText = "是否启用OpenTelemetry遥测功能,默认值为false")]
	public bool IsOpenTelemetry { get; set; }

	/// <summary>
	/// 是否监控打印超时日志
	/// </summary>
	[Option("IsMonitorMessageTimeOut", Default = false, HelpText = "是否打印超时日志,默认值为false")]
	public bool IsMonitorMessageTimeOut { get; set; }

	/// <summary>
	/// 监控处理器超时时间（秒）,默认值为1秒，只有IsMonitorTimeOut为true时有效
	/// </summary>
	[Option("MonitorMessageTimeOutSeconds", Default = 1, HelpText = "处理器超时时间（秒）,默认值为1秒，只有IsMonitorMessageTimeOut为true时有效")]
	public int MonitorMessageTimeOutSeconds { get; set; }

	/// <summary>
	/// 网络发送等待超时时间（秒）,默认值为5秒
	/// </summary>
	[Option("NetWorkSendTimeOutSeconds", Default = 5, HelpText = "网络发送等待超时时间（秒）,默认值为5秒,最小值为1秒")]
	public int NetWorkSendTimeOutSeconds { get; set; }

	/// <summary>
	/// 是否是Debug打印日志模式,默认值为false
	/// </summary>
	[Option("IsDebug", Default = false, HelpText = "是否是Debug打印日志模式,默认值为false")]
	public bool IsDebug { get; set; }

	/// <summary>
	/// 是否打印发送数据,只有在IsDebug为true时有效,默认值为false
	/// </summary>
	[Option("IsDebugSend", Default = false, HelpText = "是否打印发送数据,只有在IsDebug为true时有效,默认值为false")]
	public bool IsDebugSend { get; set; }

	/// <summary>
	/// 是否打印发送的心跳数据,只有在IsDebugSend为true时有效,默认值为false
	/// </summary>
	[Option("IsDebugSendHeartBeat", Default = false, HelpText = "是否打印发送的心跳数据,只有在IsDebugSend为true时有效,默认值为false")]
	public bool IsDebugSendHeartBeat { get; set; }

	/// <summary>
	/// 是否打印接收数据,只有在IsDebug为true时有效,默认值为false
	/// </summary>
	[Option("IsDebugReceive", Default = false, HelpText = "是否打印接收数据,只有在IsDebug为true时有效,默认值为false")]
	public bool IsDebugReceive { get; set; }

	/// <summary>
	/// 是否打印接收的心跳数据,只有在IsDebugReceive为true时有效,默认值为false
	/// </summary>
	[Option("IsDebugReceiveHeartBeat", Default = false, HelpText = "是否打印接收的心跳数据,只有在IsDebugReceive为true时有效,默认值为false")]
	public bool IsDebugReceiveHeartBeat { get; set; }

	/// <summary>
	/// 服务器ID
	/// </summary>
	[Option("ServerId", HelpText = "服务器ID")]
	public int ServerId { get; set; }

	/// <summary>
	/// 服务器实例ID
	/// </summary>
	[Option("ServerInstanceId", HelpText = "服务器实例ID")]
	public long ServerInstanceId { get; set; }

	/// <summary>
	/// 保存数据间隔,单位毫秒,默认300秒(5分钟),最小值为5秒(5000毫秒)
	/// </summary>
	[Option("SaveDataInterval", Default = 300000, HelpText = "保存数据间隔,单位毫秒,默认300秒(5分钟),最小值为5秒(5000毫秒)")]
	public int SaveDataInterval { get; set; }

	/// <summary>
	/// 内部IP
	/// </summary>
	[Option("InnerIp", Default = "0.0.0.0", HelpText = "内部IP")]
	public string InnerIp { get; set; }

	/// <summary>
	/// 内部端口
	/// </summary>
	[Option("InnerPort", HelpText = "内部端口")]
	public ushort InnerPort { get; set; }

	/// <summary>
	/// 外部IP
	/// </summary>
	[Option("OuterIp", Default = "0.0.0.0", HelpText = "外部IP")]
	public string OuterIp { get; set; }

	/// <summary>
	/// 外部端口
	/// </summary>
	[Option("OuterPort", HelpText = "外部端口")]
	public ushort OuterPort { get; set; }

	/// <summary>
	/// API接口根路径,必须以/开头和以/结尾,默认为[/game/api/]
	/// </summary>
	[Option("HttpUrl", Default = "/game/api/", HelpText = "API接口根路径,必须以/开头和以/结尾,默认为[/game/api/]")]
	public string HttpUrl { get; set; }

	/// <summary>
	/// HTTP 是否是开发模式,当是开发模式的时候将会启用Swagger
	/// </summary>
	[Option("HttpIsDevelopment", Default = false, HelpText = "HTTP 是否是开发模式,当是开发模式的时候将会启用Swagger")]
	public bool HttpIsDevelopment { get; set; }

	/// <summary>
	/// HTTP 端口
	/// </summary>
	[Option("HttpPort", Default = 28080, HelpText = "HTTP 端口")]
	public ushort HttpPort { get; set; }

	/// <summary>
	/// HTTPS 端口
	/// </summary>
	[Option("HttpsPort", HelpText = "HTTPS 端口")]
	public ushort HttpsPort { get; set; }

	/// <summary>
	/// WebSocket 端口
	/// </summary>
	[Option("WsPort", HelpText = "WebSocket 端口")]
	public ushort WsPort { get; set; }

	/// <summary>
	/// 游戏逻辑服务器的处理最小模块ID
	/// </summary>
	[Option("MinModuleId", HelpText = "游戏逻辑服务器的处理最小模块ID")]
	public short MinModuleId { get; set; }

	/// <summary>
	/// 游戏逻辑服务器的处理最大模块ID
	/// </summary>
	[Option("MaxModuleId", HelpText = "游戏逻辑服务器的处理最大模块ID")]
	public short MaxModuleId { get; set; }

	/// <summary>
	/// WebSocket 加密端口
	/// </summary>
	[Option("WssPort", HelpText = "WebSocket 加密端口")]
	public ushort WssPort { get; set; }

	/// <summary>
	/// Wss 使用的证书路径
	/// </summary>
	[Option("WssCertFilePath", HelpText = "Wss 使用的证书路径")]
	public string WssCertFilePath { get; set; }

	/// <summary>
	/// 数据库 地址
	/// </summary>
	[Option("DataBaseUrl", HelpText = "数据库 地址")]
	public string DataBaseUrl { get; set; }

	/// <summary>
	/// 数据库名称
	/// </summary>
	[Option("DataBaseName", HelpText = "数据库名称")]
	public string DataBaseName { get; set; }

	/// <summary>
	/// 语言
	/// </summary>
	[Option("Language", HelpText = "语言")]
	public string Language { get; set; }

	/// <summary>
	/// 数据中心
	/// </summary>
	[Option("DataCenter", HelpText = "数据中心")]
	public string DataCenter { get; set; }

	/// <summary>
	/// 发现中心地址
	/// </summary>
	[Option("DiscoveryCenterIp", HelpText = "发现中心地址")]
	public string DiscoveryCenterIp { get; set; }

	/// <summary>
	/// 发现中心端口
	/// </summary>
	[Option("DiscoveryCenterPort", HelpText = "发现中心端口")]
	public ushort DiscoveryCenterPort { get; set; }

	/// <summary>
	/// 数据库服务连接地址
	/// </summary>
	[Option("DBIp", HelpText = "数据库服务连接地址")]
	public string DBIp { get; set; }

	/// <summary>
	/// 数据库服务连接端口
	/// </summary>
	[Option("DBPort", HelpText = "数据库服务连接端口")]
	public ushort DBPort { get; set; }

	/// <summary>
	/// 标签名称
	/// </summary>
	[Option("TagName", Default = "", HelpText = "标签名称")]
	public string TagName { get; set; }

	/// <summary>
	/// 检查APM监控端口
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckApmPort()
	{
		if (APMPort <= 10000 || APMPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("APMPort", "APMPort必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查ServerId
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckServerId()
	{
		if (ServerId <= 0)
		{
			throw new ArgumentOutOfRangeException("ServerId", "ServerId必须大于0");
		}
	}

	/// <summary>
	/// 检查InnerIp
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckInnerIp()
	{
		if (string.IsNullOrWhiteSpace(InnerIp))
		{
			throw new ArgumentNullException("InnerIp", "内部IP不能为空");
		}
	}

	/// <summary>
	/// 检查内部端口
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckInnerPort()
	{
		if (InnerPort <= 10000 || InnerPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("InnerPort", "内部端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查OuterIp
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckOuterIp()
	{
		if (string.IsNullOrWhiteSpace(OuterIp))
		{
			throw new ArgumentNullException("OuterIp", "外部IP不能为空");
		}
	}

	/// <summary>
	/// 检查外部端口
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckOuterPort()
	{
		if (OuterPort <= 10000 || OuterPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("OuterPort", "外部端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查HttpUrl
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckHttpUrl()
	{
		if (HttpUrl.IsNullOrEmptyOrWhiteSpace())
		{
			throw new ArgumentNullException("HttpUrl", "Http 地址不能为空");
		}
		if (!HttpUrl.StartsWith('/'))
		{
			throw new ArgumentException("HttpUrl", "Http 地址必须以/开头");
		}
		if (!HttpUrl.EndsWith('/'))
		{
			throw new ArgumentException("HttpUrl", "Http 地址必须以/结尾");
		}
	}

	/// <summary>
	/// 检查HttpPort
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckHttpPort()
	{
		if (HttpPort <= 10000 || HttpPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("HttpPort", "Http 端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查HttpsPort
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckHttpsPort()
	{
		if (HttpsPort <= 10000 || HttpsPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("HttpsPort", "Https 端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查WsPort
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckWsPort()
	{
		if (WsPort <= 10000 || WsPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("WsPort", "Ws 端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查MinModuleId
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckMinModuleId()
	{
		if (MinModuleId <= 0 || MinModuleId >= 32757)
		{
			throw new ArgumentOutOfRangeException("MinModuleId", "游戏逻辑服务器的处理最小模块ID必须大于0且小于等于" + 32757);
		}
	}

	/// <summary>
	/// 检查MaxModuleId
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckMaxModuleId()
	{
		if (MaxModuleId <= 0 || MaxModuleId >= 32757)
		{
			throw new ArgumentOutOfRangeException("MaxModuleId", "游戏逻辑服务器的处理最小模块ID必须大于0且小于等于" + 32757);
		}
	}

	/// <summary>
	/// 检查WssPort
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckWssPort()
	{
		if (WssPort <= 10000 || WssPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("WssPort", "Wss 端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查WssCertFilePath
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckWssCertFilePath()
	{
		if (string.IsNullOrWhiteSpace(WssCertFilePath))
		{
			throw new ArgumentNullException("WssCertFilePath", "Wss 使用的证书路径不能为空");
		}
	}

	/// <summary>
	/// 检查DataBaseUrl
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckDataBaseUrl()
	{
		if (string.IsNullOrWhiteSpace(DataBaseUrl))
		{
			throw new ArgumentNullException("DataBaseUrl", "数据库 地址不能为空");
		}
	}

	/// <summary>
	/// 检查DataBaseName
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckDataBaseName()
	{
		if (string.IsNullOrWhiteSpace(DataBaseName))
		{
			throw new ArgumentNullException("DataBaseName", "数据库名称不能为空");
		}
	}

	/// <summary>
	/// 检查DiscoveryCenterIp
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckDiscoveryCenterIp()
	{
		if (string.IsNullOrWhiteSpace(DiscoveryCenterIp))
		{
			throw new ArgumentNullException("DiscoveryCenterIp", "发现中心地址不能为空");
		}
	}

	/// <summary>
	/// 检查DiscoveryCenterPort
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckDiscoveryCenterPort()
	{
		if (DiscoveryCenterPort <= 10000 || DiscoveryCenterPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("DiscoveryCenterPort", "发现中心端口必须大于10000且小于等于65535");
		}
	}

	/// <summary>
	/// 检查DBIp
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public void CheckDbIp()
	{
		if (string.IsNullOrWhiteSpace(DBIp))
		{
			throw new ArgumentNullException("DBIp", "数据库服务连接地址不能为空");
		}
	}

	/// <summary>
	/// 检查数据库服务连接端口
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
	public void CheckDbPort()
	{
		if (DBPort <= 10000 || DBPort >= ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException("DBPort", "数据库服务连接端口必须大于10000且小于等于65535");
		}
	}
}
