using System.Diagnostics;
using FuFramework.Core.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 打开文件夹相关的实用函数。
    /// </summary>
    public static class OpenFolder
    {
        /// <summary>
        /// 打开 Data Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Data Path文件夹", false, 10)]
        public static void OpenFolderDataPath() => Execute(Application.dataPath);

        /// <summary>
        /// 打开 Persistent Data Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Persistent Data Path文件夹", false, 11)]
        public static void OpenFolderPersistentDataPath() => Execute(Application.persistentDataPath);

        /// <summary>
        /// 打开 Streaming Assets Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Streaming Assets Path文件夹", false, 12)]
        public static void OpenFolderStreamingAssetsPath() => Execute(Application.streamingAssetsPath);

        /// <summary>
        /// 打开 Temporary Cache Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Temporary Cache Path文件夹", false, 13)]
        public static void OpenFolderTemporaryCachePath() => Execute(Application.temporaryCachePath);

        /// <summary>
        /// 打开 Console Log Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Console Log Path文件夹", false, 14)]
        public static void OpenFolderConsoleLogPath() => Execute(System.IO.Path.GetDirectoryName(Application.consoleLogPath));

        /// <summary>
        /// 打开指定路径的文件夹。
        /// </summary>
        /// <param name="folder">要打开的文件夹的路径。</param>
        public static void Execute(string folder)
        {
            folder = $"\"{folder}\"";
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Process.Start("Explorer.exe", folder.Replace('/', '\\'));
                    break;

                case RuntimePlatform.OSXEditor:
                    Process.Start("open", folder);
                    break;

                default:
                    throw new FuException($"在 '{Application.platform}' 平台不支持打开文件夹.");
            }
        }
    }
}