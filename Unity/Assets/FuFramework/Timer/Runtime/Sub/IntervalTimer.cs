using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    /// <summary>
    /// 时间间隔计时器
    /// 功能：按照固定的时间间隔重复执行回调，支持立即执行和重复次数限制
    /// 特点：
    /// - 抗时间跳跃：通过累计时间机制处理卡顿情况，确保间隔准确性
    /// - 灵活的重置策略：支持无限循环或指定次数执行
    /// - 实时性保障：在时间累积超过间隔时，会连续执行直到追赶上进度
    /// 适用场景：心跳包发送、定期数据保存、周期性状态检查等
    /// </summary>
    internal class IntervalTimer : TimerBase
    {
        /// <summary>
        /// 执行间隔时间（秒）
        /// </summary>
        public float Interval { get; private set; }

        /// <summary>
        /// 间隔到达时触发的回调
        /// </summary>
        public Action IntervalCallback { get; private set; }

        /// <summary>
        /// 最大执行次数（-1表示无限循环）
        /// </summary>
        public int MaxCount { get; private set; }

        /// <summary>
        /// 已执行次数
        /// </summary>
        public int ExecutedCount { get; private set; }

        /// <summary>
        /// 累计时间（用于处理时间跳跃）
        /// </summary>
        public float AccumulatedTime { get; private set; }


        /// <summary>
        /// 计时器名称
        /// </summary>
        public override string Name => $"类型：时间间隔计时器, Id：{Id}, 间隔更新回调：{IntervalCallback?.Method.Name}";
        
        /// <summary>
        /// 是否已完成（执行次数达到最大值）
        /// </summary>
        public override bool IsCompleted => ExecutedCount >= MaxCount;

        
        /// <summary>
        /// 清理计时器
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            Interval         = 0;
            IntervalCallback = null;
            MaxCount         = 0;
            ExecutedCount    = 0;
            AccumulatedTime  = 0;
        }

        /// <summary>
        /// 更新计时器状态
        /// 功能：累计时间增量，当达到间隔时间时触发回调，支持连续追赶机制
        /// 机制：使用while循环确保在卡顿情况下也能正确执行所有遗漏的回调
        /// </summary>
        /// <param name="deltaTime">增量时间（秒）</param>
        /// <param name="_"></param>
        public override void Update(float deltaTime, int _)
        {
            if (IsPaused) return;

            AccumulatedTime += deltaTime;

            // 处理可能的时间跳跃（如卡顿）
            while (AccumulatedTime >= Interval && ExecutedCount < MaxCount)
            {
                AccumulatedTime -= Interval;
                ExecutedCount++;
                IntervalCallback?.Invoke();
            }
        }

        /// <summary>
        /// 创建时间间隔计时器
        /// 功能：从对象池获取实例并初始化参数，支持立即执行第一次回调
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <param name="interval">计时器间隔时间</param>
        /// <param name="intervalCallback">计时器每次间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns></returns>
        public static IntervalTimer Create(int timerId, float interval, Action intervalCallback, int repeatCount, bool immediate, bool ignoreTimeScale)
        {
            var timerInfo = ReferencePool.Runtime.ReferencePool.Acquire<IntervalTimer>();
            if (timerInfo == null) return null;

            timerInfo.Id               = timerId;
            timerInfo.Interval         = interval;
            timerInfo.IntervalCallback = intervalCallback;
            timerInfo.MaxCount         = repeatCount <= 0 ? int.MaxValue : repeatCount;
            timerInfo.ExecutedCount    = 0;
            timerInfo.AccumulatedTime  = 0;
            timerInfo.IgnoreTimeScale  = ignoreTimeScale;
            timerInfo.IsPaused         = false;
            timerInfo.Cts              = new CancellationTokenSource();
            timerInfo.PlayerLoopTiming = PlayerLoopTiming.Update;

            // 是否立即执行第一次回调
            if (immediate)
            {
                timerInfo.ExecutedCount++;
                intervalCallback?.Invoke();
            }

            return timerInfo;
        }
    }
}