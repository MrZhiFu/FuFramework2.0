using System;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    /// <summary>
    /// 计时器注册器。
    /// 可用于单独管理属于自己模块的相关计时器
    /// </summary>
    public class TimerRegister : IReference
    {
        /// 计时器管理器
        private readonly TimerManager m_TimerManager = ModuleManager.GetModule<TimerManager>();

        /// <summary>
        /// 记录所有计时器任务的列表
        /// </summary>
        private readonly List<Action<object>> m_TimerHandlerList = new();

        /// <summary>
        /// 创建计时器注册器
        /// </summary>
        /// <returns></returns>
        public static TimerRegister Create() => ReferencePool.Runtime.ReferencePool.Acquire<TimerRegister>();

        /// <summary>
        /// 添加一个定时调用的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="repeat">重复次数（0 表示无限重复）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        public void AddTimer(float interval, int repeat, Action<object> callback, object callbackParam = null)
        {
            if (m_TimerHandlerList.Contains(callback)) return;
            m_TimerHandlerList.Add(callback);
            m_TimerManager.Add(interval, repeat, callback, callbackParam);
        }

        /// <summary>
        /// 添加一个只执行一次的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        public void AddTimerOnce(float interval, Action<object> callback, object callbackParam = null)
        {
            if (m_TimerHandlerList.Contains(callback)) return;
            m_TimerHandlerList.Add(callback);
            m_TimerManager.AddOnce(interval, callback, callbackParam);
        }

        /// <summary>
        /// 添加一个每帧更新执行的任务
        /// </summary>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数</param>
        public void AddTimerUpdate(Action<object> callback, object callbackParam = null)
        {
            if (m_TimerHandlerList.Contains(callback)) return;
            m_TimerHandlerList.Add(callback);
            m_TimerManager.AddUpdate(callback, callbackParam);
        }

        /// <summary>
        /// 检查指定的任务是否存在
        /// </summary>
        /// <param name="callback">要检查的回调函数</param>
        /// <returns>存在返回 true，不存在返回 false</returns>
        public bool ExistsTimer(Action<object> callback)
        {
            return m_TimerHandlerList.Contains(callback) || m_TimerManager.Exists(callback);
        }

        /// <summary>
        /// 移除指定的任务
        /// </summary>
        /// <param name="callback">要移除的回调函数</param>
        public void RemoveTimer(Action<object> callback)
        {
            if (!m_TimerHandlerList.Contains(callback)) return;
            m_TimerHandlerList.Remove(callback);
            m_TimerManager.Remove(callback);
        }

        /// <summary>
        /// 移除所有计时任务
        /// </summary>
        public void RemoveAllTimer()
        {
            foreach (var callback in m_TimerHandlerList)
            {
                m_TimerManager.Remove(callback);
            }

            m_TimerHandlerList.Clear();
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear() => RemoveAllTimer();

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Runtime.ReferencePool.Release(this);
    }
}