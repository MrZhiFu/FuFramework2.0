using System;
using System.Globalization;
using System.IO;
using System.Text;
using FuFramework.Core.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Core.Editor;
using UnityEditor;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Editor
{
    [CustomEditor(typeof(DownloadComponent))]
    internal sealed class DownloadGameComponentInspector : GameComponentInspector
    {
        private SerializedProperty m_InstanceRoot;
        private SerializedProperty m_DownloadAgentHelperCount;
        private SerializedProperty m_Timeout;
        private SerializedProperty m_FlushSize;

        private readonly HelperInfo<DownloadAgentHelperBase> m_DownloadAgentHelperInfo = new("DownloadAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var downloadComp = target as DownloadComponent;
            if (!downloadComp) return;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_InstanceRoot);
                m_DownloadAgentHelperInfo.Draw();
                m_DownloadAgentHelperCount.intValue = EditorGUILayout.IntSlider("Download Agent Helper Count", m_DownloadAgentHelperCount.intValue, 1, 16);
            }
            EditorGUI.EndDisabledGroup();

            var timeout = EditorGUILayout.Slider("Timeout", m_Timeout.floatValue, 0f, 120f);
            if (!Mathf.Approximately(timeout, m_Timeout.floatValue))
            {
                if (EditorApplication.isPlaying)
                {
                    downloadComp.Timeout = timeout;
                }
                else
                {
                    m_Timeout.floatValue = timeout;
                }
            }

            int flushSize = EditorGUILayout.DelayedIntField("Flush Size", m_FlushSize.intValue);
            if (flushSize != m_FlushSize.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    downloadComp.FlushSize = flushSize;
                }
                else
                {
                    m_FlushSize.intValue = flushSize;
                }
            }

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(downloadComp.gameObject))
            {
                EditorGUILayout.LabelField("Paused",              downloadComp.Paused.ToString());
                EditorGUILayout.LabelField("Total Agent Count",   downloadComp.TotalAgentCount.ToString());
                EditorGUILayout.LabelField("Free Agent Count",    downloadComp.FreeAgentCount.ToString());
                EditorGUILayout.LabelField("Working Agent Count", downloadComp.WorkingAgentCount.ToString());
                EditorGUILayout.LabelField("Waiting Agent Count", downloadComp.WaitingTaskCount.ToString());
                EditorGUILayout.LabelField("Current Speed",       downloadComp.CurrentSpeed.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.BeginVertical("box");
                {
                    TaskInfo[] downloadInfos = downloadComp.GetAllDownloadInfos();
                    if (downloadInfos.Length > 0)
                    {
                        foreach (TaskInfo downloadInfo in downloadInfos)
                        {
                            DrawDownloadInfo(downloadInfo);
                        }

                        if (GUILayout.Button("Export CSV Data"))
                        {
                            string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, "Download Task Data.csv", string.Empty);
                            if (!string.IsNullOrEmpty(exportFileName))
                            {
                                try
                                {
                                    int      index = 0;
                                    string[] data  = new string[downloadInfos.Length + 1];
                                    data[index++] = "Download Path,Serial Id,Tag,Priority,Status";
                                    foreach (TaskInfo downloadInfo in downloadInfos)
                                    {
                                        data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4}", downloadInfo.Description, downloadInfo.SerialId, downloadInfo.Tag ?? string.Empty,
                                                                            downloadInfo.Priority, downloadInfo.Status);
                                    }

                                    File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                    Debug.Log(Utility.Text.Format("Export download task CSV data to '{0}' success.", exportFileName));
                                }
                                catch (Exception exception)
                                {
                                    Debug.LogError(Utility.Text.Format("Export download task CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
                                }
                            }
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

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        protected override void Enable()
        {
            base.Enable();
            m_InstanceRoot             = serializedObject.FindProperty("m_InstanceRoot");
            m_DownloadAgentHelperCount = serializedObject.FindProperty("m_DownloadAgentHelperCount");
            m_Timeout                  = serializedObject.FindProperty("m_Timeout");
            m_FlushSize                = serializedObject.FindProperty("m_FlushSize");

            m_DownloadAgentHelperInfo.Init(serializedObject);
            m_DownloadAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDownloadInfo(TaskInfo downloadInfo)
        {
            EditorGUILayout.LabelField(downloadInfo.Description,
                                       Utility.Text.Format("[SerialId]{0} [Tag]{1} [Priority]{2} [Status]{3}", downloadInfo.SerialId, downloadInfo.Tag ?? "<None>", downloadInfo.Priority,
                                                           downloadInfo.Status));
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IDownloadManager));
        }
    }
}