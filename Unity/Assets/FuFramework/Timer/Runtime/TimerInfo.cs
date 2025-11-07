using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    /// <summary>
    /// 计时器信息类
    /// </summary>
    internal class TimerInfo : IReference
    {
        /// <summary>
        /// 计时器ID
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public float DurationTime { get; private set; }

        /// <summary>
        /// 剩余时间
        /// </summary>
        public float RemainingTime { get; internal set; }

        /// <summary>
        /// 是否忽略时间缩放
        /// </summary>
        public bool IgnoreTimeScale { get; private set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; internal set; }

        /// <summary>
        /// 取消计时器的令牌
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }

        /// <summary>
        /// update更新时机类型
        /// </summary>
        public PlayerLoopTiming PlayerLoopTiming { get; private set; }

        /// <summary>
        /// 完成时的回调函数
        /// </summary>
        public Action FinishCallBack { get; private set; }

        /// <summary>
        /// 计时器更新时的回调函数
        /// </summary>
        public Action UpdateCallBack { get; private set; }

        /// <summary>
        /// 当前进度
        /// </summary>
        public float Progress => 1f - RemainingTime / DurationTime;
        
        /// <summary>
        /// 清理引用。
        /// </summary>
        public void Clear()
        {
            Cts?.Cancel();
            Cts            = null;
            FinishCallBack = null;
            UpdateCallBack = null;
        }

        /// <summary>
        /// 从引用池中创建一个计时器实例
        /// </summary>
        /// <param name="timerId">计时器Id</param>
        /// <param name="duration">持续时间</param>
        /// <param name="finishCallBack">完成时的回调函数</param>
        /// <param name="updateCallBack">帧更新时的回调函数</param>
        /// <param name="playerLoopTiming">帧更新时机类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns>计时器实例</returns>
        public static TimerInfo Create(int timerId, float duration, Action finishCallBack, Action updateCallBack, PlayerLoopTiming playerLoopTiming, bool ignoreTimeScale)
        {
            var timerInfo = ReferencePool.Runtime.ReferencePool.Acquire<TimerInfo>();
            if (timerInfo == null) return null;

            timerInfo.Id               = timerId;
            timerInfo.DurationTime     = duration;
            timerInfo.RemainingTime    = duration;
            timerInfo.FinishCallBack   = finishCallBack;
            timerInfo.UpdateCallBack   = updateCallBack;
            timerInfo.PlayerLoopTiming = playerLoopTiming;
            timerInfo.IgnoreTimeScale  = ignoreTimeScale;
            timerInfo.IsPaused         = false;
            timerInfo.Cts              = new CancellationTokenSource();

            return timerInfo;
        }
    }
}