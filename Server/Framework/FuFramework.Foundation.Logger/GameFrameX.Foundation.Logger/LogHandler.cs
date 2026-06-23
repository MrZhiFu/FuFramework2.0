using System;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace FuFramework.Foundation.Logger;

/// <summary>
/// 日志处理器类，提供日志系统的初始化和配置功能
/// </summary>
public static class LogHandler
{
	private static bool _isInitSerilogDiagnosis;

	/// <summary>
	/// 启用 Serilog 的自动诊断
	/// </summary>
	private static void SerilogDiagnosis()
	{
		if (!_isInitSerilogDiagnosis)
		{
			SelfLog.Enable(delegate(string message)
			{
				Console.WriteLine("Serilog:SelfLog:" + message);
			});
			_isInitSerilogDiagnosis = true;
		}
	}

	/// <summary>
	/// 启动并配置日志系统
	/// </summary>
	/// <param name="logOptions">日志配置选项，包含日志级别、存储路径等配置信息</param>
	/// <param name="isDefault">是否设置为默认配置</param>
	/// <param name="configurationAction">自定义日志配置回调</param>
	/// <exception cref="T:System.ArgumentNullException">当logOptions参数为null时抛出</exception>
	/// <exception cref="T:System.Exception">初始化日志系统过程中发生的其他异常</exception>
	public static ILogger Create(LogOptions logOptions, bool isDefault = true, Action<LoggerConfiguration> configurationAction = null)
	{
		ArgumentNullException.ThrowIfNull(logOptions, "logOptions");
		SerilogDiagnosis();
		try
		{
			string path = "_" + (logOptions.LogType ?? AppDomain.CurrentDomain.FriendlyName) + ".log";
			string text = logOptions.LogSavePath ?? "./logs/";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path2 = Path.Combine(text, path);
			Console.WriteLine("以下为日志配置信息");
			Console.WriteLine(logOptions);
			Console.WriteLine("日志配置信息结束");
			Console.WriteLine();
			LoggerSinkConfiguration writeTo = new LoggerConfiguration().Enrich.FromLogContext().MinimumLevel.Override("Microsoft", LogEventLevel.Information).MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning).WriteTo;
			RollingInterval rollingInterval = logOptions.RollingInterval;
			bool isFileSizeLimit = logOptions.IsFileSizeLimit;
			long? fileSizeLimitBytes = logOptions.FileSizeLimitBytes;
			int? retainedFileCountLimit = logOptions.RetainedFileCountLimit;
			LoggerConfiguration loggerConfiguration = writeTo.File(path2, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, fileSizeLimitBytes, null, buffered: false, shared: false, null, rollingInterval, isFileSizeLimit, retainedFileCountLimit, null, null, null);
			configurationAction?.Invoke(loggerConfiguration);
			switch (logOptions.LogEventLevel)
			{
			case LogEventLevel.Verbose:
				loggerConfiguration.MinimumLevel.Verbose();
				break;
			case LogEventLevel.Debug:
				loggerConfiguration.MinimumLevel.Debug();
				break;
			case LogEventLevel.Information:
				loggerConfiguration.MinimumLevel.Information();
				break;
			case LogEventLevel.Warning:
				loggerConfiguration.MinimumLevel.Warning();
				break;
			case LogEventLevel.Error:
				loggerConfiguration.MinimumLevel.Error();
				break;
			case LogEventLevel.Fatal:
				loggerConfiguration.MinimumLevel.Fatal();
				break;
			}
			if (logOptions.IsConsole)
			{
				loggerConfiguration.WriteTo.Console(LogEventLevel.Verbose, "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", null, null, null);
			}
			Serilog.Core.Logger logger = loggerConfiguration.CreateLogger();
			if (isDefault)
			{
				Log.Logger = logger;
				LogHelper.SetLogger(logger);
			}
			Console.WriteLine("日志系统配置 结束");
			return logger;
		}
		catch (Exception value)
		{
			Log.Error($"配置日志系统过程中发生错误,异常:{value}");
			throw;
		}
	}
}
