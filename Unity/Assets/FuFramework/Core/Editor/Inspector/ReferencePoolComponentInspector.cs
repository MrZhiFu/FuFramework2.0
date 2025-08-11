//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FuFramework.Core.Runtime;
using UnityEditor;
using UnityEngine;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

namespace GameFrameX.Editor
{
    [CustomEditor(typeof(ReferencePoolComponent))]
    internal sealed class ReferencePoolComponentInspector : GameFrameworkInspector
    {
        /// <summary>
        /// 存储所有引用池信息的字典，key为程序集名称，value为该程序集下的所有引用池信息
        /// </summary>
        private readonly Dictionary<string, List<ReferencePoolInfo>> m_ReferencePoolDict = new(StringComparer.Ordinal);

        /// <summary>
        /// 存储所有展开的程序集名称的集合
        /// </summary>
        private readonly HashSet<string> m_OpenedItems = new();

        /// <summary>
        /// 激活严格检查的属性
        /// </summary>
        private SerializedProperty m_EnableStrictCheck = null;

        /// <summary>
        /// 是否显示完整类名的属性
        /// </summary>
        private bool m_ShowFullClassName = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ReferencePoolComponent t = (ReferencePoolComponent)target;

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                var enableStrictCheck = EditorGUILayout.Toggle("激活严格检查(打开会降低性能)", t.EnableStrictCheck);
                if (enableStrictCheck != t.EnableStrictCheck)
                    t.EnableStrictCheck = enableStrictCheck;

                EditorGUILayout.LabelField("引用池个数", ReferencePool.Count.ToString());
                m_ShowFullClassName = EditorGUILayout.Toggle("是否显示完整类名", m_ShowFullClassName);

                // 获取并遍历所有引用池信息,并按程序集分类存储
                m_ReferencePoolDict.Clear();
                var referencePoolInfos = ReferencePool.GetAllReferencePoolInfos();
                foreach (var referencePoolInfo in referencePoolInfos)
                {
                    var assemblyName = referencePoolInfo.Type.Assembly.GetName().Name;
                    if (!m_ReferencePoolDict.TryGetValue(assemblyName, out var results))
                    {
                        results = new List<ReferencePoolInfo>();
                        m_ReferencePoolDict.Add(assemblyName, results);
                    }

                    results.Add(referencePoolInfo);
                }

                // 绘制所有程序集下的引用池信息
                foreach (var assemblyReferencePoolInfo in m_ReferencePoolDict)
                {
                    var lastState    = m_OpenedItems.Contains(assemblyReferencePoolInfo.Key);
                    var currentState = EditorGUILayout.Foldout(lastState, assemblyReferencePoolInfo.Key);
                    if (currentState != lastState)
                    {
                        if (currentState)
                            m_OpenedItems.Add(assemblyReferencePoolInfo.Key);
                        else
                            m_OpenedItems.Remove(assemblyReferencePoolInfo.Key);
                    }

                    if (!currentState) continue;

                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField(m_ShowFullClassName ? "Full Class Name" : "Class Name", "Unused\tUsing\tAcquire\tRelease\tAdd\tRemove");
                        assemblyReferencePoolInfo.Value.Sort(Comparison);

                        // 绘制每一个引用池信息
                        foreach (var referencePoolInfo in assemblyReferencePoolInfo.Value)
                        {
                            DrawReferencePoolInfo(referencePoolInfo);
                        }

                        if (GUILayout.Button("导出数据到CSV"))
                        {
                            var exportFileName = EditorUtility.SaveFilePanel("导出数据到CSV", string.Empty,
                                                                             Utility.Text.Format("引用池数据-{0}.csv", assemblyReferencePoolInfo.Key), string.Empty);
                            if (!string.IsNullOrEmpty(exportFileName))
                            {
                                try
                                {
                                    var index = 0;
                                    var data  = new string[assemblyReferencePoolInfo.Value.Count + 1];
                                    data[index++] = "Class Name,Full Class Name,Unused,Using,Acquire,Release,Add,Remove";
                                    foreach (var referencePoolInfo in assemblyReferencePoolInfo.Value)
                                    {
                                        data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7}", referencePoolInfo.Type.Name, referencePoolInfo.Type.FullName,
                                                                            referencePoolInfo.UnusedReferenceCount, referencePoolInfo.UsingReferenceCount, referencePoolInfo.AcquireReferenceCount,
                                                                            referencePoolInfo.ReleaseReferenceCount, referencePoolInfo.AddReferenceCount, referencePoolInfo.RemoveReferenceCount);
                                    }

                                    File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                    Debug.Log(Utility.Text.Format("导出引用池数据到CSV '{0}' 成功.", exportFileName));
                                }
                                catch (Exception exception)
                                {
                                    Debug.LogError(Utility.Text.Format("导出引用池数据到CSV '{0}' 失败, 异常：'{1}'.", exportFileName, exception));
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Separator();
                }
            }
            else
            {
                EditorGUILayout.PropertyField(m_EnableStrictCheck);
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void OnEnable()
        {
            m_EnableStrictCheck = serializedObject.FindProperty("m_EnableStrictCheck");
        }

        /// <summary>
        /// 绘制单个引用池信息
        /// </summary>
        /// <param name="referencePoolInfo"></param>
        private void DrawReferencePoolInfo(ReferencePoolInfo referencePoolInfo)
        {
            EditorGUILayout.LabelField(m_ShowFullClassName ? referencePoolInfo.Type.FullName : referencePoolInfo.Type.Name,
                                       Utility.Text.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", referencePoolInfo.UnusedReferenceCount, referencePoolInfo.UsingReferenceCount,
                                                           referencePoolInfo.AcquireReferenceCount, referencePoolInfo.ReleaseReferenceCount, referencePoolInfo.AddReferenceCount,
                                                           referencePoolInfo.RemoveReferenceCount));
        }

        private int Comparison(ReferencePoolInfo a, ReferencePoolInfo b)
        {
            return m_ShowFullClassName ? string.Compare(a.Type.FullName, b.Type.FullName, StringComparison.Ordinal) : string.Compare(a.Type.Name, b.Type.Name, StringComparison.Ordinal);
        }
    }
}