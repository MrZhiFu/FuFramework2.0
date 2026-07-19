using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePools;
using Hotfix.Game.UI;
using Hotfix.Game.Tables;
using Hotfix.Game.Tables.Tables;
using Hotfix.Game.Proto;

namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点节点
    /// 支持两种节点类型：
    ///     1. 静态节点：由 Luban 配置表定义，通过 ERedDotKey 枚举标识
    ///     2. 动态节点：由运行时创建（如道具实例红点），通过 string 标识
    /// </summary>
    public class RedDotNode : IReference
    {
        /// <summary>
        /// 静态节点 Key（配置表定义的节点），动态节点为 null
        /// </summary>
        public ERedDotKey? StaticKey { get; private set; }

        /// <summary>
        /// 动态节点 Key（运行时创建的节点），静态节点为 null
        /// </summary>
        public string DynamicKey { get; private set; }

        /// <summary>
        /// 节点的原始计数
        /// </summary>
        public int RawCount { get; private set; }

        /// <summary>
        /// 节点的总计数（自身 + 所有子节点）
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 节点的父节点
        /// </summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>
        /// 默认显示模式（来自配置表）
        /// </summary>
        public ERedDotDisplayMode DisplayMode { get; private set; }

        /// <summary>
        /// 清理策略（来自配置表）
        /// </summary>
        public ERedDotCleanStrategy CleanStrategy { get; private set; }

        /// <summary>
        /// 节点的子节点列表
        /// </summary>
        private readonly List<RedDotNode> m_Children = new();

        /// <summary>
        /// 节点计数变化事件
        /// </summary>
        public event Action<int> OnCountChanged;

        /// <summary>
        /// 从配置表创建静态节点
        /// </summary>
        public static RedDotNode Create(ERedDotKey key, RedDotNode parent,
            ERedDotDisplayMode displayMode, ERedDotCleanStrategy cleanStrategy)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.StaticKey = key;
            node.Parent = parent;
            node.DisplayMode = displayMode;
            node.CleanStrategy = cleanStrategy;
            return node;
        }

        /// <summary>
        /// 运行时创建动态节点（默认 DotOnly + Manual）
        /// </summary>
        public static RedDotNode CreateDynamic(string key, RedDotNode parent)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.DynamicKey = key;
            node.Parent = parent;
            node.DisplayMode = ERedDotDisplayMode.DotOnly;
            node.CleanStrategy = ERedDotCleanStrategy.Manual;
            return node;
        }

        /// <summary>
        /// 两阶段构建 — 初始化后设置父节点
        /// </summary>
        public void SetParent(RedDotNode parent) => Parent = parent;

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(RedDotNode child)
        {
            if (m_Children.Contains(child))
            {
                FuLogger.LogWarning($"[RedDotNode] 无法添加重复的子节点, 在节点{StaticKey}下已经存在子节点{child.StaticKey}");
                return;
            }

            m_Children.Add(child);
        }

        /// <summary>
        /// 移除子节点（动态节点归零回收时使用）
        /// </summary>
        public void RemoveChild(RedDotNode child) => m_Children.Remove(child);

        /// <summary>
        /// 设置节点计数，自动向上传播
        /// </summary>
        public void SetCount(int count)
        {
            if (RawCount == count) return;

            RawCount = count;
            UpdateTotalCount();
        }

        /// <summary>
        /// 更新节点总计数，自动向上传播
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
            OnCountChanged?.Invoke(TotalCount); // 触发数量改变事件
            Parent?.UpdateTotalCount();
        }

        /// <summary>
        /// 获取所有子节点（只读）
        /// </summary>
        public IReadOnlyList<RedDotNode> GetChildren() => m_Children.AsReadOnly();

        /// <summary>
        /// IReference — 回收到对象池时清理
        /// </summary>
        public void Clear()
        {
            StaticKey = null;
            DynamicKey = null;
            Parent = null;
            RawCount = 0;
            TotalCount = 0;
            DisplayMode = ERedDotDisplayMode.DotOnly;
            CleanStrategy = ERedDotCleanStrategy.Manual;
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
