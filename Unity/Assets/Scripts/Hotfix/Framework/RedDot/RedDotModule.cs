using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.Config;
using Hotfix.Framework.Storage;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点管理模块
    /// 功能：
    ///     1. 树形结构管理 — 支持父子节点层级关系，LogicType(Any/Sum)控制聚合
    ///     2. 节点红点计算函数 — Calculator，业务注册 Func&lt;int&gt; 在OnUpdate 批处理重算
    ///     3. 批量广播 — 每帧统一广播 RedDotChangedEventArgs，UI 端按 Key 过滤
    ///     4. 配置化驱动 — 通过 Luban 配置表 TbRedDot 初始化红点树结构
    ///     5. 动态红点 — SyncDynamicNode 批量管理动态子节点
    ///     6. 静态红点已读持久化 — MarkRead 通过 StorageModule 持久保存已读状态。
    ///        动态红点不持久化，原因：
    ///        - 动态节点由 SyncDynamicNode 批量管理，生命周期随数据列表增删
    ///        - 下次登录数据从服务器重新拉取，本地缓存的已读状态无意义
    ///        - 每条数据的已读/未读状态应由服务端维护，客户端无需冗余存储
    ///
    /// 使用流程：
    ///     1. 在Luban配置表中定义红点树结构
    ///     2. 在UI界面中创建CompRedDot组件并绑定红点枚举
    ///     3. 业务模块调用Register注册计算函数
    ///     4. 业务触发事件 → OnUpdate 重算 → 广播红点变更事件
    ///     5. UI组件CompRedDot监听广播事件，按Key过滤自动刷新
    /// </summary>
    public partial class RedDotModule : ModuleBase
    {
        /// <summary>
        /// 模块静态单例
        /// </summary>
        public static RedDotModule Instance { get; private set; }

        /// <summary>
        /// 所有红点字典(Key: RedDotKey，Value: 红点节点)
        /// </summary>
        private static readonly Dictionary<RedDotKey, RedDotNode> NodeDict = new();

        /// <summary>
        /// 动态子节点字典(Key：父节点，Value：父节点下的所有动态子节点集合)(用于 SyncDynamicNode 增量更新)
        /// </summary>
        private readonly Dictionary<RedDotKey, HashSet<long>> m_DynamicIdDict = new();


        /// <summary>
        /// 本帧待重算的脏节点集合，用于批量重算节点
        /// </summary>
        private readonly HashSet<RedDotNode> m_DirtyNodeSet = new();

        /// <summary>
        /// 本帧发生变更的节点 Key 集合(用于去重后广播)
        /// </summary>
        private readonly HashSet<RedDotKey> m_ChangedKeySet = new();


        /// <summary>
        /// 事件对应的节点字典 (key：事件ID，Value：订阅此事件的节点集合)(用于反订阅节点的事件))
        /// </summary>
        private readonly Dictionary<string, HashSet<RedDotNode>> m_EventToNodes = new();


        #region 已读持久化

        /// <summary>
        /// 已读静态节点 Key 集合(存为 ERedDotKey 的 int 值)
        /// </summary>
        private readonly HashSet<int> m_ReadSet = new();

        /// <summary>
        /// 已读状态在 StorageModule 中的 Key
        /// </summary>
        private const string ReadStorageKey = "ReadSet";

        /// <summary>
        /// 已读状态在 StorageModule 中的文件名
        /// </summary>
        private const string ReadStorageFile = "RedDotData";

        #endregion

        #region 生命周期

        /// <summary>
        /// 模块初始化：从 Luban 配置表构建红点树
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;

            var tbRedDot = ConfigModule.Instance?.GetConfig<TbRedDot>();
            if (tbRedDot == null || tbRedDot.Count == 0)
            {
                FuLogger.LogWarning("[RedDotModule] 红点配置表不存在或为空，跳过树构建.");
                return;
            }

            NodeDict.Clear();

            var allRows = tbRedDot.All;

            // 阶段一:创建所有节点
            foreach (var row in allRows)
            {
                var node = RedDotNode.Create(row);

                if (!NodeDict.TryAdd(row.Id, node))
                {
                    FuLogger.LogError($"[RedDotModule] 重复的节点key: {row.Id}");
                    GlobalModule.ReferencePoolModule.Recycle(node);
                }
            }

            // 阶段二:建立父子关系
            foreach (var row in allRows)
            {
                if (row.ParentId == null) continue;
                RedDotKey parentKey = row.ParentId.Value;

                if (!NodeDict.TryGetValue(row.Id,    out var child) ||
                    !NodeDict.TryGetValue(parentKey, out var parent))
                    continue;

                child.SetParent(parent);
                parent.AddChild(child);
            }

            // 注入变更追踪回调
            foreach (var node in NodeDict.Values)
            {
                node.OnTotalCountChanged = OnNodeTotalCountChanged;
            }

            // 检测循环依赖，发现时切断错误关系防止运行时栈溢出
            DetectCycleAndBreak();

            // 加载已读状态
            LoadReadState();

            FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {NodeDict.Count}");
        }

        /// <summary>
        /// 模块释放：清理事件订阅、回收节点
        /// </summary>
        protected internal override void OnDispose()
        {
            // 清理事件订阅
            foreach (var eventId in m_EventToNodes.Keys)
            {
                GlobalModule.EventModule.Unsubscribe(eventId, OnTriggerEvent);
            }

            m_EventToNodes.Clear();

            // 清理所有节点
            foreach (var node in NodeDict.Values)
            {
                GlobalModule.ReferencePoolModule.Recycle(node);
            }

            NodeDict.Clear();
            m_DirtyNodeSet.Clear();
            m_ChangedKeySet.Clear();
            m_DynamicIdDict.Clear();
            m_ReadSet.Clear();
            Instance = null;
        }

        /// <summary>
        /// 每帧批处理：重算脏节点 → 聚合 → 广播变更
        /// </summary>
        /// <param name="deltaTime">游戏帧间隔时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的帧间隔时间</param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 处理脏节点
            if (m_DirtyNodeSet.Count > 0)
            {
                foreach (var node in m_DirtyNodeSet)
                {
                    node.IsDirty = false;
                    if (node.Calculator == null) continue;

                    try
                    {
                        var newCount = node.Calculator.Invoke();

                        if (node.IsRead)
                            newCount = 0;

                        node.SetCount(newCount);
                    }
                    catch (Exception ex)
                    {
                        FuLogger.LogError($"[RedDotModule] 红点计算函数执行异常: {ex.Message}");
                    }
                }

                m_DirtyNodeSet.Clear();
            }

            // 广播所有本帧累积的变更(脏节点 + MarkRead 等)
            if (m_ChangedKeySet.Count > 0)
            {
                BroadcastChangedKeys();
            }
        }

        #endregion

        #region 注册

        /// <summary>
        /// 注册红点(内部实现)
        /// </summary>
        /// <param name="node">目标节点</param>
        /// <param name="calculator">红点数量计算函数</param>
        /// <param name="triggerEvents">触发重算的事件 ID 列表</param>
        private void RegisterInternal(RedDotNode node, Func<int> calculator, string[] triggerEvents)
        {
            // 先注销旧的红点
            UnregisterInternal(node);

            node.Calculator    = calculator;
            node.TriggerEvents = triggerEvents;

            if (triggerEvents == null || triggerEvents.Length == 0) return;

            // 订阅事件
            foreach (var eventId in triggerEvents)
            {
                if (string.IsNullOrEmpty(eventId)) continue;

                if (!m_EventToNodes.TryGetValue(eventId, out var nodeSet))
                {
                    nodeSet                 = new HashSet<RedDotNode>();
                    m_EventToNodes[eventId] = nodeSet;
                }

                if (nodeSet.Count == 0)
                {
                    // 首次订阅此事件 ID
                    GlobalModule.EventModule.Subscribe(eventId, OnTriggerEvent);
                }

                nodeSet.Add(node);
            }
        }

        /// <summary>
        /// 注销红点(内部实现)
        /// </summary>
        /// <param name="node">目标节点</param>
        private void UnregisterInternal(RedDotNode node)
        {
            if (node.TriggerEvents != null)
            {
                foreach (var eventId in node.TriggerEvents)
                {
                    if (string.IsNullOrEmpty(eventId)) continue;

                    if (!m_EventToNodes.TryGetValue(eventId, out var nodeSet)) continue;
                    nodeSet.Remove(node);

                    if (nodeSet.Count != 0) continue;
                    GlobalModule.EventModule.Unsubscribe(eventId, OnTriggerEvent);
                    m_EventToNodes.Remove(eventId);
                }
            }

            node.Calculator    = null;
            node.TriggerEvents = null;
        }

        /// <summary>
        ///  事件触发回调 — 标记对应节点为脏
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnTriggerEvent(object sender, GameEventArgs e)
        {
            if (!m_EventToNodes.TryGetValue(e.Id, out var nodeSet)) return;

            foreach (var node in nodeSet)
            {
                if (node.IsDirty) continue;
                node.IsDirty = true;
                m_DirtyNodeSet.Add(node);
            }
        }

        #endregion

        #region 动态节点

        /// <summary>
        /// 为指定父节点添加动态子节点
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="childKey">动态子节点 Key</param>
        /// <returns>创建的动态节点，父节点不存在时返回 null</returns>
        private RedDotNode AddDynamicChild(RedDotKey parentKey, RedDotKey childKey)
        {
            if (!NodeDict.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] 父节点不存在: {parentKey}");
                return null;
            }

            if (NodeDict.TryGetValue(childKey, out var existingNode))
                return existingNode;

            var node = RedDotNode.CreateDynamic(childKey, parentNode);
            node.OnTotalCountChanged = OnNodeTotalCountChanged;
            parentNode.AddChild(node);
            NodeDict.Add(childKey, node);
            FuLogger.LogInfo($"[RedDotModule] 创建动态节点: {childKey}，父节点: {parentKey}");
            return node;
        }

        /// <summary>
        /// 生成动态节点 Key(格式：__dynamic__{parentKey}_{id})
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="id">动态节点 ID</param>
        /// <returns>格式化后的动态节点 Key</returns>
        private static string FormatDynamicKey(RedDotKey parentKey, long id) => $"__dynamic__{parentKey}_{id}";

        #endregion

        #region 已读持久化

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
                if (!Enum.IsDefined(typeof(ERedDotKey), id))
                {
                    FuLogger.LogWarning($"[RedDotModule] 已读状态中存在无效的枚举值: {id}，已跳过");
                    continue;
                }

                m_ReadSet.Add(id);
                var       staticKey = (ERedDotKey)id;
                RedDotKey key       = staticKey;
                if (NodeDict.TryGetValue(key, out var node))
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
        /// TotalCount 变化回调(注入到每个 RedDotNode，在 UpdateTotalCount 中触发)
        /// 收集本帧变更的节点 Key，供 OnUpdate 批量广播
        /// </summary>
        /// <param name="node">发生变化的节点</param>
        private void OnNodeTotalCountChanged(RedDotNode node) => m_ChangedKeySet.Add(node.Key);

        /// <summary>
        /// 批量广播本帧变更
        /// </summary>
        private void BroadcastChangedKeys()
        {
            var args = RedDotChangedEventArgs.Create();
            foreach (var key in m_ChangedKeySet)
            {
                args.ChangedKeys.Add(key);
            }

            GlobalModule.EventModule.Broadcast(this, args);

            m_ChangedKeySet.Clear();
        }

        /// <summary>
        /// 检测静态节点中的循环依赖，发现时切断错误关系并记录错误日志
        /// </summary>
        private void DetectCycleAndBreak()
        {
            var state = new Dictionary<RedDotNode, int>();
            foreach (var node in NodeDict.Values)
            {
                state[node] = 0; // 0=未访问, 1=访问中, 2=已完成
            }

            foreach (var node in NodeDict.Values)
            {
                if (state[node] != 0) continue;
                HasCycleDFS(node, state);
            }
        }

        /// <summary>
        /// 沿父链 DFS 检测环
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="state">节点访问状态</param>
        /// <returns>是否检测到环</returns>
        private static bool HasCycleDFS(RedDotNode node, Dictionary<RedDotNode, int> state)
        {
            state[node] = 1;
            var parent = node.Parent;

            if (parent == null)
            {
                state[node] = 2;
                return false;
            }

            if (state[parent] == 1)
            {
                FuLogger.LogError($"[RedDotModule] 检测到循环依赖: {node.Key} -> {parent.Key}，切断该关系");
                parent.RemoveChild(node);
                node.SetParent(null);
                state[node] = 2;
                return true;
            }

            if (state[parent] == 0)
            {
                HasCycleDFS(parent, state);
            }

            state[node] = 2;
            return false;
        }

        #endregion
    }
}