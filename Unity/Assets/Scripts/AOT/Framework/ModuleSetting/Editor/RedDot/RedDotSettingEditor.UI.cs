#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor.RedDot
{
    public partial class RedDotSettingEditor
    {
        /// <summary>
        /// 绘制格式错误警告信息
        /// </summary>
        private void DrawInvalidFormatWarning()
        {
            GUILayout.Space(5);

            // 主错误提示
            EditorGUILayout.HelpBox($"发现 {m_InvalidFormatNodes.Count} 个格式错误的Key，请修改以下Key确保格式正确！", MessageType.Error);
            
            GUILayout.Space(5);

            // 显示每个格式错误节点的详细信息
            foreach (var invalidNode in m_InvalidFormatNodes)
            {
                EditorGUILayout.BeginVertical("box");

                // 错误节点标题
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"错误节点: ",             EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField($"{invalidNode.path}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);

                // 显示错误信息
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("错误: ",                 GUILayout.Width(40));
                EditorGUILayout.LabelField($"{invalidNode.error}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);

                // 定位按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                using (new BackgroundColorScope(new Color(1.0f, 0.6f, 0.2f))) // 橙色
                {
                    if (GUILayout.Button("定位", GUILayout.Width(60)))
                    {
                        LocateAndExpandNode(invalidNode.node);
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(3);
            }

            // 分隔线
            EditorGUILayout.Separator();
            GUILayout.Space(5);
        }

        /// <summary>
        /// 绘制重复key错误信息
        /// </summary>
        private void DrawDuplicateKeysWarning()
        {
            GUILayout.Space(5);

            // 主错误提示
            EditorGUILayout.HelpBox($"发现 {m_DuplicateKeyPaths.Count} 个重复的Key，请修改以下重复的Key确保唯一性！", MessageType.Error);

            GUILayout.Space(5);

            // 显示每个重复key的详细信息
            foreach (var duplicate in m_DuplicateKeyPaths)
            {
                EditorGUILayout.BeginVertical("box");

                // 重复key标题
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"重复Key: ",         EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField($"{duplicate.Key}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);

                // 显示所有重复节点的路径和定位按钮
                var pathNodes = duplicate.Value;
                for (var i = 0; i < pathNodes.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i + 1}",             GUILayout.Width(20));
                    EditorGUILayout.LabelField($"{pathNodes[i].path}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                    // 定位按钮
                    using (new BackgroundColorScope(new Color(0.2f, 0.6f, 1.0f))) // 蓝色
                    {
                        if (GUILayout.Button("定位", GUILayout.Width(40)))
                        {
                            LocateAndExpandNode(pathNodes[i].node);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(3);
            }

            // 操作提示
            EditorGUILayout.HelpBox("点击\"定位\"按钮可以自动展开并定位到对应的节点。", MessageType.Info);
            GUILayout.Space(5);
        }

        /// <summary>
        /// 处理下一帧的焦点设置
        /// </summary>
        private void HandleFocusNextFrame()
        {
            // 在下一帧设置焦点
            if (UnityEngine.Event.current.type == EventType.Repaint)
            {
                m_NeedFocusNextFrame = false;
                EditorGUI.FocusTextInControl($"KeyField_{m_FocusNodeKey}");
                Repaint();
            }
        }
    }
}
#endif