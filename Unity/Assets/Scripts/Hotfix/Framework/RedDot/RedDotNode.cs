using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePools;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
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
        /// 节点的原始计数（自身，不含子节点）
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
        /// 子节点聚合逻辑（来自配置表）
        /// </summary>
        public ERedDotLogicType LogicType { get; private set; }

        /// <summary>
        /// 是否激活（false 时 TotalCount 永远为 0）
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// UI 显示排序权重（来自配置表）
        /// </summary>
        public int ShowOrder { get; private set; }

        /// <summary>
        /// 是否已读（持久化，仅抑制初始加载时的红点）
        /// </summary>
        public bool IsRead { get; internal set; }

        /// <summary>
        /// 脏标记（本帧待重算）
        /// </summary>
        public bool IsDirty { get; internal set; }

        // ========== Leaf Provider ==========

        /// <summary>
        /// 叶子节点计算函数（不为 null 时由 OnUpdate 自动调用）
        /// </summary>
        public Func<int> LeafProvider { get; internal set; }

        /// <summary>
        /// 触发重算的 EventModule 事件 ID 列表
        /// </summary>
        public string[] TriggerEvents { get; internal set; }

        /// <summary>
        /// TotalCount 变化回调（内部使用，由 RedDotModule 设置）
        /// 用于收集本帧变更节点以批量广播
        /// </summary>
        internal Action<RedDotNode> OnTotalCountChanged;

        /// <summary>
        /// 节点的子节点列表
        /// </summary>
        private readonly List<RedDotNode> m_Children = new();

        /// <summary>
        /// 从配置表创建静态节点
        /// </summary>
        public static RedDotNode Create(ERedDotKey key, RedDotNode parent,
            ERedDotDisplayMode displayMode, ERedDotCleanStrategy cleanStrategy,
            ERedDotLogicType logicType, bool isActive, int showOrder)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.StaticKey = key;
            node.Parent = parent;
            node.DisplayMode = displayMode;
            node.CleanStrategy = cleanStrategy;
            node.LogicType = logicType;
            node.IsActive = isActive;
            node.ShowOrder = showOrder;
            return node;
        }

        /// <summary>
        /// 运行时创建动态节点（默认 DotOnly + Manual + Sum）
        /// </summary>
        public static RedDotNode CreateDynamic(string key, RedDotNode parent)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.DynamicKey = key;
            node.Parent = parent;
            node.DisplayMode = ERedDotDisplayMode.DotOnly;
            node.CleanStrategy = ERedDotCleanStrategy.Manual;
            node.LogicType = ERedDotLogicType.Sum;
            node.IsActive = true;
            node.ShowOrder = 0;
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
        /// 强制重算 TotalCount 并向上传播（子节点增删后调用）
        /// </summary>
        internal void ForceRecalculate()
        {
            UpdateTotalCount();
        }

        /// <summary>
        /// 设置节点计数并向上传播（仅 RedDotModule 内部调用）
        /// </summary>
        internal void SetCount(int count)
        {
            if (RawCount == count) return;

            RawCount = count;
            UpdateTotalCount();
        }

        /// <summary>
        /// 更新节点总计数，自动向上传播
        /// 聚合逻辑受 LogicType 和 IsActive 控制
        /// </summary>
        private void UpdateTotalCount()
        {
            if (!IsActive)
            {
                if (TotalCount != 0)
                {
                    TotalCount = 0;
                    OnTotalCountChanged?.Invoke(this);
                }
                return;
            }

            var childrenTotal = LogicType == ERedDotLogicType.Any
                ? ComputeChildrenAny()
                : ComputeChildrenSum();

            var total = RawCount + childrenTotal;
            if (TotalCount == total) return;

            TotalCount = total;
            OnTotalCountChanged?.Invoke(this);
            Parent?.UpdateTotalCount();
        }

        /// <summary>
        /// Sum 模式：累加所有子节点的 TotalCount
        /// </summary>
        private int ComputeChildrenSum()
        {
            var total = 0;
            foreach (var child in m_Children)
            {
                total += child.TotalCount;
            }
            return total;
        }

        /// <summary>
        /// Any 模式：任一子节点 TotalCount > 0 则为 1
        /// </summary>
        private int ComputeChildrenAny()
        {
            foreach (var child in m_Children)
            {
                if (child.TotalCount > 0) return 1;
            }
            return 0;
        }

        /// <summary>
        /// 获取最终计数（考虑 IsActive 和 IsRead）
        /// </summary>
        public int GetFinalCount()
        {
            if (!IsActive) return 0;
            return TotalCount;
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
            LogicType = ERedDotLogicType.Sum;
            IsActive = true;
            ShowOrder = 0;
            IsRead = false;
            IsDirty = false;
            LeafProvider = null;
            TriggerEvents = null;
            OnTotalCountChanged = null;
            m_Children.Clear();
        }
    }
}
