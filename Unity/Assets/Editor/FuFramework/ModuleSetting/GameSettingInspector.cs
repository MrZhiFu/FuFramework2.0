using FuFramework.Core.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor
{
    /// <summary>
    /// 模块配置Inspector。
    /// </summary>
    [CustomEditor(typeof(Runtime.GameSetting))]
    internal sealed class GameSettingInspector : FuFrameworkInspector
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


        private SerializedProperty m_PlayMode;
        private SerializedProperty m_DefaultPackageName;
        private SerializedProperty m_DownloadingMaxNum;
        private SerializedProperty m_FailedTryAgainNum;
        private SerializedProperty m_AsyncSystemMaxSlicePerFrame;
        private SerializedProperty m_ResCdnRootURL;
        private SerializedProperty m_EnableAutoSave;
        private SerializedProperty m_AutoSaveInterval;
        private SerializedProperty m_EnableEncrypt;
        private SerializedProperty m_EncryptKey;

        private void OnEnable()
        {
            m_FrameRate                 = serializedObject.FindProperty("m_FrameRate");
            m_GameSpeed                 = serializedObject.FindProperty("m_GameSpeed");
            m_RunInBackground           = serializedObject.FindProperty("m_RunInBackground");
            m_NeverSleep                = serializedObject.FindProperty("m_NeverSleep");
            m_OpenGuide                 = serializedObject.FindProperty("m_OpenGuide");
            m_PlayMode                  = serializedObject.FindProperty("m_PlayMode");
            m_DefaultPackageName        = serializedObject.FindProperty("m_DefaultPackageName");
            m_DownloadingMaxNum         = serializedObject.FindProperty("m_DownloadingMaxNum");
            m_FailedTryAgainNum         = serializedObject.FindProperty("m_FailedTryAgainNum");
            m_AsyncSystemMaxSlicePerFrame = serializedObject.FindProperty("m_AsyncSystemMaxSlicePerFrame");
            m_ResCdnRootURL             = serializedObject.FindProperty("m_ResCdnRootURL");
            m_EnableAutoSave            = serializedObject.FindProperty("m_EnableAutoSave");
            m_AutoSaveInterval          = serializedObject.FindProperty("m_AutoSaveInterval");
            m_EnableEncrypt             = serializedObject.FindProperty("m_EnableEncrypt");
            m_EncryptKey                = serializedObject.FindProperty("m_EncryptKey");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (target is not Runtime.GameSetting gameSetting) return;

            // 帧率
            var frameRate = EditorGUILayout.IntSlider("帧率设置：", m_FrameRate.intValue, 1, 120);
            if (frameRate != m_FrameRate.intValue)
            {
                if (EditorApplication.isPlaying)
                    gameSetting.FrameRate = frameRate;
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
                        gameSetting.GameSpeed = gameSpeed;
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
                    gameSetting.RunInBackground = runInBackground;
                else
                    m_RunInBackground.boolValue = runInBackground;
            }

            // 设置是否禁止休眠
            var neverSleep = EditorGUILayout.Toggle("是否禁止休眠", m_NeverSleep.boolValue);
            if (neverSleep != m_NeverSleep.boolValue)
            {
                if (EditorApplication.isPlaying)
                    gameSetting.NeverSleep = neverSleep;
                else
                    m_NeverSleep.boolValue = neverSleep;
            }

            // 设置是否开启引导
            var openGuide = EditorGUILayout.Toggle("是否开启引导", m_OpenGuide.boolValue);
            if (openGuide != m_OpenGuide.boolValue)
            {
                if (EditorApplication.isPlaying)
                    gameSetting.OpenGuide = openGuide;
                else
                    m_OpenGuide.boolValue = openGuide;
            }

            // 资源系统配置
            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(m_PlayMode);
            EditorGUILayout.PropertyField(m_DefaultPackageName);
            EditorGUILayout.PropertyField(m_DownloadingMaxNum);
            EditorGUILayout.PropertyField(m_FailedTryAgainNum);
            EditorGUILayout.PropertyField(m_AsyncSystemMaxSlicePerFrame);
            EditorGUILayout.PropertyField(m_ResCdnRootURL);

            // 本地数据存储系统配置
            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(m_EnableAutoSave);
            EditorGUILayout.PropertyField(m_AutoSaveInterval);
            EditorGUILayout.PropertyField(m_EnableEncrypt);
            EditorGUILayout.PropertyField(m_EncryptKey);

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