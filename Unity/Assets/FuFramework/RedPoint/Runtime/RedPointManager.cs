using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;

namespace FuFramework.RedPoint.Runtime
{
    public class RedPointManager : FuModule
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;

        /// <summary>
        /// 存储所有节点的字典，key：节点的key，value：节点对象
        /// </summary>
        private static readonly Dictionary<string, RedPointNode> NodeDict = new();

        /// <summary>
        /// 存储节点状态变化的回调函数字典，key：节点的key，value：回调函数列表()
        /// </summary>
        private static readonly Dictionary<string, List<Action<int>>> BindingDict = new();


        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            // 读取红点树配置
            var redPointSetting = ModuleSetting.Runtime.ModuleSetting.Instance.RedPointSetting;

            NodeDict.Clear();
            BindingDict.Clear();
            foreach (var root in redPointSetting.m_RootNodes)
            {
                BuildNodeRecursive(null, root);
            }
            
            FuLog.Info("RedPointManager init success.");
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型</param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            NodeDict.Clear();
            BindingDict.Clear();
        }
        
        /// <summary>
        /// 递归构建节点树
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="data">节点数据</param>
        private static void BuildNodeRecursive(RedPointNode parent, RedPointNodeData data)
        {
            var node = new RedPointNode(data.m_Key, parent);

            NodeDict[data.m_Key] = node;
            parent?.AddChild(node);

            if (data.m_Children == null) return;
            
            // 递归构建子节点
            foreach (var child in data.m_Children)
            {
                BuildNodeRecursive(node, child);
            }
        }

        /// <summary>
        /// 设置节点的红点数量
        /// </summary>
        /// <param name="key">节点key</param>
        /// <param name="count">红点数量</param>
        public static void SetCount(string key, int count)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            node.SetCount(count);
        }
        
        /// <summary>
        /// 注册节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点key</param>
        /// <param name="onChange">回调函数</param>
        public static void Register(string key, Action<int> onChange)
        {
            if (!BindingDict.TryGetValue(key, out var actions))
            {
                actions = new List<Action<int>>();
                BindingDict[key] = actions;
            }

            if (!actions.Contains(onChange))
            {
                actions.Add(onChange);
            }

            // 如果节点已经存在，则通知回调函数节点状态变化
            if (NodeDict.TryGetValue(key, out var node))
            {
                var count = node.GetCount();
                onChange?.Invoke(count);
            }
        }

        /// <summary>
        /// 注销节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点key</param>
        /// <param name="onChange">回调函数</param>
        public static void Unregister(string key, Action<int> onChange)
        {
            if (BindingDict.TryGetValue(key, out var actions))
            {
                actions.Remove(onChange);
            }
        }

        /// <summary>
        /// 通知节点状态变化
        /// </summary>
        /// <param name="node">状态变化的节点对象</param>
        public static void NotifyStateChanged(RedPointNode node)
        {
            if (!BindingDict.TryGetValue(node.Key, out var list)) return;
            
            foreach (var onChange in list)
            {
                var count = node.GetCount();
                onChange?.Invoke(count);
            }
        }
    }
}