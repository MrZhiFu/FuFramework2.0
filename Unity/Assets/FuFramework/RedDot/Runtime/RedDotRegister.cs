using System;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.RedDot.Runtime
{
    /// <summary>
    /// 红点注册器。
    /// 可用于单独管理属于自己模块的相关红点
    /// </summary>
    public class RedDotRegister : IReference
    {
        /// <summary>
        /// 红点管理器
        /// </summary>
        private readonly RedDotManager m_RedDotManager = ModuleManager.GetModule<RedDotManager>();

        /// <summary>
        /// 记录所有红点节点key的列表
        /// </summary>
        private readonly List<string> m_RedNodeKeyList = new();

        /// <summary>
        /// 创建红点注册器
        /// </summary>
        /// <returns></returns>
        public static RedDotRegister Create() => ReferencePool.Runtime.ReferencePool.Acquire<RedDotRegister>();

        /// <summary>
        /// 注册节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        /// <param name="immediateNotify">是否立即通知当前状态</param>
        public void Register(string key, Action<int> onChange, bool immediateNotify = true)
        {
            if (m_RedNodeKeyList.Contains(key))
            {
                FuLog.Warning($"RedDotRegister: 注册监听时已存在节点: {key}, 请勿重复注册!");
                return;
            }
            m_RedNodeKeyList.Add(key);
            m_RedDotManager.Register(key, onChange, immediateNotify);
        }

        /// <summary>
        /// 批量注册多个节点的监听
        /// </summary>
        public void RegisterBatch(Dictionary<string, Action<int>> callbacks, bool immediateNotify = true)
        {
            foreach (var kvp in callbacks)
            {
                Register(kvp.Key, kvp.Value, immediateNotify);
            }
        }
        
        /// <summary>
        /// 注销节点状态变化的回调函数
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="onChange">节点状态变化的回调函数</param>
        public void Unregister(string key, Action<int> onChange)
        {
            if (!m_RedNodeKeyList.Contains(key))
            {
                FuLog.Warning($"RedDotRegister: 注销监听时未找到节点: {key}, 请检查是否已注册!");
                return;
            }
            m_RedDotManager.Unregister(key, onChange);
            m_RedNodeKeyList.Remove(key);
        }

        /// <summary>
        /// 注销指定key的所有回调函数
        /// </summary>
        public void UnregisterAll(string key)
        {
            if (!m_RedNodeKeyList.Contains(key))
            {
                FuLog.Warning($"RedDotRegister: 注销监听时未找到节点: {key}, 请检查是否已注册!");
                return;
            }
            m_RedDotManager.UnregisterAll(key);
            m_RedNodeKeyList.Remove(key);
        }

        /// <summary>
        /// 清理所有节点的监听器
        /// </summary>
        public void ClearAllListeners()
        {
            foreach (var key in m_RedNodeKeyList)
            {
                m_RedDotManager.UnregisterAll(key);
            }
            m_RedNodeKeyList.Clear();
        }

        #region Get

        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="key">节点的key</param>
        public RedDotNode GetNode(string key)
        {
            return !m_RedNodeKeyList.Contains(key) ? null : m_RedDotManager.GetNode(key);
        }

        /// <summary>
        /// 获取节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        public int GetCount(string key)
        {
            return !m_RedNodeKeyList.Contains(key) ? 0 : m_RedDotManager.GetCount(key);
        }

        /// <summary>
        /// 是否存在节点
        /// </summary>
        /// <param name="key">节点的key</param>
        public bool HasNode(string key)
        {
            return m_RedNodeKeyList.Contains(key);
        }

        #endregion

        #region Set

        /// <summary>
        /// 设置节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="count">红点数量</param>
        public void SetCount(string key, int count)
        {
            if (!m_RedNodeKeyList.Contains(key))
            {
                FuLog.Warning($"RedDotRegister: 设置红点数量时未找到节点: {key}, 请检查是否已注册!");
                return;
            }
            m_RedDotManager.SetCount(key, count);
        }

        /// <summary>
        /// 递增节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="value">递增的数量</param>
        public void IncrementCount(string key, int value = 1)
        {
            if (!m_RedNodeKeyList.Contains(key))
            {
                FuLog.Warning($"RedDotRegister: 递增红点数量时未找到节点: {key}, 请检查是否已注册!");
                return;
            }
            m_RedDotManager.IncrementCount(key, value);
        }

        /// <summary>
        /// 递减节点的红点数量
        /// </summary>
        /// <param name="key">节点的key</param>
        /// <param name="value">递减的数量</param>
        public void DecrementCount(string key, int value = 1)
        {
            if (!m_RedNodeKeyList.Contains(key))
            {
                FuLog.Warning($"RedDotRegister: 递减红点数量时未找到节点: {key}, 请检查是否已注册!");
                return;
            }
            m_RedDotManager.DecrementCount(key, value);
        }

        /// <summary>
        /// 重置节点的红点数量为0
        /// 适用于清除红点状态，如阅读所有邮件后
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

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            ClearAllListeners();
        }

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Runtime.ReferencePool.Release(this);
    }
}