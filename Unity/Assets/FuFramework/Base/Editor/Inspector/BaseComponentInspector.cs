//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;
using GameFrameX.Runtime;
using UnityEditor;
using UnityEngine;

namespace GameFrameX.Editor
{
    /// <summary>
    /// Base组件Inspector
    /// </summary>
    [CustomEditor(typeof(BaseComponent))]
    internal sealed class BaseComponentInspector : GameFrameworkInspector
    {
        private static readonly float[]  s_GameSpeed           = { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };                     // 游戏速度数组
        private static readonly string[] s_GameSpeedForDisplay = { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" }; // 游戏速度显示名称数组

        private SerializedProperty m_TextHelperTypeName        = null; // 文本辅助器类型名称
        private SerializedProperty m_VersionHelperTypeName     = null; // 版本辅助器类型名称
        private SerializedProperty m_LogHelperTypeName         = null; // 日志辅助器类型名称
        private SerializedProperty m_CompressionHelperTypeName = null; // 压缩辅助器类型名称
        private SerializedProperty m_JsonHelperTypeName        = null; // JSON辅助器类型名称

        private SerializedProperty m_FrameRate       = null; // 帧率
        private SerializedProperty m_GameSpeed       = null; // 游戏速度
        private SerializedProperty m_RunInBackground = null; // 是否后台运行
        private SerializedProperty m_NeverSleep      = null; // 是否禁止休眠

        private string[] m_TextHelperTypeNames     = null; // 文本辅助器类型名称数组
        private int      m_TextHelperTypeNameIndex = 0;    // 文本辅助器类型名称索引

        private string[] m_VersionHelperTypeNames     = null; // 版本辅助器类型名称数组
        private int      m_VersionHelperTypeNameIndex = 0;    // 版本辅助器类型名称索引

        private string[] m_LogHelperTypeNames     = null; // 日志辅助器类型名称数组
        private int      m_LogHelperTypeNameIndex = 0;    // 日志辅助器类型名称索引

        private string[] m_CompressionHelperTypeNames     = null; // 压缩辅助器类型名称数组
        private int      m_CompressionHelperTypeNameIndex = 0;    // 压缩辅助器类型名称索引

        private string[] m_JsonHelperTypeNames     = null; // JSON辅助器类型名称数组
        private int      m_JsonHelperTypeNameIndex = 0;    // JSON辅助器类型名称索引

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            BaseComponent t = (BaseComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("全局辅助器设置：", EditorStyles.boldLabel);

                    // 文本辅助器
                    var textHelperSelectedIndex = EditorGUILayout.Popup("文本辅助器", m_TextHelperTypeNameIndex, m_TextHelperTypeNames);
                    if (textHelperSelectedIndex != m_TextHelperTypeNameIndex)
                    {
                        m_TextHelperTypeNameIndex        = textHelperSelectedIndex;
                        m_TextHelperTypeName.stringValue = textHelperSelectedIndex <= 0 ? null : m_TextHelperTypeNames[textHelperSelectedIndex];
                    }

                    // 版本号辅助器
                    var versionHelperSelectedIndex = EditorGUILayout.Popup("版本号辅助器", m_VersionHelperTypeNameIndex, m_VersionHelperTypeNames);
                    if (versionHelperSelectedIndex != m_VersionHelperTypeNameIndex)
                    {
                        m_VersionHelperTypeNameIndex        = versionHelperSelectedIndex;
                        m_VersionHelperTypeName.stringValue = versionHelperSelectedIndex <= 0 ? null : m_VersionHelperTypeNames[versionHelperSelectedIndex];
                    }

                    // 日志辅助器
                    var logHelperSelectedIndex = EditorGUILayout.Popup("日志辅助器", m_LogHelperTypeNameIndex, m_LogHelperTypeNames);
                    if (logHelperSelectedIndex != m_LogHelperTypeNameIndex)
                    {
                        m_LogHelperTypeNameIndex        = logHelperSelectedIndex;
                        m_LogHelperTypeName.stringValue = logHelperSelectedIndex <= 0 ? null : m_LogHelperTypeNames[logHelperSelectedIndex];
                    }

                    // 压缩辅助器
                    var compressionHelperSelectedIndex = EditorGUILayout.Popup("压缩辅助器", m_CompressionHelperTypeNameIndex, m_CompressionHelperTypeNames);
                    if (compressionHelperSelectedIndex != m_CompressionHelperTypeNameIndex)
                    {
                        m_CompressionHelperTypeNameIndex        = compressionHelperSelectedIndex;
                        m_CompressionHelperTypeName.stringValue = compressionHelperSelectedIndex <= 0 ? null : m_CompressionHelperTypeNames[compressionHelperSelectedIndex];
                    }

                    // JSON辅助器
                    var jsonHelperSelectedIndex = EditorGUILayout.Popup("JSON辅助器", m_JsonHelperTypeNameIndex, m_JsonHelperTypeNames);
                    if (jsonHelperSelectedIndex != m_JsonHelperTypeNameIndex)
                    {
                        m_JsonHelperTypeNameIndex        = jsonHelperSelectedIndex;
                        m_JsonHelperTypeName.stringValue = jsonHelperSelectedIndex <= 0 ? null : m_JsonHelperTypeNames[jsonHelperSelectedIndex];
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();

            // 帧率
            var frameRate = EditorGUILayout.IntSlider("帧率设置：", m_FrameRate.intValue, 1, 120);
            if (frameRate != m_FrameRate.intValue)
            {
                if (EditorApplication.isPlaying)
                    t.FrameRate = frameRate;
                else
                    m_FrameRate.intValue = frameRate;
            }

            // 游戏速度
            EditorGUILayout.BeginVertical("box");
            {
                var gameSpeed         = EditorGUILayout.Slider("游戏速度设置：", m_GameSpeed.floatValue, 0f, 8f);
                var selectedGameSpeed = GUILayout.SelectionGrid(GetSelectedGameSpeed(gameSpeed), s_GameSpeedForDisplay, 5);
                if (selectedGameSpeed >= 0)
                {
                    gameSpeed = GetGameSpeed(selectedGameSpeed);
                }

                if (!Mathf.Approximately(gameSpeed, m_GameSpeed.floatValue))
                {
                    if (EditorApplication.isPlaying)
                        t.GameSpeed = gameSpeed;
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
                    t.RunInBackground = runInBackground;
                else
                    m_RunInBackground.boolValue = runInBackground;
            }

            // 设置是否禁止休眠
            var neverSleep = EditorGUILayout.Toggle("是否禁止休眠", m_NeverSleep.boolValue);
            if (neverSleep != m_NeverSleep.boolValue)
            {
                if (EditorApplication.isPlaying)
                    t.NeverSleep = neverSleep;
                else
                    m_NeverSleep.boolValue = neverSleep;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            m_TextHelperTypeName        = serializedObject.FindProperty("m_TextHelperTypeName");
            m_VersionHelperTypeName     = serializedObject.FindProperty("m_VersionHelperTypeName");
            m_LogHelperTypeName         = serializedObject.FindProperty("m_LogHelperTypeName");
            m_CompressionHelperTypeName = serializedObject.FindProperty("m_CompressionHelperTypeName");
            m_JsonHelperTypeName        = serializedObject.FindProperty("m_JsonHelperTypeName");

            m_FrameRate       = serializedObject.FindProperty("m_FrameRate");
            m_GameSpeed       = serializedObject.FindProperty("m_GameSpeed");
            m_RunInBackground = serializedObject.FindProperty("m_RunInBackground");
            m_NeverSleep      = serializedObject.FindProperty("m_NeverSleep");

            RefreshTypeNames();
        }

        /// <summary>
        /// 刷新类型名称
        /// </summary>
        private void RefreshTypeNames()
        {
            // 文本辅助器
            var textHelperTypeNames = new List<string> { NoneOptionName };
            textHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Text.ITextHelper)));
            m_TextHelperTypeNames     = textHelperTypeNames.ToArray();
            m_TextHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_TextHelperTypeName.stringValue))
            {
                m_TextHelperTypeNameIndex = textHelperTypeNames.IndexOf(m_TextHelperTypeName.stringValue);
                if (m_TextHelperTypeNameIndex <= 0)
                {
                    m_TextHelperTypeNameIndex        = 0;
                    m_TextHelperTypeName.stringValue = null;
                }
            }

            // 版本号辅助器
            var versionHelperTypeNames = new List<string> { NoneOptionName };
            versionHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Version.IVersionHelper)));
            m_VersionHelperTypeNames     = versionHelperTypeNames.ToArray();
            m_VersionHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_VersionHelperTypeName.stringValue))
            {
                m_VersionHelperTypeNameIndex = versionHelperTypeNames.IndexOf(m_VersionHelperTypeName.stringValue);
                if (m_VersionHelperTypeNameIndex <= 0)
                {
                    m_VersionHelperTypeNameIndex        = 0;
                    m_VersionHelperTypeName.stringValue = null;
                }
            }

            // 日志辅助器
            var logHelperTypeNames = new List<string> { NoneOptionName };
            logHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(GameFrameworkLog.ILogHelper)));
            m_LogHelperTypeNames     = logHelperTypeNames.ToArray();
            m_LogHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_LogHelperTypeName.stringValue))
            {
                m_LogHelperTypeNameIndex = logHelperTypeNames.IndexOf(m_LogHelperTypeName.stringValue);
                if (m_LogHelperTypeNameIndex <= 0)
                {
                    m_LogHelperTypeNameIndex        = 0;
                    m_LogHelperTypeName.stringValue = null;
                }
            }

            // 压缩辅助器
            var compressionHelperTypeNames = new List<string> { NoneOptionName };
            compressionHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Compression.ICompressionHelper)));
            m_CompressionHelperTypeNames     = compressionHelperTypeNames.ToArray();
            m_CompressionHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_CompressionHelperTypeName.stringValue))
            {
                m_CompressionHelperTypeNameIndex = compressionHelperTypeNames.IndexOf(m_CompressionHelperTypeName.stringValue);
                if (m_CompressionHelperTypeNameIndex <= 0)
                {
                    m_CompressionHelperTypeNameIndex        = 0;
                    m_CompressionHelperTypeName.stringValue = null;
                }
            }

            // JSON辅助器
            var jsonHelperTypeNames = new List<string> { NoneOptionName };
            jsonHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Json.IJsonHelper)));
            m_JsonHelperTypeNames     = jsonHelperTypeNames.ToArray();
            m_JsonHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_JsonHelperTypeName.stringValue))
            {
                m_JsonHelperTypeNameIndex = jsonHelperTypeNames.IndexOf(m_JsonHelperTypeName.stringValue);
                if (m_JsonHelperTypeNameIndex <= 0)
                {
                    m_JsonHelperTypeNameIndex        = 0;
                    m_JsonHelperTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 获取游戏速度
        /// </summary>
        /// <param name="selectedGameSpeed"></param>
        /// <returns></returns>
        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0) return s_GameSpeed[0];
            return selectedGameSpeed >= s_GameSpeed.Length ? s_GameSpeed[s_GameSpeed.Length - 1] : s_GameSpeed[selectedGameSpeed];
        }

        /// <summary>
        /// 获取当前游戏速度的索引
        /// </summary>
        /// <param name="gameSpeed"></param>
        /// <returns></returns>
        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (var i = 0; i < s_GameSpeed.Length; i++)
            {
                if (Mathf.Approximately(gameSpeed, s_GameSpeed[i]))
                    return i;
            }

            return -1;
        }
    }
}