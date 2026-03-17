using System.IO;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 删除本地游戏数据
    /// </summary>
    public static class DeleteGameData
    {
        /// <summary>
        /// 删除本地游戏数据。
        /// </summary>
        [MenuItem("FuFramework/删除本地游戏数据", false, 1200)]
        public static void OpenFolderTemporaryCachePath()
        {
            var dataPath = Path.Combine(Application.persistentDataPath, "GameData");
    
            if (Directory.Exists(dataPath))
            {
                if (FileUtil.DeleteFileOrDirectory(dataPath))
                {
                    Debug.Log($"成功删除游戏数据: {dataPath}");
                }
                else
                {
                    Debug.LogError($"删除失败，请手动删除目录: {dataPath}");
                    EditorUtility.RevealInFinder(dataPath);
                }
            }
            else
            {
                Debug.Log($"游戏数据目录不存在: {dataPath}");
            }
    
            AssetDatabase.Refresh();
        }
    }
}