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
using GameFrameX.Runtime;
using UnityEditor;
using UnityEngine;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 对象池组件Inspector
    /// </summary>
    [CustomEditor(typeof(ObjectPoolComponent))]
    internal sealed class ObjectPoolComponentInspector : ComponentTypeComponentInspector
    {
        /// <summary>
        /// 已打开的对象池项
        /// </summary>
        private readonly HashSet<string> m_OpenedItems = new();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("对象池信息仅在运行时可见，请在运行时查看.", MessageType.Info);
                return;
            }

            ObjectPoolComponent t = (ObjectPoolComponent)target;

            if (IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("对象池总个数：", t.Count.ToString());

                // 获取并绘制所有对象池信息
                ObjectPoolBase[] objectPools = t.GetAllObjectPools(true);
                foreach (ObjectPoolBase objectPool in objectPools)
                {
                    DrawObjectPool(objectPool);
                }
            }

            // 重绘Inspector
            Repaint();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IObjectPoolManager));
        }

        /// <summary>
        /// 绘制单个对象池信息
        /// </summary>
        /// <param name="objectPool"></param>
        private void DrawObjectPool(ObjectPoolBase objectPool)
        {
            bool lastState    = m_OpenedItems.Contains(objectPool.FullName);
            bool currentState = EditorGUILayout.Foldout(lastState, objectPool.FullName);
            if (currentState != lastState)
            {
                if (currentState)
                    m_OpenedItems.Add(objectPool.FullName);
                else
                    m_OpenedItems.Remove(objectPool.FullName);
            }

            if (!currentState) return;

            // 绘制对象池信息
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("名称",          objectPool.Name);
                EditorGUILayout.LabelField("类型",          objectPool.ObjectType.FullName);
                EditorGUILayout.LabelField("自动释放可释放对象间隔", objectPool.AutoReleaseInterval.ToString());
                EditorGUILayout.LabelField("容量",          objectPool.Capacity.ToString());
                EditorGUILayout.LabelField("已用数量",        objectPool.Count.ToString());
                EditorGUILayout.LabelField("可释放数量",       objectPool.CanReleaseCount.ToString());
                EditorGUILayout.LabelField("过期时间",        objectPool.ExpireTime.ToString());
                EditorGUILayout.LabelField("优先级",         objectPool.Priority.ToString());

                var objectInfos = objectPool.GetAllObjectInfos();
                if (objectInfos.Length > 0)
                {
                    EditorGUILayout.LabelField("Name", objectPool.AllowSpawnInUse ? "Locked\tCount\tFlag\tPriority\tLast Use Time" : "Locked\tIn Use\tFlag\tPriority\tLast Use Time");
                    foreach (var objectInfo in objectInfos)
                    {
                        EditorGUILayout.LabelField(string.IsNullOrEmpty(objectInfo.Name) ? "<None>" : objectInfo.Name,
                                                   objectPool.AllowSpawnInUse
                                                       ? Utility.Text.Format("{0}\t{1}\t{2}\t{3}\t{4:yyyy-MM-dd HH:mm:ss}", objectInfo.Locked, objectInfo.SpawnCount, objectInfo.CustomCanReleaseFlag,
                                                                             objectInfo.Priority, objectInfo.LastUseTime.ToLocalTime())
                                                       : Utility.Text.Format("{0}\t{1}\t{2}\t{3}\t{4:yyyy-MM-dd HH:mm:ss}", objectInfo.Locked, objectInfo.IsInUse, objectInfo.CustomCanReleaseFlag,
                                                                             objectInfo.Priority, objectInfo.LastUseTime.ToLocalTime()));
                    }

                    if (GUILayout.Button("释放"))
                    {
                        objectPool.Release();
                    }

                    if (GUILayout.Button("释放所有未使用的对象"))
                    {
                        objectPool.ReleaseAllUnused();
                    }

                    if (GUILayout.Button("导出CSV数据"))
                    {
                        var exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, Utility.Text.Format("Object Pool Data - {0}.csv", objectPool.Name), string.Empty);
                        if (!string.IsNullOrEmpty(exportFileName))
                        {
                            try
                            {
                                var index = 0;
                                var data  = new string[objectInfos.Length + 1];
                                data[index++] = Utility.Text.Format("Name,Locked,{0},Custom Can Release Flag,Priority,Last Use Time", objectPool.AllowSpawnInUse ? "Count" : "In Use");
                                foreach (var objectInfo in objectInfos)
                                {
                                    data[index++] = objectPool.AllowSpawnInUse
                                        ? Utility.Text.Format("{0},{1},{2},{3},{4},{5:yyyy-MM-dd HH:mm:ss}", objectInfo.Name, objectInfo.Locked, objectInfo.SpawnCount, objectInfo.CustomCanReleaseFlag,
                                                              objectInfo.Priority, objectInfo.LastUseTime.ToLocalTime())
                                        : Utility.Text.Format("{0},{1},{2},{3},{4},{5:yyyy-MM-dd HH:mm:ss}", objectInfo.Name, objectInfo.Locked, objectInfo.IsInUse, objectInfo.CustomCanReleaseFlag,
                                                              objectInfo.Priority, objectInfo.LastUseTime.ToLocalTime());
                                }

                                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                Debug.Log(Utility.Text.Format("对象池 CSV 数据导出为 {0}成功.", exportFileName));
                            }
                            catch (Exception exception)
                            {
                                Debug.LogError(Utility.Text.Format("对象池 CSV 数据导出到 “{0}” 失败，异常为 “{1}”..", exportFileName, exception));
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label("对象池中没有对象...");
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }
    }
}