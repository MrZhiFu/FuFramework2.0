using System;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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
        /// 计时器管理模块（实例字段：每个注册器持有自己的引用，池化对象 Clear 置 null 不影响其他实例。
        /// 原为 static——单实例 Clear 会清空共享引用，后续实例的 StopAllTimers/OnTimerFinished 访问 null 抛 NRE）
        /// </summary>
        private TimerModule m_TimerModule;

        /// <summary>
        /// 记录所有计时器任务的列表
        /// </summary>
        private readonly List<int> m_TimerList = new();

        /// <summary>
        /// 批量操作（PauseAll/ResumeAll/StopAll）的复用快照缓冲。
        /// 这些操作会经 m_TimerModule 触发 OnTimerFinished → m_TimerList.Remove，边遍历边移除会
        /// 「遍历中修改容器」；先快照到本缓冲再遍历即可。复用实例字段避免每次 ToArray 分配。
        /// </summary>
        private readonly List<int> m_TempTimerList = new();

        /// <summary>
        /// 创建计时器注册器
        /// </summary>
        /// <returns>计时器注册器；计时器管理模块不存在时返回 null（并回收本次获取的实例，避免引用池泄漏）</returns>
        public static TimerRegister Create()
        {
            var register = ReferencePool.Acquire<TimerRegister>();
            register.m_TimerModule = ModuleManager.GetModule<TimerModule>();

            // 原实现未判空：模块不存在时下一行直接 NRE，且 Acquire 出来的 register 永不回收
            //（引用池「使用中」计数永久虚高）。
            if (register.m_TimerModule == null)
            {
                FuLogger.LogError("[TimerRegister] 计时器管理模块不存在，创建计时器注册器失败.");
                ReferencePool.Recycle(register);
                return null;
            }

            register.m_TimerModule.OnTimerFinished += register.OnTimerFinished;
            return register;
        }

        /// <summary>
        /// 把 m_TimerList 快照到复用缓冲（供批量操作遍历；避免 ToArray 分配与遍历中修改容器）
        /// </summary>
        private void SnapshotTimerIds()
        {
            m_TempTimerList.Clear();
            for (var i = 0; i < m_TimerList.Count; i++)
            {
                m_TempTimerList.Add(m_TimerList[i]);
            }
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
            if (m_TimerModule == null) return;

            SnapshotTimerIds();
            for (var i = 0; i < m_TempTimerList.Count; i++)
            {
                m_TimerModule.PauseTimer(m_TempTimerList[i]);
            }

            m_TempTimerList.Clear();
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            if (m_TimerModule == null) return;

            SnapshotTimerIds();
            for (var i = 0; i < m_TempTimerList.Count; i++)
            {
                m_TimerModule.ResumeTimer(m_TempTimerList[i]);
            }

            m_TempTimerList.Clear();
        }

        /// <summary>
        /// 停止所有计时器
        /// </summary>
        public void StopAllTimers()
        {
            if (m_TimerModule == null) return;

            SnapshotTimerIds();
            for (var i = 0; i < m_TempTimerList.Count; i++)
            {
                StopTimer(m_TempTimerList[i]);
            }

            m_TempTimerList.Clear();
        }

        /// <summary>
        /// 检查计时器是否存在
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerExist(int timerId) => m_TimerModule != null && m_TimerModule.IsTimerExist(timerId);

        /// <summary>
        /// 检查计时器是否处于暂停状态
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerPaused(int timerId) => m_TimerModule != null && m_TimerModule.IsTimerPaused(timerId);

        /// <summary>
        /// 清理。
        /// 判空原因：Create 在模块缺失时会 Recycle 半成品实例，Recycle → Clear 会走到这里，
        /// 原实现对 m_TimerModule 直接解引用会 NRE（掩盖 Create 的失败路径）。
        /// </summary>
        public void Clear()
        {
            if (m_TimerModule != null)
            {
                StopAllTimers();
                m_TimerModule.OnTimerFinished -= OnTimerFinished;
                m_TimerModule = null;
            }

            m_TimerList.Clear();
            m_TempTimerList.Clear();
        }

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Recycle(this);
    }
}
