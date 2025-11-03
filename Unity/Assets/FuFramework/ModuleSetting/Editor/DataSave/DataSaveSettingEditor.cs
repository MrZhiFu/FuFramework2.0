#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 本地数据存储配置文件Inspector
    /// </summary>
    [CustomEditor(typeof(DataSaveSetting))]
    public class DataSaveSettingEditor : UnityEditor.Editor
    {
        private SerializedProperty m_EnableAutoSave;   // 自动保存开关
        private SerializedProperty m_AutoSaveInterval; // 自动保存间隔

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_EnableAutoSave   = serializedObject.FindProperty("m_EnableAutoSave");
            m_AutoSaveInterval = serializedObject.FindProperty("m_AutoSaveInterval");
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var dataSaveSetting = target as DataSaveSetting;
            if (!dataSaveSetting) return;
            
            // 自动保存开关
            EditorGUI.BeginChangeCheck();
            var isAutoSave = m_EnableAutoSave.boolValue;
            isAutoSave = EditorGUILayout.Toggle("是否自动保存", isAutoSave);
            if (EditorGUI.EndChangeCheck())
            {
                m_EnableAutoSave.boolValue = isAutoSave;
            }
            
            EditorGUILayout.Space(20);
            
            // 自动保存间隔
            EditorGUI.BeginChangeCheck();
            var interval = m_AutoSaveInterval.floatValue;
            interval = EditorGUILayout.FloatField("自动保存间隔(秒)", interval);
            if (EditorGUI.EndChangeCheck())
            {
                m_AutoSaveInterval.floatValue = interval;
            }
            
            EditorGUILayout.Space(20);
            
            // 重置配置
            if (GUILayout.Button("重置配置")) 
                dataSaveSetting.Reset();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif