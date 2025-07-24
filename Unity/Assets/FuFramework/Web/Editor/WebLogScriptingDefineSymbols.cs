//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using UnityEditor;

namespace GameFrameX.Web.Editor
{
    /// <summary>
    /// 网络日志脚本宏定义。
    /// </summary>
    public static class WebLogScriptingDefineSymbols
    {
        public const string EnableNetworkReceiveLogScriptingDefineSymbol = "ENABLE_GAMEFRAMEX_WEB_RECEIVE_LOG";
        public const string EnableNetworkSendLogScriptingDefineSymbol = "ENABLE_GAMEFRAMEX_WEB_SEND_LOG";

        /// <summary>
        /// 禁用Web接收日志脚本宏定义。
        /// </summary>
        [MenuItem("GameFrameX/Scripting Define Symbols/Disable Web Receive Logs(关闭Web接收日志打印)", false, 460)]
        public static void DisableNetworkReceiveLogs()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableNetworkReceiveLogScriptingDefineSymbol);
        }

        /// <summary>
        /// 开启Web接收日志脚本宏定义。
        /// </summary>
        [MenuItem("GameFrameX/Scripting Define Symbols/Enable Web Receive Logs(开启Web接收日志打印)", false, 461)]
        public static void EnableNetworkReceiveLogs()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableNetworkReceiveLogScriptingDefineSymbol);
        }

        /// <summary>
        /// 禁用Web发送日志脚本宏定义。
        /// </summary>
        [MenuItem("GameFrameX/Scripting Define Symbols/Disable Web Send Logs(关闭Web发送日志打印)", false, 450)]
        public static void DisableNetworkSendLogs()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableNetworkSendLogScriptingDefineSymbol);
        }

        /// <summary>
        /// 开启Web发送日志脚本宏定义。
        /// </summary>
        [MenuItem("GameFrameX/Scripting Define Symbols/Enable Web Send Logs(开启Web发送日志打印)", false, 451)]
        public static void EnableNetworkSendLogs()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableNetworkSendLogScriptingDefineSymbol);
        }
    }
}