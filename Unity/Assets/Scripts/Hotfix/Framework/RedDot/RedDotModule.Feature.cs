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
    /// RedDotModule 功能扩展：动态红点批量同步、已读持久化、清理策略、内部广播
    /// </summary>
    public partial class RedDotModule
    {
        #region 动态红点

        /// <summary>
        /// 父节点 → 已同步的 id 集合（用于 SyncDynamicNode 增量更新）
        /// </summary>
        private readonly Dictionary<ERedDotKey, HashSet<long>> m_DynamicIdDict = new();

        /// <summary>
        /// 同步动态红点集合：比对增删，新增时自动创建节点 + 注册计算红点的函数
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="ids">当前活跃的 id 列表</param>
        /// <param name="calculateFun">根据 id 返回红点数量的计算函数</param>
        public void SyncDynamicNode(ERedDotKey parentKey, IReadOnlyList<long> ids, Func<long, int> calculateFun)
        {
            if (!StaticNodeDict.ContainsKey(parentKey))
            {
                FuLogger.LogError($"[RedDotModule] SyncDynamicNode 未找到父节点: {parentKey}");
                return;
            }

            if (!m_DynamicIdDict.TryGetValue(parentKey, out var existing))
            {
                existing = new HashSet<long>();
                m_DynamicIdDict[parentKey] = existing;
            }

            // 收集新增的 id
            var newIds = new HashSet<long>();
            foreach (var id in ids)
            {
                newIds.Add(id);
            }

            // 移除不在新列表中的实例
            var removedIds = new List<long>();
            foreach (var id in existing)
            {
                if (newIds.Contains(id)) continue;
                removedIds.Add(id);
            }

            foreach (var id in removedIds)
            {
                RemoveDynamicChild(FormatDynamicKey(parentKey, id));
                existing.Remove(id);
            }

            // 添加新增的实例 — 框架内部包装闭包
            foreach (var id in ids)
            {
                if (existing.Contains(id)) continue;

                var dynamicKey = FormatDynamicKey(parentKey, id);
                var idCapture  = id; // 避免闭包捕获循环变量
                
                Register(parentKey, dynamicKey, () => calculateFun(idCapture));
                existing.Add(id);
            }
        }

        /// <summary>
        /// 生成动态节点 Key（格式：__dynamic_{parentKey}_{id}）
        /// </summary>
        private static string FormatDynamicKey(ERedDotKey parentKey, long id)
        {
            return $"__dynamic__{parentKey}_{id}";
        }

        #endregion

        #region 已读持久化

        /// <summary>
        /// 标记红点已读（计数归零 + 持久化）
        /// </summary>
        /// <param name="key">静态节点 Key</param>
        public void MarkRead(ERedDotKey key)
        {
            if (!StaticNodeDict.TryGetValue(key, out var node)) return;

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
            if (!DynamicNodeDict.TryGetValue(key, out var node)) return;

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
            return DynamicNodeDict.TryGetValue(key, out var node) && node.IsRead;
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
                if (StaticNodeDict.TryGetValue(key, out var node))
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
            if (!StaticNodeDict.TryGetValue(key, out var node)) return;
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
            {
                CleanNodeRecursive(child);
            }
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
            {
                m_ChangedStaticSet.Add(node.StaticKey.Value);
            }
            else if (node.DynamicKey != null)
            {
                m_ChangedDynamicSet.Add(node.DynamicKey);
            }
        }

        /// <summary>
        /// 通过 EventModule 批量广播本帧变更
        /// </summary>
        private void BroadcastChangedIds()
        {
            var args = RedDotChangedEventArgs.Create();
            foreach (var key in m_ChangedStaticSet)
            {
                args.ChangedStaticKeys.Add(key);
            }
            foreach (var key in m_ChangedDynamicSet)
            {
                args.ChangedDynamicKeys.Add(key);
            }

            GlobalModule.EventModule.Broadcast(this, args);

            m_ChangedStaticSet.Clear();
            m_ChangedDynamicSet.Clear();
        }

        #endregion
    }
}
