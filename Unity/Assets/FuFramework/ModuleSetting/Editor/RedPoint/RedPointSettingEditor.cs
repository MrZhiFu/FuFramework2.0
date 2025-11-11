#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 红点模块配置Inspector
    /// </summary>
    [CustomEditor(typeof(RedPointSetting))]
    public class RedPointSettingEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 根节点列表属性
        /// </summary>
        private SerializedProperty m_RootNodesProperty;

        /// <summary>
        /// 用于跟踪节点展开状态的字典
        /// </summary>
        private readonly Dictionary<RedPointNodeData, bool> m_NodeExpanded = new();


        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_RootNodesProperty = serializedObject.FindProperty("m_RootNodes");
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("红点节点树配置", EditorStyles.boldLabel);
            
            // 绘制根节点列表
            if (m_RootNodesProperty != null)
            {
                DrawNodeListProperty(m_RootNodesProperty, 0);
            }
            else
            {
                EditorGUILayout.HelpBox("未找到根节点属性", MessageType.Error);
            }

            // 添加创建根节点的按钮 - 绿色
            GUILayout.Space(10);
            using (new BackgroundColorScope(Color.green))
            {
                if (GUILayout.Button("添加根节点", GUILayout.Height(30)))
                {
                    AddRootNode();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

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
            if (nodeProperty?.managedReferenceValue is not RedPointNodeData nodeData) return;

            // 确保节点在字典中存在
            m_NodeExpanded.TryAdd(nodeData, false);

            GUILayout.BeginVertical("box");

            // 节点标题行
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20);
            m_NodeExpanded[nodeData] = EditorGUILayout.Foldout(m_NodeExpanded[nodeData], $"🔹 {nodeData.m_Key}", true);
            
            // 删除按钮 - 红色
            using (new BackgroundColorScope(Color.red))
            {
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    if (ShowDeleteConfirmation(nodeData.m_Key))
                    {
                        parentListProperty.DeleteArrayElementAtIndex(index);
                        m_NodeExpanded.Remove(nodeData);
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
                    EditorGUILayout.PropertyField(keyProperty, new GUIContent("Key"));
                }

                // 添加子节点按钮 - 青色
                using (new BackgroundColorScope(Color.cyan))
                {
                    if (GUILayout.Button("添加子节点"))
                    {
                        AddChildNode(nodeProperty);
                    }
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
        /// 显示删除确认对话框
        /// </summary>
        private bool ShowDeleteConfirmation(string nodeKey)
        {
            return EditorUtility.DisplayDialog(
                "确认删除",
                $"确定要删除节点 '{nodeKey}' 吗？\n此操作无法撤销！",
                "确定删除",
                "取消"
            );
        }

        /// <summary>
        /// 添加根节点
        /// </summary>
        private void AddRootNode()
        {
            m_RootNodesProperty.arraySize++;
            var newElement = m_RootNodesProperty.GetArrayElementAtIndex(m_RootNodesProperty.arraySize - 1);
            newElement.managedReferenceValue = new RedPointNodeData { m_Key = "NewRootNode" };
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        private void AddChildNode(SerializedProperty parentNodeProperty)
        {
            var childrenProperty = parentNodeProperty.FindPropertyRelative("m_Children");
            if (childrenProperty != null)
            {
                childrenProperty.arraySize++;
                var newElement = childrenProperty.GetArrayElementAtIndex(childrenProperty.arraySize - 1);
                newElement.managedReferenceValue = new RedPointNodeData { m_Key = "NewChildNode" };
            }
        }

        /// <summary>
        /// 背景颜色作用域辅助类
        /// </summary>
        private class BackgroundColorScope : System.IDisposable
        {
            private readonly Color m_OriginalColor;

            public BackgroundColorScope(Color newColor)
            {
                m_OriginalColor = GUI.backgroundColor;
                GUI.backgroundColor = newColor;
            }

            public void Dispose()
            {
                GUI.backgroundColor = m_OriginalColor;
            }
        }

        /// <summary>
        /// 当Inspector被销毁时清理字典
        /// </summary>
        private void OnDestroy()
        {
            m_NodeExpanded.Clear();
        }
    }
}
#endif