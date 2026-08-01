using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
    /// <summary>
    /// 帧间隔计时器。
    /// 目标：按照固定的帧数间隔重复执行回调，基于帧率而非时间。
    /// 功能：
    ///     1. 帧率无关性：执行频率与游戏帧率绑定，不受时间缩放影响。
    ///     2. 帧精确控制：适用于需要与渲染帧同步的逻辑。
    ///     3. 抗帧率波动：通过累计帧数机制处理帧率波动，确保执行准确性。
    /// </summary>
    internal class FrameTimer : TimerBase
    {
        /// <summary>
        /// 执行间隔帧数
        /// </summary>
        public int FrameInterval { get; private set; }

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
        /// 累计帧数（用于处理帧率波动）
        /// </summary>
        public int AccumulatedFrames { get; private set; }


        /// <summary>
        /// 计时器名称
        /// </summary>
        public override string Name => $"类型：帧间隔计时器, Id：{Id}, 间隔更新回调：{IntervalCallback?.Method.Name}";

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
            FrameInterval     = 0;
            IntervalCallback  = null;
            MaxCount          = 0;
            ExecutedCount     = 0;
            AccumulatedFrames = 0;
        }

        /// <summary>
        /// 更新计时器状态
        /// 功能：累计帧数增量，当达到间隔帧数时触发回调，支持连续追赶机制
        /// 机制：使用while循环确保在帧率波动情况下也能正确执行所有遗漏的回调
        /// </summary>
        /// <param name="_">暂不使用</param>
        /// <param name="deltaFrames"> 增量帧数 </param>
        public override void Update(float _, int deltaFrames)
        {
            if (IsPaused) return;

            // 确保至少增加1帧
            AccumulatedFrames += Math.Max(deltaFrames, 1);

            while (AccumulatedFrames >= FrameInterval && ExecutedCount < MaxCount)
            {
                AccumulatedFrames -= FrameInterval;
                ExecutedCount++;
                IntervalCallback?.Invoke();
            }
        }

        /// <summary>
        /// 创建帧间隔计时器
        /// 功能：从对象池获取实例并初始化参数，支持立即执行第一次回调
        /// 注意：帧间隔计时器始终忽略时间缩放，确保与帧率同步
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <param name="frameInterval">计时器帧间隔</param>
        /// <param name="intervalCallback">计时器每次帧间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <returns></returns>
        public static FrameTimer Create(int timerId, int frameInterval, Action intervalCallback, int repeatCount, bool immediate, PlayerLoopTiming playerLoopTiming)
        {
            var timerInfo = GlobalModule.ReferencePoolModule.Acquire<FrameTimer>();
            if (timerInfo == null) return null;

            timerInfo.Id                = timerId;
            timerInfo.FrameInterval     = frameInterval;
            timerInfo.IntervalCallback  = intervalCallback;
            timerInfo.MaxCount          = repeatCount < 0 ? int.MaxValue : repeatCount;
            timerInfo.ExecutedCount     = 0;
            timerInfo.AccumulatedFrames = 0;
            timerInfo.IgnoreTimeScale   = true; // 帧间隔计时器总是忽略时间缩放
            timerInfo.IsPaused          = false;
            timerInfo.Cts               = new CancellationTokenSource();
            timerInfo.PlayerLoopTiming  = playerLoopTiming;

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
