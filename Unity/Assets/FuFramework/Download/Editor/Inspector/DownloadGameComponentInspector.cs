using UnityEditor;
using UnityEngine;
using System.Globalization;
using FuFramework.Core.Editor;
using FuFramework.Download.Runtime;
using FuFramework.TaskPool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Editor
{
    /// <summary>
    /// 自定义下载组件的Inspector
    /// </summary>
    [CustomEditor(typeof(DownloadManager))]
    internal sealed class DownloadGameComponentInspector : FuFrameworkInspector
    {
        private SerializedProperty m_InstanceRoot;
        private SerializedProperty m_DownloadAgentHelperCount;
        private SerializedProperty m_Timeout;
        private SerializedProperty m_FlushSize;

        private void OnEnable()
        {
            m_InstanceRoot             = serializedObject.FindProperty("m_InstanceRoot");
            m_DownloadAgentHelperCount = serializedObject.FindProperty("m_DownloadAgentHelperCount");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var downloadManager = target as DownloadManager;
            if (!downloadManager) return;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_InstanceRoot);
                m_DownloadAgentHelperCount.intValue = EditorGUILayout.IntSlider("下载代理辅助器个数：", m_DownloadAgentHelperCount.intValue, 1, 16);
            }
            EditorGUI.EndDisabledGroup();


            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("是否暂停",              downloadManager.Paused.ToString());
                EditorGUILayout.LabelField("下载总代理个数",   downloadManager.TotalAgentCount.ToString());
                EditorGUILayout.LabelField("空闲的下载代理个数",    downloadManager.FreeAgentCount.ToString());
                EditorGUILayout.LabelField("工作中的下载代理个数", downloadManager.WorkingAgentCount.ToString());
                EditorGUILayout.LabelField("等待中的下载代理个数", downloadManager.WaitingTaskCount.ToString());
                EditorGUILayout.LabelField("当前下载速度",       downloadManager.CurrentSpeed.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.BeginVertical("box");
                {
                    TaskInfo[] downloadInfos = downloadManager.GetAllDownloadInfos();
                    if (downloadInfos.Length > 0)
                    {
                        foreach (TaskInfo taskInfo in downloadInfos)
                        {
                            DrawDownloadInfo(taskInfo);
                        }
                    }
                    else
                    {
                        GUILayout.Label("Download Task is Empty ...");
                    }
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void DrawDownloadInfo(TaskInfo downloadInfo)
        {
            var taskDesc = $"[Id]{downloadInfo.SerialId} [Tag]{downloadInfo.Tag ?? "<None>"} [优先级]{downloadInfo.Priority} [状态]{downloadInfo.Status}";
            EditorGUILayout.LabelField(downloadInfo.Description, taskDesc);
        }
    }
}