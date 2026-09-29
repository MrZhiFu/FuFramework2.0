using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// 日志脚本宏定义。
	/// 功能：
	///     1. 配合日志系统使用，在编译阶段控制日志的输出级别。
	/// </summary>
	public static class LogScriptingDefineSymbols
	{
		private const string ENABLE_LOG_SYMBOL = "ENABLE_LOG"; // 开启所有级别日志(预定义符号)

		private const string ENABLE_INFO_AND_ABOVE_LOG_SYMBOL    = "ENABLE_INFO_AND_ABOVE_LOG";    // 开启信息(Info)及以上级别的日志(预定义符号)
		private const string ENABLE_DEBUG_AND_ABOVE_LOG_SYMBOL   = "ENABLE_DEBUG_AND_ABOVE_LOG";   // 开启调试(Debug)及以上级别的日志(预定义符号)
		private const string ENABLE_WARNING_AND_ABOVE_LOG_SYMBOL = "ENABLE_WARNING_AND_ABOVE_LOG"; // 开启警告(Warning)及以上级别的日志(预定义符号)
		private const string ENABLE_ERROR_AND_ABOVE_LOG_SYMBOL   = "ENABLE_ERROR_AND_ABOVE_LOG";   // 开启错误(Error)及以上级别的日志(预定义符号)
		private const string ENABLE_FATAL_AND_ABOVE_LOG_SYMBOL   = "ENABLE_FATAL_AND_ABOVE_LOG";   // 开启严重错误(Fatal)及以上级别的日志(预定义符号)

		private const string ENABLE_INFO_LOG_SYMBOL    = "ENABLE_INFO_LOG";    // 仅开启信息(Info)级别的日志(预定义符号)
		private const string ENABLE_DEBUG_LOG_SYMBOL   = "ENABLE_DEBUG_LOG";   // 仅开启调试(Debug)级别的日志(预定义符号)
		private const string ENABLE_WARNING_LOG_SYMBOL = "ENABLE_WARNING_LOG"; // 仅开启警告(Warning)级别的日志(预定义符号)
		private const string ENABLE_ERROR_LOG_SYMBOL   = "ENABLE_ERROR_LOG";   // 仅开启错误(Error)级别的日志(预定义符号)
		private const string ENABLE_FATAL_LOG_SYMBOL   = "ENABLE_FATAL_LOG";   // 仅开启严重错误(Fatal)级别的日志(预定义符号)

		/// <summary>
		/// 指定级别及以上级别的日志预定义符号。
		/// </summary>
		private static readonly string[] AboveLogSymbols =
		{
			ENABLE_INFO_AND_ABOVE_LOG_SYMBOL,
			ENABLE_DEBUG_AND_ABOVE_LOG_SYMBOL,
			ENABLE_WARNING_AND_ABOVE_LOG_SYMBOL,
			ENABLE_ERROR_AND_ABOVE_LOG_SYMBOL,
			ENABLE_FATAL_AND_ABOVE_LOG_SYMBOL
		};

		/// <summary>
		/// 指定的级别的日志预定义符号。
		/// </summary>
		private static readonly string[] SpecifyLogSymbols =
		{
			ENABLE_INFO_LOG_SYMBOL,
			ENABLE_DEBUG_LOG_SYMBOL,
			ENABLE_WARNING_LOG_SYMBOL,
			ENABLE_ERROR_LOG_SYMBOL,
			ENABLE_FATAL_LOG_SYMBOL
		};

		/// <summary>
		/// 开启所有日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/开启所有日志", false, FuMenuPriority.LOG_ENABLE_ALL)]
		public static void EnableAllLogs()
		{
			DisableAllLogs();
			ScriptingDefineSymbols.AddScriptingDefineSymbol(ENABLE_LOG_SYMBOL);
		}

		/// <summary>
		/// 禁用所有日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/禁用所有日志", false, FuMenuPriority.LOG_DISABLE_ALL)]
		public static void DisableAllLogs()
		{
			ScriptingDefineSymbols.RemoveScriptingDefineSymbol(ENABLE_LOG_SYMBOL);

			foreach (var specifyLogScriptingDefineSymbol in SpecifyLogSymbols)
			{
				ScriptingDefineSymbols.RemoveScriptingDefineSymbol(specifyLogScriptingDefineSymbol);
			}

			foreach (var aboveLogScriptingDefineSymbol in AboveLogSymbols)
			{
				ScriptingDefineSymbols.RemoveScriptingDefineSymbol(aboveLogScriptingDefineSymbol);
			}
		}

		/// <summary>
		/// 开启信息及以上级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/开启信息(Info)及以上级别的日志", false, FuMenuPriority.LOG_ENABLE_INFO_ABOVE)]
		public static void EnableInfoAndAboveLogs()
		{
			SetAboveLogScriptingDefineSymbol(ENABLE_INFO_AND_ABOVE_LOG_SYMBOL);
		}

		/// <summary>
		/// 开启调试及以上级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/开启调试(Debug)及以上级别的日志", false, FuMenuPriority.LOG_ENABLE_DEBUG_ABOVE)]
		public static void EnableDebugAndAboveLogs()
		{
			SetAboveLogScriptingDefineSymbol(ENABLE_DEBUG_AND_ABOVE_LOG_SYMBOL);
		}

		/// <summary>
		/// 开启警告及以上级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/开启警告(Warning)及以上级别的日志", false, FuMenuPriority.LOG_ENABLE_WARNING_ABOVE)]
		public static void EnableWarningAndAboveLogs()
		{
			SetAboveLogScriptingDefineSymbol(ENABLE_WARNING_AND_ABOVE_LOG_SYMBOL);
		}

		/// <summary>
		/// 开启错误及以上级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/开启错误(Error)及以上级别的日志", false, FuMenuPriority.LOG_ENABLE_ERROR_ABOVE)]
		public static void EnableErrorAndAboveLogs()
		{
			SetAboveLogScriptingDefineSymbol(ENABLE_ERROR_AND_ABOVE_LOG_SYMBOL);
		}

		/// <summary>
		/// 开启严重错误及以上级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/开启严重错误(Fatal)及以上级别的日志", false, FuMenuPriority.LOG_ENABLE_FATAL_ABOVE)]
		public static void EnableFatalAndAboveLogs()
		{
			SetAboveLogScriptingDefineSymbol(ENABLE_FATAL_AND_ABOVE_LOG_SYMBOL);
		}

		/// <summary>
		/// 仅开启信息级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/仅开启信息(Info)级别日志", false, FuMenuPriority.LOG_ONLY_INFO)]
		public static void EnableInfoLogOnly()
		{
			SetSpecifyLogScriptingDefineSymbols(new[] { ENABLE_INFO_LOG_SYMBOL });
		}

		/// <summary>
		/// 仅开启调试级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/仅开启调试(Debug)级别日志", false, FuMenuPriority.LOG_ONLY_DEBUG)]
		public static void EnableDebugLogOnly()
		{
			SetSpecifyLogScriptingDefineSymbols(new[] { ENABLE_DEBUG_LOG_SYMBOL });
		}

		/// <summary>
		/// 仅开启警告级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/仅开启警告(Warning)级别日志", false, FuMenuPriority.LOG_ONLY_WARNING)]
		public static void EnableWarningLogOnly()
		{
			SetSpecifyLogScriptingDefineSymbols(new[] { ENABLE_WARNING_LOG_SYMBOL });
		}

		/// <summary>
		/// 仅开启错误级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/仅开启错误(Error)级别日志", false, FuMenuPriority.LOG_ONLY_ERROR)]
		public static void EnableErrorLogOnly()
		{
			SetSpecifyLogScriptingDefineSymbols(new[] { ENABLE_ERROR_LOG_SYMBOL });
		}

		/// <summary>
		/// 仅开启严重错误级别的日志。
		/// </summary>
		[MenuItem("FuFramework/日志设置/仅开启严重错误(Fatal)级别日志", false, FuMenuPriority.LOG_ONLY_FATAL)]
		public static void EnableFatalLogOnly()
		{
			SetSpecifyLogScriptingDefineSymbols(new[] { ENABLE_FATAL_LOG_SYMBOL });
		}


		/// <summary>
		/// 设置日志预定义符号。
		/// </summary>
		/// <param name="logSymbol">要设置的日志预定义符号。</param>
		private static void SetAboveLogScriptingDefineSymbol(string logSymbol)
		{
			if (string.IsNullOrEmpty(logSymbol)) return;

			foreach (var i in AboveLogSymbols)
			{
				if (i != logSymbol) continue;
				DisableAllLogs();
				ScriptingDefineSymbols.AddScriptingDefineSymbol(logSymbol);
				return;
			}
		}

		/// <summary>
		/// 设置特殊指定的日志预定义符号。
		/// </summary>
		/// <param name="logSymbols">要设置的日志预定义符号数组。</param>
		private static void SetSpecifyLogScriptingDefineSymbols(string[] logSymbols)
		{
			if (logSymbols is not { Length: > 0 }) return;

			// 先禁用所有日志
			DisableAllLogs();

			// 添加指定的日志符号
			foreach (var logSymbol in logSymbols)
			{
				if (string.IsNullOrEmpty(logSymbol)) continue;

				// 验证是否是有效的指定级别日志符号
				if (IsValidSpecifyLogSymbol(logSymbol))
				{
					ScriptingDefineSymbols.AddScriptingDefineSymbol(logSymbol);
				}
			}
		}

		/// <summary>
		/// 验证是否是有效的指定级别日志符号。
		/// </summary>
		private static bool IsValidSpecifyLogSymbol(string symbol)
		{
			foreach (var validSymbol in SpecifyLogSymbols)
			{
				if (validSymbol == symbol) return true;
			}

			return false;
		}
	}
}