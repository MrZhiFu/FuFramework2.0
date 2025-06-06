using System;
using System.Collections.Generic;
using GameFrameX.Runtime;

namespace GameFrameX.Timer.Runtime
{
    /// <summary>
    /// 定时器管理器。
    /// 用于管理定时器任务，提供添加、移除、检查定时器等功能。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class TimerManager : GameFrameworkModule, ITimerManager
    {
        /// <summary>
        /// 定时器项
        /// </summary>
        private class TimerItem
        {
            /// <summary>
            /// 间隔时间（以秒为单位）
            /// </summary>
            public float Interval;

            /// <summary>
            /// 重复次数（0 表示无限重复）
            /// </summary>
            public int Repeat;

            /// <summary>
            /// 已过时间（以秒为单位）
            /// </summary>
            public float Elapsed;

            /// <summary>
            /// 是否已删除
            /// </summary>
            public bool Deleted;

            /// <summary>
            /// 回调函数参数
            /// </summary>
            public object Param;

            /// <summary>
            /// 回调函数
            /// </summary>
            public Action<object> Callback;

            /// <summary>
            /// 设置一个定时器
            /// </summary>
            /// <param name="interval"> 间隔时间 </param>
            /// <param name="repeat"> 重复次数 </param>
            /// <param name="callback"> 回调函数 </param>
            /// <param name="param"> 回调函数参数 </param>
            public void Set(float interval, int repeat, Action<object> callback, object param)
            {
                Interval = interval;
                Repeat   = repeat;
                Callback = callback;
                Param    = param;
            }
        }

        /// <summary>
        /// 正在更新的定时器字典，key为回调函数，value为定时器项
        /// </summary>
        private readonly Dictionary<Action<object>, TimerItem> m_UpdatingDict = new();

        /// <summary>
        /// 待添加的定时器字典，key为回调函数，value为定时器项
        /// </summary>
        private readonly Dictionary<Action<object>, TimerItem> m_WaitToAddDict = new();

        /// <summary>
        /// 待移除的定时器列表
        /// </summary>
        private readonly List<TimerItem> m_WaitToRemoveList = new();

        /// <summary>
        /// 定时器池，用于复用定时器项
        /// </summary>
        private readonly List<TimerItem> m_PoolList = new(100);

        /// <summary>
        /// 静态锁对象，用于同步多线程环境下的操作
        /// </summary>
        private static readonly object s_Locker = new();

        /// <summary>
        /// 触发回调时是否需要捕获异常
        /// </summary>
        public static bool s_CatchCallbackExceptions = false;


        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            lock (s_Locker)
            {
                if (m_UpdatingDict.Count > 0)
                {
                    // 遍历正在更新的定时器字典，执行/更新/删除每个定时器
                    foreach (var (_, timerItem) in m_UpdatingDict)
                    {
                        if (timerItem.Deleted)
                        {
                            m_WaitToRemoveList.Add(timerItem);
                            continue;
                        }

                        // 计算已过时间是否大于间隔时间
                        timerItem.Elapsed += realElapseSeconds;
                        if (timerItem.Elapsed < timerItem.Interval)
                        {
                            continue;
                        }

                        // 已过时间大于间隔时间，已过时间减去间隔时间，理论上应该等于0，为了防止误差出现负数和超过一帧的时间，即小于0或大于0.03f，则重置为0
                        // 0.03f是参考30Fps的一帧的时间，防止出现超过一帧的时间
                        timerItem.Elapsed -= timerItem.Interval;
                        if (timerItem.Elapsed is < 0 or > 0.03f)
                            timerItem.Elapsed = 0;

                        // 重复次数大于0，则重复次数减1，如果等于0，则添加到待移除列表中
                        if (timerItem.Repeat > 0)
                        {
                            timerItem.Repeat--;
                            if (timerItem.Repeat == 0)
                            {
                                timerItem.Deleted = true;
                                m_WaitToRemoveList.Add(timerItem);
                            }
                        }

                        // 调用回调函数
                        if (timerItem.Callback != null)
                        {
                            if (s_CatchCallbackExceptions)
                            {
                                try
                                {
                                    timerItem.Callback(timerItem.Param);
                                }
                                catch (Exception e)
                                {
                                    timerItem.Deleted = true;
                                    Log.Error($"计时器：timer(internal={timerItem.Interval}, repeat={timerItem.Repeat}) 调用错误：{e.Message}");
                                }
                            }
                            else
                            {
                                timerItem.Callback(timerItem.Param);
                            }
                        }
                    }
                }

                // 处理待移除的定时器:从正在更新的字典中移除,并返回到池中，待下次使用，并清理待移除列表
                foreach (var timerItem in m_WaitToRemoveList)
                {
                    if (!timerItem.Deleted) continue;
                    if (timerItem.Callback == null) continue;
                    m_UpdatingDict.Remove(timerItem.Callback);
                    RecycleTimerItem(timerItem);
                }

                m_WaitToRemoveList.Clear();

                // 处理待添加的定时器:从待添加列表中取出，添加到正在更新的字典中，并清理待添加列表
                foreach (var (action, timerItem) in m_WaitToAddDict)
                {
                    m_UpdatingDict.Add(action, timerItem);
                }

                m_WaitToAddDict.Clear();
            }
        }

        protected override void Shutdown()
        {
            lock (s_Locker)
            {
                m_WaitToRemoveList.Clear();
                m_WaitToAddDict.Clear();
                m_UpdatingDict.Clear();
            }
        }

        /// <summary>
        /// 添加一个定时调用的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="repeat">重复次数（0 表示无限重复）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        public void Add(float interval, int repeat, Action<object> callback, object callbackParam = null)
        {
            lock (s_Locker)
            {
                if (callback == null)
                {
                    Log.Error($"计时器添加失败, 回调函数为空, 间隔时间={interval}, 重复次数={repeat}");
                    return;
                }

                // 如果回调函数已经存在，则更新定时器项
                if (m_UpdatingDict.TryGetValue(callback, out var timerItem))
                {
                    timerItem.Set(interval, repeat, callback, callbackParam);
                    timerItem.Elapsed = 0;
                    timerItem.Deleted = false;
                    return;
                }

                // 如果回调函数已经在待添加列表中，则更新定时器项
                if (m_WaitToAddDict.TryGetValue(callback, out timerItem))
                {
                    timerItem.Set(interval, repeat, callback, callbackParam);
                    return;
                }

                // 如果回调函数不存在，则创建定时器项并添加到待添加列表中
                timerItem = GetTimerItem();
                timerItem.Set(interval, repeat, callback, callbackParam);
                m_WaitToAddDict[callback] = timerItem;
            }
        }

        /// <summary>
        /// 添加一个只执行一次的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        public void AddOnce(float interval, Action<object> callback, object callbackParam = null)
        {
            Add(interval, 1, callback, callbackParam);
        }

        /// <summary>
        /// 添加一个每帧更新执行的任务
        /// </summary>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数</param>
        public void AddUpdate(Action<object> callback, object callbackParam = null)
        {
            Add(0.001f, 0, callback, callbackParam);
        }

        /// <summary>
        /// 检查指定的任务是否存在
        /// </summary>
        /// <param name="callback">要检查的回调函数</param>
        /// <returns>存在返回 true，不存在返回 false</returns>
        public bool Exists(Action<object> callback)
        {
            lock (s_Locker)
            {
                return m_WaitToAddDict.ContainsKey(callback) || (m_UpdatingDict.TryGetValue(callback, out var at) && !at.Deleted);
            }
        }

        /// <summary>
        /// 移除指定的任务
        /// </summary>
        /// <param name="callback">要移除的回调函数</param>
        public void Remove(Action<object> callback)
        {
            lock (s_Locker)
            {
                if (m_WaitToAddDict.Remove(callback, out var timerItem))
                {
                    RecycleTimerItem(timerItem);
                    return;
                }

                if (m_UpdatingDict.TryGetValue(callback, out timerItem))
                {
                    timerItem.Deleted = true;
                }
            }
        }

        /// <summary>
        /// 获取一个定时器项
        /// </summary>
        /// <returns></returns>
        private TimerItem GetTimerItem()
        {
            // 如果池中有可用的定时器项，则从池中取出，否则创建一个新的定时器项
            if (m_PoolList.Count > 0)
            {
                var tempTimer = m_PoolList[m_PoolList.Count - 1];
                m_PoolList.RemoveAt(m_PoolList.Count - 1);
                tempTimer.Deleted = false;
                tempTimer.Elapsed = 0;
                return tempTimer;
            }

            return new TimerItem();
        }

        /// <summary>
        /// 回收定时器项到池中
        /// </summary>
        /// <param name="t"></param>
        private void RecycleTimerItem(TimerItem t)
        {
            t.Callback = null;
            m_PoolList.Add(t);
        }
    }
}