using System.Collections.Generic;
using FairyGUI;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePool;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// FUI事件注册器
    /// 目标：用于管理FUI组件的UI响应事件。
    /// 功能：
    ///     1. 添加UI上指定组件的监听事件。
    ///     2. 设置UI上指定组件的监听事件(会删除以前添加的事件)。
    ///     3. 移除UI上指定组件的监听事件。
    /// </summary>
    public class FuiEventRegister : IReference
    {
        /// <summary>
        /// 记录UI响应事件的字典, key: 可相应UI元素，如按钮，value：该UI元素上的可响应事件回调
        /// </summary>
        private readonly Dictionary<EventListener, List<EventCallback1>> m_UIEventListenerDic = new();

        /// <summary>
        /// 创建FUI事件注册器
        /// </summary>
        /// <returns></returns>
        public static FuiEventRegister Create() => GlobalModule.ReferencePoolModule.Acquire<FuiEventRegister>();

        /// <summary>
        /// 添加UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        /// <param name="callback">回调函数</param>
        public void AddUIListener(EventListener listener, EventCallback1 callback)
        {
            if (listener == null)
            {
                FuLogger.LogError("[FuiEventRegister] 添加FUI监听事件失败, 监听器为空");
                return;
            }

            if (!m_UIEventListenerDic.ContainsKey(listener))
            {
                m_UIEventListenerDic[listener] = new List<EventCallback1>();
            }

            m_UIEventListenerDic[listener].Add(callback);
            listener.Add(callback);
        }

        /// <summary>
        /// 移除UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        /// <param name="callback">回调函数</param>
        public void RemoveUIListener(EventListener listener, EventCallback1 callback)
        {
            if (!m_UIEventListenerDic.TryGetValue(listener, out var handlers))
            {
                FuLogger.LogError($"[FuiEventRegister] 移除FUI监听事件失败, 监听器 {listener} 不存在");
                return;
            }

            for (var i = handlers.Count; i >= 0; i--)
            {
                if (handlers[i] != callback) continue;
                handlers.RemoveAt(i);
                listener.Remove(callback);
                return;
            }
        }

        /// <summary>
        /// 设置UI上指定组件的监听事件(会删除以前添加的事件)
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        /// <param name="callback">回调函数</param>
        public void SetUIListener(EventListener listener, EventCallback1 callback)
        {
            if (listener == null)
            {
                FuLogger.LogError("[FuiEventRegister] 设置FUI监听事件失败, 监听器为空");
                return;
            }

            if (m_UIEventListenerDic.TryGetValue(listener, out var handlers))
            {
                if (handlers.Count > 0)
                {
                    foreach (var cb in handlers)
                    {
                        listener.Remove(cb);
                    }

                    handlers.Clear();
                }
            }
            else
            {
                handlers                       = new List<EventCallback1>();
                m_UIEventListenerDic[listener] = handlers;
            }

            handlers.Add(callback);
            listener.Add(callback);
        }

        /// <summary>
        /// 清理UI上指定组件的所有监听事件
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        public void ClearUIListener(EventListener listener)
        {
            if (listener == null)
            {
                FuLogger.LogError("[FuiEventRegister] 清理FUI监听事件失败, 监听器为空");
                return;
            }

            if (!m_UIEventListenerDic.TryGetValue(listener, out var handlers))
            {
                FuLogger.LogError($"[FuiEventRegister] 清理FUI监听事件失败, 监听器 {listener} 不存在");
                return;
            }

            foreach (var handler in handlers)
            {
                listener.Remove(handler);
            }

            m_UIEventListenerDic[listener].Clear();
        }

        /// <summary>
        /// 清理所有UI上组件的所有监听事件
        /// </summary>
        public void ClearAllUIListener()
        {
            foreach (var listener in m_UIEventListenerDic.Keys)
            {
                ClearUIListener(listener);
            }

            m_UIEventListenerDic.Clear();
        }

        /// <summary>
        /// 清理所有注册的事件
        /// </summary>
        public void Clear() => ClearAllUIListener();

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release() => GlobalModule.ReferencePoolModule.Release(this);
    }
}
