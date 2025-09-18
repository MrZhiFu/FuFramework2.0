using UnityEditor;
using UnityEngine;
using FuFramework.Core.Editor;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// Base组件Inspector
    /// </summary>
    [CustomEditor(typeof(Runtime.ModuleSetting))]
    internal sealed class ModuleSettingInspector : FuFrameworkInspector
    {
        /// 游戏速度数组
        private static readonly float[] m_SGameSpeed = { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };

        /// 游戏速度显示名称数组
        private static readonly string[] m_SGameSpeedForDisplay = { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty m_FrameRate;       // 帧率
        private SerializedProperty m_GameSpeed;       // 游戏速度
        private SerializedProperty m_RunInBackground; // 是否后台运行
        private SerializedProperty m_NeverSleep;      // 是否禁止休眠

        
        private SerializedProperty m_SoundSetting;   // 音频系统配置
        private SerializedProperty m_AssetSetting;   // 资源系统配置
        private SerializedProperty m_EntitySetting;  // 实体系统配置

        private void OnEnable()
        {
            m_FrameRate       = serializedObject.FindProperty("m_FrameRate");
            m_GameSpeed       = serializedObject.FindProperty("m_GameSpeed");
            m_RunInBackground = serializedObject.FindProperty("m_RunInBackground");
            m_NeverSleep      = serializedObject.FindProperty("m_NeverSleep");
            m_SoundSetting     = serializedObject.FindProperty("m_SoundSetting");
            m_AssetSetting     = serializedObject.FindProperty("m_AssetSetting");    
            m_EntitySetting    = serializedObject.FindProperty("m_EntitySetting");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var moduleSetting = target as Runtime.ModuleSetting;
            if (!moduleSetting) return;

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
                var selectedGameSpeed = GUILayout.SelectionGrid(GetSelectedGameSpeed(gameSpeed), m_SGameSpeedForDisplay, 5);
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

            // 框架模块配置
            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(m_SoundSetting);
            EditorGUILayout.PropertyField(m_AssetSetting);
            EditorGUILayout.PropertyField(m_EntitySetting);
        
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 获取游戏速度
        /// </summary>
        /// <param name="selectedGameSpeed"></param>
        /// <returns></returns>
        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0) return m_SGameSpeed[0];
            return selectedGameSpeed >= m_SGameSpeed.Length ? m_SGameSpeed[m_SGameSpeed.Length - 1] : m_SGameSpeed[selectedGameSpeed];
        }

        /// <summary>
        /// 获取当前游戏速度的索引
        /// </summary>
        /// <param name="gameSpeed"></param>
        /// <returns></returns>
        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (var i = 0; i < m_SGameSpeed.Length; i++)
            {
                if (Mathf.Approximately(gameSpeed, m_SGameSpeed[i]))
                    return i;
            }

            return -1;
        }
    }
}