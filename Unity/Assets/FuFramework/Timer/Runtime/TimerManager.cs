using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable AccessToModifiedClosure
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
        private readonly Dictionary<int, TimerInfo> m_TimerDict = new();


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
        /// 启动一个基础计时器
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="updateCallBack">每帧/每秒回调(如果loopTiming为Update，则每帧回调；如果loopTiming为TimeUpdate，则每秒回调)</param>
        /// <param name="playerLoopTiming">计时器执行时机</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <param name="finishCallBack">计时器执行回调</param>
        /// <returns>计时器ID，用于停止指定计时器</returns>
        public int StartTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            if (duration <= 0)
            {
                FuLog.Error("[TimerManager] 计时器持续时间必须大于0");
                return -1;
            }

            var timerId   = Guid.NewGuid().GetHashCode();
            
            FuLog.Info($"[TimerManager] 启动计时器{timerId}:{updateCallBack?.Method.Name}，持续时间:{duration}秒，执行时机:{playerLoopTiming}，是否忽略时间缩放:{ignoreTimeScale}");
            
            var timerInfo = TimerInfo.Create(timerId, duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);
            if (timerInfo == null)
            {
                FuLog.Error("[TimerManager] 启动计时器失败，计时器创建失败！");
                return -1;
            }

            m_TimerDict[timerId] = timerInfo;

            // 执行计时器
            ExecuteTimerAsync(timerId, timerInfo).Forget();
            return timerId;
        }

        /// <summary>
        /// 启动一个时间间隔计时器
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="intervalCallback">每次间隔时间执行的回调函数</param>
        /// <param name="repeatCount">循环次数，小于等于0表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public int StartTimerInterval(float interval, Action intervalCallback, int repeatCount = -1,  bool immediate = false, bool ignoreTimeScale = false)
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

            var elapsedTime = 0f;
            var executedCount = 0;
            var maxCount = repeatCount <= 0 ? int.MaxValue : repeatCount;

            // 立即执行第一次回调
            if (immediate)
            {
                intervalCallback();
                executedCount = 1;
        
                if (executedCount >= maxCount) return -1;
            }
            var timerId = -1;
            timerId = StartTimer(float.MaxValue, null, UpdateCallBack, PlayerLoopTiming.Update, ignoreTimeScale);
            return timerId;

            // 每帧更新回调
            void UpdateCallBack()
            {
                // 检查是否已完成
                if (executedCount >= maxCount) return;

                elapsedTime += ignoreTimeScale ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
        
                // 处理可能的时间跳跃（如卡顿）
                while (elapsedTime >= interval && executedCount < maxCount)
                {
                    elapsedTime -= interval;
                    executedCount++;
                    intervalCallback();
                }

                // 检查是否达到执行次数
                if (executedCount < maxCount) return;
                
                FuLog.Info($"[TimerManager] 间隔计时器{timerId}已完成，共执行{executedCount}次");
                
                // 使用下一帧来停止，避免在当前回调中操作
                UniTask.DelayFrame(1).ContinueWith(() =>
                {
                    if (IsTimerExist(timerId)) // 额外检查是否存在
                        StopTimer(timerId);
                }).Forget();
            }
        }

        /// <summary>
        /// 启动一个帧间隔计时器
        /// </summary>
        /// <param name="frameInterval">间隔帧数</param>
        /// <param name="intervalCallback">每次间隔帧数执行的回调函数</param>
        /// <param name="repeatCount">循环次数，小于等于0表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="playerLoopTiming">计时器执行时机</param>
        /// <returns>计时器ID</returns>
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

            var elapsedFrames = 0;
            var executedCount = 0;
            var maxCount = repeatCount <= 0 ? int.MaxValue : repeatCount;

            // 立即执行第一次回调
            if (immediate)
            {
                intervalCallback();
                executedCount = 1;
        
                // 如果只需要执行一次且立即执行了，直接返回
                if (maxCount == 1) return -1;
            }

            // 使用局部变量避免闭包问题
            var timerId = -1;
            timerId = StartTimer(float.MaxValue, null, UpdateCallback, playerLoopTiming, true);
            return timerId;

            // 每帧更新回调
            void UpdateCallback()
            {
                // 先检查是否已经完成（防御性检查）
                if (executedCount >= maxCount) return;

                elapsedFrames++;
                if (elapsedFrames < frameInterval) return;

                elapsedFrames = 0;
                executedCount++;
                intervalCallback();

                // 检查是否达到执行次数
                if (executedCount < maxCount) return;
                
                FuLog.Info($"[TimerManager] 间隔计时器{timerId}已完成，共执行{executedCount}次");
                
                // 使用下一帧来停止，避免在当前回调中操作
                UniTask.DelayFrame(1).ContinueWith(() =>
                    {
                        if (IsTimerExist(timerId)) // 额外检查
                            StopTimer(timerId);
                    })
                    .Forget();
            }
        }
        
        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void PauseTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLog.Warning($"[TimerManager] 暂停计时器{timerId}失败，不存在该计时器！");
                return;
            }

            if (timerInfo.IsPaused)
            {
                FuLog.Warning($"[TimerManager] 暂停计时器{timerId}:{timerInfo.FinishCallBack.Method.Name}失败，该计时器已处于暂停状态！");
                return;
            }

            timerInfo.IsPaused = true;
            FuLog.Info($"[TimerManager] 暂停计时器{timerId}:{timerInfo.FinishCallBack.Method.Name}成功，剩余时间:{timerInfo.RemainingTime}秒");
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void ResumeTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLog.Warning($"[TimerManager] 恢复计时器{timerId}失败，不存在该计时器！");
                return;
            }

            if (!timerInfo.IsPaused)
            {
                FuLog.Warning($"[TimerManager] 恢复计时器{timerId}:{timerInfo.FinishCallBack.Method.Name}失败，该计时器已处于运行状态！");
                return;
            }

            timerInfo.IsPaused = false;
            FuLog.Info($"[TimerManager] 恢复计时器{timerId}:{timerInfo.FinishCallBack.Method.Name}成功，剩余时间:{timerInfo.RemainingTime}秒");
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
            timerInfo.Cts.Dispose();
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
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerExist(int timerId) => m_TimerDict.ContainsKey(timerId);

        /// <summary>
        /// 检查计时器是否处于暂停状态
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerPaused(int timerId) => m_TimerDict.TryGetValue(timerId, out var timerInfo) && timerInfo.IsPaused;

        /// <summary>
        /// 获取计时器进度
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public float GetProgress(int timerId)
        {
            if (m_TimerDict.TryGetValue(timerId, out var timerInfo)) return timerInfo.Progress;
            FuLog.Warning($"[TimerManager] 获取计时器{timerId}进度失败，不存在该计时器！");
            return 0;
        }
        
        /// <summary>
        /// 获取所有计时器名称
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllTimerNames() => m_TimerDict.Values.Select(x => x.FinishCallBack.Method.Name);

        #endregion

        #region Private Methods

        /// <summary>
        /// 执行计时器的异步方法
        /// </summary>
        private async UniTaskVoid ExecuteTimerAsync(int timerId, TimerInfo timerInfo)
        {
            var totalElapsedTime = 0f;
            var lastUpdateTime   = GetCurrentTime(timerInfo.IgnoreTimeScale);

            try
            {
                // 等待计时器结束，或计时器被取消(计时器被取消时，会抛出OperationCanceledException异常，Finally块会清理资源)
                while (totalElapsedTime < timerInfo.DurationTime)
                {
                    if (timerInfo.IsPaused)
                    {
                        // 暂停时等待恢复
                        await UniTask.WaitUntil(() => !timerInfo.IsPaused, cancellationToken: timerInfo.Cts.Token);
                        lastUpdateTime = GetCurrentTime(timerInfo.IgnoreTimeScale);
                        continue;
                    }

                    // 计算时间间隔
                    var currentTime = GetCurrentTime(timerInfo.IgnoreTimeScale);
                    var deltaTime   = currentTime - lastUpdateTime;

                    // 限制最大 deltaTime 防止卡顿后的时间跳跃
                    deltaTime      = Math.Min(deltaTime, 0.1f);
                    lastUpdateTime = currentTime;

                    totalElapsedTime        += deltaTime;
                    timerInfo.RemainingTime =  timerInfo.DurationTime - totalElapsedTime;

                    // 执行每帧回调
                    timerInfo.UpdateCallBack?.Invoke();

                    await UniTask.Yield(timerInfo.PlayerLoopTiming, timerInfo.Cts.Token);
                }

                // 执行完成回调
                timerInfo.FinishCallBack?.Invoke();
            }
            finally
            {
                // 计时器结束/取消时清理资源
                CleanupTimer(timerId);
            }
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <param name="ignoreTimeScale"></param>
        /// <returns></returns>
        private float GetCurrentTime(bool ignoreTimeScale)
        {
            return ignoreTimeScale ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;
        }

        /// <summary>
        /// 清理计时器资源
        /// </summary>
        /// <param name="timerId"></param>
        private void CleanupTimer(int timerId)
        {
            if (!m_TimerDict.TryGetValue(timerId, out var timerInfo))
            {
                FuLog.Warning($"[TimerManager] 清理计时器{timerId}失败，不存在该计时器！");
                return;
            }

            FuLog.Info($"[TimerManager] 清理计时器{timerId}");
            ReferencePool.Runtime.ReferencePool.Release(timerInfo);
            m_TimerDict.Remove(timerId);
        }

        #endregion
    }
}