#if UNITY_WEBGL

using UnityEditor;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 小游戏宏定义帮助类。
    /// 功能：
    ///     1. 开启微信小游戏的适配。
    ///     2. 关闭微信小游戏的适配。
    ///     3. 开启抖音小游戏的适配。
    ///     4. 关闭抖音小游戏的适配。
    /// </summary>
    public static class MiniGameDefineSymbolHelper
    {
        /// <summary>
        /// 开启微信小游戏的适配的宏定义
        /// </summary>
        public const string EnableWeChatMiniGameScriptingDefineSymbol = "ENABLE_WECHAT_MINI_GAME";

        /// <summary>
        /// 开启抖音小游戏的适配的宏定义
        /// </summary>
        public const string EnableDouYinMiniGameScriptingDefineSymbol = "ENABLE_DOUYIN_MINI_GAME";


        /// <summary>
        /// 开启微信小游戏的适配
        /// </summary>
        [MenuItem("FuFramework/MiniGame/WeChat/Open", false, 10)]
        public static void OpenWeChatMiniGame()
        {
            if (!ScriptingDefineSymbols.HasScriptingDefineSymbol(BuildTargetGroup.WebGL, EnableWeChatMiniGameScriptingDefineSymbol))
            {
                ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableWeChatMiniGameScriptingDefineSymbol);
            }

            Debug.Log($"微信小游戏宏定义 [{EnableDouYinMiniGameScriptingDefineSymbol}] 已经打开");
        }


        /// <summary>
        /// 关闭微信小游戏的适配
        /// </summary>
        [MenuItem("FuFramework/MiniGame/WeChat/Close", false, 11)]
        public static void CloseWeChatMiniGame()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableWeChatMiniGameScriptingDefineSymbol);
        }

        /// <summary>
        /// 开启抖音小游戏的适配
        /// </summary>
        [MenuItem("FuFramework/MiniGame/DouYin/Open", false, 20)]
        public static void OpenDouYinMiniGame()
        {
            if (!ScriptingDefineSymbols.HasScriptingDefineSymbol(BuildTargetGroup.WebGL, EnableDouYinMiniGameScriptingDefineSymbol))
            {
                ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableDouYinMiniGameScriptingDefineSymbol);
            }

            Debug.Log($"抖音小游戏宏定义 [{EnableDouYinMiniGameScriptingDefineSymbol}] 已经打开");
        }

        /// <summary>
        /// 关闭抖音小游戏的适配
        /// </summary>
        [MenuItem("FuFramework/MiniGame/DouYin/Close", false, 21)]
        public static void CloseDouYinMiniGame()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableDouYinMiniGameScriptingDefineSymbol);
        }
    }
}

#endif