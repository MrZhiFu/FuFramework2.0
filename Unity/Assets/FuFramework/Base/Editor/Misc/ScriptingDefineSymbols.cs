//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 脚本宏定义。
    /// </summary>
    public static class ScriptingDefineSymbols
    {
        /// <summary>
        /// 打包平台。
        /// </summary>
        private static readonly BuildTargetGroup[] BuildTargetGroups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Android,
            BuildTargetGroup.WSA,
            BuildTargetGroup.WebGL
        };

        /// <summary>
        /// 检查指定平台是否存在指定的脚本宏定义。
        /// </summary>
        /// <param name="buildTargetGroup">要检查脚本宏定义的平台。</param>
        /// <param name="symbol">要检查的脚本宏定义。</param>
        /// <returns>指定平台是否存在指定的脚本宏定义。</returns>
        public static bool HasScriptingDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return false;

            var symbols = GetScriptingDefineSymbols(buildTargetGroup);
            return symbols.Any(symbolTemp => symbolTemp == symbol);
        }

        /// <summary>
        /// 为指定平台增加指定的脚本宏定义。
        /// </summary>
        /// <param name="buildTargetGroup">要增加脚本宏定义的平台。</param>
        /// <param name="symbol">要增加的脚本宏定义。</param>
        public static void AddScriptingDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;
            if (HasScriptingDefineSymbol(buildTargetGroup, symbol)) return;

            var symbols = new List<string>(GetScriptingDefineSymbols(buildTargetGroup))
            {
                symbol
            };

            SetScriptingDefineSymbols(buildTargetGroup, symbols.ToArray());
        }

        /// <summary>
        /// 为指定平台移除指定的脚本宏定义。
        /// </summary>
        /// <param name="buildTargetGroup">要移除脚本宏定义的平台。</param>
        /// <param name="symbol">要移除的脚本宏定义。</param>
        public static void RemoveScriptingDefineSymbol(BuildTargetGroup buildTargetGroup, string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;
            if (!HasScriptingDefineSymbol(buildTargetGroup, symbol)) return;

            var symbols = new List<string>(GetScriptingDefineSymbols(buildTargetGroup));
            while (symbols.Contains(symbol))
            {
                symbols.Remove(symbol);
            }

            SetScriptingDefineSymbols(buildTargetGroup, symbols.ToArray());
        }

        /// <summary>
        /// 为所有平台增加指定的脚本宏定义。
        /// </summary>
        /// <param name="symbol">要增加的脚本宏定义。</param>
        public static void AddScriptingDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;
            foreach (BuildTargetGroup buildTargetGroup in BuildTargetGroups)
            {
                AddScriptingDefineSymbol(buildTargetGroup, symbol);
            }
        }

        /// <summary>
        /// 为所有平台移除指定的脚本宏定义。
        /// </summary>
        /// <param name="symbol">要移除的脚本宏定义。</param>
        public static void RemoveScriptingDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;
            foreach (BuildTargetGroup buildTargetGroup in BuildTargetGroups)
            {
                RemoveScriptingDefineSymbol(buildTargetGroup, symbol);
            }
        }

        /// <summary>
        /// 获取指定平台的脚本宏定义。
        /// </summary>
        /// <param name="buildTargetGroup">要获取脚本宏定义的平台。</param>
        /// <returns>平台的脚本宏定义。</returns>
        public static string[] GetScriptingDefineSymbols(BuildTargetGroup buildTargetGroup)
        {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');
        }

        /// <summary>
        /// 设置指定平台的脚本宏定义。
        /// </summary>
        /// <param name="buildTargetGroup">要设置脚本宏定义的平台。</param>
        /// <param name="symbols">要设置的脚本宏定义。</param>
        public static void SetScriptingDefineSymbols(BuildTargetGroup buildTargetGroup, string[] symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbols));
        }
    }
}