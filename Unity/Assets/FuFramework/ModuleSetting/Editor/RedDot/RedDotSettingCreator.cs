#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 红点模块配置-RedDotSetting 创建器
    /// </summary>
    public static class RedDotSettingCreator
    {
        private const string AssetName = "RedDotSetting.asset";                          // 资源名称
        private const string AssetPath = "Assets/FuFramework/ModuleSetting/SettingAssets"; // 配置路径

        private static readonly string FullPath = $"{AssetPath}/{AssetName}"; // 完整路径

        [MenuItem("FuFramework/框架模块配置/红点模块配置/创建", false, 7)]
        public static void CreateDefaultAssetSetting()
        {
            // 检查资源是否已存在
            var existingSetting = AssetDatabase.LoadAssetAtPath<RedDotSetting>(FullPath);
            if (existingSetting)
            {
                EditorUtility.DisplayDialog("创建失败", "RedDotSetting 资源已存在, 请勿重复创建!", "确定");
                Selection.activeObject = existingSetting;
                EditorUtility.FocusProjectWindow();
                return;
            }

            // 确保目录存在
            EnsureDirectoryExists(AssetPath);

            var assetSetting = ScriptableObject.CreateInstance<RedDotSetting>();
            AssetDatabase.CreateAsset(assetSetting, FullPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = assetSetting;

            Debug.Log($"已创建默认资源配置: {FullPath}");
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        /// <param name="path">目录路径</param>
        private static void EnsureDirectoryExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            // 使用 System.IO 创建物理目录，然后导入到 AssetDatabase
            var physicalPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
            Directory.CreateDirectory(physicalPath);
            AssetDatabase.Refresh();
        }

        [MenuItem("FuFramework/框架模块配置/红点模块配置/查找", false, 8)]
        public static void FindAssetSetting()
        {
            var guids = AssetDatabase.FindAssets("t:RedDotSetting");
            if (guids.Length > 0)
            {
                var path         = AssetDatabase.GUIDToAssetPath(guids[0]);
                var assetSetting = AssetDatabase.LoadAssetAtPath<RedDotSetting>(path);
                Selection.activeObject = assetSetting;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(assetSetting); // 高亮显示资源
            }
            else
            {
                var createNew = EditorUtility.DisplayDialog("未找到资源", "未找到任何 RedDotSetting 资源，是否创建新的？", "创建", "取消");
                if (createNew) CreateDefaultAssetSetting();
            }
        }
    }
}
#endif