using System;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 事件注册器。
    /// 可用于单独管理属于自己模块的相关事件，如每个UI界面都可以单独管理自己的事件。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class EventRegister : IReference
    {
        /// <summary>
        /// 持有者
        /// </summary>
        public object Owner { get; private set; }

        /// <summary>
        /// 事件处理字典，key为消息ID，value为处理对象
        /// </summary>
        private readonly GameFrameworkMultiDictionary<string, EventHandler<GameEventArgs>> m_DicEventHandlerDict = new();

        /// <summary>
        /// 创建事件订阅器
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <returns></returns>
        public static EventRegister Create(object owner)
        {
            var eventSubscriber = ReferencePool.Acquire<EventRegister>();
            eventSubscriber.Owner = owner;
            return eventSubscriber;
        }

        /// <summary>
        /// 检查订阅
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        /// <exception cref="Exception"></exception>
        public void CheckSubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (handler == null) throw new Exception("事件处理对象不能为空.");

            m_DicEventHandlerDict.Add(id, handler);
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
            if (!m_DicEventHandlerDict.Remove(id, handler))
                throw new Exception(Utility.Text.Format("事件订阅器中不存在指定消息ID '{0}' 的处理对象.", id));

            GameEntry.GetComponent<EventComponent>().Unsubscribe(id, handler);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="e">消息对象</param>
        public void Fire(string id, GameEventArgs e)
        {
            if (!m_DicEventHandlerDict.Contains(id)) return;
            GameEntry.GetComponent<EventComponent>().Fire(this, e);
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            if (m_DicEventHandlerDict.Count == 0) return;

            foreach (var (id, eventHandlers) in m_DicEventHandlerDict)
            {
                foreach (var eventHandler in eventHandlers)
                {
                    GameEntry.GetComponent<EventComponent>().Unsubscribe(id, eventHandler);
                }
            }

            m_DicEventHandlerDict.Clear();
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            m_DicEventHandlerDict.Clear();
            Owner = null;
        }
    }
}