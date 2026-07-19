using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;
using AOT.Framework.Core.Log;
using Hotfix.Config;
using Hotfix.Config.Tables;
using Hotfix.ModuleConfig;

namespace Hotfix.RedDot
{
    /// <summary>
    /// 红点管理模块
    /// 功能：
    ///     1. 树形结构管理 - 支持父子节点层级关系，自动计算总计数
    ///     2. 事件通知机制 - 计数变化时自动通知所有监听者
    ///     3. 对象池管理 - 使用引用池减少GC分配
    ///     4. 配置化驱动 - 通过 Luban 配置表 TbRedDot 初始化红点树结构
    ///     5. 静态+动态节点 - 静态节点由配置表定义(ERedDotKey枚举)，动态节点运行时创建(string)
    ///     6. 清理策略 - 支持 Manual(手动清除) 和 ViewAutoClean(界面关闭自动清除)
    ///
    /// 使用流程：
    ///     1. 在 Luban 配置表 R-RedDot-红点表.xlsx 中定义红点树结构
    ///     2. 系统启动时自动构建节点树
    ///     3. 业务逻辑调用接口设置红点计数，如：RedDotModule.Instance.SetCount(ERedDotKey.Bag, 10)
    ///     4. UI组件注册监听并更新显示状态，如：RedDotModule.Instance.Register(ERedDotKey.Bag, (count) => { ... })
    /// </summary>
    public class RedDotModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static RedDotModule Instance { get; private set; }

        /// <summary>
        /// 静态节点字典，key: ERedDotKey，value: 节点对象
        /// </summary>
        private static readonly Dictionary<ERedDotKey, RedDotNode> StaticNodes = new();

        /// <summary>
        /// 动态节点字典，key: string，value: 节点对象
        /// </summary>
        private static readonly Dictionary<string, RedDotNode> DynamicNodes = new();

        /// <summary>
        /// 红点树是否已构建（延迟初始化，等待 ConfigModule 和配置表就绪）
        /// </summary>
        private bool _isTreeBuilt = false;

        /// <summary>
        /// 初始化（注册时调用，此时 ConfigModule 和配置表可能尚未就绪）
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            foreach (var node in StaticNodes.Values)
                ReferencePool.Release(node);
            foreach (var node in DynamicNodes.Values)
                ReferencePool.Release(node);

            StaticNodes.Clear();
            DynamicNodes.Clear();
            _isTreeBuilt = false;
            Instance = null;
        }

        /// <summary>
        /// 确保红点树已构建（延迟初始化，首次访问时从配置表构建）
        /// </summary>
        private void EnsureTreeBuilt()
        {
            if (_isTreeBuilt) return;
            _isTreeBuilt = true;

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
                var node = RedDotNode.Create(row.Id, null, row.DisplayMode, row.CleanStrategy);

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

            FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {StaticNodes.Count}");
        }

        #region 动态节点

        /// <summary>
        /// 为指定静态父节点添加动态子节点
        /// </summary>
        /// <param name="parentKey">静态父节点 Key</param>
        /// <param name="childName">动态子节点名称</param>
        /// <returns>创建的动态节点，父节点不存在时返回 null</returns>
        public RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] 父节点不存在: {parentKey}");
                return null;
            }

            if (DynamicNodes.ContainsKey(childName))
                return DynamicNodes[childName];

            var node = RedDotNode.CreateDynamic(childName, parentNode);
            parentNode.AddChild(node);
            DynamicNodes.Add(childName, node);
            FuLogger.LogInfo($"[RedDotModule] 创建动态节点: {childName}，父节点: {parentKey}");
            return node;
        }

        #endregion

        #region 清理策略

        /// <summary>
        /// 尝试自动清除红点（仅对 ViewAutoClean 策略的节点生效）
        /// 业务代码在合适时机调用，如点击页签后清除该页签下的红点
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void TryAutoClean(ERedDotKey key)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(key, out var node)) return;
            if (node.CleanStrategy != ERedDotCleanStrategy.ViewAutoClean) return;
            CleanNodeRecursive(node);
        }

        /// <summary>
        /// 递归清除节点及所有子节点
        /// </summary>
        private void CleanNodeRecursive(RedDotNode node)
        {
            node.SetCount(0);
            foreach (var child in node.GetChildren())
                CleanNodeRecursive(child);
        }

        #endregion

        #region 静态节点 API（ERedDotKey 重载）

        /// <summary>
        /// 注册节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点的 Key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        /// <param name="immediateNotify">是否立即通知当前状态</param>
        public void Register(ERedDotKey key, Action<int> onChange, bool immediateNotify = true)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] 注册监听时未找到静态节点: {key}");
                return;
            }

            node.OnCountChanged += onChange;

            if (immediateNotify)
                onChange?.Invoke(node.TotalCount);
        }

        /// <summary>
        /// 注销节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点的 Key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        public void Unregister(ERedDotKey key, Action<int> onChange)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(key, out var node)) return;
            node.OnCountChanged -= onChange;
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="key">节点的 Key</param>
        public RedDotNode GetNode(ERedDotKey key)
        {
            EnsureTreeBuilt();
            return StaticNodes.GetValueOrDefault(key);
        }

        /// <summary>
        /// 获取节点的红点数量
        /// </summary>
        /// <param name="key">节点的 Key</param>
        public int GetCount(ERedDotKey key)
        {
            EnsureTreeBuilt();
            return StaticNodes.TryGetValue(key, out var node) ? node.TotalCount : 0;
        }

        /// <summary>
        /// 是否存在节点
        /// </summary>
        /// <param name="key">节点的 Key</param>
        public bool HasNode(ERedDotKey key)
        {
            EnsureTreeBuilt();
            return StaticNodes.ContainsKey(key);
        }

        /// <summary>
        /// 设置节点的红点数量
        /// </summary>
        /// <param name="key">节点的 Key</param>
        /// <param name="count">红点数量</param>
        public void SetCount(ERedDotKey key, int count)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] SetCount 未找到静态节点: {key}");
                return;
            }

            node.SetCount(count);
        }

        /// <summary>
        /// 增加节点的红点数量
        /// </summary>
        /// <param name="key">节点的 Key</param>
        /// <param name="value">递增的数量</param>
        public void AddCount(ERedDotKey key, int value = 1)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(key, out var node)) return;
            node.SetCount(node.RawCount + value);
        }

        /// <summary>
        /// 减少节点的红点数量
        /// </summary>
        /// <param name="key">节点的 Key</param>
        /// <param name="value">递减的数量</param>
        public void SubCount(ERedDotKey key, int value = 1)
        {
            EnsureTreeBuilt();

            if (!StaticNodes.TryGetValue(key, out var node)) return;
            node.SetCount(Math.Max(0, node.RawCount - value));
        }

        /// <summary>
        /// 重置节点的红点数量为 0
        /// </summary>
        /// <param name="key">节点的 Key</param>
        public void ResetCount(ERedDotKey key)
        {
            EnsureTreeBuilt();
            SetCount(key, 0);
        }

        #endregion

        #region 动态节点 API（string 重载）

        /// <summary>
        /// 注册动态节点状态变化的回调函数
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        /// <param name="immediateNotify">是否立即通知当前状态</param>
        public void Register(string key, Action<int> onChange, bool immediateNotify = true)
        {
            EnsureTreeBuilt();

            if (!DynamicNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] 注册监听时未找到动态节点: {key}");
                return;
            }

            node.OnCountChanged += onChange;

            if (immediateNotify)
                onChange?.Invoke(node.TotalCount);
        }

        /// <summary>
        /// 注销动态节点状态变化的回调函数
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        public void Unregister(string key, Action<int> onChange)
        {
            EnsureTreeBuilt();

            if (!DynamicNodes.TryGetValue(key, out var node)) return;
            node.OnCountChanged -= onChange;
        }

        /// <summary>
        /// 注销动态节点所有回调函数
        /// </summary>
        public void UnregisterAll(string key)
        {
            EnsureTreeBuilt();

            if (DynamicNodes.TryGetValue(key, out var node))
            {
                node.ClearAllListeners();
            }
        }

        /// <summary>
        /// 获取动态节点
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        public RedDotNode GetNode(string key)
        {
            EnsureTreeBuilt();
            return DynamicNodes.GetValueOrDefault(key);
        }

        /// <summary>
        /// 获取动态节点的红点数量
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        public int GetCount(string key)
        {
            EnsureTreeBuilt();
            return DynamicNodes.TryGetValue(key, out var node) ? node.TotalCount : 0;
        }

        /// <summary>
        /// 是否存在动态节点
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        public bool HasNode(string key)
        {
            EnsureTreeBuilt();
            return DynamicNodes.ContainsKey(key);
        }

        /// <summary>
        /// 设置动态节点的红点数量
        /// 计数归零时自动从 DynamicNodes 移除并回收节点
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        /// <param name="count">红点数量</param>
        public void SetCount(string key, int count)
        {
            EnsureTreeBuilt();

            if (!DynamicNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] SetCount 未找到动态节点: {key}");
                return;
            }

            node.SetCount(count);

            // 归零自动回收
            if (node.RawCount == 0)
            {
                node.Parent?.RemoveChild(node);
                DynamicNodes.Remove(key);
                ReferencePool.Release(node);
            }
        }

        /// <summary>
        /// 增加动态节点的红点数量
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        /// <param name="value">递增的数量</param>
        public void AddCount(string key, int value = 1)
        {
            EnsureTreeBuilt();

            if (!DynamicNodes.TryGetValue(key, out var node)) return;
            node.SetCount(node.RawCount + value);
        }

        /// <summary>
        /// 减少动态节点的红点数量
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        /// <param name="value">递减的数量</param>
        public void SubCount(string key, int value = 1)
        {
            EnsureTreeBuilt();

            if (!DynamicNodes.TryGetValue(key, out var node)) return;
            node.SetCount(Math.Max(0, node.RawCount - value));
        }

        /// <summary>
        /// 重置动态节点的红点数量为 0
        /// </summary>
        /// <param name="key">动态节点的 Key</param>
        public void ResetCount(string key)
        {
            EnsureTreeBuilt();
            SetCount(key, 0);
        }

        #endregion

        #region 通用 API

        /// <summary>
        /// 清理所有节点的监听器
        /// </summary>
        public void ClearAllListeners()
        {
            EnsureTreeBuilt();

            foreach (var node in StaticNodes.Values)
                node.ClearAllListeners();
            foreach (var node in DynamicNodes.Values)
                node.ClearAllListeners();
        }

        #endregion
    }
}
