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
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var downloadManager = target as DownloadManager;
            if (!downloadManager) return;

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
                        GUILayout.Label("当前下载任务为空...");
                    }
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        /// <summary>
        /// 绘制下载任务信息
        /// </summary>
        /// <param name="downloadInfo"></param>
        private void DrawDownloadInfo(TaskInfo downloadInfo)
        {
            var taskDesc = $"[Id]{downloadInfo.SerialId} [Tag]{downloadInfo.Tag ?? "<None>"} [优先级]{downloadInfo.Priority} [状态]{downloadInfo.Status}";
            EditorGUILayout.LabelField(downloadInfo.Description, taskDesc);
        }
    }
}