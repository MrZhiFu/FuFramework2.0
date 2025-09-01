using UnityEditor;
using UnityEngine;
using System.Linq;
using FuFramework.Core.Editor;
using System.Collections.Generic;
using FuFramework.Procedure.Runtime;
using System;
using System.Reflection;
using Type = FuFramework.Core.Editor.Type;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Editor
{
    /// <summary>
    /// 自定义流程组件的Inspector
    /// </summary>
    [CustomEditor(typeof(ProcedureManager))]
    internal sealed class ProcedureGameComponentInspector : GameComponentInspector
    {
        private SerializedProperty m_AvailableProcedureTypeNames; // 可用的流程类型名称列表
        private SerializedProperty m_EntranceProcedureTypeName;   // 入口流程类型名称

        private string[]     m_ProcedureTypeNames;          // 所有流程类型名称列表
        private List<string> m_SelectedProcedureTypeNames;  // 已选择的流程类型名称列表
        private int          m_EntranceProcedureIndex = -1; // 入口流程索引


        private readonly Dictionary<string, int> m_ProcedurePriorityCache = new(); // 缓存类型和类型显示优先级的映射

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            var procedureComp = target as ProcedureManager;
            if (!procedureComp) return;

            if (string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("入口流程不能为空!.", MessageType.Error);
            }
            else if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("当前流程：", procedureComp.CurrentProcedure == null ? "None" : procedureComp.CurrentProcedure.GetType().ToString());
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Label("所有可用的流程类型：", EditorStyles.boldLabel);
                if (m_ProcedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        // 按优先级排序显示
                        var sortedProcedureNames = GetSortedProcedureNamesByPriority(m_ProcedureTypeNames.ToList());
                        foreach (var procedureTypeName in sortedProcedureNames)
                        {
                            var selected    = m_SelectedProcedureTypeNames.Contains(procedureTypeName);
                            var displayName = $"{procedureTypeName}";

                            if (selected == EditorGUILayout.ToggleLeft(displayName, selected)) continue;
                            if (!selected)
                            {
                                m_SelectedProcedureTypeNames.Add(procedureTypeName);
                                WriteAvailableProcedureTypeNames();
                            }
                            else if (procedureTypeName != m_EntranceProcedureTypeName.stringValue)
                            {
                                m_SelectedProcedureTypeNames.Remove(procedureTypeName);
                                WriteAvailableProcedureTypeNames();
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("没有找到可用的流程类型!", MessageType.Warning);
                }

                if (m_SelectedProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    // 按优先级排序下拉选项
                    var sortedAvailableProcedureNames = GetSortedProcedureNamesByPriority(m_ProcedureTypeNames.ToList());
                    var displayOptions                = sortedAvailableProcedureNames.Select(typeName => $"{typeName}").ToArray();

                    var currentEntranceIndex = sortedAvailableProcedureNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                    var selectedIndex        = EditorGUILayout.Popup("入口流程", currentEntranceIndex, displayOptions);
                    if (selectedIndex != currentEntranceIndex && selectedIndex >= 0)
                    {
                        m_EntranceProcedureTypeName.stringValue = sortedAvailableProcedureNames[selectedIndex];
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
            m_EntranceProcedureTypeName   = serializedObject.FindProperty("m_EntranceProcedureTypeName");
            _RefreshTypeNames();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(ProcedureManager));
        }

        /// <summary>
        /// 根据优先级对流程名称进行排序
        /// </summary>
        private List<string> GetSortedProcedureNamesByPriority(List<string> procedureNames)
        {
            return procedureNames
                   .OrderBy(GetProcedurePriority)
                   .ThenBy(typeName => typeName) // 如果优先级相同，按名称排序
                   .ToList();
        }

        /// <summary>
        /// 获取流程的优先级
        /// </summary>
        private int GetProcedurePriority(string procedureTypeName)
        {
            // 如果已经在缓存中，直接返回
            if (m_ProcedurePriorityCache.TryGetValue(procedureTypeName, out var priority))
            {
                return priority;
            }

            // 尝试从所有已加载的程序集中查找类型
            var type = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(assembly => assembly.GetTypes())
                                .FirstOrDefault(t => t.FullName == procedureTypeName && typeof(ProcedureBase).IsAssignableFrom(t));

            if (type != null)
            {
                try
                {
                    // 使用反射获取优先级属性
                    var priorityProperty = type.GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (priorityProperty != null && priorityProperty.PropertyType == typeof(int))
                    {
                        // 创建实例并获取优先级
                        if (Activator.CreateInstance(type) is ProcedureBase instance)
                        {
                            priority                                    = instance.Priority;
                            m_ProcedurePriorityCache[procedureTypeName] = priority;
                            return priority;
                        }
                    }

                    // 如果无法通过实例获取，尝试获取默认值（对于抽象类）
                    const int defaultPriority = 0;
                    m_ProcedurePriorityCache[procedureTypeName] = defaultPriority;
                    return defaultPriority;
                }
                catch
                {
                    // 如果出错，返回默认优先级
                    m_ProcedurePriorityCache[procedureTypeName] = 0;
                    return 0;
                }
            }

            // 如果找不到类型，返回默认优先级
            m_ProcedurePriorityCache[procedureTypeName] = 0;
            return 0;
        }

        /// <summary>
        /// 刷新流程类型名称列表
        /// </summary>
        private void _RefreshTypeNames()
        {
            // 清空缓存
            m_ProcedurePriorityCache.Clear();

            m_ProcedureTypeNames = Type.GetRuntimeTypeNames(typeof(ProcedureBase));
            ReadAvailableProcedureTypeNames();

            var oldCount = m_SelectedProcedureTypeNames.Count;
            m_SelectedProcedureTypeNames = m_SelectedProcedureTypeNames
                                           .Where(x => m_ProcedureTypeNames.Contains(x))
                                           .ToList();

            if (m_SelectedProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex = m_SelectedProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0)
                    m_EntranceProcedureTypeName.stringValue = null;
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 读取可用的流程类型名称列表
        /// </summary>
        private void ReadAvailableProcedureTypeNames()
        {
            m_SelectedProcedureTypeNames = new List<string>();
            var count = m_AvailableProcedureTypeNames.arraySize;
            for (var i = 0; i < count; i++)
            {
                m_SelectedProcedureTypeNames.Add(m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }

        /// <summary>
        /// 写入可用的流程类型名称列表
        /// </summary>
        private void WriteAvailableProcedureTypeNames()
        {
            m_AvailableProcedureTypeNames.ClearArray();
            if (m_SelectedProcedureTypeNames == null) return;

            var count = m_SelectedProcedureTypeNames.Count;
            for (var i = 0; i < count; i++)
            {
                m_AvailableProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = m_SelectedProcedureTypeNames[i];
            }

            // 更新入口流程索引
            if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex = m_SelectedProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0)
                {
                    m_EntranceProcedureTypeName.stringValue = null;
                }
            }
        }
    }
}