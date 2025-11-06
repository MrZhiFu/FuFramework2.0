using System;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private readonly List<int> m_TimerList = new();

        /// <summary>
        /// 创建计时器注册器
        /// </summary>
        /// <returns></returns>
        public static TimerRegister Create() => ReferencePool.Runtime.ReferencePool.Acquire<TimerRegister>();

       
        /// <summary>
        /// 启动一个计时器
        /// </summary>
        /// <param name="duration">持续时间(以秒为单位)</param>
        /// <param name="updateCallBack">每帧/每秒回调(如果loopTiming为Update，则每帧回调；如果loopTiming为TimeUpdate，则每秒回调)</param>
        /// <param name="playerLoopTiming">计时器执行时机</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <param name="finishCallBack">计时器执行回调</param>
        /// <returns>计时器ID，用于停止指定计时器</returns>
        public void StartTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            var timerId = m_TimerManager.StartTimer(duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);
            if (m_TimerList.Contains(timerId)) return;
            m_TimerList.Add(timerId);
        }

        /// <summary>
        /// 启动一个只执行一次的计时器(即延时操作)
        /// </summary>
        /// <param name="duration">持续时间（以秒为单位）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimerOnce(float duration, Action callback, bool ignoreTimeScale = false)
        {
            var timerId = m_TimerManager.StartTimer(duration, callback, null, PlayerLoopTiming.Update, ignoreTimeScale);
            if (m_TimerList.Contains(timerId)) return;
            m_TimerList.Add(timerId);
        }

        /// <summary>
        /// 启动一个指定时间间隔的循环计时器
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="intervalCallback">每次间隔时间执行的回调函数</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimerInterval(float interval, Action intervalCallback, bool ignoreTimeScale = false)
        {
            var timerId = m_TimerManager.StartTimerInterval(interval, intervalCallback, ignoreTimeScale);
            if (m_TimerList.Contains(timerId)) return;
            m_TimerList.Add(timerId);
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void PauseTimer(int timerId)
        {
            if (!m_TimerList.Contains(timerId)) return;
            m_TimerManager.PauseTimer(timerId);
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void ResumeTimer(int timerId)
        {
            if (!m_TimerList.Contains(timerId)) return;
            m_TimerManager.ResumeTimer(timerId);
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void StopTimer(int timerId)
        {
            if (!m_TimerList.Contains(timerId)) return;
            m_TimerManager.StopTimer(timerId);
            m_TimerList.Remove(timerId);
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers()
        {
            foreach (var timerId in m_TimerList)
            {
                m_TimerManager.PauseTimer(timerId);
            }
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            foreach (var timerId in m_TimerList)
            {
                m_TimerManager.ResumeTimer(timerId);
            }
        }

        /// <summary>
        /// 停止所有计时器
        /// </summary>
        public void StopAllTimers()
        {
            var timerIds = m_TimerList.ToArray();
            foreach (var timerId in timerIds)
            {
                StopTimer(timerId);
            }
        }


        /// <summary>
        /// 检查计时器是否存在
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerExist(int timerId) => m_TimerManager.IsTimerExist(timerId);

        /// <summary>
        /// 检查计时器是否处于暂停状态
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerPaused(int timerId) => m_TimerManager.IsTimerPaused(timerId);


        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            StopAllTimers();
            m_TimerList.Clear();
        }

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Runtime.ReferencePool.Release(this);
    }
}