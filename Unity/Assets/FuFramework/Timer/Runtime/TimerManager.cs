using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    /// <summary>
    /// 基于 UniTask 的计时器系统。
    /// 用于管理计时器任务，提供启动计时器、暂停计时器、恢复计时器、停止计时器、获取计时器状态等功能。
    /// </summary>
    public sealed class TimerManager : FuModule
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;

        /// <summary>
        /// 计时器字典，key为计时器Id，value为计时器项
        /// </summary>
        private readonly Dictionary<int, TimerInfoBase> m_TimerDict = new();

        /// <summary>
        /// 异步锁，防止多个协程可能同时操作字典导致异常
        /// </summary>
        private readonly object m_Lock = new();

        /// <summary>
        /// 获取当前计时器数量
        /// </summary>
        public int Count => m_TimerDict.Count;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            foreach (var timerInfo in m_TimerDict.Values)
            {
                timerInfo.Cts.Cancel();
                timerInfo.Cts.Dispose();
                ReferencePool.Runtime.ReferencePool.Release(timerInfo);
            }

            m_TimerDict.Clear();
        }

        #region Public Methods

        /// <summary>
        /// 启动一个基础的一次性计时器
        /// </summary>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="finishCallBack">计时器结束回调</param>
        /// <param name="updateCallBack">计时器更新回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns></returns>
        public int StartTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            if (duration <= 0)
            {
                FuLog.Error("[TimerManager] 计时器持续时间必须大于0");
                return -1;
            }

            var timerId   = Guid.NewGuid().GetHashCode();
            var timerInfo = TimerOnceInfo.Create(timerId, duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);

            if (timerInfo == null)
            {
                FuLog.Error("[TimerManager] 启动计时器失败，计时器创建失败！");
                return -1;
            }

            m_TimerDict[timerId] = timerInfo;
            ExecuteTimerAsync(timerId, timerInfo).Forget();

            return timerId;
        }

        /// <summary>
        /// 启动一个基础的一次性计时器
        /// </summary>
        /// <param name="interval">计时器间隔时间</param>
        /// <param name="intervalCallback">计时器每次间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns></returns>
        public int StartTimerInterval(float interval, Action intervalCallback, int repeatCount = -1, bool immediate = false, bool ignoreTimeScale = false)
        {
            if (interval <= 0)
            {
                FuLog.Error("[TimerManager] 间隔时间必须大于0");
                return -1;
            }

            if (intervalCallback == null)
            {
                FuLog.Error("[TimerManager] 时间间隔计时器回调函数不能为null");
                return -1;
            }

            var timerId   = Guid.NewGuid().GetHashCode();
            var timerInfo = TimerTimeIntervalInfo.Create(timerId, interval, intervalCallback, repeatCount, immediate, ignoreTimeScale);

            if (timerInfo == null)
            {
                FuLog.Error("[TimerManager] 启动间隔计时器失败，计时器创建失败！");
                return -1;
            }

            m_TimerDict[timerId] = timerInfo;
            ExecuteTimerAsync(timerId, timerInfo).Forget();

            return timerId;
        }

        /// <summary>
        /// 启动一个帧间隔计时器
        /// </summary>
        /// <param name="frameInterval">计时器帧间隔</param>
        /// <param name="intervalCallback">计时器每次帧间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <returns></returns>
        public int StartTimerFrameInterval(int frameInterval, Action intervalCallback, int repeatCount = -1, bool immediate = false, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update)
        {
            if (frameInterval <= 0)
            {
                FuLog.Error("[TimerManager] 帧间隔必须大于0");
                return -1;
            }

            if (intervalCallback == null)
            {
                FuLog.Error("[TimerManager] 帧间隔计时器回调函数不能为null");
                return -1;
            }

            var timerId   = Guid.NewGuid().GetHashCode();
            var timerInfo = TimerFrameIntervalInfo.Create(timerId, frameInterval, intervalCallback, repeatCount, immediate, playerLoopTiming);

            if (timerInfo == null)
            {
                FuLog.Error("[TimerManager] 启动帧间隔计时器失败，计时器创建失败！");
                return -1;
            }

            m_TimerDict[timerId] = timerInfo;
            ExecuteTimerAsync(timerId, timerInfo).Forget();

            return timerId;
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void PauseTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLog.Warning($"[TimerManager] 暂停计时器{timerId}失败，不存在该计时器！");
                return;
            }

            if (timerInfo.IsPaused)
            {
                FuLog.Warning($"[TimerManager] 暂停计时器{timerId}:{timerInfo.Name}失败，该计时器已处于暂停状态！");
                return;
            }

            timerInfo.IsPaused = true;
            FuLog.Info($"[TimerManager] 暂停计时器{timerId}:{timerInfo.Name}成功");
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void ResumeTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLog.Warning($"[TimerManager] 恢复计时器{timerId}失败，不存在该计时器！");
                return;
            }

            if (!timerInfo.IsPaused)
            {
                FuLog.Warning($"[TimerManager] 恢复计时器{timerId}:{timerInfo.Name}失败，该计时器已处于运行状态！");
                return;
            }

            timerInfo.IsPaused = false;
            FuLog.Info($"[TimerManager] 恢复计时器{timerId}:{timerInfo.Name}成功");
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void StopTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLog.Warning($"[TimerManager] 停止计时器{timerId}失败，不存在该计时器！");
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
            var timerIds = m_TimerDict.Keys.ToArray();
            foreach (var timerId in timerIds)
            {
                StopTimer(timerId);
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
        public IEnumerable<string> GetAllTimerNames() => m_TimerDict.Values.Select(x => x.Name);

        #endregion

        #region Private Methods

        /// <summary>
        /// 执行计时器的异步方法(统一处理所有类型的计时器)
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <param name="timerInfo">计时器对象</param>
        private async UniTaskVoid ExecuteTimerAsync(int timerId, TimerInfoBase timerInfo)
        {
            var lastUpdateTime = GetCurrentTime(timerInfo.IgnoreTimeScale);
            var lastFrameCount = UnityEngine.Time.frameCount;

            try
            {
                while (!timerInfo.IsCompleted)
                {
                    if (timerInfo.IsPaused)
                    {
                        // 暂停时等待恢复，如果等待过程中被取消，则跳出循环，执行 finally 块，清理资源
                        await UniTask.WaitUntil(() => !timerInfo.IsPaused, cancellationToken: timerInfo.Cts.Token);
                        lastUpdateTime = GetCurrentTime(timerInfo.IgnoreTimeScale);
                        lastFrameCount = UnityEngine.Time.frameCount;
                        continue;
                    }

                    // 计算时间间隔和帧间隔
                    var currentTime       = GetCurrentTime(timerInfo.IgnoreTimeScale);
                    var currentFrameCount = UnityEngine.Time.frameCount;

                    var deltaTime   = currentTime       - lastUpdateTime;
                    var deltaFrames = currentFrameCount - lastFrameCount;

                    // 限制最大 deltaTime 防止卡顿后的时间跳跃
                    deltaTime = Math.Min(deltaTime, 0.1f);

                    lastUpdateTime = currentTime;
                    lastFrameCount = currentFrameCount;

                    // 更新计时器
                    timerInfo.Update(deltaTime, deltaFrames);

                    // 检查普通计时器是否完成
                    if (timerInfo is TimerOnceInfo { IsCompleted: true } normalTimer)
                    {
                        normalTimer.FinishCallBack?.Invoke();
                        break;
                    }

                    await UniTask.Yield(timerInfo.PlayerLoopTiming, timerInfo.Cts.Token);
                }
            }
            finally
            {
                // 计时器结束/取消时清理资源
                ReleaseTimer(timerId);
            }
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns>当前时间</returns>
        private float GetCurrentTime(bool ignoreTimeScale)
        {
            return ignoreTimeScale ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;
        }
        
        /// <summary>
        /// 清理计时器资源
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        private void ReleaseTimer(int timerId)
        {
            lock (m_Lock)
            {
                if (!m_TimerDict.Remove(timerId, out var timerInfo)) return;
                if (timerInfo == null) return;
                FuLog.Info($"[TimerManager] 清理计时器{timerId}");
                ReferencePool.Runtime.ReferencePool.Release(timerInfo);
            }
        }

        #endregion
    }
}