using FuFramework.Core.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor
{
    /// <summary>
    /// 模块配置Inspector。
    /// </summary>
    [CustomEditor(typeof(Runtime.ModuleSetting))]
    internal sealed class ModuleSettingInspector : FuFrameworkInspector
    {
        /// 游戏速度数组
        private static readonly float[] GameSpeed = { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };

        /// 游戏速度显示名称数组
        private static readonly string[] GameSpeedForDisplay = { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty m_FrameRate;       // 帧率
        private SerializedProperty m_GameSpeed;       // 游戏速度
        private SerializedProperty m_RunInBackground; // 是否后台运行
        private SerializedProperty m_NeverSleep;      // 是否禁止休眠
        private SerializedProperty m_OpenGuide;       // 是否开启引导


        private SerializedProperty m_AssetSetting;   // 资源管理模块配置
        private SerializedProperty m_StorageSetting; // 本地数据存储模块配置
        private SerializedProperty m_GuideSetting;   // 引导模块配置

        private void OnEnable()
        {
            m_FrameRate       = serializedObject.FindProperty("m_FrameRate");
            m_GameSpeed       = serializedObject.FindProperty("m_GameSpeed");
            m_RunInBackground = serializedObject.FindProperty("m_RunInBackground");
            m_NeverSleep      = serializedObject.FindProperty("m_NeverSleep");
            m_OpenGuide       = serializedObject.FindProperty("m_OpenGuide");
            m_AssetSetting    = serializedObject.FindProperty("m_AssetSetting");
            m_StorageSetting  = serializedObject.FindProperty("m_StorageSetting");
            m_GuideSetting    = serializedObject.FindProperty("m_GuideSetting");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (target is not Runtime.ModuleSetting moduleSetting) return;

            // 帧率
            var frameRate = EditorGUILayout.IntSlider("帧率设置：", m_FrameRate.intValue, 1, 120);
            if (frameRate != m_FrameRate.intValue)
            {
                if (EditorApplication.isPlaying)
                    moduleSetting.FrameRate = frameRate;
                else
                    m_FrameRate.intValue = frameRate;
            }

            // 游戏速度
            EditorGUILayout.BeginVertical("box");
            {
                var gameSpeed         = EditorGUILayout.Slider("游戏速度设置：", m_GameSpeed.floatValue, 0f, 8f);
                var selectedGameSpeed = GUILayout.SelectionGrid(GetSelectedGameSpeed(gameSpeed), GameSpeedForDisplay, 5);
                if (selectedGameSpeed >= 0)
                {
                    gameSpeed = GetGameSpeed(selectedGameSpeed);
                }

                if (!Mathf.Approximately(gameSpeed, m_GameSpeed.floatValue))
                {
                    if (EditorApplication.isPlaying)
                        moduleSetting.GameSpeed = gameSpeed;
                    else
                        m_GameSpeed.floatValue = gameSpeed;
                }
            }
            EditorGUILayout.EndVertical();

            // 设置是否后台运行
            var runInBackground = EditorGUILayout.Toggle("是否可在后台运行", m_RunInBackground.boolValue);
            if (runInBackground != m_RunInBackground.boolValue)
            {
                if (EditorApplication.isPlaying)
                    moduleSetting.RunInBackground = runInBackground;
                else
                    m_RunInBackground.boolValue = runInBackground;
            }

            // 设置是否禁止休眠
            var neverSleep = EditorGUILayout.Toggle("是否禁止休眠", m_NeverSleep.boolValue);
            if (neverSleep != m_NeverSleep.boolValue)
            {
                if (EditorApplication.isPlaying)
                    moduleSetting.NeverSleep = neverSleep;
                else
                    m_NeverSleep.boolValue = neverSleep;
            }

            // 设置是否开启引导
            var openGuide = EditorGUILayout.Toggle("是否开启引导", m_OpenGuide.boolValue);
            if (openGuide != m_OpenGuide.boolValue)
            {
                if (EditorApplication.isPlaying)
                    moduleSetting.OpenGuide = openGuide;
                else
                    m_OpenGuide.boolValue = openGuide;
            }

            // 框架模块配置
            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(m_AssetSetting);
            EditorGUILayout.PropertyField(m_StorageSetting);
            EditorGUILayout.PropertyField(m_GuideSetting);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 获取游戏速度
        /// </summary>
        /// <param name="selectedGameSpeed"></param>
        /// <returns></returns>
        private static float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0) return GameSpeed[0];
            return selectedGameSpeed >= GameSpeed.Length ? GameSpeed[^1] : GameSpeed[selectedGameSpeed];
        }

        /// <summary>
        /// 获取当前游戏速度的索引
        /// </summary>
        /// <param name="gameSpeed"></param>
        /// <returns></returns>
        private static int GetSelectedGameSpeed(float gameSpeed)
        {
            for (var i = 0; i < GameSpeed.Length; i++)
            {
                if (Mathf.Approximately(gameSpeed, GameSpeed[i]))
                    return i;
            }

            return -1;
        }
    }
}