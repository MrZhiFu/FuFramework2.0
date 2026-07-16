using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 打开文件夹工具。
    /// 功能：
    ///     1. 打开 Data Path 文件夹。
    ///     2. 打开 Persistent Data Path 文件夹。
    ///     3. 打开 Streaming Assets Path 文件夹。
    ///     4. 打开 Temporary Cache Path 文件夹。
    ///     5. 打开 Console Log Path 文件夹。
    /// </summary>
    public static class OpenFolder
    {
        /// <summary>
        /// 打开 Data Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Data Path文件夹", false, 500)]
        public static void OpenFolderDataPath() => Open(Application.dataPath);

        /// <summary>
        /// 打开 Persistent Data Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Persistent Data Path文件夹", false, 501)]
        public static void OpenFolderPersistentDataPath() => Open(Application.persistentDataPath);

        /// <summary>
        /// 打开 Streaming Assets Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Streaming Assets Path文件夹", false, 502)]
        public static void OpenFolderStreamingAssetsPath() => Open(Application.streamingAssetsPath);

        /// <summary>
        /// 打开 Temporary Cache Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Temporary Cache Path文件夹", false, 503)]
        public static void OpenFolderTemporaryCachePath() => Open(Application.temporaryCachePath);

        /// <summary>
        /// 打开 Console Log Path 文件夹。
        /// </summary>
        [MenuItem("FuFramework/打开文件夹/打开Console Log Path文件夹", false, 504)]
        public static void OpenFolderConsoleLogPath() => Open(System.IO.Path.GetDirectoryName(Application.consoleLogPath));

        /// <summary>
        /// 打开指定路径的文件夹。
        /// </summary>
        /// <param name="folder">要打开的文件夹的路径。</param>
        private static void Open(string folder)
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
                    throw new InvalidOperationException($"在 '{Application.platform}' 平台不支持打开文件夹.");
            }
        }
    }
}