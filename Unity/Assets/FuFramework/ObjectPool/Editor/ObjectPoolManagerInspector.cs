using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Globalization;
using FuFramework.Core.Editor;
using System.Collections.Generic;
using FuFramework.ObjectPool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Editor
{
    /// <summary>
    /// 对象池管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(ObjectPoolManager))]
    internal sealed class ObjectPoolManagerInspector : FuFrameworkInspector
    {
        /// <summary>
        /// 已打开的对象池项
        /// </summary>
        private readonly HashSet<string> m_OpenedItems = new();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var poolManager = target as ObjectPoolManager;
            if (!poolManager) return;

            EditorGUILayout.LabelField("对象池总个数：", poolManager.Count.ToString());

            // 获取并绘制所有对象池信息
            var objectPools = poolManager.GetAllObjectPools(true);
            foreach (var objectPool in objectPools)
            {
                DrawObjectPool(objectPool);
            }
        }

        /// <summary>
        /// 绘制单个对象池信息
        /// </summary>
        /// <param name="objectPool"></param>
        private void DrawObjectPool(ObjectPoolBase objectPool)
        {
            bool lastState = m_OpenedItems.Contains(objectPool.FullName);
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
                EditorGUILayout.LabelField("名称", objectPool.Name);
                EditorGUILayout.LabelField("类型", objectPool.ObjectType.FullName);
                EditorGUILayout.LabelField("自动释放可释放对象间隔", objectPool.AutoReleaseInterval.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.LabelField("容量", objectPool.Capacity.ToString());
                EditorGUILayout.LabelField("已用数量", objectPool.Count.ToString());
                EditorGUILayout.LabelField("可释放数量", objectPool.CanReleaseCount.ToString());
                EditorGUILayout.LabelField("过期时间", objectPool.ExpireTime.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.LabelField("优先级", objectPool.Priority.ToString());

                var objectInfos = objectPool.GetAllObjectInfos();
                if (objectInfos.Length > 0)
                {
                    EditorGUILayout.LabelField("Name", objectPool.AllowSpawnInUse ? "Locked\tCount\tFlag\tPriority\tLast Use Time" : "Locked\tIn Use\tFlag\tPriority\tLast Use Time");
                    
                    foreach (var objectInfo in objectInfos)
                    {
                        EditorGUILayout.LabelField(string.IsNullOrEmpty(objectInfo.Name) ? "<None>" : objectInfo.Name,
                            objectPool.AllowSpawnInUse
                                ? $"{objectInfo.Locked}\t{objectInfo.SpawnCount}\t{objectInfo.CustomCanReleaseFlag}\t{objectInfo.Priority}\t{objectInfo.LastUseTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                                : $"{objectInfo.Locked}\t{objectInfo.IsInUse}\t{objectInfo.CustomCanReleaseFlag}\t{objectInfo.Priority}\t{objectInfo.LastUseTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
                    }

                    if (GUILayout.Button("尝试释放超过对象池容量的数量对象")) objectPool.Release();
                    if (GUILayout.Button("释放对象池中所有未使用的对象")) objectPool.ReleaseAllUnused();

                    if (GUILayout.Button("导出CSV数据"))
                    {
                        var exportFileName = EditorUtility.SaveFilePanel("导出CSV数据", string.Empty, $"Object Pool Data - {objectPool.Name}.csv", string.Empty);
                        if (!string.IsNullOrEmpty(exportFileName))
                        {
                            try
                            {
                                var index = 0;
                                var data = new string[objectInfos.Length + 1];
                                var stateStr = objectPool.AllowSpawnInUse ? "Count" : "In Use";
                                data[index++] = $"Name,Locked,{stateStr},Custom Can Release Flag,Priority,Last Use Time";
                                foreach (var objectInfo in objectInfos)
                                {
                                    data[index++] = objectPool.AllowSpawnInUse
                                        ? $"{objectInfo.Name},{objectInfo.Locked},{objectInfo.SpawnCount},{objectInfo.CustomCanReleaseFlag},{objectInfo.Priority},{objectInfo.LastUseTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                                        : $"{objectInfo.Name},{objectInfo.Locked},{objectInfo.IsInUse},{objectInfo.CustomCanReleaseFlag},{objectInfo.Priority},{objectInfo.LastUseTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
                                }

                                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                Debug.Log($"对象池 CSV 数据导出为 {exportFileName}成功.");
                            }
                            catch (Exception exception)
                            {
                                Debug.LogError($"对象池 CSV 数据导出到 “{exportFileName}” 失败，异常为 “{exception}”.");
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