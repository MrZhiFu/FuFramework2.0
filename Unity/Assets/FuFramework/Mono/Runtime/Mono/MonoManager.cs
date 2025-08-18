using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Mono.Runtime
{
    /// <summary>
    /// Mono管理器。
    /// 管理游戏中 MonoBehaviour 的生命周期事件，例如 FixedUpdate、LateUpdate、OnDestroy等，并提供了一种简便的方式来添加和移除这些事件的监听。
    /// </summary>
    public sealed class MonoManager : FuModule, IMonoManager
    {
        private readonly List<Action> m_WaitUpdateList = new(); // 等待调用的 Update 回调列表
        private readonly List<Action> m_InvokingUpdateList = new(); // 正在调用的 Update 回调列表

        private readonly List<Action> m_WaitFixedUpdateList = new(); // 等待调用的 FixedUpdate 回调列表
        private readonly List<Action> m_InvokingFixedUpdateList = new(); // 正在调用的 FixedUpdate 回调列表

        private readonly List<Action> m_WaitLateUpdateList = new(); // 等待调用的 LateUpdate 回调列表
        private readonly List<Action> m_InvokingLateUpdateList = new(); // 正在调用的 LateUpdate 回调列表

        private readonly List<Action> m_WaitDestroyList = new(); // 等待调用的 Destroy 回调列表
        private readonly List<Action> m_InvokingDestroyList = new(); // 正在调用的 Destroy 回调列表

        private List<Action<bool>> m_WaitOnApplicationPauseList = new(); // 等待调用的 OnApplicationPause 回调列表
        private List<Action<bool>> m_InvokeOnApplicationPauseList = new(); // 正在调用的 OnApplicationPause 回调列表

        private List<Action<bool>> m_WaitOnApplicationFocusList = new(); // 等待调用的 OnApplicationFocus 回调列表
        private List<Action<bool>> m_InvokeOnApplicationFocusList = new(); // 正在调用的 OnApplicationFocus 回调列表

        /// <summary>
        /// 静态锁对象，用于同步多线程环境下的操作
        /// </summary>
        private static readonly object s_Lock = new();

        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            QueueInvoking(m_InvokingUpdateList, m_WaitUpdateList);
        }

        /// <summary>
        /// 在固定的帧率下调用。
        /// </summary>
        public void FixedUpdate()
        {
            QueueInvoking(m_InvokingFixedUpdateList, m_WaitFixedUpdateList);
        }

        /// <summary>
        /// 在所有 Update 函数调用后每帧调用。
        /// </summary>
        public void LateUpdate()
        {
            QueueInvoking(m_InvokingLateUpdateList, m_WaitLateUpdateList);
        }

        /// <summary>
        /// 当 MonoBehaviour 将被销毁时调用。
        /// </summary>
        public void OnDestroy()
        {
            QueueInvoking(m_InvokingDestroyList, m_WaitDestroyList);
        }

        /// <summary>
        /// 当应用程序失去或获得焦点时调用。
        /// </summary>
        /// <param name="focusStatus">应用程序的焦点状态</param>
        public void OnApplicationFocus(bool focusStatus)
        {
            QueueInvoking(ref m_InvokeOnApplicationFocusList, ref m_WaitOnApplicationFocusList, focusStatus);
        }

        /// <summary>
        /// 当应用程序暂停或恢复时调用。
        /// </summary>
        /// <param name="pauseStatus">应用程序的暂停状态</param>
        public void OnApplicationPause(bool pauseStatus)
        {
            QueueInvoking(ref m_InvokeOnApplicationPauseList, ref m_WaitOnApplicationPauseList, pauseStatus);
        }


        /// <summary>
        /// 添加一个在 Update 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddUpdateListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitUpdateList.Add(action);
        }

        /// <summary>
        /// 添加一个在 LateUpdate 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddLateUpdateListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitLateUpdateList.Add(action);
        }

        /// <summary>
        /// 从 LateUpdate 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveLateUpdateListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitLateUpdateList.Remove(action);
        }

        /// <summary>
        /// 添加一个在 FixedUpdate 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddFixedUpdateListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitFixedUpdateList.Add(action);
        }

        /// <summary>
        /// 从 FixedUpdate 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveFixedUpdateListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitFixedUpdateList.Remove(action);
        }

        /// <summary>
        /// 从 Update 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveUpdateListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitUpdateList.Remove(action);
        }


        /// <summary>
        /// 添加一个在 Destroy 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddDestroyListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitDestroyList.Add(action);
        }

        /// <summary>
        /// 从 Destroy 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveDestroyListener(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitDestroyList.Remove(action);
        }

        /// <summary>
        /// 添加一个在 OnApplicationPause 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddOnApplicationPauseListener(Action<bool> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitOnApplicationPauseList.Add(action);
        }

        /// <summary>
        /// 从 OnApplicationPause 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveOnApplicationPauseListener(Action<bool> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitOnApplicationPauseList.Remove(action);
        }

        /// <summary>
        /// 添加一个在 OnApplicationFocus 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddOnApplicationFocusListener(Action<bool> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitOnApplicationFocusList.Add(action);
        }

        /// <summary>
        /// 从 OnApplicationFocus 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveOnApplicationFocusListener(Action<bool> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (s_Lock) m_WaitOnApplicationFocusList.Remove(action);
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Release()
        {
            m_WaitUpdateList.Clear();
            m_WaitDestroyList.Clear();
            m_WaitFixedUpdateList.Clear();
            m_WaitLateUpdateList.Clear();
            m_WaitOnApplicationFocusList.Clear();
            m_WaitOnApplicationPauseList.Clear();
        }

        protected override void Shutdown() => Release();

        /// <summary>
        /// 使用交互引用的形式实现队列调用效果，确保在多线程环境下安全，在执行回调函数时不会发生竞态条件:
        /// 1. 先将 invokeList 与 waitInvokeList 进行交换引用，这样 invokeList 就指向waitInvokeList，而 waitInvokeList指向了invokeList.
        /// 2. 交换后，waitInvokeList可以继续收集新的回调函数，为下一次执行做准备。
        /// 3. 遍历 invokeList，调用其中的函数.
        /// </summary>
        /// <param name="invokeList"></param>
        /// <param name="waitInvokeList"></param>
        private static void QueueInvoking(List<Action> invokeList, List<Action> waitInvokeList)
        {
            lock (s_Lock)
            {
                ObjectHelper.Swap(ref invokeList, ref waitInvokeList);

                foreach (var action in invokeList)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        private static void QueueInvoking(ref List<Action<bool>> a, ref List<Action<bool>> b, bool value)
        {
            lock (s_Lock)
            {
                ObjectHelper.Swap(ref a, ref b);

                foreach (var action in a)
                {
                    try
                    {
                        action.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }
    }
}