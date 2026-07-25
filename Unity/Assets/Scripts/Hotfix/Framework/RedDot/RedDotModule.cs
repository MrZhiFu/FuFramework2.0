using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.ReferencePools;
using Hotfix.Framework.Config;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点管理模块
    /// 功能：
    ///     1. 树形结构管理 — 支持父子节点层级关系，LogicType（Any/Sum）控制聚合
    ///     2. Leaf Provider — 业务注册 Func<int> 计算函数，OnUpdate 批处理重算
    ///     3. EventModule 批量广播 — 每帧统一广播 RedDotChangedEventArgs，UI 端按 ID 过滤
    ///     4. 配置化驱动 — 通过 Luban 配置表 TbRedDot 初始化红点树结构
    ///     5. 静态+动态节点 — 静态节点由配置表定义(ERedDotKey)，动态节点运行时创建(string)
    ///     6. 实例红点 — SyncInstances 管理 per-instance 动态子节点
    ///     7. 已读持久化 — MarkRead 通过 StorageModule 持久保存已读状态
    ///
    /// 使用流程：
    ///     1. 在Luban配置表中定义红点树结构
    ///     2. 业务模块调用 RegisterLeaf 注册计算函数
    ///     3. 业务触发事件 → OnUpdate 重算 → 广播红点变更事件
    ///     4. UI组件CompRedDot 监听广播事件，按 ID 过滤刷新 UI
    /// </summary>
    public partial class RedDotModule : ModuleBase
    {
        /// <summary>
        /// 模块静态单例
        /// </summary>
        public static RedDotModule Instance { get; private set; }

        // ========== 节点存储 ==========

        /// <summary>
        /// 静态节点字典（配置表驱动，Key: ERedDotKey）
        /// </summary>
        private static readonly Dictionary<ERedDotKey, RedDotNode> m_StaticNodes = new();

        /// <summary>
        /// 动态节点字典（运行时创建，Key: string）
        /// </summary>
        private static readonly Dictionary<string, RedDotNode> m_DynamicNodes = new();

        // ========== 脏标记批处理 ==========

        /// <summary>本帧待重算的脏节点集合</summary>
        private readonly HashSet<RedDotNode> m_DirtyNodes = new();

        /// <summary>本帧发生变更的节点 Key（用于去重后广播）</summary>
        private readonly HashSet<ERedDotKey> m_ChangedStaticSet = new();

        /// <summary>本帧发生变更的动态节点 Key</summary>
        private readonly HashSet<string> m_ChangedDynamicSet = new();

        // ========== Leaf Provider 事件订阅反向映射 ==========

        /// <summary>EventModule 事件 ID → 订阅该事件的节点集合</summary>
        private readonly Dictionary<string, HashSet<RedDotNode>> m_EventToNodes = new();

        // ========== 实例红点追踪 ==========

        /// <summary>静态父节点 → (instanceKey → 动态节点名)</summary>
        private readonly Dictionary<ERedDotKey, Dictionary<long, string>> m_InstanceNodes = new();

        // ========== 已读持久化 ==========

        /// <summary>已读节点 Key 集合（存为 ERedDotKey 的 int 值）</summary>
        private readonly HashSet<int> m_ReadSet = new();

        /// <summary>
        /// 已读状态在 StorageModule 中的 Key
        /// </summary>
        private const string ReadStorageKey = "ReadSet";

        /// <summary>
        /// 已读状态在 StorageModule 中的文件名
        /// </summary>
        private const string ReadStorageFile = "RedDotData";

        // ========== 生命周期 ==========

        protected internal override void OnInit()
        {
            Instance = this;

            var tbRedDot = ConfigModule.Instance?.GetConfig<TbRedDot>();
            if (tbRedDot == null || tbRedDot.Count == 0)
            {
                FuLogger.LogWarning("[RedDotModule] 红点配置表不存在或为空，跳过树构建.");
                return;
            }

            m_StaticNodes.Clear();
            m_DynamicNodes.Clear();

            var allRows = tbRedDot.All;

            // 阶段一：创建所有节点
            foreach (var row in allRows)
            {
                var node = RedDotNode.Create(row.Id, null, row.DisplayMode, row.CleanStrategy,
                    row.LogicType, row.IsActive, row.ShowOrder);

                if (!m_StaticNodes.TryAdd(row.Id, node))
                {
                    FuLogger.LogError($"[RedDotModule] 重复的节点key: {row.Id}");
                    ReferencePool.Release(node);
                }
            }

            // 阶段二：建立父子关系
            foreach (var row in allRows)
            {
                if (row.ParentId == null) continue;
                var parentKey = row.ParentId.Value;

                if (!m_StaticNodes.TryGetValue(row.Id, out var child) ||
                    !m_StaticNodes.TryGetValue(parentKey, out var parent))
                    continue;

                child.SetParent(parent);
                parent.AddChild(child);
            }

            // 注入变更追踪回调
            foreach (var node in m_StaticNodes.Values)
                node.OnTotalCountChanged = OnNodeTotalCountChanged;

            // 加载已读状态
            LoadReadState();

            FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {m_StaticNodes.Count}");
        }

        protected internal override void OnDispose()
        {
            // 清理事件订阅
            foreach (var eventId in m_EventToNodes.Keys)
            {
                GlobalModule.EventModule.Unsubscribe(eventId, OnLeafTriggerEvent);
            }
            m_EventToNodes.Clear();

            // 回收节点
            foreach (var node in m_StaticNodes.Values)
                ReferencePool.Release(node);
            foreach (var node in m_DynamicNodes.Values)
                ReferencePool.Release(node);

            m_StaticNodes.Clear();
            m_DynamicNodes.Clear();
            m_DirtyNodes.Clear();
            m_ChangedStaticSet.Clear();
            m_ChangedDynamicSet.Clear();
            m_InstanceNodes.Clear();
            m_ReadSet.Clear();
            Instance = null;
        }

        /// <summary>
        /// 每帧批处理：重算脏节点 → 聚合 → 广播变更
        /// </summary>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 处理脏节点
            if (m_DirtyNodes.Count > 0)
            {
                foreach (var node in m_DirtyNodes)
                {
                    node.IsDirty = false;

                    if (node.LeafProvider == null) continue;

                    try
                    {
                        var newCount = node.LeafProvider.Invoke();
                        if (node.IsRead) newCount = 0;
                        node.SetCount(newCount);
                    }
                    catch (Exception ex)
                    {
                        FuLogger.LogError($"[RedDotModule] LeafProvider 执行异常: {ex.Message}");
                    }
                }

                m_DirtyNodes.Clear();
            }

            // 广播所有本帧累积的变更（脏节点 + MarkRead/RefreshInstance 等）
            if (m_ChangedStaticSet.Count > 0 || m_ChangedDynamicSet.Count > 0)
            {
                BroadcastChangedIds();
            }
        }

        // ========== Leaf Provider 注册 ==========

        /// <summary>
        /// 为静态节点注册 Leaf Provider（由业务模块调用）
        /// </summary>
        /// <param name="key">静态节点 Key</param>
        /// <param name="provider">返回红点数量的计算函数</param>
        /// <param name="triggerEvents">触发重算的 EventModule 事件 ID 列表（可变参数）</param>
        public void RegisterLeaf(ERedDotKey key, Func<int> provider, params string[] triggerEvents)
        {
            if (!m_StaticNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogError($"[RedDotModule] RegisterLeaf 未找到静态节点: {key}");
                return;
            }

            RegisterLeafInternal(node, provider, triggerEvents);
        }

        /// <summary>
        /// 为动态节点注册 Leaf Provider
        /// </summary>
        public void RegisterLeaf(string key, Func<int> provider, params string[] triggerEvents)
        {
            if (!m_DynamicNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogError($"[RedDotModule] RegisterLeaf 未找到动态节点: {key}");
                return;
            }

            RegisterLeafInternal(node, provider, triggerEvents);
        }

        /// <summary>
        /// 注册 Leaf Provider（内部实现）
        /// </summary>
        private void RegisterLeafInternal(RedDotNode node, Func<int> provider, string[] triggerEvents)
        {
            // 先注销旧的 Leaf Provider
            UnregisterLeafInternal(node);

            node.LeafProvider = provider;
            node.TriggerEvents = triggerEvents;

            if (triggerEvents == null || triggerEvents.Length == 0) return;

            // 订阅 EventModule 事件
            foreach (var eventId in triggerEvents)
            {
                if (string.IsNullOrEmpty(eventId)) continue;

                if (!m_EventToNodes.TryGetValue(eventId, out var nodeSet))
                {
                    nodeSet = new HashSet<RedDotNode>();
                    m_EventToNodes[eventId] = nodeSet;
                }

                if (nodeSet.Count == 0)
                {
                    // 首次订阅此事件 ID
                    GlobalModule.EventModule.Subscribe(eventId, OnLeafTriggerEvent);
                }

                nodeSet.Add(node);
            }
        }

        /// <summary>
        /// 注销静态节点的 Leaf Provider
        /// </summary>
        public void UnregisterLeaf(ERedDotKey key)
        {
            if (m_StaticNodes.TryGetValue(key, out var node))
                UnregisterLeafInternal(node);
        }

        /// <summary>
        /// 注销动态节点的 Leaf Provider
        /// </summary>
        public void UnregisterLeaf(string key)
        {
            if (m_DynamicNodes.TryGetValue(key, out var node))
                UnregisterLeafInternal(node);
        }

        /// <summary>
        /// 注销 Leaf Provider（内部实现）
        /// </summary>
        private void UnregisterLeafInternal(RedDotNode node)
        {
            if (node.TriggerEvents != null)
            {
                foreach (var eventId in node.TriggerEvents)
                {
                    if (string.IsNullOrEmpty(eventId)) continue;

                    if (m_EventToNodes.TryGetValue(eventId, out var nodeSet))
                    {
                        nodeSet.Remove(node);
                        if (nodeSet.Count == 0)
                        {
                            GlobalModule.EventModule.Unsubscribe(eventId, OnLeafTriggerEvent);
                            m_EventToNodes.Remove(eventId);
                        }
                    }
                }
            }

            node.LeafProvider = null;
            node.TriggerEvents = null;
        }

        /// <summary>
        /// EventModule 事件触发回调 — 标记对应节点为脏
        /// </summary>
        private void OnLeafTriggerEvent(object sender, GameEventArgs e)
        {
            if (m_EventToNodes.TryGetValue(e.Id, out var nodeSet))
            {
                foreach (var node in nodeSet)
                {
                    if (node.IsDirty) continue;
                    node.IsDirty = true;
                    m_DirtyNodes.Add(node);
                }
            }
        }

        // ========== 状态查询 ==========

        /// <summary>
        /// 查询静态节点状态
        /// </summary>
        public RedDotState GetState(ERedDotKey key)
        {
            if (!m_StaticNodes.TryGetValue(key, out var node))
                return RedDotState.Empty;

            return new RedDotState
            {
                Count = node.GetFinalCount(),
                ShowOrder = node.ShowOrder,
                IsActive = node.IsActive,
                DisplayMode = node.DisplayMode
            };
        }

        /// <summary>
        /// 查询动态节点状态
        /// </summary>
        public RedDotState GetState(string key)
        {
            if (!m_DynamicNodes.TryGetValue(key, out var node))
                return RedDotState.Empty;

            return new RedDotState
            {
                Count = node.GetFinalCount(),
                ShowOrder = node.ShowOrder,
                IsActive = node.IsActive,
                DisplayMode = node.DisplayMode
            };
        }

        /// <summary>
        /// 是否存在静态节点
        /// </summary>
        public bool HasNode(ERedDotKey key) => m_StaticNodes.ContainsKey(key);

        /// <summary>
        /// 是否存在动态节点
        /// </summary>
        public bool HasNode(string key) => m_DynamicNodes.ContainsKey(key);

        // ========== 动态节点 ==========

        /// <summary>
        /// 为指定静态父节点添加动态子节点
        /// </summary>
        /// <param name="parentKey">静态父节点 Key</param>
        /// <param name="childName">动态子节点名称</param>
        /// <returns>创建的动态节点，父节点不存在时返回 null</returns>
        public RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)
        {
            if (!m_StaticNodes.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] 父节点不存在: {parentKey}");
                return null;
            }

            if (m_DynamicNodes.TryGetValue(childName, out var dynamicNode))
                return dynamicNode;

            var node = RedDotNode.CreateDynamic(childName, parentNode);
            node.OnTotalCountChanged = OnNodeTotalCountChanged;
            parentNode.AddChild(node);
            m_DynamicNodes.Add(childName, node);
            FuLogger.LogInfo($"[RedDotModule] 创建动态节点: {childName}，父节点: {parentKey}");
            return node;
        }

        /// <summary>
        /// 移除动态节点（清理 Leaf Provider 和事件订阅后回收）
        /// </summary>
        public void RemoveDynamicChild(string childName)
        {
            if (!m_DynamicNodes.TryGetValue(childName, out var node)) return;

            var parent = node.Parent;
            UnregisterLeafInternal(node);
            parent?.RemoveChild(node);
            m_DynamicNodes.Remove(childName);
            ReferencePool.Release(node);

            // 移除子节点后触发父节点重算
            parent?.ForceRecalculate();
        }

    }
}
