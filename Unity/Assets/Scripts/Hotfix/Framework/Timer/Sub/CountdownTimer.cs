using Hotfix.Framework.ReferencePools;
﻿using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
    /// <summary>
    /// 倒计时计时器。
    /// 目标：在指定的持续时间结束后触发完成回调，期间可接收每帧更新回调。
    /// 功能：
    ///     1. 精确控制执行时长，支持进度查询。
    ///     2. 可配置是否忽略时间缩放，适用于UI动画、游戏逻辑延时等场景。
    ///     3. 提供更新回调，便于实现进度条、倒计时显示等需求。
    /// </summary>
    internal class CountdownTimer : TimerBase
    {
        /// <summary>
        /// 总持续时间（秒）
        /// </summary>
        public float DurationTime { get; private set; }

        /// <summary>
        /// 剩余时间（秒）
        /// </summary>
        public float RemainingTime { get; private set; }

        /// <summary>
        /// 计时器结束时触发的回调
        /// </summary>
        public Action FinishCallBack { get; private set; }

        /// <summary>
        /// 每帧更新时触发的回调
        /// </summary>
        public Action UpdateCallBack { get; private set; }

        /// <summary>
        /// 当前进度（0到1的归一化值）
        /// </summary>
        public float Progress => 1f - RemainingTime / DurationTime;


        /// <summary>
        /// 计时器名称
        /// </summary>
        public override string Name => $"类型：一次性计时器, Id：{Id}, 完成回调：{FinishCallBack?.Method.Name}, 帧更新回调：{UpdateCallBack.Method.Name}";

        /// <summary>
        /// 是否已完成（剩余时间小于等于0）
        /// </summary>
        public override bool IsCompleted => RemainingTime <= 0;


        /// <summary>
        /// 清理计时器
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            DurationTime   = 0;
            RemainingTime  = 0;
            FinishCallBack = null;
            UpdateCallBack = null;
        }

        /// <summary>
        /// 更新计时器
        /// 功能：根据时间增量减少剩余时间，并触发更新回调 
        /// </summary>
        /// <param name="deltaTime">增量时间（秒）</param>
        /// <param name="_">暂不使用</param>
        public override void Update(float deltaTime, int _)
        {
            if (IsPaused) return;

            RemainingTime -= deltaTime;
            UpdateCallBack?.Invoke();
        }

        /// <summary>
        /// 当计时器完成时调用
        /// </summary>
        public override void OnComplete() => FinishCallBack?.Invoke();

        /// <summary>
        /// 创建一次性计时器
        /// 功能：从对象池获取实例并初始化参数
        /// </summary>
        /// <param name="timerId">计时器Id</param>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="finishCallBack">计时器结束回调</param>
        /// <param name="updateCallBack">计时器更新回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <returns></returns>
        public static CountdownTimer Create(int timerId, float duration, Action finishCallBack, Action updateCallBack, PlayerLoopTiming playerLoopTiming, bool ignoreTimeScale)
        {
            var timerInfo = ReferencePool.Acquire<CountdownTimer>();
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
