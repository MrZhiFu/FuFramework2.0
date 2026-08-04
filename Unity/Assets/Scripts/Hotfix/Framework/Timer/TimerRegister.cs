using System;
using Hotfix.Framework.Core;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.ReferencePool;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
    /// <summary>
    /// 计时器注册器。
    /// 目标： 用于单独管理属于自己模块的相关计时器。
    /// 功能：
    ///     1. 启动倒计时计时器、时间间隔计时器、帧间隔计时器。
    ///     2. 暂停，继续，取消，停止(单个/所有)计时器。
    /// </summary>
    public class TimerRegister : IReference
    {
        /// <summary>
        /// 计时器管理模块
        /// </summary>
        private static TimerModule m_TimerModule;

        /// <summary>
        /// 记录所有计时器任务的列表
        /// </summary>
        private readonly List<int> m_TimerList = new();

        /// <summary>
        /// 创建计时器注册器
        /// </summary>
        /// <returns></returns>
        public static TimerRegister Create()
        {
            m_TimerModule = ModuleManager.GetModule<TimerModule>();
            var register = GlobalModule.ReferencePoolModule.Acquire<TimerRegister>();
            m_TimerModule.OnTimerFinished += register.OnTimerFinished;
            return register;
        }

        /// <summary>
        /// 计时器完成时的回调
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        private void OnTimerFinished(int timerId)
        {
            m_TimerList.Remove(timerId);
        }

        /// <summary>
        /// 启动一个倒计时计时器
        /// </summary>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="finishCallBack">计时器结束回调</param>
        /// <param name="updateCallBack">计时器更新回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartCountdownTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update,
                                        bool ignoreTimeScale = false)
        {
            var timerId = m_TimerModule.StartCountdownTimer(duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);
            if (timerId < 0) return;
            if (m_TimerList.Contains(timerId)) return;
            m_TimerList.Add(timerId);
        }

        /// <summary>
        /// 启动一个时间间隔计时器
        /// </summary>
        /// <param name="interval">计时器间隔时间</param>
        /// <param name="intervalCallback">计时器每次间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartIntervalTimer(float interval, Action intervalCallback, int repeatCount = -1, bool immediate = false, bool ignoreTimeScale = false)
        {
            var timerId = m_TimerModule.StartIntervalTimer(interval, intervalCallback, repeatCount, immediate, ignoreTimeScale);
            if (timerId < 0) return;
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
        public void StartFrameTimer(int frameInterval, Action intervalCallback, int repeatCount = -1, bool immediate = false, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update)
        {
            var timerId = m_TimerModule.StartFrameTimer(frameInterval, intervalCallback, repeatCount, immediate, playerLoopTiming);
            if (timerId < 0) return;
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
            m_TimerModule.PauseTimer(timerId);
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void ResumeTimer(int timerId)
        {
            if (!m_TimerList.Contains(timerId)) return;
            m_TimerModule.ResumeTimer(timerId);
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void StopTimer(int timerId)
        {
            if (!m_TimerList.Contains(timerId)) return;
            m_TimerModule.StopTimer(timerId);
            m_TimerList.Remove(timerId);
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers()
        {
            var timerIds = m_TimerList.ToArray();
            foreach (var timerId in timerIds)
            {
                m_TimerModule.PauseTimer(timerId);
            }
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            var timerIds = m_TimerList.ToArray();
            foreach (var timerId in timerIds)
            {
                m_TimerModule.ResumeTimer(timerId);
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
        public bool IsTimerExist(int timerId) => m_TimerModule.IsTimerExist(timerId);

        /// <summary>
        /// 检查计时器是否处于暂停状态
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerPaused(int timerId) => m_TimerModule.IsTimerPaused(timerId);

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            StopAllTimers();
            m_TimerList.Clear();
            m_TimerModule.OnTimerFinished -= OnTimerFinished;

            m_TimerModule = null;
        }

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => GlobalModule.ReferencePoolModule.Recycle(this);
    }
}
