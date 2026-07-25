using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.ReferencePools;
using Hotfix.Framework.Storage;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using AOT.Framework.Core.Log;

namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点管理模块（Pull 模式 — Leaf Provider 驱动）
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
    ///     1. 在 Luban 配置表中定义红点树结构
    ///     2. 业务模块调用 RegisterLeaf 注册计算函数
    ///     3. 业务触发事件 → OnUpdate 重算 → EventModule 广播变更
    ///     4. CompRedDot 监听广播事件，按 ID 过滤刷新 UI
    /// </summary>
    public class RedDotModule : ModuleBase
    {
        public static RedDotModule Instance { get; private set; }

        // ========== 节点存储 ==========

        private static readonly Dictionary<ERedDotKey, RedDotNode> StaticNodes = new();
        private static readonly Dictionary<string, RedDotNode> DynamicNodes = new();

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

        private const string ReadStorageKey = "ReadSet";
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

            StaticNodes.Clear();
            DynamicNodes.Clear();

            var allRows = tbRedDot.All;

            // 阶段一：创建所有节点
            foreach (var row in allRows)
            {
                var node = RedDotNode.Create(row.Id, null, row.DisplayMode, row.CleanStrategy,
                    row.LogicType, row.IsActive, row.ShowOrder);

                if (!StaticNodes.TryAdd(row.Id, node))
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

                if (!StaticNodes.TryGetValue(row.Id, out var child) ||
                    !StaticNodes.TryGetValue(parentKey, out var parent))
                    continue;

                child.SetParent(parent);
                parent.AddChild(child);
            }

            // 注入变更追踪回调
            foreach (var node in StaticNodes.Values)
                node.OnTotalCountChanged = OnNodeTotalCountChanged;

            // 加载已读状态
            LoadReadState();

            FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {StaticNodes.Count}");
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
            foreach (var node in StaticNodes.Values)
                ReferencePool.Release(node);
            foreach (var node in DynamicNodes.Values)
                ReferencePool.Release(node);

            StaticNodes.Clear();
            DynamicNodes.Clear();
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
            if (m_DirtyNodes.Count == 0) return;

            m_ChangedStaticSet.Clear();
            m_ChangedDynamicSet.Clear();

            // 收集所有需要从脏集合移除的节点（避免迭代时修改集合）
            var processedNodes = new List<RedDotNode>(m_DirtyNodes.Count);
            foreach (var node in m_DirtyNodes)
            {
                node.IsDirty = false;
                processedNodes.Add(node);

                // 仅 Leaf Provider 节点需要重算
                if (node.LeafProvider == null) continue;

                var newCount = node.LeafProvider.Invoke();
                if (node.IsRead) newCount = 0;
                node.SetCount(newCount);
            }

            m_DirtyNodes.Clear();

            // 批量广播（SetCount/UpdateTotalCount 过程中 OnNodeTotalCountChanged 已收集变更 Key）
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
            if (!StaticNodes.TryGetValue(key, out var node))
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
            if (!DynamicNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogError($"[RedDotModule] RegisterLeaf 未找到动态节点: {key}");
                return;
            }

            RegisterLeafInternal(node, provider, triggerEvents);
        }

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
            if (StaticNodes.TryGetValue(key, out var node))
                UnregisterLeafInternal(node);
        }

        /// <summary>
        /// 注销动态节点的 Leaf Provider
        /// </summary>
        public void UnregisterLeaf(string key)
        {
            if (DynamicNodes.TryGetValue(key, out var node))
                UnregisterLeafInternal(node);
        }

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
            node.InstanceLeafProvider = null;
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
                    node.PreviousTotalCount = node.TotalCount;
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
            if (!StaticNodes.TryGetValue(key, out var node))
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
            if (!DynamicNodes.TryGetValue(key, out var node))
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
        public bool HasNode(ERedDotKey key) => StaticNodes.ContainsKey(key);

        /// <summary>
        /// 是否存在动态节点
        /// </summary>
        public bool HasNode(string key) => DynamicNodes.ContainsKey(key);

        // ========== 动态节点 ==========

        /// <summary>
        /// 为指定静态父节点添加动态子节点
        /// </summary>
        /// <param name="parentKey">静态父节点 Key</param>
        /// <param name="childName">动态子节点名称</param>
        /// <returns>创建的动态节点，父节点不存在时返回 null</returns>
        public RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)
        {
            if (!StaticNodes.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] 父节点不存在: {parentKey}");
                return null;
            }

            if (DynamicNodes.ContainsKey(childName))
                return DynamicNodes[childName];

            var node = RedDotNode.CreateDynamic(childName, parentNode);
            node.OnTotalCountChanged = OnNodeTotalCountChanged;
            parentNode.AddChild(node);
            DynamicNodes.Add(childName, node);
            FuLogger.LogInfo($"[RedDotModule] 创建动态节点: {childName}，父节点: {parentKey}");
            return node;
        }

        /// <summary>
        /// 移除动态节点（清理 Leaf Provider 和事件订阅后回收）
        /// </summary>
        public void RemoveDynamicChild(string childName)
        {
            if (!DynamicNodes.TryGetValue(childName, out var node)) return;

            UnregisterLeafInternal(node);
            node.Parent?.RemoveChild(node);
            DynamicNodes.Remove(childName);
            ReferencePool.Release(node);
        }

        // ========== 实例红点 ==========

        /// <summary>
        /// 同步实例红点集合：根据当前 key 列表增删实例
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="instanceKeys">当前活跃的实例 Key 列表</param>
        /// <param name="providerFactory">根据 instanceKey 创建 Leaf Provider 的工厂</param>
        public void SyncInstances(ERedDotKey parentKey, IReadOnlyList<long> instanceKeys,
            Func<long, Func<int>> providerFactory)
        {
            if (!StaticNodes.TryGetValue(parentKey, out var parentNode))
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
            for (int i = 0; i < instanceKeys.Count; i++)
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
            for (int i = 0; i < instanceKeys.Count; i++)
            {
                long instanceKey = instanceKeys[i];
                if (existingInstances.ContainsKey(instanceKey)) continue;

                var childName = $"__inst__{parentKey}_{instanceKey}";
                var node = AddDynamicChild(parentKey, childName);
                if (node != null)
                {
                    var provider = providerFactory(instanceKey);
                    RegisterLeafInternal(node, provider, null);
                    existingInstances[instanceKey] = childName;
                }
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
            if (!DynamicNodes.TryGetValue(childName, out var node)) return;
            if (node.LeafProvider == null) return;

            var newCount = node.LeafProvider.Invoke();
            if (node.IsRead) newCount = 0;
            node.SetCount(newCount);
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

            m_ChangedStaticSet.Clear();
            m_ChangedDynamicSet.Clear();

            foreach (var kvp in instances)
            {
                if (DynamicNodes.TryGetValue(kvp.Value, out var node) && node.LeafProvider != null)
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
            if (!StaticNodes.TryGetValue(key, out var node)) return;

            node.IsRead = true;
            node.SetCount(0);
            m_ReadSet.Add((int)key);
            SaveReadState();
        }

        /// <summary>
        /// 标记红点已读
        /// </summary>
        public void MarkRead(string key)
        {
            if (!DynamicNodes.TryGetValue(key, out var node)) return;

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
            return DynamicNodes.TryGetValue(key, out var node) && node.IsRead;
        }

        private void LoadReadState()
        {
            var list = StorageModule.Instance.GetObject<List<int>>(ReadStorageKey, ReadStorageFile);
            if (list == null) return;

            foreach (var id in list)
            {
                m_ReadSet.Add(id);
                var key = (ERedDotKey)id;
                if (StaticNodes.TryGetValue(key, out var node))
                {
                    node.IsRead = true;
                    node.SetCount(0);
                }
            }
        }

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
            if (!StaticNodes.TryGetValue(key, out var node)) return;
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
        }
    }
}
