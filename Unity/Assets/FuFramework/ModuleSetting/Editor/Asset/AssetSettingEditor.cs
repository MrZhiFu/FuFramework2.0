#if UNITY_EDITOR
using YooAsset;
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 资源配置文件Inspector
    /// </summary>
    [CustomEditor(typeof(AssetSetting))]
    public class AssetSettingEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PlayModeProp;            // 资源运行模式
        private SerializedProperty m_DefaultPackagesNameProp; // 所有资源包列表属性
        private SerializedProperty m_DownloadingMaxNumProp;   // 资源下载最大并发数量
        private SerializedProperty m_FailedTryAgainNumProp;   // 资源下载失败重试次数

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_PlayModeProp            = serializedObject.FindProperty("m_PlayMode");
            m_DefaultPackagesNameProp = serializedObject.FindProperty("m_DefaultPackageName");
            m_DownloadingMaxNumProp   = serializedObject.FindProperty("m_DownloadingMaxNum");
            m_FailedTryAgainNumProp   = serializedObject.FindProperty("m_FailedTryAgainNum");
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var assetSetting = target as AssetSetting;
            if (!assetSetting) return;

            // 显示资源运行模式属性（使用枚举弹出菜单）
            EditorGUI.BeginChangeCheck();
            var playMode = (EPlayMode)m_PlayModeProp.enumValueIndex;
            playMode = (EPlayMode)EditorGUILayout.EnumPopup(new GUIContent("资源运行模式"), playMode);
            if (EditorGUI.EndChangeCheck()) 
                m_PlayModeProp.enumValueIndex = (int)playMode;
            
            EditorGUILayout.Space(10);
            
            // 显示默认资源包属性
            EditorGUI.BeginChangeCheck();
            var defaultPackageName = m_DefaultPackagesNameProp.stringValue;
            defaultPackageName = EditorGUILayout.TextField(new GUIContent("默认资源包名称"), defaultPackageName);
            if (EditorGUI.EndChangeCheck()) 
                m_DefaultPackagesNameProp.stringValue = defaultPackageName;

            EditorGUILayout.Space(10);
            
            // 显示资源下载最大并发数量属性
            EditorGUI.BeginChangeCheck();
            var downloadingMaxNum = m_DownloadingMaxNumProp.intValue;
            downloadingMaxNum = EditorGUILayout.IntField(new GUIContent("下载最大并发数量"), downloadingMaxNum);
            if (EditorGUI.EndChangeCheck()) 
                m_DownloadingMaxNumProp.intValue = downloadingMaxNum;

            EditorGUILayout.Space(10);
            
            // 显示资源下载失败重试次数属性
            EditorGUI.BeginChangeCheck();
            var failedTryAgainNum = m_FailedTryAgainNumProp.intValue;
            failedTryAgainNum = EditorGUILayout.IntField(new GUIContent("下载失败重试次数"), failedTryAgainNum);
            if (EditorGUI.EndChangeCheck()) 
                m_FailedTryAgainNumProp.intValue = failedTryAgainNum;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif