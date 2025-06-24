using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FUI的UI事件注册器，更方便清晰的管理FUI的事件关系。
    /// 主要负责管理该界面或该组件的响应UI管理
    /// </summary>
    public class FUIEventRegister
    {
        /// 记录该界面的可响应UI的事件字典, key: 可相应UI元素，如按钮，value：该UI元素上的可响应事件回调
        private readonly Dictionary<EventListener, List<EventCallback1>> m_UIEventListenerDic = new();

        /// <summary>
        /// 清理所有注册的事件
        /// </summary>
        public void Clear()
        {
            m_UIEventListenerDic.Clear();
        }

        /// <summary>
        /// 添加FUI某个可响应UI上的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        public void AddUIListener(EventListener listener, EventCallback1 callback)
        {
            if (listener == null)
            {
                Log.Error("AddFUIListener failed, listener is null");
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
        /// 移除FUI某个可响应UI上的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        public void RemoveUIListener(EventListener listener, EventCallback1 callback)
        {
            if (!m_UIEventListenerDic.ContainsKey(listener))
            {
                Log.Error("RemoveFUIListener failed, listener {0} not exist", listener.ToString());
                return;
            }

            var handlers = m_UIEventListenerDic[listener];
            for (var i = handlers.Count; i >= 0; i--)
            {
                if (handlers[i] != callback) continue;
                handlers.RemoveAt(i);
                listener.Remove(callback);
                return;
            }

            Log.Error("RemoveFUIListener failed, callback handler {0} not exist", callback.ToString());
        }

        /// <summary>
        /// 设置FUI某个可响应UI上的监听事件(会删除以前添加的事件)
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        public void SetUIListener(EventListener listener, EventCallback1 callback)
        {
            if (listener == null)
            {
                Log.Error("SetFUIListener failed, listener is null");
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
        /// 清理FUI某个可响应UI上的所有监听事件
        /// </summary>
        /// <param name="listener"></param>
        public void ClearUIListener(EventListener listener)
        {
            if (listener == null)
            {
                Log.Error("ClearFUIListener failed, listener is null");
                return;
            }

            if (!m_UIEventListenerDic.ContainsKey(listener))
            {
                Log.Error("ClearFUIListener failed, listener {0} not exist", listener.ToString());
                return;
            }

            var handlers = m_UIEventListenerDic[listener];
            foreach (var handler in handlers)
            {
                listener.Remove(handler);
            }

            m_UIEventListenerDic[listener].Clear();
        }
    }
}