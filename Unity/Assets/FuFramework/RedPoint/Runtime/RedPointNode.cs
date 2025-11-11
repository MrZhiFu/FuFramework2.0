using System;
using System.Collections.Generic;
using System.Linq;
using FuFramework.Core.Runtime;

namespace FuFramework.RedPoint.Runtime
{
    /// <summary>
    /// 红点节点
    /// </summary>
    public class RedPointNode
    {
        /// <summary>
        /// 节点kay
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 节点的原始计数
        /// </summary>
        private int m_RawCount;
        
        /// <summary>
        /// 节点的总计数
        /// </summary>
        public int TotalCount { get; private set; }
        
        /// <summary>
        /// 节点的父节点
        /// </summary>
        public RedPointNode Parent { get; private set; }

        /// <summary>
        /// 节点的子节点列表
        /// </summary>
        private readonly List<RedPointNode> m_Children = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">节点key</param>
        /// <param name="parent">父节点</param>
        public RedPointNode(string key, RedPointNode parent)
        {
            Key = key;
            Parent = parent;
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(RedPointNode child)
        {
            if (m_Children.Contains(child))
            {
                FuLog.Warning($"RedPointNode: 无法添加重复的子节点, 在节点{Key}下已经存在子节点{child.Key}");
                return;
            }
            m_Children.Add(child);
        }

        /// <summary>
        /// 设置节点计数
        /// </summary>
        public void SetCount(int count)
        {
            m_RawCount = count;
            UpdateTotalCount();
        }

        /// <summary>
        /// 获取节点计数
        /// </summary>
        public int GetCount() => TotalCount;

        /// <summary>
        /// 更新节点总计数
        /// </summary>
        private void UpdateTotalCount()
        {
            var total = m_RawCount + m_Children.Sum(child => child.TotalCount);
            if (TotalCount == total) return;
            
            TotalCount = total;
            Parent?.UpdateTotalCount();
            RedPointManager.NotifyStateChanged(this);
        }
    }
}