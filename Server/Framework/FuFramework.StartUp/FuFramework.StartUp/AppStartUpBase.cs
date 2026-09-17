using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.HTTP;
using FuFramework.NetWork.Message;
using FuFramework.StartUp.Abstractions;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.Server.Host;
using FuFramework.SuperSocket.WebSocket;
using FuFramework.SuperSocket.WebSocket.Server;
using FuFramework.Utility;
using FuFramework.Utility.Extensions;
using FuFramework.Utility.Setting;
using Grafana.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace FuFramework.StartUp;

/// <summary>
/// 程序启动器基类
/// </summary>
/// <summary>
/// 程序启动器基类 - 提供TCP和WebSocket服务器的基础功能实现
/// </summary>
/// <summary>
/// 程序启动器基类 - 提供TCP和WebSocket服务器的基础功能实现
/// </summary>
public abstract class AppStartUpBase : IAppStartUp
{
	/// <summary>
	/// 应用退出
	/// </summary>
	protected readonly TaskCompletionSource<string> AppExitSource = new TaskCompletionSource<string>();

	private IHost _gameServer;

	/// <summary>
	/// 服务器类型
	/// </summary>
	public ServerType ServerType { get; private set; }

	/// <summary>
	/// 配置信息
	/// </summary>
	public AppSetting Setting { get; protected set; }

	/// <summary>
	/// 应用退出
	/// </summary>
	public Task<string> AppExitToken => AppExitSource.Task;

	/// <summary>
	/// 初始化
	/// </summary>
	/// <param name="serverType">服务器类型</param>
	/// <param name="setting">配置信息对象</param>
	/// <param name="args">参数</param>
	/// <returns></returns>
	public bool Init(ServerType serverType, AppSetting setting, string[] args = null)
	{
		ServerType = serverType;
		Setting = setting;
		Init();
		Setting.CheckNotNull("Setting");
		GlobalSettings.SetCurrentSetting(Setting);
		return true;
	}

	/// <summary>
	/// 启动
	/// </summary>
	public abstract Task StartAsync();

	/// <summary>
	/// 终止服务器
	/// </summary>
	/// <param name="message">终止原因</param>
	public virtual async Task StopAsync(string message = "")
	{
		GlobalSettings.IsAppRunning = false;
		LogHelper.ErrorConsole($"服务器类型:{Setting.ServerType} 停止! 终止原因：{message}  配置信息: {Setting.ToFormatString()}");
		await StopServerAsync();
		AppExitSource?.TrySetResult(message);
		LogHelper.FlushAndSave();
		await Task.CompletedTask;
	}

	/// <summary>
	/// 初始化
	/// </summary>
	protected virtual void Init()
	{
	}

	/// <summary>
	/// 配置启动,当InnerIP为空时.将使用Any
	/// </summary>
	/// <param name="options"></param>
	protected virtual void ConfigureSuperSocket(ServerOptions options)
	{
		if (Setting.InnerIp.IsNotNullOrWhiteSpace() && Setting.InnerPort <= 1000)
		{
			throw new ArgumentOutOfRangeException("InnerPort", $"InnerPort参数必须大于1000,当前值为{Setting.InnerPort}");
		}
		FuFramework.SuperSocket.Server.Abstractions.ListenOptions listener = new FuFramework.SuperSocket.Server.Abstractions.ListenOptions
		{
			Ip = (Setting.InnerIp.IsNullOrEmpty() ? IPAddress.Any.ToString() : Setting.InnerIp),
			Port = Setting.InnerPort
		};
		options.AddListener(listener);
	}

	/// <summary>
	/// 启动 HTTP 服务器的异步方法
	/// </summary>
	/// <param name="hostBuilder">多服务器主机构建器,用于配置和构建服务器实例</param>
	/// <param name="baseHandler">HTTP处理器列表,用于处理不同的HTTP请求</param>
	/// <param name="httpFactory">HTTP处理器工厂,根据命令标识符创建对应的处理器实例</param>
	/// <param name="aopHandlerTypes">AOP处理器列表,用于在HTTP请求处理前后执行额外的逻辑</param>
	/// <param name="minimumLevelLogLevel">日志记录的最小级别,用于控制日志输出</param>
	/// <exception cref="T:System.ArgumentException">当HTTP URL格式不正确时抛出</exception>
	/// <exception cref="T:System.NotImplementedException">当启用HTTPS但未实现时抛出</exception>
	private async Task StartHttpServerAsync(MultipleServerHostBuilder hostBuilder, List<BaseHttpHandler> baseHandler, Func<string, BaseHttpHandler> httpFactory, List<IHttpAopHandler> aopHandlerTypes = null, LogLevel minimumLevelLogLevel = LogLevel.Debug)
	{
		if (!Setting.HttpUrl.StartsWith('/'))
		{
			throw new ArgumentException("Http 地址必须以/开头", "HttpUrl");
		}
		if (!Setting.HttpUrl.EndsWith('/'))
		{
			throw new ArgumentException("Http 地址必须以/结尾", "HttpUrl");
		}
		LogHelper.InfoConsole("启动 [HTTP] 服务器...");
		ushort httpPort = Setting.HttpPort;
		if (httpPort > 0 && httpPort < ushort.MaxValue && NetHelper.PortIsAvailable(Setting.HttpPort))
		{
			WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
			bool flag = Setting.HttpIsDevelopment || webApplicationBuilder.Environment.IsDevelopment();
			OpenApiInfo openApiInfo = GetOpenApiInfo();
			if (flag)
			{
				webApplicationBuilder.Services.AddEndpointsApiExplorer();
				webApplicationBuilder.Services.AddSwaggerGen(delegate(SwaggerGenOptions options)
				{
					options.SwaggerDoc(openApiInfo.Version, openApiInfo);
					options.SchemaFilter<PreservePropertyCasingSchemaFilter>(Array.Empty<object>());
					options.OperationFilter<SwaggerOperationFilter>(new object[1] { baseHandler });
					options.CustomSchemaIds((Type type) => type.Name);
				});
			}
			webApplicationBuilder.WebHost.UseKestrel(delegate(KestrelServerOptions options)
			{
				options.ListenAnyIP(Setting.HttpPort);
				if (Setting.HttpsPort > 0 && NetHelper.PortIsAvailable(Setting.HttpsPort))
				{
					throw new NotImplementedException("HTTPS 未实现,请取消HTTPS端口配置");
				}
			}).ConfigureLogging(delegate(ILoggingBuilder logging)
			{
				logging.ClearProviders();
				logging.AddSerilog(Log.Logger);
				logging.SetMinimumLevel(minimumLevelLogLevel);
			});
			WebApplication webApplication = webApplicationBuilder.Build();
			if (flag)
			{
				webApplication.UseSwagger();
				webApplication.UseSwaggerUI(delegate(SwaggerUIOptions options)
				{
					options.SwaggerEndpoint("/swagger/" + openApiInfo.Version + "/swagger.json", openApiInfo.Title);
					options.RoutePrefix = "swagger";
				});
				foreach (string localIp in NetHelper.GetLocalIpList())
				{
					LogHelper.DebugConsole($"Swagger UI 可通过 http://{localIp}:{Setting.HttpPort}/swagger 访问");
				}
			}
			webApplication.UseExceptionHandler(ExceptionHandler);
			foreach (BaseHttpHandler item in baseHandler)
			{
				HttpMessageMappingAttribute customAttribute = item.GetType().GetCustomAttribute<HttpMessageMappingAttribute>();
				if (customAttribute == null)
				{
					continue;
				}
				string pattern = GlobalSettings.CurrentSetting.HttpUrl + customAttribute.StandardCmd;
				RouteHandlerBuilder builder = webApplication.MapPost(pattern, (Func<HttpContext, string, Task>)async delegate(HttpContext context, string text)
				{
					await HttpHandler.HandleRequest(context, httpFactory, aopHandlerTypes);
				});
				if (flag)
				{
					builder.WithOpenApi(delegate(OpenApiOperation operation)
					{
						operation.Summary = "处理 POST 请求";
						operation.Description = "处理来自游戏客户端的 POST 请求";
						return operation;
					});
				}
			}
			await webApplication.StartAsync();
			LogHelper.InfoConsole($"启动 [HTTP] 服务器启动完成 - 端口: {Setting.HttpPort}");
		}
		else
		{
			LogHelper.Error($"启动 [HTTP] 服务器 端口 [{Setting.HttpPort}] 被占用，无法启动HTTP服务");
		}
	}

	/// <summary>
	/// 启动 HTTP 服务器的同步方法
	/// </summary>
	/// <param name="baseHandler">HTTP处理器列表,用于处理不同的HTTP请求</param>
	/// <param name="httpFactory">HTTP处理器工厂,根据命令标识符创建对应的处理器实例</param>
	/// <param name="aopHandlerTypes">AOP处理器列表,用于在HTTP请求处理前后执行额外的逻辑</param>
	/// <param name="minimumLevelLogLevel">日志记录的最小级别,用于控制日志输出</param>
	/// <exception cref="T:System.ArgumentException">当HTTP URL格式不正确时抛出</exception>
	/// <exception cref="T:System.NotImplementedException">当启用HTTPS但未实现时抛出</exception>
	private async Task StartHttpServer(List<BaseHttpHandler> baseHandler, Func<string, BaseHttpHandler> httpFactory, List<IHttpAopHandler> aopHandlerTypes = null, LogLevel minimumLevelLogLevel = LogLevel.Debug)
	{
		if (!Setting.HttpUrl.StartsWith('/'))
		{
			throw new ArgumentException("Http 地址必须以/开头", "HttpUrl");
		}
		if (!Setting.HttpUrl.EndsWith('/'))
		{
			throw new ArgumentException("Http 地址必须以/结尾", "HttpUrl");
		}
		LogHelper.InfoConsole("启动 [HTTP] 服务器...");
		ushort httpPort = Setting.HttpPort;
		if (httpPort > 0 && httpPort < ushort.MaxValue && NetHelper.PortIsAvailable(Setting.HttpPort))
		{
			WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
			bool flag = Setting.HttpIsDevelopment || EnvironmentHelper.IsDevelopment();
			OpenApiInfo openApiInfo = GetOpenApiInfo();
			if (flag)
			{
				webApplicationBuilder.Services.AddEndpointsApiExplorer();
				webApplicationBuilder.Services.AddSwaggerGen(delegate(SwaggerGenOptions options)
				{
					options.SwaggerDoc(openApiInfo.Version, openApiInfo);
					options.SchemaFilter<PreservePropertyCasingSchemaFilter>(Array.Empty<object>());
					options.OperationFilter<SwaggerOperationFilter>(new object[1] { baseHandler });
					options.CustomSchemaIds((Type type) => type.Name);
				});
			}
			IWebHostBuilder webHostBuilder = webApplicationBuilder.WebHost.UseKestrel(delegate(KestrelServerOptions options)
			{
				options.ListenAnyIP(Setting.HttpPort);
				if (Setting.HttpsPort > 0 && NetHelper.PortIsAvailable(Setting.HttpsPort))
				{
					throw new NotImplementedException("HTTPS 未实现,请取消HTTPS端口配置");
				}
			});
			if (Setting.IsOpenTelemetry)
			{
				webHostBuilder.ConfigureServices(delegate(IServiceCollection services)
				{
					OpenTelemetryBuilder openTelemetryBuilder = services.AddOpenTelemetry().ConfigureResource(delegate(ResourceBuilder configure)
					{
						configure.AddService("HTTP:" + Setting.ServerName + "-" + Setting.TagName, "FuFramework.HTTP").AddTelemetrySdk();
					});
					if (Setting.IsOpenTelemetryMetrics)
					{
						openTelemetryBuilder.WithMetrics(delegate(MeterProviderBuilder configure)
						{
							configure.AddAspNetCoreInstrumentation();
							if (EnvironmentHelper.IsDevelopment())
							{
								configure.AddConsoleExporter();
							}
							configure.AddMeter("Microsoft.AspNetCore.Hosting");
							configure.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
							configure.AddMeter("System.Net.Http");
							configure.AddMeter("System.Net.NameResolution");
							configure.AddPrometheusExporter();
						});
					}
					if (Setting.IsOpenTelemetryTracing)
					{
						openTelemetryBuilder.WithTracing(delegate(TracerProviderBuilder configure)
						{
							configure.AddAspNetCoreInstrumentation();
							configure.AddHttpClientInstrumentation();
							configure.AddSource("HTTP:FuFramework." + Setting.ServerName + "." + Setting.TagName);
							if (EnvironmentHelper.IsDevelopment())
							{
								configure.AddConsoleExporter();
							}
						});
					}
				});
			}
			webHostBuilder.ConfigureLogging(delegate(ILoggingBuilder logging)
			{
				logging.ClearProviders();
				logging.AddSerilog(Log.Logger);
				logging.SetMinimumLevel(minimumLevelLogLevel);
				if (Setting.IsOpenTelemetry)
				{
					logging.AddOpenTelemetry(delegate(OpenTelemetryLoggerOptions configure)
					{
						configure.UseGrafana();
					});
				}
			});
			WebApplication webApplication = webApplicationBuilder.Build();
			if (flag)
			{
				webApplication.UseSwagger();
				webApplication.UseSwaggerUI(delegate(SwaggerUIOptions options)
				{
					options.SwaggerEndpoint("/swagger/" + openApiInfo.Version + "/swagger.json", openApiInfo.Title);
					options.RoutePrefix = "swagger";
				});
				foreach (string localIp in NetHelper.GetLocalIpList())
				{
					LogHelper.DebugConsole($"Swagger UI 可通过 http://{localIp}:{Setting.HttpPort}/swagger 访问");
				}
			}
			webApplication.UseExceptionHandler(ExceptionHandler);
			foreach (BaseHttpHandler item in baseHandler)
			{
				HttpMessageMappingAttribute customAttribute = item.GetType().GetCustomAttribute<HttpMessageMappingAttribute>();
				if (customAttribute == null)
				{
					continue;
				}
				string pattern = GlobalSettings.CurrentSetting.HttpUrl + customAttribute.StandardCmd;
				RouteHandlerBuilder builder = webApplication.MapPost(pattern, (Func<HttpContext, string, Task>)async delegate(HttpContext context, string text)
				{
					await HttpHandler.HandleRequest(context, httpFactory, aopHandlerTypes);
				});
				if (flag)
				{
					builder.WithOpenApi(delegate(OpenApiOperation operation)
					{
						operation.Summary = "处理 POST 请求";
						operation.Description = "处理来自游戏客户端的 POST 请求";
						return operation;
					});
				}
			}
			await webApplication.StartAsync();
			LogHelper.InfoConsole($"启动 [HTTP] 服务器启动完成 - 端口: {Setting.HttpPort}");
		}
		else
		{
			LogHelper.Error($"启动 [HTTP] 服务器 端口 [{Setting.HttpPort}] 被占用，无法启动HTTP服务");
		}
	}

	/// <summary>
	/// 配置启动,当InnerIP为空时.将使用Any
	/// </summary>
	/// <param name="options"></param>
	protected virtual void ConfigureHttp(ServerOptions options)
	{
		FuFramework.SuperSocket.Server.Abstractions.ListenOptions listener = new FuFramework.SuperSocket.Server.Abstractions.ListenOptions
		{
			Ip = IPAddress.Any.ToString(),
			Port = Setting.HttpPort
		};
		options.AddListener(listener);
	}

	/// <summary>
	/// 获取或创建 Swagger信息
	/// </summary>
	/// <returns></returns>
	private OpenApiInfo GetOpenApiInfo()
	{
		Version version = Assembly.GetExecutingAssembly().GetName().Version;
		if (version == null)
		{
			version = new Version(1, 0, 0);
		}
		return new OpenApiInfo
		{
			Title = "FuFramework API",
			Version = $"v{version.Major}.{version.Minor}",
			TermsOfService = new Uri("https://fuframework.doc.alianblank.com"),
			Contact = new OpenApiContact
			{
				Url = new Uri("https://fuframework.doc.alianblank.com"),
				Name = "Blank",
				Email = "wangfj11@foxmail.com"
			},
			License = new OpenApiLicense
			{
				Name = "FuFramework",
				Url = new Uri("https://github.com/MrZhiFu/FuFramework2.0")
			},
			Description = "FuFramework HTTP API documentation"
		};
	}

	/// <summary>
	/// 异常处理
	/// </summary>
	/// <param name="errorContext"></param>
	private static void ExceptionHandler(IApplicationBuilder errorContext)
	{
		errorContext.Run(async delegate(HttpContext context)
		{
			IExceptionHandlerPathFeature exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
			await context.Response.WriteAsync(exceptionHandlerPathFeature.Error.Message);
		});
	}

	/// <summary>
	/// 启动服务器 - 同时启动TCP和WebSocket服务
	/// </summary>
	/// <typeparam name="TMessageDecoderHandler">消息解码处理器类型，必须实现IMessageDecoderHandler和IPackageDecoder接口</typeparam>
	/// <typeparam name="TMessageEncoderHandler">消息编码处理器类型，必须实现IMessageEncoderHandler和IPackageEncoder接口</typeparam>
	/// <param name="messageCompressHandler">消息编码的时候使用的压缩处理器，如果为空则不处理压缩消息</param>
	/// <param name="messageDecompressHandler">消息解码的时候使用的解压处理器,如果为空则不处理压缩消息</param>
	/// <param name="baseHandler">HTTP处理器列表,用于处理不同的HTTP请求</param>
	/// <param name="httpFactory">HTTP处理器工厂,根据命令标识符创建对应的处理器实例</param>
	/// <param name="aopHandlerTypes">AOP处理器列表,用于在HTTP请求处理前后执行额外的逻辑</param>
	/// <param name="minimumLevelLogLevel">日志记录的最小级别,用于控制日志输出</param>
	protected async Task StartServerAsync<TMessageDecoderHandler, TMessageEncoderHandler>(IMessageCompressHandler messageCompressHandler, IMessageDecompressHandler messageDecompressHandler, List<BaseHttpHandler> baseHandler, Func<string, BaseHttpHandler> httpFactory, List<IHttpAopHandler> aopHandlerTypes = null, LogLevel minimumLevelLogLevel = LogLevel.Debug) where TMessageDecoderHandler : class, IMessageDecoderHandler, new() where TMessageEncoderHandler : class, IMessageEncoderHandler, new()
	{
		MessageHelper.SetMessageDecoderHandler(new TMessageDecoderHandler(), messageDecompressHandler);
		MessageHelper.SetMessageEncoderHandler(new TMessageEncoderHandler(), messageCompressHandler);
		await StartServer(baseHandler, httpFactory, aopHandlerTypes, minimumLevelLogLevel);
		GlobalSettings.LaunchTime = DateTime.UtcNow;
		GlobalSettings.IsAppRunning = true;
	}

	/// <summary>
	/// 停止服务器 - 关闭所有网络服务
	/// </summary>
	protected async Task StopServerAsync()
	{
		GlobalSettings.IsAppRunning = false;
		if (_gameServer != null)
		{
			await _gameServer.StopAsync();
			_gameServer = null;
		}
	}

	/// <summary>
	/// 消息处理异常处理方法
	/// </summary>
	/// <param name="appSession">会话对象</param>
	/// <param name="exception">异常信息</param>
	/// <returns>返回true表示继续处理，返回false表示终止处理</returns>
	protected virtual ValueTask<bool> PackageErrorHandler(IAppSession appSession, PackageHandlingException<IMessage> exception)
	{
		return ValueTask.FromResult(result: true);
	}

	/// <summary>
	/// 客户端断开连接时的处理方法
	/// </summary>
	/// <param name="appSession">断开连接的会话对象</param>
	/// <param name="disconnectEventArgs">断开连接的相关参数</param>
	protected virtual ValueTask OnDisconnected(IAppSession appSession, CloseEventArgs disconnectEventArgs)
	{
		LogHelper.Info($"客户端断开连接 - SessionID: {appSession.SessionID}, 断开原因: {disconnectEventArgs.Reason}");
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// 客户端连接成功时的处理方法
	/// </summary>
	/// <param name="appSession">新建立的会话对象</param>
	protected virtual ValueTask OnConnected(IAppSession appSession)
	{
		LogHelper.Info($"新客户端连接 - SessionID: {appSession.SessionID}, 远程终端: {appSession.RemoteEndPoint}");
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// 收到消息包的处理方法
	/// </summary>
	/// <param name="session">会话对象</param>
	/// <param name="message">接收到的消息</param>
	protected virtual ValueTask PackageHandler(IAppSession session, IMessage message)
	{
		if (Setting.IsDebug && Setting.IsDebugReceive)
		{
			LogHelper.Debug($"收到消息 - 服务器类型: [{ServerType}], 消息内容: {message.ToFormatMessageString()}");
		}
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// 异步消息处理方法
	/// </summary>
	/// <param name="handler">消息处理器</param>
	/// <param name="message">网络消息</param>
	/// <param name="netWorkChannel">网络通道</param>
	/// <param name="timeout">超时时间(毫秒)</param>
	/// <param name="cancellationToken">取消令牌</param>
	protected async Task InvokeMessageHandler(IMessageHandler handler, INetworkMessage message, INetWorkChannel netWorkChannel, int timeout = 30000, CancellationToken cancellationToken = default(CancellationToken))
	{
		await Task.Run((Action)InvokeAction, cancellationToken);
		async void InvokeAction()
		{
			await handler.Init(message, netWorkChannel);
			await handler.InnerAction(timeout, cancellationToken);
		}
	}

	/// <summary>
	/// 启动TCP服务器
	/// </summary>
	/// <typeparam name="TMessageDecoderHandler">消息解码处理器类型</typeparam>
	/// <param name="baseHandler">HTTP处理器列表,用于处理不同的HTTP请求</param>
	/// <param name="httpFactory">HTTP处理器工厂,根据命令标识符创建对应的处理器实例</param>
	/// <param name="aopHandlerTypes">AOP处理器列表,用于在HTTP请求处理前后执行额外的逻辑</param>
	/// <param name="minimumLevelLogLevel">日志记录的最小级别,用于控制日志输出</param>
	private async Task StartServer(List<BaseHttpHandler> baseHandler, Func<string, BaseHttpHandler> httpFactory, List<IHttpAopHandler> aopHandlerTypes = null, LogLevel minimumLevelLogLevel = LogLevel.Debug)
	{
		MultipleServerHostBuilder multipleServerHostBuilder = MultipleServerHostBuilder.Create();
		if (Setting.InnerPort > 0 && NetHelper.PortIsAvailable(Setting.InnerPort))
		{
			LogHelper.InfoConsole($"启动 [TCP] 服务器 - 类型: {ServerType}, 地址: {Setting.InnerIp}, 端口: {Setting.InnerPort}");
			multipleServerHostBuilder.AddServer<IMessage, MessageObjectPipelineFilter>((Action<ISuperSocketHostBuilder<IMessage>>)delegate(ISuperSocketHostBuilder<IMessage> builder)
			{
				builder.UseClearIdleSession().UseSessionHandler(OnConnected, OnDisconnected).UsePackageHandler(PackageHandler, PackageErrorHandler)
					.UseInProcSessionContainer()
					.ConfigureServices(delegate(HostBuilderContext context, IServiceCollection serviceCollection)
					{
						serviceCollection.Configure(delegate(ServerOptions options)
						{
							FuFramework.SuperSocket.Server.Abstractions.ListenOptions listener = new FuFramework.SuperSocket.Server.Abstractions.ListenOptions
							{
								Ip = "Any",
								Port = Setting.InnerPort
							};
							options.AddListener(listener);
						});
					});
			});
			LogHelper.InfoConsole($"启动 [TCP] 服务器启动完成 - 类型: {ServerType}, 地址: {Setting.InnerIp}, 端口: {Setting.InnerPort}");
		}
		else
		{
			LogHelper.WarnConsole($"启动 [TCP] 服务器启动失败 - 类型: {ServerType}, 地址: {Setting.InnerIp}, 端口: {Setting.InnerPort}, 原因: 端口无效或已被占用");
		}
		ushort wsPort = Setting.WsPort;
		if (wsPort > 0 && wsPort < ushort.MaxValue && NetHelper.PortIsAvailable(Setting.WsPort))
		{
			LogHelper.InfoConsole("启动 [WebSocket] 服务器...");
			multipleServerHostBuilder.AddWebSocketServer(delegate(ISuperSocketHostBuilder<WebSocketPackage> builder)
			{
				builder.UseWebSocketMessageHandler(WebSocketMessageHandler).UseSessionHandler(OnConnected, OnDisconnected).ConfigureServices(delegate(HostBuilderContext context, IServiceCollection serviceCollection)
				{
					serviceCollection.Configure(delegate(ServerOptions options)
					{
						FuFramework.SuperSocket.Server.Abstractions.ListenOptions listener2 = new FuFramework.SuperSocket.Server.Abstractions.ListenOptions
						{
							Ip = "Any",
							Port = Setting.WsPort
						};
						options.AddListener(listener2);
					});
				});
			});
			LogHelper.InfoConsole($"启动 [WebSocket] 服务器启动完成 - 类型: {ServerType}, 端口: {Setting.WsPort}");
		}
		else
		{
			LogHelper.WarnConsole($"启动 [WebSocket] 服务器启动失败 - 类型: {ServerType}, 端口: {Setting.WsPort}, 原因: 端口无效或已被占用");
		}
		await StartHttpServer(baseHandler, httpFactory, aopHandlerTypes, minimumLevelLogLevel);
		if (Setting.IsOpenTelemetry)
		{
			multipleServerHostBuilder.ConfigureServices(delegate(IServiceCollection services)
			{
				OpenTelemetryBuilder openTelemetryBuilder = services.AddOpenTelemetry().ConfigureResource(delegate(ResourceBuilder configure)
				{
					configure.AddService(Setting.ServerName + "-" + Setting.TagName, "FuFramework").AddTelemetrySdk();
				});
				if (Setting.IsOpenTelemetryMetrics)
				{
					openTelemetryBuilder.WithMetrics(delegate(MeterProviderBuilder configure)
					{
						configure.AddAspNetCoreInstrumentation();
						if (EnvironmentHelper.IsDevelopment())
						{
							configure.AddConsoleExporter();
						}
						configure.AddMeter("Microsoft.AspNetCore.Hosting");
						configure.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
						configure.AddMeter("System.Net.Http");
						configure.AddMeter("System.Net.NameResolution");
						configure.AddPrometheusExporter();
					});
				}
				if (Setting.IsOpenTelemetryTracing)
				{
					openTelemetryBuilder.WithTracing(delegate(TracerProviderBuilder configure)
					{
						configure.AddAspNetCoreInstrumentation();
						configure.AddHttpClientInstrumentation();
						configure.AddSource("FuFramework." + Setting.ServerName + "." + Setting.TagName);
						if (EnvironmentHelper.IsDevelopment())
						{
							configure.AddConsoleExporter();
						}
					});
				}
				openTelemetryBuilder.UseGrafana();
			});
		}
		multipleServerHostBuilder.ConfigureLogging(delegate(ILoggingBuilder logging)
		{
			logging.ClearProviders();
			logging.AddSerilog(Log.Logger, dispose: true);
			logging.SetMinimumLevel(minimumLevelLogLevel);
			if (Setting.IsOpenTelemetry)
			{
				logging.AddOpenTelemetry(delegate(OpenTelemetryLoggerOptions configure)
				{
					configure.UseGrafana();
				});
			}
		});
		using (Sdk.CreateTracerProviderBuilder().UseGrafana(delegate(GrafanaOpenTelemetrySettings config)
		{
			config.ServiceName = Setting.ServerName + "-" + Setting.TagName;
			config.ServiceVersion = Assembly.GetCallingAssembly().ImageRuntimeVersion;
			config.ServiceInstanceId = Setting.ServerId + "-" + Setting.ServerInstanceId;
			config.DeploymentEnvironment = ((!EnvironmentHelper.GetEnvironmentName().IsNullOrEmpty()) ? EnvironmentHelper.GetEnvironmentName() : (Setting.IsDebug ? "Debug" : "Release"));
		}).Build())
		{
			_gameServer = multipleServerHostBuilder.Build();
			await _gameServer.StartAsync();
		}
	}

	/// <summary>
	/// 配置WebSocket服务器参数
	/// </summary>
	private void ConfigureWebServer(ServerOptions serverOptions)
	{
		FuFramework.SuperSocket.Server.Abstractions.ListenOptions listener = new FuFramework.SuperSocket.Server.Abstractions.ListenOptions
		{
			Ip = IPAddress.Any.ToString(),
			Port = Setting.WsPort
		};
		serverOptions.AddListener(listener);
	}

	/// <summary>
	/// WebSocket消息处理方法
	/// </summary>
	/// <param name="session">WebSocket会话对象</param>
	/// <param name="messagePackage">接收到的消息包</param>
	private async ValueTask WebSocketMessageHandler(WebSocketSession session, WebSocketPackage messagePackage)
	{
		if (messagePackage.OpCode != OpCode.Binary)
		{
			await session.CloseAsync(FuFramework.SuperSocket.WebSocket.CloseReason.ProtocolError);
			return;
		}
		ReadOnlySequence<byte> sequence = messagePackage.Data;
		IMessage message = MessageHelper.DecoderHandler.Handler(ref sequence);
		await PackageHandler(session, message);
	}
}
