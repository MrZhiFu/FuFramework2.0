using System;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// UI事件订阅器
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIEventSubscriber : IReference
    {
        private readonly GameFrameworkMultiDictionary<string, EventHandler<GameEventArgs>> m_DicEventHandler = new GameFrameworkMultiDictionary<string, EventHandler<GameEventArgs>>();

        /// <summary>
        /// 持有者
        /// </summary>
        public object Owner { get; private set; }

        public UIEventSubscriber()
        {
            m_DicEventHandler = new GameFrameworkMultiDictionary<string, EventHandler<GameEventArgs>>();
            Owner = null;
        }

        /// <summary>
        /// 检查订阅
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        /// <exception cref="Exception"></exception>
        public void CheckSubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (handler == null)
            {
                throw new Exception("Event handler is invalid.");
            }

            m_DicEventHandler.Add(id, handler);
            GameEntry.GetComponent<EventComponent>().CheckSubscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        /// <exception cref="Exception"></exception>
        public void UnSubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (!m_DicEventHandler.Remove(id, handler))
            {
                throw new Exception(Utility.Text.Format("Event '{0}' not exists specified handler.", id.ToString()));
            }

            GameEntry.GetComponent<EventComponent>().Unsubscribe(id, handler);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="e">消息对象</param>
        public void Fire(string id, GameEventArgs e)
        {
            if (m_DicEventHandler.TryGetValue(id, out var handlers))
            {
                foreach (var eventHandler in handlers)
                {
                    try
                    {
                        eventHandler.Invoke(this, e);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                    }
                }

                GameEntry.GetComponent<EventComponent>().Fire(this, e);
            }
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            if (m_DicEventHandler == null)
            {
                return;
            }

            foreach (var item in m_DicEventHandler)
            {
                foreach (var eventHandler in item.Value)
                {
                    GameEntry.GetComponent<EventComponent>().Unsubscribe(item.Key, eventHandler);
                }
            }

            m_DicEventHandler.Clear();
        }

        /// <summary>
        /// 创建事件订阅器
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <returns></returns>
        public static UIEventSubscriber Create(object owner)
        {
            var eventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
            eventSubscriber.Owner = owner;

            return eventSubscriber;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            m_DicEventHandler.Clear();
            Owner = null;
        }
    }
}