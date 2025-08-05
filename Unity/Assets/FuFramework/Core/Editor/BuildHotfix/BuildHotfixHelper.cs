using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GameFrameX.Runtime;
using UnityEditor;
using UnityEngine;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 热更新编辑器
    /// </summary>
    [InitializeOnLoad]
    public static class BuildHotfixHelper
    {
        // Unity代码生成dll位置
        private const           string HotFixAssembliesDir = "Library/ScriptAssemblies";
        private static readonly string ScriptAssembliesDir = $"HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}";

        // 热更DLL名称
        private static readonly string[] HotfixDlls = { "Unity.Hotfix.dll" };

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
                CopyHotfixCode();// 拷贝热更代码到Assets/Bundles/Code目录
            }

            _ = WaitExecute();
        }

        /// <summary>
        /// 复制热更新代码
        /// </summary>
        [MenuItem("GameFrameX/Build/Copy Hotfix Code(复制热更新代码到Assets>Bundles>Code)", false, 10)]
        public static void CopyHotfixCode()
        {
            if (!Directory.Exists(CodeDir))
            {
                Directory.CreateDirectory(CodeDir);
            }

            foreach (var hotfix in HotfixDlls)
            {
                var srcPath = Path.Combine(HotFixAssembliesDir, hotfix);
                File.Copy(srcPath, Path.Combine(CodeDir, hotfix + Utility.Const.FileNameSuffix.Binary), true);
            }

            Debug.Log($"复制Hotfix DLL到{CodeDir}完成");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 复制AOT代码
        /// </summary>
        [MenuItem("GameFrameX/Build/Copy AOT Code(复制AOT代码到Assets>Bundles>AOTCode)", false, 11)]
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
                    fileInfo.CopyTo(AOTCodeDir + "/" + fileInfo.Name + Utility.Const.FileNameSuffix.Binary, true);
                }

                Debug.Log(stringBuilder);
            }

            Debug.Log($"复制AOT DLL到{CodeDir}完成");
            AssetDatabase.Refresh();
        }
    }
}