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
        private const string EnableLogSymbol = "ENABLE_LOG"; // 开启所有级别日志(预定义符号)

        private const string EnableInfoAndAboveLogSymbol    = "ENABLE_INFO_AND_ABOVE_LOG";    // 开启信息(Info)及以上级别的日志(预定义符号)
        private const string EnableDebugAndAboveLogSymbol   = "ENABLE_DEBUG_AND_ABOVE_LOG";   // 开启调试(Debug)及以上级别的日志(预定义符号)
        private const string EnableWarningAndAboveLogSymbol = "ENABLE_WARNING_AND_ABOVE_LOG"; // 开启警告(Warning)及以上级别的日志(预定义符号)
        private const string EnableErrorAndAboveLogSymbol   = "ENABLE_ERROR_AND_ABOVE_LOG";   // 开启错误(Error)及以上级别的日志(预定义符号)
        private const string EnableFatalAndAboveLogSymbol   = "ENABLE_FATAL_AND_ABOVE_LOG";   // 开启严重错误(Fatal)及以上级别的日志(预定义符号)

        private const string EnableInfoLogSymbol    = "ENABLE_INFO_LOG";    // 仅开启信息(Info)级别的日志(预定义符号)
        private const string EnableDebugLogSymbol   = "ENABLE_DEBUG_LOG";   // 仅开启调试(Debug)级别的日志(预定义符号)
        private const string EnableWarningLogSymbol = "ENABLE_WARNING_LOG"; // 仅开启警告(Warning)级别的日志(预定义符号)
        private const string EnableErrorLogSymbol   = "ENABLE_ERROR_LOG";   // 仅开启错误(Error)级别的日志(预定义符号)
        private const string EnableFatalLogSymbol   = "ENABLE_FATAL_LOG";   // 仅开启严重错误(Fatal)级别的日志(预定义符号)

        /// <summary>
        /// 指定级别及以上级别的日志预定义符号。
        /// </summary>
        private static readonly string[] AboveLogSymbols =
        {
            EnableInfoAndAboveLogSymbol,
            EnableDebugAndAboveLogSymbol,
            EnableWarningAndAboveLogSymbol,
            EnableErrorAndAboveLogSymbol,
            EnableFatalAndAboveLogSymbol
        };

        /// <summary>
        /// 指定的级别的日志预定义符号。
        /// </summary>
        private static readonly string[] SpecifyLogSymbols =
        {
            EnableInfoLogSymbol,
            EnableDebugLogSymbol,
            EnableWarningLogSymbol,
            EnableErrorLogSymbol,
            EnableFatalLogSymbol
        };

        /// <summary>
        /// 开启所有日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启所有日志", false, 600)]
        public static void EnableAllLogs()
        {
            DisableAllLogs();
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableLogSymbol);
        }

        /// <summary>
        /// 禁用所有日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/禁用所有日志", false, 601)]
        public static void DisableAllLogs()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableLogSymbol);

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
        [MenuItem("FuFramework/日志设置/开启信息(Info)及以上级别的日志", false, 700)]
        public static void EnableInfoAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(EnableInfoAndAboveLogSymbol);
        }

        /// <summary>
        /// 开启调试及以上级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启调试(Debug)及以上级别的日志", false, 701)]
        public static void EnableDebugAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(EnableDebugAndAboveLogSymbol);
        }

        /// <summary>
        /// 开启警告及以上级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启警告(Warning)及以上级别的日志", false, 702)]
        public static void EnableWarningAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(EnableWarningAndAboveLogSymbol);
        }

        /// <summary>
        /// 开启错误及以上级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启错误(Error)及以上级别的日志", false, 703)]
        public static void EnableErrorAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(EnableErrorAndAboveLogSymbol);
        }

        /// <summary>
        /// 开启严重错误及以上级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启严重错误(Fatal)及以上级别的日志", false, 704)]
        public static void EnableFatalAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(EnableFatalAndAboveLogSymbol);
        }

        /// <summary>
        /// 仅开启信息级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/仅开启信息(Info)级别日志", false, 800)]
        public static void EnableInfoLogOnly()
        {
            SetSpecifyLogScriptingDefineSymbols(new[] { EnableInfoLogSymbol });
        }

        /// <summary>
        /// 仅开启调试级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/仅开启调试(Debug)级别日志", false, 801)]
        public static void EnableDebugLogOnly()
        {
            SetSpecifyLogScriptingDefineSymbols(new[] { EnableDebugLogSymbol });
        }

        /// <summary>
        /// 仅开启警告级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/仅开启警告(Warning)级别日志", false, 802)]
        public static void EnableWarningLogOnly()
        {
            SetSpecifyLogScriptingDefineSymbols(new[] { EnableWarningLogSymbol });
        }

        /// <summary>
        /// 仅开启错误级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/仅开启错误(Error)级别日志", false, 803)]
        public static void EnableErrorLogOnly()
        {
            SetSpecifyLogScriptingDefineSymbols(new[] { EnableErrorLogSymbol });
        }

        /// <summary>
        /// 仅开启严重错误级别的日志。
        /// </summary>
        [MenuItem("FuFramework/日志设置/仅开启严重错误(Fatal)级别日志", false, 804)]
        public static void EnableFatalLogOnly()
        {
            SetSpecifyLogScriptingDefineSymbols(new[] { EnableFatalLogSymbol });
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