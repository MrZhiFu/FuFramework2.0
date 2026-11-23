using System;
using Hotfix.Framework.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
	/// <summary>
	/// 一次性倒计时计时器：指定时长结束后触发完成回调，期间可接收每帧更新（进度查询，
	/// 可忽略时间缩放，适用于 UI 动画/逻辑延时/进度条等场景）。
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
		/// 当前进度（0到1的归一化值）。
		/// Clamp01：最后一帧超时会算出负的剩余时间，不钳制会短暂出现 >1 的进度。
		/// </summary>
		public float Progress => Mathf.Clamp01(1f - RemainingTime / DurationTime);


		/// <summary>
		/// 是否已完成（剩余时间小于等于0）
		/// </summary>
		public override bool IsCompleted => RemainingTime <= 0;


		/// <summary>
		/// 创建一次性倒计时计时器
		/// 功能：从对象池获取实例并初始化参数
		/// </summary>
		/// <param name="timerId">计时器Id</param>
		/// <param name="duration">计时器持续时间</param>
		/// <param name="finishCallBack">计时器结束回调</param>
		/// <param name="updateCallBack">计时器更新回调</param>
		/// <param name="ignoreTimeScale">是否忽略时间缩放</param>
		public static CountdownTimer Create(int timerId, float duration, Action finishCallBack, Action updateCallBack, bool ignoreTimeScale)
		{
			var timerInfo = ReferencePool.Acquire<CountdownTimer>();
			if (timerInfo == null) return null;

			timerInfo.InitCommon(timerId, ignoreTimeScale);
			timerInfo.DurationTime   = duration;
			timerInfo.RemainingTime  = duration;
			timerInfo.FinishCallBack = finishCallBack;
			timerInfo.UpdateCallBack = updateCallBack;

			return timerInfo;
		}

		/// <summary>
		/// 帧更新计时器
		/// 功能：根据时间增量减少剩余时间，并触发更新回调
		/// </summary>
		/// <param name="deltaTime">增量时间（秒）</param>
		/// <param name="_">暂不使用</param>
		public override void OnUpdate(float deltaTime, int _)
		{
			if (IsPaused) return;

			RemainingTime -= deltaTime;
			UpdateCallBack?.Invoke();
		}

		/// <summary>
		/// 重置计时器进度：剩余时间回满
		/// </summary>
		public override void Reset()
		{
			RemainingTime = DurationTime;
		}

		/// <summary>
		/// 当计时器完成时调用
		/// </summary>
		public override void OnComplete() => FinishCallBack?.Invoke();

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
	}
}