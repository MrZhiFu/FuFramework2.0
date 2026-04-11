using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 热更新编辑器帮助类。
    /// 功能：
    ///     1. 复制热更新代码DLL到Assets/Bundles/Code目录。
    ///     2. 复制AOT代码DLL到Assets/Bundles/AOTCode目录。
    /// </summary>
    [InitializeOnLoad]
    public static class BuildHotfixHelper
    {
        // Unity代码生成dll位置
        private const string HotFixAssembliesDir = "Library/ScriptAssemblies";

        // 热更DLL名称数组
        private static readonly string[] HotfixDlls = { "Game.Hotfix.dll" };

        // 热更代码存放位置
        private const string CodeDir    = "Assets/Bundles/Code/";
        private const string AOTCodeDir = "Assets/Bundles/AOTCode/";


        /// <summary>
        /// 每次Unity编译完毕后，等待一秒后执行热更新代码拷贝
        /// </summary>
        static BuildHotfixHelper()
        {
            async Task WaitExecute()
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                CopyHotfixCode(); // 拷贝热更代码到Assets/Bundles/Code目录
            }

            _ = WaitExecute();
        }

        /// <summary>
        /// 复制热更新代码Dll到Assets/Bundles/Code目录
        /// </summary>
        [MenuItem("FuFramework/Build/Copy Hotfix Code(复制热更新代码DLL到Assets>Bundles>Code)", false, 300)]
        public static void CopyHotfixCode()
        {
            if (!Directory.Exists(CodeDir))
            {
                Directory.CreateDirectory(CodeDir);
            }

            foreach (var hotfix in HotfixDlls)
            {
                // 源DLL相对路径，相对于Unity工程根目录。Unity编辑器运行时，当前工作目录自动设置为项目根目录。
                var srcRelativePath = Path.Combine(HotFixAssembliesDir, hotfix);
                File.Copy(srcRelativePath, Path.Combine(CodeDir,        $"{hotfix}.bytes"), true);
                Debug.Log($"复制热更代码DLL--{srcRelativePath}到{CodeDir}完成");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 复制AOT代码DLL到Assets/Bundles/AOTCode目录。
        /// "AssembliesPostIl2CppStrip": IL2CPP裁剪后的AOT程序集目录
        /// </summary>
        [MenuItem("FuFramework/Build/Copy AOT Code(复制AOT代码DLL到Assets>Bundles>AOTCode)", false, 301)]
        public static void CopyAOTCode()
        {
            if (!Directory.Exists(AOTCodeDir))
            {
                Directory.CreateDirectory(AOTCodeDir);
            }

            var directoryInfo = new DirectoryInfo(Application.dataPath);
            if (directoryInfo.Parent != null)
            {
                var path = Path.Combine(directoryInfo.Parent.FullName, "HybridCLRData", "AssembliesPostIl2CppStrip", EditorUserBuildSettings.activeBuildTarget.ToString());

                var aotCodeDir    = new DirectoryInfo(path);
                var files         = aotCodeDir.GetFiles("*.dll");
                var stringBuilder = new StringBuilder();
                foreach (var fileInfo in files)
                {
                    stringBuilder.AppendLine(fileInfo.Name);
                    fileInfo.CopyTo(AOTCodeDir + "/" + $"{fileInfo.Name}.bytes", true);
                }

                Debug.Log(stringBuilder);
            }

            Debug.Log($"复制AOT DLL到{CodeDir}完成");
            AssetDatabase.Refresh();
        }
    }
}