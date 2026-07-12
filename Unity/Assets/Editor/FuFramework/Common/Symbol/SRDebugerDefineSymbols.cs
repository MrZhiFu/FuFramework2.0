using UnityEditor;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// SRDebugger工具的脚本宏定义。
    /// 功能：
    ///     1. 控制SRDebugger的开启/关闭。
    /// </summary>
    public static class SRDebuggerDefineSymbols
    {
        /// 开启SRDebugger工具(预定义符号)
        private const string EnableLogSymbol = "ENABLE_SRDEBUGGER";

        /// <summary>
        /// 开启所有日志。
        /// </summary>
        [MenuItem("FuFramework/SRDebugger工具/开启", false, 601)]
        public static void EnableSRDebugger()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableLogSymbol);
        }

        /// <summary>
        /// 开启所有日志。
        /// </summary>
        [MenuItem("FuFramework/SRDebugger工具/关闭", false, 602)]
        public static void EnableAllLogs()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableLogSymbol);
        }
    }
}