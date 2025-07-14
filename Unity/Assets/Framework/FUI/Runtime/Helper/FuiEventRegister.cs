using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FUI事件注册器，更方便清晰的管理FUI的事件关系。
    /// 主要负责管理该界面或该组件的响应UI管理
    /// </summary>
    public class FuiEventRegister : IReference
    {
        /// 记录该界面的可响应UI的事件字典, key: 可相应UI元素，如按钮，value：该UI元素上的可响应事件回调
        private readonly Dictionary<EventListener, List<EventCallback1>> m_UIEventListenerDic = new();

        /// <summary>
        /// 创建FUI事件注册器
        /// </summary>
        /// <returns></returns>
        public static FuiEventRegister Create() => ReferencePool.Acquire<FuiEventRegister>();

        /// <summary>
        /// 添加UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        public void AddUIListener(EventListener listener, EventCallback1 callback)
        {
            if (listener == null)
            {
                Log.Error("[FuiEventRegister]添加FUI监听事件失败, 监听器为空");
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
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        public void RemoveUIListener(EventListener listener, EventCallback1 callback)
        {
            if (!m_UIEventListenerDic.TryGetValue(listener, out var handlers))
            {
                Log.Error($"[FuiEventRegister]移除FUI监听事件失败, 监听器 {listener} 不存在");
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
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        public void SetUIListener(EventListener listener, EventCallback1 callback)
        {
            if (listener == null)
            {
                Log.Error("[FuiEventRegister]设置FUI监听事件失败, 监听器为空");
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
                handlers = new List<EventCallback1>();
                m_UIEventListenerDic[listener] = handlers;
            }

            handlers.Add(callback);
            listener.Add(callback);
        }

        /// <summary>
        /// 清理UI上指定组件的所有监听事件
        /// </summary>
        /// <param name="listener"></param>
        public void ClearUIListener(EventListener listener)
        {
            if (listener == null)
            {
                Log.Error("[FuiEventRegister]清理FUI监听事件失败, 监听器为空");
                return;
            }

            if (!m_UIEventListenerDic.TryGetValue(listener, out var handlers))
            {
                Log.Error($"[FuiEventRegister]清理FUI监听事件失败, 监听器 {listener} 不存在");
                return;
            }

            foreach (var handler in handlers)
            {
                listener.Remove(handler);
            }

            m_UIEventListenerDic[listener].Clear();
        }

        /// <summary>
        /// 清理所有注册的事件
        /// </summary>
        public void Clear()
        {
            foreach (var listener in m_UIEventListenerDic.Keys)
            {
                ClearUIListener(listener);
            }
            
            m_UIEventListenerDic.Clear();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release() => ReferencePool.Release(this);
    }
}