using UnityEditor;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 热更新程序集的编辑器编译指令帮助类
    /// </summary>
    public static class HotFixEditorCompilerHelper
    {
        /// <summary>
        /// 移除热更新程序集的编辑器编译指令
        /// </summary>
        [MenuItem("GameFrameX/Build/HotFix Editor Compiler Remove(移除热更新程序集的目标平台下的Editor平台)", false, 15)]
        public static void RemoveEditor()
        {
            const string path = "Assets/Scripts/Hotfix/Unity.HotFix.asmdef";
            HotFixAssemblyDefinitionHelper.RemoveEditor(path);
        }

        /// <summary>
        /// 增加热更新程序集的编辑器编译指令
        /// </summary>
        [MenuItem("GameFrameX/Build/HotFix Editor Compiler Add(增加热更新程序集的目标平台下的Editor平台)", false, 16)]
        public static void AddEditor()
        {
            const string path = "Assets/Scripts/Hotfix/Unity.HotFix.asmdef";
            HotFixAssemblyDefinitionHelper.AddEditor(path);
        }
    }
}