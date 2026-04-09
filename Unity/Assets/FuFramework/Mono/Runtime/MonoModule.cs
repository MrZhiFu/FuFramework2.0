using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Mono.Runtime
{
    /// <summary>
    /// Mono管理模块。
    /// 管理游戏中 MonoBehaviour 的生命周期事件，例如 FixedUpdate、LateUpdate、OnDestroy等，并提供了一种简便的方式来添加和移除这些事件的监听。
    /// </summary>
    public class MonoModule : FuModule
    {
        /// <summary>
        /// 等待执行的 Update 回调列表
        /// </summary>
        private readonly List<Action> m_WaitUpdateList = new(); // 

        /// <summary>
        /// 正在执行的 Update 回调列表
        /// </summary>
        private readonly List<Action> m_DoingUpdateList = new();


        /// <summary>
        /// 等待执行的 FixedUpdate 回调列表
        /// </summary>
        private readonly List<Action> m_WaitFixedUpdateList = new();

        /// <summary>
        /// 正在执行的 FixedUpdate 回调列表
        /// </summary>
        private readonly List<Action> m_DoingFixedUpdateList = new();


        /// <summary>
        /// 等待执行的 LateUpdate 回调列表
        /// </summary>
        private readonly List<Action> m_WaitLateUpdateList = new();

        /// <summary>
        /// 正在执行的 LateUpdate 回调列表
        /// </summary>
        private readonly List<Action> m_DoingLateUpdateList = new();


        /// <summary>
        /// 等待执行的 Destroy 回调列表
        /// </summary>
        private readonly List<Action> m_WaitDestroyList = new();

        /// <summary>
        /// 正在执行的 Destroy 回调列表
        /// </summary>
        private readonly List<Action> m_DoingDestroyList = new();


        /// <summary>
        /// 等待执行的 OnApplicationPause 回调列表
        /// </summary>
        private List<Action<bool>> m_WaitOnApplicationPauseList = new();

        /// <summary>
        /// 正在执行的 OnApplicationPause 回调列表
        /// </summary>
        private List<Action<bool>> m_DoOnApplicationPauseList = new();


        /// <summary>
        /// 等待执行的 OnApplicationFocus 回调列表
        /// </summary>
        private List<Action<bool>> m_WaitOnApplicationFocusList = new();

        /// <summary>
        /// 正在执行的 OnApplicationFocus 回调列表
        /// </summary>
        private List<Action<bool>> m_DoOnApplicationFocusList = new();


        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 帧更新。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            QueueInvoking(m_DoingUpdateList, m_WaitUpdateList);
        }

        /// <summary>
        /// 固定帧更新
        /// </summary>
        protected override void OnFixedUpdate()
        {
            QueueInvoking(m_DoingFixedUpdateList, m_WaitFixedUpdateList);
        }

        /// <summary>
        /// 延迟帧更新
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected override void OnLateUpdate(float deltaTime, float unscaledDeltaTime)
        {
            QueueInvoking(m_DoingLateUpdateList, m_WaitLateUpdateList);
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected override void OnDispose()
        {
            QueueInvoking(m_DoingDestroyList, m_WaitDestroyList);

            m_WaitUpdateList.Clear();
            m_WaitDestroyList.Clear();
            m_WaitFixedUpdateList.Clear();
            m_WaitLateUpdateList.Clear();
            m_WaitOnApplicationFocusList.Clear();
            m_WaitOnApplicationPauseList.Clear();
        }

        /// <summary>
        /// 当应用程序失去或获得焦点时调用。
        /// </summary>
        /// <param name="focusStatus">应用程序的焦点状态</param>
        public void OnApplicationFocus(bool focusStatus)
        {
            QueueInvoking(ref m_DoOnApplicationFocusList, ref m_WaitOnApplicationFocusList, focusStatus);
        }

        /// <summary>
        /// 当应用程序暂停或恢复时调用。
        /// </summary>
        /// <param name="pauseStatus">应用程序的暂停状态</param>
        public void OnApplicationPause(bool pauseStatus)
        {
            QueueInvoking(ref m_DoOnApplicationPauseList, ref m_WaitOnApplicationPauseList, pauseStatus);
        }


        /// <summary>
        /// 添加一个在 Update 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddUpdateListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitUpdateList.Add(action);
        }

        /// <summary>
        /// 添加一个在 LateUpdate 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddLateUpdateListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitLateUpdateList.Add(action);
        }

        /// <summary>
        /// 从 LateUpdate 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveLateUpdateListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitLateUpdateList.Remove(action);
        }

        /// <summary>
        /// 添加一个在 FixedUpdate 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddFixedUpdateListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitFixedUpdateList.Add(action);
        }

        /// <summary>
        /// 从 FixedUpdate 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveFixedUpdateListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitFixedUpdateList.Remove(action);
        }

        /// <summary>
        /// 从 Update 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveUpdateListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitUpdateList.Remove(action);
        }


        /// <summary>
        /// 添加一个在 Destroy 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddDestroyListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitDestroyList.Add(action);
        }

        /// <summary>
        /// 从 Destroy 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveDestroyListener(Action action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitDestroyList.Remove(action);
        }

        /// <summary>
        /// 添加一个在 OnApplicationPause 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddOnApplicationPauseListener(Action<bool> action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitOnApplicationPauseList.Add(action);
        }

        /// <summary>
        /// 从 OnApplicationPause 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveOnApplicationPauseListener(Action<bool> action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitOnApplicationPauseList.Remove(action);
        }

        /// <summary>
        /// 添加一个在 OnApplicationFocus 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void AddOnApplicationFocusListener(Action<bool> action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitOnApplicationFocusList.Add(action);
        }

        /// <summary>
        /// 从 OnApplicationFocus 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数</param>
        public void RemoveOnApplicationFocusListener(Action<bool> action)
        {
            FuGuard.NotNull(action, nameof(action));
            m_WaitOnApplicationFocusList.Remove(action);
        }

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
            Utility.Object.Swap(ref invokeList, ref waitInvokeList);

            foreach (var action in invokeList)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    FuLogger.LogError(e);
                }
            }
        }

        /// <summary>
        /// 使用交互引用的形式实现队列调用效果，确保在多线程环境下安全，在执行回调函数时不会发生竞态条件:
        /// 1. 先将 invokeList 与 waitInvokeList 进行交换引用，这样 invokeList 就指向waitInvokeList，而 waitInvokeList指向了invokeList.
        /// 2. 交换后，waitInvokeList可以继续收集新的回调函数，为下一次执行做准备。
        /// 3. 遍历 invokeList，调用其中的函数.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        private static void QueueInvoking(ref List<Action<bool>> a, ref List<Action<bool>> b, bool value)
        {
            Utility.Object.Swap(ref a, ref b);

            foreach (var action in a)
            {
                try
                {
                    action.Invoke(value);
                }
                catch (Exception e)
                {
                    FuLogger.LogError(e);
                }
            }
        }
    }
}