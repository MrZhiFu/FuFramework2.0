using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePools;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// RedDotModule 公开 API
    /// </summary>
    public partial class RedDotModule
    {
        #region 注册

        /// <summary>
        /// 注册红点
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        /// <param name="calculator">返回红点数量的计算函数</param>
        /// <param name="triggerEvents">触发重算的事件ID列表(可变参数)</param>
        public void Register(RedDotKey key, Func<int> calculator, params string[] triggerEvents)
        {
            if (!NodeDict.TryGetValue(key, out var node))
            {
                FuLogger.LogError($"[RedDotModule] Register 未找到节点: {key}");
                return;
            }

            if (node.GetChildren().Count > 0)
            {
                FuLogger.LogError($"[RedDotModule] Register 只能给叶子节点注册 Calculator: {key}");
                return;
            }

            RegisterInternal(node, calculator, triggerEvents);
        }

        /// <summary>
        /// 注销红点
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void Unregister(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            UnregisterInternal(node);
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 查询节点状态        /// </summary>
        /// <param name="key">红点节点 Key</param>
        /// <returns>节点的 RedDotState，未找到时返回 Empty</returns>
        public RedDotState GetState(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return RedDotState.Empty;

            return new RedDotState
            {
                Count       = node.GetFinalCount(),
                IsActive    = node.IsActive,
                DisplayMode = node.DisplayMode
            };
        }

        /// <summary>
        /// 是否存在节点        /// </summary>
        /// <param name="key">红点节点 Key</param>
        /// <returns>存在返回 true，否则返回 false</returns>
        public bool HasNode(RedDotKey key) => NodeDict.ContainsKey(key);

        /// <summary>
        /// 获取所有节点（调试用）
        /// </summary>
        /// <returns>所有红点节点的只读集合</returns>
        public IReadOnlyCollection<RedDotNode> GetAllNodes() => NodeDict.Values;

        #endregion

        #region 动态节点

        /// <summary>
        /// 同步动态红点集合：比对增删，新增时自动创建节点 + 注册计算红点的函数
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="ids">当前活跃的 id 列表</param>
        /// <param name="calculateFun">根据 id 返回红点数量的计算函数</param>
        public void SyncDynamicNode(RedDotKey parentKey, IReadOnlyList<long> ids, Func<long, int> calculateFun)
        {
            if (!NodeDict.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] SyncDynamicNode 未找到父节点: {parentKey}");
                return;
            }

            if (!m_DynamicIdDict.TryGetValue(parentKey, out var existing))
            {
                existing                   = new HashSet<long>();
                m_DynamicIdDict[parentKey] = existing;
            }

            // 收集新增 id
            var newIds = new HashSet<long>();
            foreach (var id in ids)
            {
                newIds.Add(id);
            }

            // 找出待移除的 id
            var removedIds = new List<long>();
            foreach (var id in existing)
            {
                if (newIds.Contains(id)) continue;
                removedIds.Add(id);
            }

            // 阶段1: 移除(跳过单次 parent 重算，最后统一重算)
            foreach (var id in removedIds)
            {
                var childKey = FormatDynamicKey(parentKey, id);
                if (NodeDict.TryGetValue(childKey, out var node))
                {
                    UnregisterInternal(node);
                    parentNode.RemoveChild(node);
                    NodeDict.Remove(childKey);
                    ReferencePool.Release(node);
                }

                existing.Remove(id);
            }

            // 阶段2: 新增(用 SetCountSilent 跳过单次 parent 重算)
            foreach (var id in ids)
            {
                if (existing.Contains(id)) continue;

                var dynamicKey = FormatDynamicKey(parentKey, id);
                var idCapture  = id; // 避免闭包捕获循环变量

                var node = AddDynamicChild(parentKey, dynamicKey);
                if (node == null) continue;

                RegisterInternal(node, () => calculateFun(idCapture), null);
                var count              = calculateFun(idCapture);
                if (node.IsRead) count = 0;
                node.SetCountSilent(count);
                existing.Add(id);
            }

            // 阶段3: 一次性重算父节点并向上传播
            parentNode.ForceRecalculate();
        }

        #endregion

        #region 已读持久化

        /// <summary>
        /// 标记红点已读(计数归零 + 持久化，仅静态键持久化)
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void MarkRead(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;

            node.IsRead = true;
            node.SetCount(0);

            // 仅静态枚举键进行持久化(通过 RedDotNode.IsStatic 标记判断)
            if (node.IsStatic && key.TryGetEnumValue(out var enumValue))
            {
                m_ReadSet.Add(enumValue);
                SaveReadState();
                BroadcastChangedKeys();
            }
        }

        /// <summary>
        /// 检查是否已读
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        /// <returns>已读返回 true，否则返回 false</returns>
        public bool IsRead(RedDotKey key)
        {
            // 静态键检查持久化集合
            if (NodeDict.TryGetValue(key, out var node) && node.IsStatic && key.TryGetEnumValue(out var enumValue))
                return m_ReadSet.Contains(enumValue);

            // 动态键检查节点自身标记
            return NodeDict.TryGetValue(key, out node) && node.IsRead;
        }

        #endregion

        #region 清理策略

        /// <summary>
        /// 尝试自动清除红点(仅对 ViewAutoClean 策略的节点生效)
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void TryAutoClean(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            if (node.CleanStrategy != ERedDotCleanStrategy.ViewAutoClean) return;
            CleanNodeRecursive(node);
        }

        #endregion
    }
}
