using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using Hotfix.Framework.Storage;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// RedDotModule 功能扩展：实例红点、已读持久化、清理策略、内部广播
    /// </summary>
    public partial class RedDotModule
    {
        // ========== 实例红点 ==========

        /// <summary>
        /// 同步实例红点集合：根据当前 key 列表增删实例
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="instanceKeys">当前活跃的实例 Key 列表</param>
        /// <param name="providerFactory">根据 instanceKey 创建 Leaf Provider 的工厂</param>
        public void SyncInstances(ERedDotKey parentKey, IReadOnlyList<long> instanceKeys, Func<long, Func<int>> providerFactory)
        {
            if (!m_StaticNodes.ContainsKey(parentKey))
            {
                FuLogger.LogError($"[RedDotModule] SyncInstances 未找到父节点: {parentKey}");
                return;
            }

            if (!m_InstanceNodes.TryGetValue(parentKey, out var existingInstances))
            {
                existingInstances = new Dictionary<long, string>();
                m_InstanceNodes[parentKey] = existingInstances;
            }

            // 收集新增的 key
            var newKeys = new HashSet<long>();
            for (var i = 0; i < instanceKeys.Count; i++)
            {
                newKeys.Add(instanceKeys[i]);
            }

            // 移除不在新列表中的实例
            var removedKeys = new List<long>();
            foreach (var kvp in existingInstances)
            {
                if (!newKeys.Contains(kvp.Key))
                    removedKeys.Add(kvp.Key);
            }
            
            foreach (var key in removedKeys)
            {
                RemoveDynamicChild(existingInstances[key]);
                existingInstances.Remove(key);
            }

            // 添加新增的实例
            foreach (var instanceKey in instanceKeys)
            {
                if (existingInstances.ContainsKey(instanceKey)) continue;

                var childName = $"__inst__{parentKey}_{instanceKey}";
                var node      = AddDynamicChild(parentKey, childName);
                if (node == null) continue;
                var provider = providerFactory(instanceKey);
                RegisterLeafInternal(node, provider, null);
                existingInstances[instanceKey] = childName;
            }

            // 重算所有实例
            RecalculateAllInstances(parentKey);
        }

        /// <summary>
        /// 刷新单个实例的 Leaf Provider
        /// </summary>
        public void RefreshInstance(ERedDotKey parentKey, long instanceKey)
        {
            if (!m_InstanceNodes.TryGetValue(parentKey, out var instances)) return;
            if (!instances.TryGetValue(instanceKey, out var childName)) return;
            if (!m_DynamicNodes.TryGetValue(childName, out var node)) return;
            if (node.LeafProvider == null) return;

            var newCount = node.LeafProvider.Invoke();
            if (node.IsRead) newCount = 0;
            node.SetCount(newCount);
            BroadcastChangedIds();
        }

        /// <summary>
        /// 查询实例红点状态
        /// </summary>
        public RedDotState GetInstanceState(ERedDotKey parentKey, long instanceKey)
        {
            if (!m_InstanceNodes.TryGetValue(parentKey, out var instances)) return RedDotState.Empty;
            if (!instances.TryGetValue(instanceKey, out var childName)) return RedDotState.Empty;
            return GetState(childName);
        }

        /// <summary>
        /// 重算某个父节点下所有实例
        /// </summary>
        private void RecalculateAllInstances(ERedDotKey parentKey)
        {
            if (!m_InstanceNodes.TryGetValue(parentKey, out var instances)) return;

            foreach (var kvp in instances)
            {
                if (m_DynamicNodes.TryGetValue(kvp.Value, out var node) && node.LeafProvider != null)
                {
                    var newCount = node.LeafProvider.Invoke();
                    if (node.IsRead) newCount = 0;
                    node.SetCount(newCount);
                }
            }

            if (m_ChangedStaticSet.Count > 0 || m_ChangedDynamicSet.Count > 0)
            {
                BroadcastChangedIds();
            }
        }

        // ========== 已读持久化 ==========

        /// <summary>
        /// 标记红点已读（计数归零 + 持久化）
        /// </summary>
        public void MarkRead(ERedDotKey key)
        {
            if (!m_StaticNodes.TryGetValue(key, out var node)) return;

            node.IsRead = true;
            node.SetCount(0);
            m_ReadSet.Add((int)key);
            SaveReadState();
            BroadcastChangedIds();
        }

        /// <summary>
        /// 标记红点已读
        /// </summary>
        public void MarkRead(string key)
        {
            if (!m_DynamicNodes.TryGetValue(key, out var node)) return;

            node.IsRead = true;
            node.SetCount(0);
        }

        /// <summary>
        /// 检查是否已读
        /// </summary>
        public bool IsRead(ERedDotKey key) => m_ReadSet.Contains((int)key);

        /// <summary>
        /// 检查是否已读
        /// </summary>
        public bool IsRead(string key)
        {
            return m_DynamicNodes.TryGetValue(key, out var node) && node.IsRead;
        }

        /// <summary>
        /// 从 StorageModule 加载已读状态
        /// </summary>
        private void LoadReadState()
        {
            if (StorageModule.Instance == null) return;

            var list = StorageModule.Instance.GetObject<List<int>>(ReadStorageKey, ReadStorageFile);
            if (list == null) return;

            foreach (var id in list)
            {
                m_ReadSet.Add(id);
                var key = (ERedDotKey)id;
                if (m_StaticNodes.TryGetValue(key, out var node))
                {
                    node.IsRead = true;
                    node.SetCount(0);
                }
            }
        }

        /// <summary>
        /// 保存已读状态到 StorageModule
        /// </summary>
        private void SaveReadState()
        {
            var list = new List<int>(m_ReadSet);
            StorageModule.Instance.SetObject(ReadStorageKey, list, ReadStorageFile);
        }

        // ========== 清理策略 ==========

        /// <summary>
        /// 尝试自动清除红点（仅对 ViewAutoClean 策略的节点生效）
        /// </summary>
        public void TryAutoClean(ERedDotKey key)
        {
            if (!m_StaticNodes.TryGetValue(key, out var node)) return;
            if (node.CleanStrategy != ERedDotCleanStrategy.ViewAutoClean) return;
            CleanNodeRecursive(node);
        }

        private static void CleanNodeRecursive(RedDotNode node)
        {
            node.SetCount(0);
            foreach (var child in node.GetChildren())
                CleanNodeRecursive(child);
        }

        // ========== 内部方法 ==========

        /// <summary>
        /// TotalCount 变化回调（注入到每个 RedDotNode，在 UpdateTotalCount 中触发）
        /// 收集本帧变更的节点 Key，供 OnUpdate 批量广播
        /// </summary>
        private void OnNodeTotalCountChanged(RedDotNode node)
        {
            if (node.StaticKey.HasValue)
                m_ChangedStaticSet.Add(node.StaticKey.Value);
            else if (node.DynamicKey != null)
                m_ChangedDynamicSet.Add(node.DynamicKey);
        }

        /// <summary>
        /// 通过 EventModule 批量广播本帧变更
        /// </summary>
        private void BroadcastChangedIds()
        {
            var args = RedDotChangedEventArgs.Create();
            foreach (var key in m_ChangedStaticSet)
                args.ChangedStaticKeys.Add(key);
            foreach (var key in m_ChangedDynamicSet)
                args.ChangedDynamicKeys.Add(key);

            GlobalModule.EventModule.Broadcast(this, args);

            m_ChangedStaticSet.Clear();
            m_ChangedDynamicSet.Clear();
        }
    }
}
