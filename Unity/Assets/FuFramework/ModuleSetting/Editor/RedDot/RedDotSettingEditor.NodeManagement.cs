#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    public partial class RedDotSettingEditor
    {
        /// <summary>
        /// 使用 SerializedProperty 绘制节点列表
        /// </summary>
        private void DrawNodeListProperty(SerializedProperty listProperty, int indent)
        {
            if (listProperty == null || !listProperty.isArray) return;

            for (var i = 0; i < listProperty.arraySize; i++)
            {
                var elementProperty = listProperty.GetArrayElementAtIndex(i);
                DrawNodeProperty(elementProperty, indent, i, listProperty);
            }
        }

        /// <summary>
        /// 绘制单个节点
        /// </summary>
        private void DrawNodeProperty(SerializedProperty nodeProperty, int indent, int index, SerializedProperty parentListProperty)
        {
            // 获取节点数据引用
            if (nodeProperty?.managedReferenceValue is not RedDotNodeData nodeData) return;

            // 确保节点在字典中存在
            m_NodeExpanded.TryAdd(nodeData, false);

            // 检查当前节点是否有错误
            bool isDuplicate    = m_DuplicateKeyPaths.Count > 0 && m_DuplicateKeyPaths.ContainsKey(nodeData.m_Key);
            bool hasFormatError = m_InvalidFormatNodes.Any(x => x.node == nodeData);

            GUILayout.BeginVertical("box");

            // 节点标题行
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20);

            // 根据节点状态显示不同的前缀
            string displayText = nodeData.m_Key;
            if (hasFormatError)
            {
                using (new GUIColorScope(Color.red))
                {
                    m_NodeExpanded[nodeData] = EditorGUILayout.Foldout(m_NodeExpanded[nodeData], $"[格式错误] {displayText}", true);
                }
            }
            else if (isDuplicate)
            {
                using (new GUIColorScope(Color.yellow))
                {
                    m_NodeExpanded[nodeData] = EditorGUILayout.Foldout(m_NodeExpanded[nodeData], $"[重复] {displayText}", true);
                }
            }
            else
            {
                m_NodeExpanded[nodeData] = EditorGUILayout.Foldout(m_NodeExpanded[nodeData], displayText, true);
            }

            // 添加子节点按钮 - 青色（在删除按钮左边）
            using (new BackgroundColorScope(Color.cyan))
            {
                if (GUILayout.Button("添加子节点", GUILayout.Width(80)))
                {
                    AddChildNode(nodeProperty, nodeData);
                }
            }

            // 删除按钮 - 红色
            using (new BackgroundColorScope(Color.red))
            {
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    if (ShowDeleteConfirmation(nodeData.m_Key))
                    {
                        parentListProperty.DeleteArrayElementAtIndex(index);
                        m_NodeExpanded.Remove(nodeData);

                        // 立即应用序列化修改
                        serializedObject.ApplyModifiedProperties();

                        // 删除节点后立即检查验证
                        ForceRefreshValidationCheck();
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        return;
                    }
                }
            }

            GUILayout.EndHorizontal();

            if (m_NodeExpanded[nodeData])
            {
                // 节点内容
                GUILayout.BeginHorizontal();
                GUILayout.Space(indent * 20 + 10);
                EditorGUILayout.BeginVertical();

                // 绘制 Key 字段
                var keyProperty = nodeProperty.FindPropertyRelative("m_Key");
                if (keyProperty != null)
                {
                    // 如果是焦点节点，设置控件名称用于聚焦
                    var controlName = $"KeyField_{nodeData.m_Key}";
                    GUI.SetNextControlName(controlName);

                    EditorGUILayout.PropertyField(keyProperty, new GUIContent("Key"));
                }

                EditorGUILayout.EndVertical();
                GUILayout.EndHorizontal();

                // 绘制子节点
                var childrenProperty = nodeProperty.FindPropertyRelative("m_Children");
                if (childrenProperty != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(indent * 20 + 20);
                    EditorGUILayout.BeginVertical();
                    DrawNodeListProperty(childrenProperty, indent + 1);
                    EditorGUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 添加根节点
        /// </summary>
        private void AddRootNode()
        {
            var uniqueKey = GenerateUniqueKey("NewRootNode");

            m_RootNodesProperty.arraySize++;
            var newElement = m_RootNodesProperty.GetArrayElementAtIndex(m_RootNodesProperty.arraySize - 1);
            var newNode    = new RedDotNodeData { m_Key = uniqueKey };
            newElement.managedReferenceValue = newNode;

            // 设置焦点节点
            m_FocusNode          = newNode;
            m_FocusNodeKey       = uniqueKey;
            m_NeedFocusNextFrame = true;

            // 确保新节点展开
            m_NodeExpanded[newNode] = true;

            // 立即重绘
            Repaint();
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        private void AddChildNode(SerializedProperty parentNodeProperty, RedDotNodeData parentNode)
        {
            var childrenProperty = parentNodeProperty.FindPropertyRelative("m_Children");
            if (childrenProperty == null) return;

            var uniqueKey = GenerateUniqueKey("NewChild", parentNode);

            childrenProperty.arraySize++;
            var newElement = childrenProperty.GetArrayElementAtIndex(childrenProperty.arraySize - 1);
            var newNode    = new RedDotNodeData { m_Key = uniqueKey };
            newElement.managedReferenceValue = newNode;

            // 展开父节点
            m_NodeExpanded[parentNode] = true;

            // 设置焦点节点
            m_FocusNode          = newNode;
            m_FocusNodeKey       = uniqueKey;
            m_NeedFocusNextFrame = true;

            // 自动定位并展开到新节点
            LocateAndExpandNode(newNode);

            // 立即重绘
            Repaint();
        }

        /// <summary>
        /// 生成唯一的key
        /// </summary>
        private string GenerateUniqueKey(string baseName, RedDotNodeData parentNode = null)
        {
            if (parentNode == null)
            {
                // 根节点
                var newKey  = baseName;
                var counter = 1;

                while (IsKeyDuplicate(newKey))
                {
                    newKey = $"{baseName}{counter}";
                    counter++;
                }

                return newKey;
            }
            else
            {
                // 子节点：格式为 父节点.子节点
                var newKey  = $"{parentNode.m_Key}.{baseName}";
                var counter = 1;

                while (IsKeyDuplicate(newKey))
                {
                    newKey = $"{parentNode.m_Key}.{baseName}{counter}";
                    counter++;
                }

                return newKey;
            }
        }

        /// <summary>
        /// 显示删除确认对话框
        /// </summary>
        private bool ShowDeleteConfirmation(string nodeKey)
        {
            return EditorUtility.DisplayDialog("确认删除", $"确定要删除节点 '{nodeKey}' 吗？\n此操作无法撤销！", "确定删除", "取消");
        }
    }
}
#endif