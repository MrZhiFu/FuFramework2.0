#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 声音配置-SoundSetting 创建器
    /// </summary>
    public static class SoundSettingCreator
    {
        private const string AssetName = "SoundSetting.asset";                             // 资源名称
        private const string AssetPath = "Assets/FuFramework/ModuleSetting/SettingAssets"; // 配置路径

        private static readonly string FullPath = $"{AssetPath}/{AssetName}"; // 完整路径

        [MenuItem("FuFramework/框架模块配置/音频配置/创建")]
        public static void CreateDefaultSoundSetting()
        {
            // 检查资源是否已存在
            var existingSetting = AssetDatabase.LoadAssetAtPath<SoundSetting>(FullPath);
            if (existingSetting != null)
            {
                EditorUtility.DisplayDialog("创建失败", "SoundSetting 资源已存在, 请勿重复创建!", "确定");
                Selection.activeObject = existingSetting;
                EditorUtility.FocusProjectWindow();
                return;
            }

            // 确保目录存在
            EnsureDirectoryExists(AssetPath);

            var soundSetting = ScriptableObject.CreateInstance<SoundSetting>();
            soundSetting.AddDefaultSoundGroups(); // 添加默认声音组 "BGM", "SFX", "UI"
            AssetDatabase.CreateAsset(soundSetting, FullPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = soundSetting;

            Debug.Log($"已创建默认声音设置: {FullPath}");
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

        [MenuItem("FuFramework/框架模块配置/音频配置/查找")]
        public static void FindSoundSetting()
        {
            var guids = AssetDatabase.FindAssets("t:SoundSetting");
            if (guids.Length > 0)
            {
                var path         = AssetDatabase.GUIDToAssetPath(guids[0]);
                var soundSetting = AssetDatabase.LoadAssetAtPath<SoundSetting>(path);
                Selection.activeObject = soundSetting;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(soundSetting); // 高亮显示资源
            }
            else
            {
                var createNew = EditorUtility.DisplayDialog("未找到资源", "未找到任何 SoundSetting 资源，是否创建新的？", "创建", "取消");
                if (createNew) CreateDefaultSoundSetting();
            }
        }
    }
}
#endif