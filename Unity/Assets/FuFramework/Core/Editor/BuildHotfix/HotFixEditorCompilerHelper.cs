using FuFramework.Core.Editor;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace GameFrameX.Editor
{
    /// <summary>
    /// 热更新程序集编译选项帮助类
    /// </summary>
    public static class HotFixEditorCompilerHelper
    {
        /// <summary>
        /// 移除热更新程序集的编辑器编译指令
        /// </summary>
        [MenuItem("GameFrameX/Build/HotFix Editor Compiler Remove(标记HotFix.asmdef程序集在Editor环境下也可使用)", false, 15)]
        public static void RemoveEditorInExcludePlatforms()
        {
            const string path = "Assets/Scripts/Hotfix/Unity.HotFix.asmdef";
            HotFixAssemblyDefinitionHelper.RemoveEditorInExcludePlatforms(path);
        }

        /// <summary>
        /// 增加热更新程序集的编辑器编译指令
        /// </summary>
        [MenuItem("GameFrameX/Build/HotFix Editor Compiler Add(标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用)", false, 16)]
        public static void AddEditorInExcludePlatforms()
        {
            const string path = "Assets/Scripts/Hotfix/Unity.HotFix.asmdef";
            HotFixAssemblyDefinitionHelper.AddEditorInExcludePlatforms(path);
        }
    }
}