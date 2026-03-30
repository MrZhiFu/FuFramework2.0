using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.RedDot.Runtime
{
    /// <summary>
    /// 红点系统管理器
    /// 
    /// 主要功能：
    /// 1. 树形结构管理 - 支持父子节点层级关系，自动计算总计数
    /// 2. 事件通知机制 - 计数变化时自动通知所有监听者
    /// 3. 对象池管理 - 使用引用池减少GC分配
    /// 4. 配置化驱动 - 通过配置RedDotSetting(ScriptableObject)初始化红点树结构
    /// 
    /// 使用流程：
    /// 1. 在配置RedDotSetting(ScriptableObject)中定义红点树结构
    /// 2. 系统启动时自动构建节点树
    /// 3. 业务逻辑调用接口设置红点计数，如：RedDotModule.Instance.SetCount("node1", 10)
    /// 4. UI组件注册监听并更新显示状态，如：RedDotModule.Instance.Register("node1", (count) => { ui.SetText(count); })
    /// </summary>
    public class RedDotModule : FuModule
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;

        /// <summary>
        /// 存储所有节点的字典，key：节点的key，value：节点对象
        /// </summary>
        private static readonly Dictionary<string, RedDotNode> NodeDict = new();

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            // 读取红点树配置
            var redDotSetting = ModuleSetting.Runtime.ModuleSetting.Instance.RedDotSetting;

            if (redDotSetting == null)
            {
                FuLogger.LogError("[RedDotModule] 红点树配置文件不存在.");
                return;
            }
            
            NodeDict.Clear();
            foreach (var root in redDotSetting.m_RootNodes)
            {
                BuildNodeRecursive(null, root);
            }

            FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {NodeDict.Count}");
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型</param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            // 释放所有节点回对象池
            foreach (var node in NodeDict.Values)
            {
                ReferencePool.Runtime.ReferencePool.Release(node);
            }

            NodeDict.Clear();
        }


        /// <summary>
        /// 递归构建节点
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="data">节点数据</param>
        private void BuildNodeRecursive(RedDotNode parent, RedDotNodeData data)
        {
            var node = RedDotNode.Create(data.m_Key, parent);

            if (!NodeDict.TryAdd(data.m_Key, node))
            {
                FuLogger.LogError($"[RedDotModule] 重复的节点key: {data.m_Key}");
                ReferencePool.Runtime.ReferencePool.Release(node);
                return;
            }

            parent?.AddChild(node);
            if (data.m_Children == null) return;

            foreach (var child in data.m_Children)
            {
                BuildNodeRecursive(node, child);
            }
        }

        
        /// <summary>
        /// 注册节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        /// <param name="immediateNotify">是否立即通知当前状态</param>
        public void Register(string key, Action<int> onChange, bool immediateNotify = true)
        {
            if (!NodeDict.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] 注册监听时未找到节点: {key}");
                return;
            }

            node.OnCountChanged += onChange;

            // 可选立即通知当前状态
            if (immediateNotify)
                onChange?.Invoke(node.TotalCount);
        }

        /// <summary>
        /// 注销节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        public void Unregister(string key, Action<int> onChange)
        {
            if (!NodeDict.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] 移除监听时未找到节点: {key}");
                return;
            }

            node.OnCountChanged -= onChange;
        }

        /// <summary>
        /// 注销指定key的所有回调函数
        /// </summary>
        public void UnregisterAll(string key)
        {
            if (NodeDict.TryGetValue(key, out var node))
            {
                node.ClearAllListeners();
            }
        }

        /// <summary>
        /// 清理所有节点的监听器
        /// </summary>
        public void ClearAllListeners()
        {
            foreach (var node in NodeDict.Values)
            {
                node.ClearAllListeners();
            }
        }

        #region Get

        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="key">节点的key</param>
        public RedDotNode GetNode(string key) => NodeDict.GetValueOrDefault(key);

        /// <summary>
        /// 获取节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        public int GetCount(string key) => NodeDict.TryGetValue(key, out var node) ? node.TotalCount : 0;

        /// <summary>
        /// 是否存在节点
        /// </summary>
        /// <param name="key">节点的key</param>
        public bool HasNode(string key) => NodeDict.ContainsKey(key);

        #endregion

        #region Set

        /// <summary>
        /// 设置节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="count">红点数量</param>
        public void SetCount(string key, int count)
        {
            if (!NodeDict.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] 未找到节点: {key}");
                return;
            }

            node.SetCount(count);
        }

        /// <summary>
        /// 递增节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="value">递增的数量</param>
        public void IncrementCount(string key, int value = 1)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            node.SetCount(node.RawCount + value);
        }

        /// <summary>
        /// 递减节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="value">递减的数量</param>
        public void DecrementCount(string key, int value = 1)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            node.SetCount(Math.Max(0, node.RawCount - value));
        }

        /// <summary>
        /// 重置节点的红点数量为0
        /// 适用于清除红点状态，如阅读所有邮件后重置
        /// </summary>
        /// <param name="key">节点路径</param>
        public void ResetCount(string key) => SetCount(key, 0);

        /// <summary>
        /// 批量重置多个节点的红点数量
        /// 适用于同时清除多个相关红点
        /// </summary>
        public void ResetCounts(params string[] keys)
        {
            foreach (var key in keys)
            {
                ResetCount(key);
            }
        }

        #endregion
    }
}