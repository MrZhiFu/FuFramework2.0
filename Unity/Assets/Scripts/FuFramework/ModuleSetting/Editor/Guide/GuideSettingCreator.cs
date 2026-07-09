#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 引导配置-GuideSetting 创建器
    /// </summary>
    public static class GuideSettingCreator
    {
        private const string AssetName = "GuideSetting.asset";                             // 资源名称
        private const string AssetPath = "Assets/FuFramework/ModuleSetting/SettingAssets"; // 配置路径

        private static readonly string FullPath = $"{AssetPath}/{AssetName}"; // 完整路径


        [MenuItem("FuFramework/框架模块配置/引导配置/创建", false, 5)]
        public static void CreateDefaultGuideSetting()
        {
            // 检查资源是否已存在，如果存在则提示用户并选中聚焦到资源
            var existingSetting = AssetDatabase.LoadAssetAtPath<GuideSetting>(FullPath);
            if (existingSetting)
            {
                EditorUtility.DisplayDialog("创建失败", "GuideSetting 资源已存在, 请勿重复创建!", "确定");
                Selection.activeObject = existingSetting;
                EditorUtility.FocusProjectWindow();
                return;
            }

            // 确保目录存在
            EnsureDirectoryExists(AssetPath);

            // 创建资源实例对象
            var guideSetting = ScriptableObject.CreateInstance<GuideSetting>();

            // 添加默认引导
            AddDefaultGuides(guideSetting);

            // 创建并保存资源
            AssetDatabase.CreateAsset(guideSetting, FullPath);
            AssetDatabase.SaveAssets();

            // 选中资源并聚焦到资源
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = guideSetting;

            Debug.Log($"已创建默认引导设置: {FullPath}");

            // 验证配置
            var setting = AssetDatabase.LoadAssetAtPath<GuideSetting>(FullPath);
            if (setting != null)
            {
                if (setting.Validate(out var errors))
                {
                    Debug.Log("引导配置验证通过！");
                }
                else
                {
                    Debug.LogWarning($"引导配置验证失败，错误数量：{errors.Count}");
                    foreach (var error in errors)
                    {
                        Debug.LogWarning($"  - {error}");
                    }
                }
            }
        }

        [MenuItem("FuFramework/框架模块配置/引导配置/查找", false, 6)]
        public static void FindGuideSetting()
        {
            var guids = AssetDatabase.FindAssets("t:GuideSetting");
            if (guids.Length > 0)
            {
                var path         = AssetDatabase.GUIDToAssetPath(guids[0]);
                var guideSetting = AssetDatabase.LoadAssetAtPath<GuideSetting>(path);
                Selection.activeObject = guideSetting;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(guideSetting); // 高亮显示资源
            }
            else
            {
                var createNew = EditorUtility.DisplayDialog("未找到资源", "未找到任何 GuideSetting 资源，是否创建新的？", "创建", "取消");
                if (createNew) CreateDefaultGuideSetting();
            }
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

        /// <summary>
        /// 添加默认引导配置
        /// </summary>
        private static void AddDefaultGuides(GuideSetting setting)
        {
            // 示例1：新手引导
            var newbieGuide = setting.CreateGuide("引导一");
            if (newbieGuide != null)
            {
                // 步骤1：欢迎对话
                var step1 = setting.CreateStep(newbieGuide.m_GuideId, "欢迎对话", EStepType.Dialog);
                step1.m_DialogContent = "欢迎来到游戏世界！让我来引导你熟悉基本操作。";
                step1.m_NextStepId    = "Step_点击开始按钮";

                // 步骤2：点击开始按钮
                var step2 = setting.CreateStep(newbieGuide.m_GuideId, "点击开始按钮", EStepType.ClickUI);
                step2.m_TargetWindow = "WinMain";
                step2.m_TargetUI     = "btn_start";
                step2.m_NextStepId   = "Step_等待2秒";

                // 步骤3：等待2秒
                var step3 = setting.CreateStep(newbieGuide.m_GuideId, "等待2秒", EStepType.Wait);
                step3.m_IsCanJump = false;
                step3.m_WaitTime  = 2f;

                newbieGuide.m_StartStepId = step1.m_StepId;
            }
        }
    }
}
#endif