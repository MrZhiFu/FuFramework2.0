using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.Core.Log
{
	/// <summary>
	/// 游戏框架日志类。
	/// 输出自动携带调用方类名前缀：编译期经 [CallerFilePath] 注入调用方源文件路径，
	/// 剥去扩展名与部分类后缀得到类名，并按类名 FNV-1a 哈希从调色板稳定取色染色（见 LogColorEx）。
	/// </summary>
	public static class FuLogger
	{
		/// <summary>
		/// 打印信息级别日志，用于记录程序正常运行日志信息。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_INFO_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		public static void LogInfo(object message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Info, message, callerFile);

		/// <summary>
		/// 打印信息级别日志，用于记录程序正常运行日志信息。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_INFO_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		public static void LogInfo(string message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Info, message, callerFile);


		/// <summary>
		/// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_WARNING_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		[Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
		public static void LogWarning(object message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Warning, message, callerFile);

		/// <summary>
		/// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_WARNING_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		[Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
		public static void LogWarning(string message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Warning, message, callerFile);


		/// <summary>
		/// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_ERROR_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		[Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
		[Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
		public static void LogError(object message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Error, message, callerFile);

		/// <summary>
		/// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_ERROR_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		[Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
		[Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
		public static void LogError(string message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Error, message, callerFile);


		/// <summary>
		/// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_FATAL_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		[Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
		[Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
		[Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
		public static void LogFatal(object message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Fatal, message, callerFile);

		/// <summary>
		/// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
		/// </summary>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		[Conditional("ENABLE_LOG")]
		[Conditional("ENABLE_FATAL_LOG")]
		[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
		[Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
		[Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
		[Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
		[Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
		public static void LogFatal(string message, [CallerFilePath] string callerFile = "") => Log(ELogLevel.Fatal, message, callerFile);


		/// <summary>
		/// 记录日志。
		/// </summary>
		/// <param name="level">日志等级。</param>
		/// <param name="message">日志内容。</param>
		/// <param name="callerFile">调用方源文件路径，由公开方法经 [CallerFilePath] 注入。</param>
		private static void Log(ELogLevel level, object message, string callerFile)
		{
			var tag  = LogColorEx.ClassTag(callerFile);
			var text = string.IsNullOrEmpty(tag) ? $"{message}" : $"{tag} {message}";

			switch (level)
			{
				// @formatter:off
				case ELogLevel.Info:    UnityEngine.Debug.Log(text);        break;
				case ELogLevel.Warning: UnityEngine.Debug.LogWarning(text); break;
				case ELogLevel.Error:   UnityEngine.Debug.LogError(text);   break;
				case ELogLevel.Fatal:   UnityEngine.Debug.LogError(text);   break;
				default:                throw new ArgumentOutOfRangeException(nameof(level), level, text);
				// @formatter:on
			}
		}
	}
}