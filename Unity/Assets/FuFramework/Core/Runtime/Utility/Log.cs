using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 日志工具集。
    /// 目的：通过宏定义控制的，在编译时决定是否编译用于打印日志的代码，从而实现日志的开关。
    /// 
    /// 注意点：1.为了完整地屏蔽掉日志开销，在书写日志时需注意格式，这里以开启仅打印错误及以上级别日志为例。
    ///           1.1 有内存开销，在开启仅打印错误及以上级别日志后，信息日志虽然不打印了，但 welcomeMessage 字符串临时变量的拼接会导致内存分配，如：
    ///             string welcomeMessage = Utility.Text.Format("Hello Game Framework {0}.", Version.GameFrameworkVersion);
    ///             Log.Info(welcomeMessage);
    ///
    ///           1.2 在开启仅打印错误及以上级别日志后，以下两种写法均没有任何开销，如：
    ///             Log.Info("Hello Game Framework {0}.", Version.GameFrameworkVersion);
    ///             Log.Info(Utility.Text.Format("Hello Game Framework {0}.", Version.GameFrameworkVersion));
    /// 
    ///        2.日志参数数量尽可能不要超过 3 个(日志参数超过 3 个（≥4）时，将会生成可变参数数组，导致额外的内存分配)。
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(object message)
        {
            FuLog.Debug(message);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(string message)
        {
            FuLog.Debug(message);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T>(string format, T arg)
        {
            FuLog.Debug(format, arg);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            FuLog.Debug(format, arg1, arg2);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            FuLog.Debug(format, arg1, arg2, arg3);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                    T11 arg11, T12 arg12)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                         T11 arg11, T12 arg12, T13 arg13)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                              T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                   T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <typeparam name="T16">日志参数 16 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <param name="arg16">日志参数 16。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_DEBUG_LOG 或 ENABLE_DEBUG_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                        T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            FuLog.Debug(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info(object message)
        {
            FuLog.Info(message);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info(string message)
        {
            FuLog.Info(message);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T>(string format, T arg)
        {
            FuLog.Info(format, arg);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            FuLog.Info(format, arg1, arg2);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            FuLog.Info(format, arg1, arg2, arg3);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11,
                                                                                   T12 arg12)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                        T11 arg11, T12 arg12, T13 arg13)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                             T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                  T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <typeparam name="T16">日志参数 16 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <param name="arg16">日志参数 16。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_INFO_LOG、ENABLE_DEBUG_AND_ABOVE_LOG 或 ENABLE_INFO_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                       T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            FuLog.Info(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning(object message)
        {
            FuLog.Warning(message);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning(string message)
        {
            FuLog.Warning(message);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T>(string format, T arg)
        {
            FuLog.Warning(format, arg);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            FuLog.Warning(format, arg1, arg2);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            FuLog.Warning(format, arg1, arg2, arg3);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                      T11 arg11, T12 arg12)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                           T11 arg11, T12 arg12, T13 arg13)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                     T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <typeparam name="T16">日志参数 16 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <param name="arg16">日志参数 16。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_WARNING_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG 或 ENABLE_WARNING_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8,
                                                                                                          T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            FuLog.Warning(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error(object message)
        {
            FuLog.Error(message);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error(string message)
        {
            FuLog.Error(message);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T>(string format, T arg)
        {
            FuLog.Error(format, arg);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            FuLog.Error(format, arg1, arg2);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            FuLog.Error(format, arg1, arg2, arg3);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                    T11 arg11, T12 arg12)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                         T11 arg11, T12 arg12, T13 arg13)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                              T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                   T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致游戏崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <typeparam name="T16">日志参数 16 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <param name="arg16">日志参数 16。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_ERROR_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG 或 ENABLE_ERROR_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                        T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            FuLog.Error(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal(object message)
        {
            FuLog.Fatal(message);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal(string message)
        {
            FuLog.Fatal(message);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T>(string format, T arg)
        {
            FuLog.Fatal(format, arg);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            FuLog.Fatal(format, arg1, arg2);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            FuLog.Fatal(format, arg1, arg2, arg3);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                    T11 arg11, T12 arg12)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                         T11 arg11, T12 arg12, T13 arg13)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10,
                                                                                              T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                   T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致游戏崩溃或异常时使用，此时应尝试重启进程或重建游戏框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <typeparam name="T11">日志参数 11 的类型。</typeparam>
        /// <typeparam name="T12">日志参数 12 的类型。</typeparam>
        /// <typeparam name="T13">日志参数 13 的类型。</typeparam>
        /// <typeparam name="T14">日志参数 14 的类型。</typeparam>
        /// <typeparam name="T15">日志参数 15 的类型。</typeparam>
        /// <typeparam name="T16">日志参数 16 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        /// <param name="arg11">日志参数 11。</param>
        /// <param name="arg12">日志参数 12。</param>
        /// <param name="arg13">日志参数 13。</param>
        /// <param name="arg14">日志参数 14。</param>
        /// <param name="arg15">日志参数 15。</param>
        /// <param name="arg16">日志参数 16。</param>
        /// <remarks>仅在带有 ENABLE_LOG、ENABLE_FATAL_LOG、ENABLE_DEBUG_AND_ABOVE_LOG、ENABLE_INFO_AND_ABOVE_LOG、ENABLE_WARNING_AND_ABOVE_LOG、ENABLE_ERROR_AND_ABOVE_LOG 或 ENABLE_FATAL_AND_ABOVE_LOG 预编译选项时生效。</remarks>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9,
                                                                                                        T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            FuLog.Fatal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
    }
}