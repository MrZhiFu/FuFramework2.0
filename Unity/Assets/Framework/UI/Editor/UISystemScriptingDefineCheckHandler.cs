using GameFrameX.Editor;
using UnityEditor;

namespace GameFrameX.UI.Editor
{
    /// <summary>
    /// 用于检查当前项目是否已定义了UI系统的宏定义符号。
    /// 如果未定义任何UI系统的宏定义，则提示用户选择一个UI系统并启用相应的宏定义。
    /// </summary>
    public sealed class UISystemScriptingDefineCheckHandler
    {
        /// <summary>
        /// 在加载时运行的方法，用于检查UI系统的宏定义符号。
        /// </summary>
        [InitializeOnLoadMethod]
        static void Run()
        {
            // 检查是否定义了UGUI的宏定义符号。
            var hasUGUIScriptingDefineSymbol = ScriptingDefineSymbols.HasScriptingDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, UISystemScriptingDefineSymbols.UGUIScriptingDefineSymbol);


            // 检查是否定义了FairyGUI的宏定义符号。
            var hasFairyGUIScriptingDefineSymbol = ScriptingDefineSymbols.HasScriptingDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, UISystemScriptingDefineSymbols.FairyGUIScriptingDefineSymbol);


            // 如果未定义任何UI系统的宏定义符号，则提示用户选择一个UI系统。
            if (!(hasFairyGUIScriptingDefineSymbol || hasUGUIScriptingDefineSymbol))
            {
                /*// 显示对话框，提示用户选择所需的UI系统。
                var result = EditorUtility.DisplayDialog("没有检测到UI系统的宏定义存在", "请选择您所需要的UI系统,可以在菜单 GameFrameX/Scripting Define Symbols 中切换", "使用UGUI", "使用FairyGUI");

                // 根据用户的选择启用相应的UI系统宏定义。
                if (result)
                {
                    UISystemScriptingDefineSymbols.EnableUGUISystem();
                }
                else
                {
                    UISystemScriptingDefineSymbols.EnableFairyGUISystem();
                }*/

                // 显示对话框，提示用户选择所需的UI系统。
                EditorUtility.DisplayDialog("没有检测到UI系统的宏定义存在", "将自动启用FairyGUI的UI系统,可以在菜单 GameFrameX/Scripting Define Symbols 中切换", "我知道了");

                // 选择启用相应的UI系统宏定义。
                UISystemScriptingDefineSymbols.EnableFairyGUISystem();
            }
        }
    }
}