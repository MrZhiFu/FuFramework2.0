using UnityEditor;
using UnityEngine;
using System.Linq;
using FuFramework.Core.Editor;
using System.Collections.Generic;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Editor
{
    /// <summary>
    /// 自定义流程组件的Inspector
    /// </summary>
    [CustomEditor(typeof(ProcedureComponent))]
    internal sealed class ProcedureGameComponentInspector : GameComponentInspector
    {
        private SerializedProperty m_AvailableProcedureTypeNames;
        private SerializedProperty m_EntranceProcedureTypeName;

        private string[] m_ProcedureTypeNames; // 所有流程类型名称列表
        private List<string> m_CurrentAvailableProcedureTypeNames; // 当前可用的流程类型名称列表
        private int m_EntranceProcedureIndex = -1; // 入口流程索引

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            var procedureComp = (ProcedureComponent)target;

            if (string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("入口流程不能为空!.", MessageType.Error);
            }
            else if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("当前流程：", procedureComp.CurrentProcedure is null ? "None" : procedureComp.CurrentProcedure.GetType().ToString());
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Label("所有可用的流程类型：", EditorStyles.boldLabel);
                if (m_ProcedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        foreach (var procedureTypeName in m_ProcedureTypeNames)
                        {
                            var selected = m_CurrentAvailableProcedureTypeNames.Contains(procedureTypeName);
                            if (selected != EditorGUILayout.ToggleLeft(procedureTypeName, selected))
                            {
                                if (!selected)
                                {
                                    m_CurrentAvailableProcedureTypeNames.Add(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                                else if (procedureTypeName != m_EntranceProcedureTypeName.stringValue)
                                {
                                    m_CurrentAvailableProcedureTypeNames.Remove(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("没有找到可用的流程类型!", MessageType.Warning);
                }

                if (m_CurrentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    var selectedIndex = EditorGUILayout.Popup("入口流程", m_EntranceProcedureIndex, m_CurrentAvailableProcedureTypeNames.ToArray());
                    if (selectedIndex != m_EntranceProcedureIndex)
                    {
                        m_EntranceProcedureIndex = selectedIndex;
                        m_EntranceProcedureTypeName.stringValue = m_CurrentAvailableProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("请选择至少一个流程类型!.", MessageType.Info);
                }
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            _RefreshTypeNames();
        }

        protected override void Enable()
        {
            m_AvailableProcedureTypeNames = serializedObject.FindProperty("m_AvailableProcedureTypeNames");
            m_EntranceProcedureTypeName = serializedObject.FindProperty("m_EntranceProcedureTypeName");
            _RefreshTypeNames();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IProcedureManager));
        }

        /// <summary>
        /// 刷新流程类型名称列表
        /// </summary>
        private void _RefreshTypeNames()
        {
            m_ProcedureTypeNames = Type.GetRuntimeTypeNames(typeof(ProcedureBase));
            ReadAvailableProcedureTypeNames();
            var oldCount = m_CurrentAvailableProcedureTypeNames.Count;
            m_CurrentAvailableProcedureTypeNames = m_CurrentAvailableProcedureTypeNames.Where(x => m_ProcedureTypeNames.Contains(x)).ToList();
            if (m_CurrentAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex = m_CurrentAvailableProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0) m_EntranceProcedureTypeName.stringValue = null;
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 读取可用的流程类型名称列表
        /// </summary>
        private void ReadAvailableProcedureTypeNames()
        {
            m_CurrentAvailableProcedureTypeNames = new List<string>();
            var count = m_AvailableProcedureTypeNames.arraySize;
            for (var i = 0; i < count; i++)
            {
                m_CurrentAvailableProcedureTypeNames.Add(m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }

        /// <summary>
        /// 写入可用的流程类型名称列表
        /// </summary>
        private void WriteAvailableProcedureTypeNames()
        {
            m_AvailableProcedureTypeNames.ClearArray();
            if (m_CurrentAvailableProcedureTypeNames == null) return;

            m_CurrentAvailableProcedureTypeNames.Sort();
            var count = m_CurrentAvailableProcedureTypeNames.Count;
            for (var i = 0; i < count; i++)
            {
                m_AvailableProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = m_CurrentAvailableProcedureTypeNames[i];
            }

            if (string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue)) return;
            m_EntranceProcedureIndex = m_CurrentAvailableProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
            if (m_EntranceProcedureIndex < 0) m_EntranceProcedureTypeName.stringValue = null;
        }
    }
}