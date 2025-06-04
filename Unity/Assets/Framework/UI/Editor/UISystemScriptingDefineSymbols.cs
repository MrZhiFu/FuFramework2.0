//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using UnityEditor;

namespace GameFrameX.UI.Editor
{
    /// <summary>
    /// UI系统脚本宏定义。
    /// </summary>
    public static class UISystemScriptingDefineSymbols
    {
        public const string UGUIScriptingDefineSymbol = "ENABLE_UI_UGUI";
        public const string FairyGUIScriptingDefineSymbol = "ENABLE_UI_FAIRYGUI";

        /// <summary>
        /// 开启UGUI UI脚本宏定义。
        /// </summary>
        [MenuItem("GameFrameX/Scripting Define Symbols/Enable UGUI(开启UGUI适配)", false, 1000)]
        public static void EnableUGUISystem()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(FairyGUIScriptingDefineSymbol);
            ScriptingDefineSymbols.AddScriptingDefineSymbol(UGUIScriptingDefineSymbol);
        }

        /// <summary>
        /// 开启FairyGUI UI脚本宏定义。
        /// </summary>
        [MenuItem("GameFrameX/Scripting Define Symbols/Enable FairyGUI(开启FairyGUI适配)", false, 1001)]
        public static void EnableFairyGUISystem()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(UGUIScriptingDefineSymbol);
            ScriptingDefineSymbols.AddScriptingDefineSymbol(FairyGUIScriptingDefineSymbol);
        }
    }
}