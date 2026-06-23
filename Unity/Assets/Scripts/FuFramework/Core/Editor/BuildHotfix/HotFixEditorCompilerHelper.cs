using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 热更新程序集编译选项帮助类。
    /// 功能：
    ///     1. 移除热更新程序集的编辑器编译指令。
    ///     2. 增加热更新程序集的编辑器编译指令。
    /// </summary>
    public static class HotFixEditorCompilerHelper
    {
        /// <summary>
        /// 移除热更新程序集的编辑器编译指令
        /// </summary>
        [MenuItem("FuFramework/Build/标记HotFix.asmdef程序集在Editor环境下也可使用", false, 400)]
        public static void RemoveEditorInExcludePlatforms()
        {
            const string path = "Assets/Scripts/Hotfix/Game.Hotfix.asmdef";
            HotFixAssemblyDefinitionHelper.RemoveEditorInExcludePlatforms(path);
        }

        /// <summary>
        /// 增加热更新程序集的编辑器编译指令
        /// </summary>
        [MenuItem("FuFramework/Build/标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用", false, 401)]
        public static void AddEditorInExcludePlatforms()
        {
            const string path = "Assets/Scripts/Hotfix/Game.Hotfix.asmdef";
            HotFixAssemblyDefinitionHelper.AddEditorInExcludePlatforms(path);
        }
    }
}