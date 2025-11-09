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
        /// <summary>
        /// 计时器管理器
        /// </summary>
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
        /// 启动一个基础的一次性计时器
        /// </summary>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="finishCallBack">计时器结束回调</param>
        /// <param name="updateCallBack">计时器更新回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            var timerId = m_TimerManager.StartTimer(duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);
            if (m_TimerList.Contains(timerId)) return;
            m_TimerList.Add(timerId);
        }

        /// <summary>
        /// 启动一个基础的一次性计时器
        /// </summary>
        /// <param name="interval">计时器间隔时间</param>
        /// <param name="intervalCallback">计时器每次间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimerInterval(float interval, Action intervalCallback, int repeatCount = -1, bool immediate = false, bool ignoreTimeScale = false)
        {
            var timerId = m_TimerManager.StartTimerInterval(interval, intervalCallback, repeatCount, immediate, ignoreTimeScale);
            if (m_TimerList.Contains(timerId)) return;
            m_TimerList.Add(timerId);
        }

        /// <summary>
        /// 启动一个帧间隔计时器
        /// </summary>
        /// <param name="frameInterval">计时器帧间隔</param>
        /// <param name="intervalCallback">计时器每次帧间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        public void StartTimerFrameInterval(int frameInterval, Action intervalCallback, int repeatCount = -1, bool immediate = false, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update)
        {
            var timerId = m_TimerManager.StartTimerFrameInterval(frameInterval, intervalCallback, repeatCount, immediate, playerLoopTiming);
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