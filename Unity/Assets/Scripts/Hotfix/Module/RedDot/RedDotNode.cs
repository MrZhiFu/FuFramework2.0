using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;

namespace Hotfix.RedDot
{
    /// <summary>
    /// 红点节点
    /// </summary>
    public class RedDotNode : IReference
    {
        /// <summary>
        /// 节点key
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 节点的原始计数
        /// </summary>
        public int RawCount { get; private set; }

        /// <summary>
        /// 节点的总计数
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 节点的父节点
        /// </summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>
        /// 节点的子节点列表
        /// </summary>
        private readonly List<RedDotNode> m_Children = new();

        /// <summary>
        /// 节点路径缓存（从根节点到当前节点的完整路径）
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// 节点计数变化事件
        /// </summary>
        public event Action<int> OnCountChanged;

        /// <summary>
        /// 创建节点
        /// </summary>
        public static RedDotNode Create(string key, RedDotNode parent)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.Key = key;
            node.Parent = parent;
            node.Path = parent != null ? $"{parent.Path}/{key}" : key;
            return node;
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(RedDotNode child)
        {
            if (m_Children.Contains(child))
            {
                FuLogger.LogWarning($"[RedDotNode] 无法添加重复的子节点, 在节点{Key}下已经存在子节点{child.Key}");
                return;
            }

            m_Children.Add(child);
        }

        /// <summary>
        /// 设置节点计数
        /// </summary>
        public void SetCount(int count)
        {
            if (RawCount == count) return;

            RawCount = count;
            UpdateTotalCount();
        }

        /// <summary>
        /// 更新节点总计数
        /// </summary>
        private void UpdateTotalCount()
        {
            var childrenTotal = 0;
            foreach (var child in m_Children)
            {
                childrenTotal += child.TotalCount;
            }

            var total = RawCount + childrenTotal;
            if (TotalCount == total) return;

            TotalCount = total;
            OnCountChanged?.Invoke(TotalCount);// 触发数量改变事件
            Parent?.UpdateTotalCount();
        }

        /// <summary>
        /// 获取所有子节点（只读）
        /// </summary>
        public IReadOnlyList<RedDotNode> GetChildren() => m_Children.AsReadOnly();

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            Key = "";
            Path = "";
            Parent = null;
            RawCount = 0;
            TotalCount = 0;
            m_Children.Clear();
            OnCountChanged = null;
        }

        /// <summary>
        /// 清除所有事件监听
        /// </summary>
        public void ClearAllListeners()
        {
            OnCountChanged = null;
        }
    }
}
