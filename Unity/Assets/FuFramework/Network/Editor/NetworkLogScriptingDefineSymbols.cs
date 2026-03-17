using FuFramework.Core.Editor;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
    /// <summary>
    /// 网络日志脚本宏定义。
    /// </summary>
    public static class NetworkLogScriptingDefineSymbols
    {
        private const string EnableNetworkRspLogSymbol  = "ENABLE_NETWORK_RSP_LOG";  // 开启网络响应日志(预定义符号)
        private const string EnableNetworkReqLogSymbol  = "ENABLE_NETWORK_REQ_LOG";  // 开启网络请求日志(预定义符号)
        private const string ForceEnableWebSocketSymbol = "FORCE_ENABLE_WEB_SOCKET"; // 强制使用WebSocket网络(预定义符号)

        /// <summary>
        /// 开启网络响应日志打印。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启网络响应日志打印", false, 900)]
        public static void EnableNetworkRspLog()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableNetworkRspLogSymbol);
        }

        /// <summary>
        /// 关闭网络响应日志打印。
        /// </summary>
        [MenuItem("FuFramework/日志设置/关闭网络响应日志打印", false, 901)]
        public static void DisableNetworkRspLog()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableNetworkRspLogSymbol);
        }

        /// <summary>
        /// 开启网络请求日志打印。
        /// </summary>
        [MenuItem("FuFramework/日志设置/开启网络请求日志打印", false, 903)]
        public static void EnableNetworkReqLog()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableNetworkReqLogSymbol);
        }

        /// <summary>
        /// 关闭网络请求日志打印。
        /// </summary>
        [MenuItem("FuFramework/日志设置/关闭网络请求日志打印", false, 904)]
        public static void DisableNetworkReqLog()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableNetworkReqLogSymbol);
        }

        /// <summary>
        /// 不强制使用WebSocket网络。
        /// </summary>
        [MenuItem("FuFramework/网络类型设置/不强制使用WebSocket网络", false, 1100)]
        public static void DisableForceWebSocketNetwork()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(ForceEnableWebSocketSymbol);
        }

        /// <summary>
        /// 不强制使用WebSocket网络。
        /// </summary>
        [MenuItem("FuFramework/网络类型设置/强制使用WebSocket网络", false, 1101)]
        public static void EnableForceWebSocketNetwork()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(ForceEnableWebSocketSymbol);
        }
    }
}