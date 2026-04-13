using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    /// <summary>
    /// 基于 UniTask 的计时器管理模块。
    /// 目标： 用于管理计时器任务，提供启动计时器、暂停计时器、恢复计时器、停止计时器、获取计时器状态等功能。
    /// 功能：
    ///     1. 启动倒计时计时器、时间间隔计时器、帧间隔计时器。
    ///     2. 暂停，继续，取消，停止(单个/所有)计时器。
    /// </summary>
    public sealed class TimerModule : FuModule
    {
        /// <summary>
        /// 计时器字典，key为计时器Id，value为计时器项
        /// </summary>
        private readonly Dictionary<int, TimerBase> m_TimerDict = new();

        /// <summary>
        /// 下一个计时器ID
        /// </summary>
        private int m_NextTimerId = 0;

        /// <summary>
        /// 获取当前计时器数量
        /// </summary>
        public int Count => m_TimerDict.Count;

        /// <summary>
        /// 计时器完成/停止时触发的事件
        /// </summary>
        public event Action<int> OnTimerFinished;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 释放。
        /// </summary>
        protected override void OnDispose()
        {
            foreach (var timerInfo in m_TimerDict.Values)
            {
                timerInfo.Cts.Cancel();
                ReferencePool.Runtime.ReferencePool.Release(timerInfo);
            }

            m_TimerDict.Clear();
        }

        #region Public Methods

        /// <summary>
        /// 启动一个倒计时计时器
        /// </summary>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="finishCallBack">计时器结束回调</param>
        /// <param name="updateCallBack">计时器更新回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns>倒计时Id</returns>
        public int StartCountdownTimer(float duration, Action finishCallBack, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            if (duration <= 0)
            {
                FuLogger.LogError("[TimerModule] 计时器持续时间必须大于0");
                return -1;
            }

            return StartTimer(() => CountdownTimer.Create(++m_NextTimerId, duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale), "计时器");
        }

        /// <summary>
        /// 启动一个时间间隔计时器
        /// </summary>
        /// <param name="interval">计时器间隔时间</param>
        /// <param name="intervalCallback">计时器每次间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns>倒计时Id</returns>
        public int StartIntervalTimer(float interval, Action intervalCallback, int repeatCount = -1, bool immediate = false, bool ignoreTimeScale = false)
        {
            if (interval <= 0)
            {
                FuLogger.LogError("[TimerModule] 间隔时间必须大于0");
                return -1;
            }

            if (intervalCallback == null)
            {
                FuLogger.LogError("[TimerModule] 时间间隔计时器回调函数不能为null");
                return -1;
            }

            return StartTimer(() => IntervalTimer.Create(++m_NextTimerId, interval, intervalCallback, repeatCount, immediate, ignoreTimeScale), "间隔计时器");
        }

        /// <summary>
        /// 启动一个帧间隔计时器
        /// </summary>
        /// <param name="frameInterval">计时器帧间隔</param>
        /// <param name="intervalCallback">计时器每次帧间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <returns>倒计时Id</returns>
        public int StartFrameTimer(int frameInterval, Action intervalCallback, int repeatCount = -1, bool immediate = false, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update)
        {
            if (frameInterval <= 0)
            {
                FuLogger.LogError("[TimerModule] 帧间隔必须大于0");
                return -1;
            }

            if (intervalCallback == null)
            {
                FuLogger.LogError("[TimerModule] 帧间隔计时器回调函数不能为null");
                return -1;
            }

            return StartTimer(() => FrameTimer.Create(++m_NextTimerId, frameInterval, intervalCallback, repeatCount, immediate, playerLoopTiming), "帧间隔计时器");
        }

        /// <summary>
        /// 启动计时器的通用方法
        /// </summary>
        /// <param name="createFunc">创建计时器的函数</param>
        /// <param name="timerTypeName">计时器类型名称（用于日志）</param>
        /// <returns>计时器Id，失败返回-1</returns>
        private int StartTimer(Func<TimerBase> createFunc, string timerTypeName)
        {
            var timerInfo = createFunc();

            if (timerInfo == null)
            {
                FuLogger.LogError($"[TimerModule] 启动{timerTypeName}失败，计时器创建失败！");
                return -1;
            }

            m_TimerDict[timerInfo.Id] = timerInfo;
            ExecuteTimerAsync(timerInfo).Forget();

            return timerInfo.Id;
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void PauseTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLogger.LogWarning($"[TimerModule] 暂停计时器{timerId}失败，不存在该计时器！");
                return;
            }

            if (timerInfo.IsPaused)
            {
                FuLogger.LogWarning($"[TimerModule] 暂停计时器{timerId}:{timerInfo.Name}失败，该计时器已处于暂停状态！");
                return;
            }

            timerInfo.IsPaused = true;
            FuLogger.LogInfo($"[TimerModule] 暂停计时器{timerId}:{timerInfo.Name}成功");
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void ResumeTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLogger.LogWarning($"[TimerModule] 恢复计时器{timerId}失败，不存在该计时器！");
                return;
            }

            if (!timerInfo.IsPaused)
            {
                FuLogger.LogWarning($"[TimerModule] 恢复计时器{timerId}:{timerInfo.Name}失败，该计时器已处于运行状态！");
                return;
            }

            timerInfo.IsPaused = false;
            FuLogger.LogInfo($"[TimerModule] 恢复计时器{timerId}:{timerInfo.Name}成功");
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void StopTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLogger.LogWarning($"[TimerModule] 停止计时器{timerId}失败，不存在该计时器！");
                return;
            }

            timerInfo.Cts.Cancel();
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers()
        {
            foreach (var (_, timerInfo) in m_TimerDict)
            {
                timerInfo.IsPaused = true;
            }
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            foreach (var (_, timerInfo) in m_TimerDict)
            {
                timerInfo.IsPaused = false;
            }
        }

        /// <summary>
        /// 停止所有计时器
        /// </summary>
        public void StopAllTimers()
        {
            foreach (var timerInfo in m_TimerDict.Values)
            {
                timerInfo.Cts.Cancel();
            }
        }

        /// <summary>
        /// 检查计时器是否存在
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns></returns>
        public bool IsTimerExist(int timerId) => m_TimerDict.ContainsKey(timerId);

        /// <summary>
        /// 检查计时器是否处于暂停状态
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns></returns>
        public bool IsTimerPaused(int timerId) => m_TimerDict.TryGetValue(timerId, out var timerInfo) && timerInfo.IsPaused;

        /// <summary>
        /// 获取所有计时器名称
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllTimerNames()
        {
            foreach (var timerInfo in m_TimerDict.Values)
            {
                yield return timerInfo.Name;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 执行计时器的异步方法(统一处理所有类型的计时器)
        /// </summary>
        /// <param name="timer">计时器对象</param>
        private async UniTaskVoid ExecuteTimerAsync(TimerBase timer)
        {
            var lastUpdateTime = timer.IgnoreTimeScale ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;
            var lastFrameCount = UnityEngine.Time.frameCount;

            try
            {
                while (!timer.IsCompleted)
                {
                    if (timer.IsPaused)
                    {
                        // 暂停时等待恢复，如果等待过程中被取消，则跳出循环，执行 finally 块，清理资源
                        await UniTask.WaitUntil(() => !timer.IsPaused, cancellationToken: timer.Cts.Token);
                        lastUpdateTime = timer.IgnoreTimeScale ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;
                        lastFrameCount = UnityEngine.Time.frameCount;
                        continue;
                    }

                    // 计算时间间隔和帧间隔
                    var currentTime       = timer.IgnoreTimeScale ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;
                    var currentFrameCount = UnityEngine.Time.frameCount;

                    var deltaTime   = currentTime       - lastUpdateTime;
                    var deltaFrames = currentFrameCount - lastFrameCount;

                    // 限制最大 deltaTime 防止卡顿后的时间跳跃
                    deltaTime = Math.Min(deltaTime, 0.1f);

                    lastUpdateTime = currentTime;
                    lastFrameCount = currentFrameCount;

                    // 更新计时器
                    timer.Update(deltaTime, deltaFrames);

                    // 检查计时器是否完成，调用完成回调
                    if (timer.IsCompleted)
                    {
                        timer.OnComplete();
                        break;
                    }

                    await UniTask.Yield(timer.PlayerLoopTiming, timer.Cts.Token);
                }
            }
            finally
            {
                // 计时器结束/取消时清理资源
                ReleaseTimer(timer.Id);
            }
        }

        /// <summary>
        /// 清理计时器资源
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        private void ReleaseTimer(int timerId)
        {
            if (!m_TimerDict.Remove(timerId, out var timerInfo)) return;
            if (timerInfo == null) return;
            FuLogger.LogInfo($"[TimerModule] 清理计时器{timerId}");
            ReferencePool.Runtime.ReferencePool.Release(timerInfo);
            OnTimerFinished?.Invoke(timerId);
        }

        #endregion
    }
}