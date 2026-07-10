using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Editor
{
    /// <summary>
    /// 自定义下载管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(DownloadModule))]
    internal sealed class DownloadModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            serializedObject.Update();

            if (target is not DownloadModule module) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("是否暂停",       module.Paused.ToString());
                EditorGUILayout.LabelField("下载总代理个数",    module.TotalAgentCount.ToString());
                EditorGUILayout.LabelField("空闲的下载代理个数",  module.FreeAgentCount.ToString());
                EditorGUILayout.LabelField("工作中的下载代理个数", module.WorkingAgentCount.ToString());
                EditorGUILayout.LabelField("等待中的下载代理个数", module.WaitingTaskCount.ToString());
                EditorGUILayout.LabelField("当前下载速度",     module.CurrentSpeed.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.BeginVertical("box");
                {
                    TaskInfo[] downloadInfos = module.GetAllDownloadInfos();
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
            */

            /*
            private void DrawDownloadInfo(TaskInfo downloadInfo)
            {
                var taskDesc = $"[Id]{downloadInfo.SerialId} [Tag]{downloadInfo.Tag ?? "<None>"} [优先级]{downloadInfo.Priority} [状态]{downloadInfo.Status}";
                EditorGUILayout.LabelField(downloadInfo.Description, taskDesc);
            }
            */
        }
    }
}
