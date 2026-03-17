using FuFramework.Core.Editor;
using UnityEditor;

namespace FuFramework.Config.Editor
{
    /// <summary>
    /// 配置表二进制功能脚本宏定义。
    /// </summary>
    public static class ConfigDefineSymbols
    {
        public const string EnableBinaryConfigSymbol = "ENABLE_BINARY_CONFIG";

        /// <summary>
        /// 开启配置表为二进制的脚本宏定义。
        /// </summary>
        [MenuItem("FuFramework/配置表设置/开启二进制配置表", false, 1000)]
        public static void EnableBinaryConfig()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableBinaryConfigSymbol);
        }

        /// <summary>
        /// 禁用配置表为二进制的脚本宏定义。
        /// </summary>
        [MenuItem("FuFramework/配置表设置/关闭二进制配置表", false, 1001)]
        public static void DisableBinaryConfig()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableBinaryConfigSymbol);
        }
    }
}