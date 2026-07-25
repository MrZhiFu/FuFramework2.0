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
    /// RedDotModule 功能扩展：动态红点、已读持久化、清理策略、内部广播
    /// </summary>
    public partial class RedDotModule
    {
        #region 动态红点

        /// <summary>
        /// 同步动态红点集合：根据当前 key 列表增删实例
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="dynamicKeys">当前活跃的实例 Key 列表</param>
        /// <param name="providerFactory">根据 dynamicKey 创建 Leaf Provider 的工厂</param>
        public void SyncDynamicNodes(ERedDotKey parentKey, IReadOnlyList<long> dynamicKeys, Func<long, Func<int>> providerFactory)
        {
            if (!m_StaticNodes.ContainsKey(parentKey))
            {
                FuLogger.LogError($"[RedDotModule] SyncDynamicNodes 未找到父节点: {parentKey}");
                return;
            }

            if (!m_DynamicInstanceMap.TryGetValue(parentKey, out var existingDynamic))
            {
                existingDynamic = new Dictionary<long, string>();
                m_DynamicInstanceMap[parentKey] = existingDynamic;
            }

            // 收集新增的 key
            var newDynamicKeys = new HashSet<long>();
            for (var i = 0; i < dynamicKeys.Count; i++)
            {
                newDynamicKeys.Add(dynamicKeys[i]);
            }

            // 移除不在新列表中的实例
            var removedDynamicKeys = new List<long>();
            foreach (var kvp in existingDynamic)
            {
                if (!newDynamicKeys.Contains(kvp.Key))
                    removedDynamicKeys.Add(kvp.Key);
            }

            foreach (var key in removedDynamicKeys)
            {
                RemoveDynamicChild(existingDynamic[key]);
                existingDynamic.Remove(key);
            }

            // 添加新增的实例
            foreach (var dynamicKey in dynamicKeys)
            {
                if (existingDynamic.ContainsKey(dynamicKey)) continue;

                var childName = $"__inst__{parentKey}_{dynamicKey}";
                var node      = AddDynamicChild(parentKey, childName);
                if (node == null) continue;
                var provider = providerFactory(dynamicKey);
                RegisterInternal(node, provider, null);
                existingDynamic[dynamicKey] = childName;
            }

            // 重算所有实例
            RecalculateAllDynamic(parentKey);
        }

        /// <summary>
        /// 刷新单个实例的 Leaf Provider
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="dynamicKey">实例 Key</param>
        public void RefreshDynamicNode(ERedDotKey parentKey, long dynamicKey)
        {
            if (!m_DynamicInstanceMap.TryGetValue(parentKey, out var instances)) return;
            if (!instances.TryGetValue(dynamicKey, out var childName)) return;
            if (!m_DynamicNodes.TryGetValue(childName, out var node)) return;
            if (node.CalculateProvider == null) return;

            var newCount = node.CalculateProvider.Invoke();
            if (node.IsRead) newCount = 0;
            node.SetCount(newCount);
            BroadcastChangedIds();
        }

        /// <summary>
        /// 查询动态红点状态
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="dynamicKey">实例 Key</param>
        /// <returns>实例的 RedDotState，未找到时返回 Empty</returns>
        public RedDotState GetDynamicState(ERedDotKey parentKey, long dynamicKey)
        {
            if (!m_DynamicInstanceMap.TryGetValue(parentKey, out var instances)) return RedDotState.Empty;
            if (!instances.TryGetValue(dynamicKey, out var childName)) return RedDotState.Empty;
            return GetState(childName);
        }

        /// <summary>
        /// 重算某个父节点下所有实例
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        private void RecalculateAllDynamic(ERedDotKey parentKey)
        {
            if (!m_DynamicInstanceMap.TryGetValue(parentKey, out var instances)) return;

            foreach (var kvp in instances)
            {
                if (m_DynamicNodes.TryGetValue(kvp.Value, out var node) && node.CalculateProvider != null)
                {
                    var newCount = node.CalculateProvider.Invoke();
                    if (node.IsRead) newCount = 0;
                    node.SetCount(newCount);
                }
            }

            if (m_ChangedStaticSet.Count > 0 || m_ChangedDynamicSet.Count > 0)
            {
                BroadcastChangedIds();
            }
        }

        #endregion

        #region 已读持久化

        /// <summary>
        /// 标记红点已读（计数归零 + 持久化）
        /// </summary>
        /// <param name="key">静态节点 Key</param>
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
        /// <param name="key">动态节点 Key</param>
        public void MarkRead(string key)
        {
            if (!m_DynamicNodes.TryGetValue(key, out var node)) return;

            node.IsRead = true;
            node.SetCount(0);
        }

        /// <summary>
        /// 检查是否已读
        /// </summary>
        /// <param name="key">静态节点 Key</param>
        /// <returns>已读返回 true，否则返回 false</returns>
        public bool IsRead(ERedDotKey key) => m_ReadSet.Contains((int)key);

        /// <summary>
        /// 检查是否已读
        /// </summary>
        /// <param name="key">动态节点 Key</param>
        /// <returns>已读返回 true，否则返回 false</returns>
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

        #endregion

        #region 清理策略

        /// <summary>
        /// 尝试自动清除红点（仅对 ViewAutoClean 策略的节点生效）
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void TryAutoClean(ERedDotKey key)
        {
            if (!m_StaticNodes.TryGetValue(key, out var node)) return;
            if (node.CleanStrategy != ERedDotCleanStrategy.ViewAutoClean) return;
            CleanNodeRecursive(node);
        }

        /// <summary>
        /// 递归清除节点及所有子节点的计数
        /// </summary>
        /// <param name="node">起始节点</param>
        private static void CleanNodeRecursive(RedDotNode node)
        {
            node.SetCount(0);
            foreach (var child in node.GetChildren())
                CleanNodeRecursive(child);
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// TotalCount 变化回调（注入到每个 RedDotNode，在 UpdateTotalCount 中触发）
        /// 收集本帧变更的节点 Key，供 OnUpdate 批量广播
        /// </summary>
        /// <param name="node">发生变化的节点</param>
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

        #endregion
    }
}
