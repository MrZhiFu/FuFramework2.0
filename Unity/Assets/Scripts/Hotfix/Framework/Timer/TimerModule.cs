using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
	/// <summary>
	/// 计时器管理模块。
	/// 目标： 用于管理计时器任务，提供启动计时器、暂停计时器、恢复计时器、停止计时器、获取计时器状态等功能。
	/// 功能：
	///     1. 启动一次性倒计时计时器、时间间隔计时器、帧间隔计时器。
	///     2. 暂停，继续，取消，停止(单个/所有)计时器。
	/// 说明：统一帧驱动——本模块 override OnUpdate，每帧一次遍历驱动所有计时器，
	/// 不为每个计时器起独立异步链（无 UniTask 任务、无每计时器 CancellationTokenSource）。
	/// </summary>
	public sealed partial class TimerModule : ModuleBase
	{
		/// <summary>
		/// 计时器字典，key为计时器Id，value为计时器项
		/// </summary>
		private readonly Dictionary<int, TimerBase> m_timerDict = new();

		/// <summary>
		/// 帧驱动遍历用的复用快照缓冲。
		/// 用户回调内可能调用 StopTimer/StopAllTimers/StartTimer 修改 m_timerDict，
		/// 直接遍历字典会「遍历中修改容器」抛异常，故每帧先快照到本缓冲再遍历。
		/// 复用实例字段避免每帧分配。
		/// </summary>
		private readonly List<TimerBase> m_updateSnapshot = new();

		/// <summary>
		/// 下一个计时器ID
		/// </summary>
		private int m_nextTimerId = 0;

		/// <summary>
		/// 累计启动数（生命周期统计，供 GetStatistics）
		/// </summary>
		private int m_startedCount;

		/// <summary>
		/// 累计自然完成数（倒计时到点/次数打满）
		/// </summary>
		private int m_finishedCount;

		/// <summary>
		/// 累计被主动停止数（StopTimer/StopAllTimers）
		/// </summary>
		private int m_stoppedCount;

		/// <summary>
		/// 累计异常终止数（回调抛异常被隔离回收）
		/// </summary>
		private int m_errorCount;

		/// <summary>
		/// 获取当前计时器数量
		/// </summary>
		public int Count => m_timerDict.Count;

		/// <summary>
		/// 计时器完成/停止时触发的事件
		/// </summary>
		public event Action<int> OnTimerFinished;

		/// <summary>
		/// 初始化
		/// </summary>
		protected internal override void OnInit() { }

		/// <summary>
		/// 帧驱动：每帧一次遍历驱动所有计时器（由 GameDriven.Update → ModuleManager.Update 统一调用）。
		/// 暂停的计时器跳过——暂停期间不累计时间，恢复后无需修正时间基准。
		/// </summary>
		protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
		{
			if (m_timerDict.Count == 0) return;

			// 快照：遍历前先快照到本缓冲，避免用户回调内修改 m_timerDict 时字典遍历抛异常。
			SnapshotTimers();

			foreach (var timer in m_updateSnapshot)
			{
				// 有效性校验：快照中的实例可能已在本次遍历期间被用户回调 Stop/回收（引用池同槽位复用
				// 会让旧引用指向新计时器）。以「Id 仍映射到本实例」为准，失效条目直接跳过，不触碰。
				if (!m_timerDict.TryGetValue(timer.Id, out var mapped) || mapped != timer) continue;

				try
				{
					UpdateTimer(timer, deltaTime, unscaledDeltaTime);
				}
				catch (Exception e)
				{
					// 单计时器回调异常只终结它自己（与其余计时器隔离，同原异步链「异常只终结本链」的行为），
					// 回收后继续驱动其余计时器。
					FuLogger.LogError($"[TimerModule] 计时器{timer.Id}({timer.GetType().Name})更新异常，已终止: {e.Message}\n{e.StackTrace}");
					m_errorCount++;
					ReleaseTimer(timer.Id);
				}
			}
		}

		/// <summary>
		/// 释放。
		/// 直接遍历回收：统一帧驱动下没有在途异步链需要取消，无需原实现的「先快照清字典再逐个取消」防御
		/// （若本方法发生在帧驱动遍历途中——如回调内触发重启——遍历余下的快照条目因有效性校验失败被跳过，
		/// 不会触碰已回收实例）。与原实现一致：本方法不派发 OnTimerFinished，回收由本方法唯一负责。
		/// </summary>
		protected internal override void OnDispose()
		{
			if (m_timerDict.Count == 0) return;

			foreach (var (_, timerInfo) in m_timerDict)
			{
				if (timerInfo != null) ReferencePool.Recycle(timerInfo);
			}

			m_timerDict.Clear();
#if UNITY_EDITOR
			ClearAllStartStackTraces();
#endif
		}

		#region Public Methods

		/// <summary>
		/// 启动一个一次性倒计时计时器
		/// </summary>
		/// <param name="duration">计时器持续时间秒</param>
		/// <param name="finishCallBack">计时器结束回调</param>
		/// <param name="updateCallBack">计时器持续期间每帧更新回调</param>
		/// <param name="ignoreTimeScale">是否忽略时间缩放</param>
		/// <returns>倒计时Id</returns>
		public int StartCountdownTimer(float duration, Action finishCallBack, Action updateCallBack = null, bool ignoreTimeScale = false)
		{
			if (duration <= 0)
			{
				FuLogger.LogError("[TimerModule] 计时器持续时间必须大于0");
				return -1;
			}

			return StartTimer(() => CountdownTimer.Create(++m_nextTimerId, duration, finishCallBack, updateCallBack, ignoreTimeScale), "一次性倒计时计时器");
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

			return StartTimer(() => IntervalTimer.Create(++m_nextTimerId, interval, intervalCallback, repeatCount, immediate, ignoreTimeScale), "间隔计时器");
		}

		/// <summary>
		/// 启动一个帧间隔计时器
		/// </summary>
		/// <param name="frameInterval">计时器帧间隔</param>
		/// <param name="intervalCallback">计时器每次帧间隔回调</param>
		/// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
		/// <param name="immediate">是否立即执行第一次回调</param>
		/// <returns>倒计时Id</returns>
		public int StartFrameTimer(int frameInterval, Action intervalCallback, int repeatCount = -1, bool immediate = false)
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

			return StartTimer(() => FrameTimer.Create(++m_nextTimerId, frameInterval, intervalCallback, repeatCount, immediate), "帧间隔计时器");
		}

		/// <summary>
		/// 启动计时器的通用方法
		/// </summary>
		/// <param name="createFunc">创建计时器的函数</param>
		/// <param name="timerTypeName">计时器类型名称（用于日志）</param>
		/// <returns>计时器Id，失败返回-1</returns>
		private int StartTimer(Func<TimerBase> createFunc, string timerTypeName)
		{
			// Create 不执行 immediate 首帧回调（见 TimerBase.Immediate）：回调统一延迟到计时器入字典、
			// 被帧驱动遍历到时执行，故这里不会因用户回调抛异常而漏回收已 Acquire 的实例。
			TimerBase timerInfo;
			try
			{
				timerInfo = createFunc();
			}
			catch (Exception e)
			{
				// 兜底：Create 内的 Acquire/字段初始化失败时无实例可回收（引用池 Acquire 失败返回 null 而非抛异常），
				// 此处只保证不把异常抛给调用方（与其它失败路径一致返回 -1）。
				FuLogger.LogError($"[TimerModule] 启动{timerTypeName}失败: {e.Message}\n{e.StackTrace}");
				return -1;
			}

			if (timerInfo == null)
			{
				FuLogger.LogError($"[TimerModule] 启动{timerTypeName}失败，计时器创建失败！");
				return -1;
			}

			var timerId = timerInfo.Id;
			m_timerDict[timerId] = timerInfo;

			m_startedCount++;
#if UNITY_EDITOR
			CaptureStartStackTrace(timerId);
#endif

			// 统一帧驱动：无需为计时器单独起链，本帧（或下一帧）的 OnUpdate 会遍历到它；
			// 其 immediate 回调届时执行，异常由该次遍历的 catch 兜底回收。
			return timerId;
		}

		/// <summary>
		/// 暂停计时器
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		public void PauseTimer(int timerId)
		{
			if (!m_timerDict.TryGetValue(timerId, out var timerInfo))
			{
				FuLogger.LogWarning($"[TimerModule] 暂停计时器{timerId}失败，不存在该计时器！");
				return;
			}

			if (timerInfo.IsPaused)
			{
				FuLogger.LogWarning($"[TimerModule] 暂停计时器{timerId}({timerInfo.GetType().Name})失败，该计时器已处于暂停状态！");
				return;
			}

			timerInfo.IsPaused = true;
			FuLogger.LogInfo($"[TimerModule] 暂停计时器{timerId}({timerInfo.GetType().Name})成功");
		}

		/// <summary>
		/// 恢复计时器
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		public void ResumeTimer(int timerId)
		{
			if (!m_timerDict.TryGetValue(timerId, out var timerInfo))
			{
				FuLogger.LogWarning($"[TimerModule] 恢复计时器{timerId}失败，不存在该计时器！");
				return;
			}

			if (!timerInfo.IsPaused)
			{
				FuLogger.LogWarning($"[TimerModule] 恢复计时器{timerId}({timerInfo.GetType().Name})失败，该计时器已处于运行状态！");
				return;
			}

			timerInfo.IsPaused = false;
			FuLogger.LogInfo($"[TimerModule] 恢复计时器{timerId}({timerInfo.GetType().Name})成功");
		}

		/// <summary>
		/// 停止计时器
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		public void StopTimer(int timerId)
		{
			if (!m_timerDict.ContainsKey(timerId))
			{
				FuLogger.LogWarning($"[TimerModule] 停止计时器{timerId}失败，不存在该计时器！");
				return;
			}

			m_stoppedCount++;
			ReleaseTimer(timerId);
		}

		/// <summary>
		/// 重置计时器进度（仅清零进度，不重发 immediate，不改暂停状态；
		/// 不增删字典，可在计时器回调内安全调用）
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		public void ResetTimer(int timerId)
		{
			if (!m_timerDict.TryGetValue(timerId, out var timerInfo))
			{
				FuLogger.LogWarning($"[TimerModule] 重置计时器{timerId}失败，不存在该计时器！");
				return;
			}

			timerInfo.Reset();
		}

		/// <summary>
		/// 暂停所有计时器
		/// </summary>
		public void PauseAllTimers()
		{
			foreach (var (_, timerInfo) in m_timerDict)
			{
				timerInfo.IsPaused = true;
			}
		}

		/// <summary>
		/// 恢复所有计时器
		/// </summary>
		public void ResumeAllTimers()
		{
			foreach (var (_, timerInfo) in m_timerDict)
			{
				timerInfo.IsPaused = false;
			}
		}

		/// <summary>
		/// 停止所有计时器
		/// </summary>
		public void StopAllTimers()
		{
			// 快照到局部列表再逐个 ReleaseTimer：ReleaseTimer 会从字典移除，直接遍历字典会「遍历中修改容器」。
			// 帧驱动遍历途中（回调内）调用本方法也安全：本帧外层快照的失效条目会被有效性校验跳过，
			// 局部快照不会被外层复用缓冲干扰。此调用低频，局部列表的一次分配可接受。
			if (m_timerDict.Count == 0) return;

			var timerSnapshot = new List<TimerBase>(m_timerDict.Values);
			m_stoppedCount += timerSnapshot.Count;
			for (var i = 0; i < timerSnapshot.Count; i++)
			{
				ReleaseTimer(timerSnapshot[i].Id);
			}
		}

		/// <summary>
		/// 重置所有计时器进度（同 ResetTimer 语义；不增删字典、不触发 OnTimerFinished，遍历安全无需快照防御）
		/// </summary>
		public void ResetAllTimers()
		{
			foreach (var (_, timerInfo) in m_timerDict)
			{
				timerInfo.Reset();
			}
		}

		/// <summary>
		/// 检查计时器是否存在
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		/// <returns></returns>
		public bool IsTimerExist(int timerId) => m_timerDict.ContainsKey(timerId);

		/// <summary>
		/// 检查计时器是否处于暂停状态
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		/// <returns></returns>
		public bool IsTimerPaused(int timerId) => m_timerDict.TryGetValue(timerId, out var timerInfo) && timerInfo.IsPaused;

		#endregion

		#region Private Methods

		/// <summary>
		/// 把活跃计时器快照到复用缓冲（供帧驱动遍历，防用户回调中修改容器）
		/// </summary>
		private void SnapshotTimers()
		{
			m_updateSnapshot.Clear();
			m_updateSnapshot.AddRange(m_timerDict.Values);
		}

		/// <summary>
		/// 驱动单个计时器一次更新
		/// </summary>
		/// <param name="timer">计时器对象</param>
		/// <param name="deltaTime">缩放后的帧间隔</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔</param>
		private void UpdateTimer(TimerBase timer, float deltaTime, float unscaledDeltaTime)
		{
			// 首帧「立即」回调：在计时器已入字典、被帧驱动遍历到时执行。先于暂停判断——
			// immediate 语义是「启动即执行一次」，不受启动后、首个驱动帧前的暂停影响。
			if (timer.Immediate)
			{
				timer.Immediate = false;
				timer.RunImmediate();

				// immediate 可能把次数打满（如 repeatCount=1），此时直接完成
				if (timer.IsCompleted)
				{
					timer.OnComplete();
					m_finishedCount++;
					ReleaseTimer(timer.Id);
					return;
				}
			}

			if (timer.IsPaused) return;

			var dt = timer.IgnoreTimeScale ? unscaledDeltaTime : deltaTime;

			// 限制最大 dt 防止卡顿后的时间跳跃
			if (dt > 0.1f) dt = 0.1f;

			// 统一帧驱动每帧恰好遍历一次，帧增量恒为 1
			timer.OnUpdate(dt, 1);

			// 检查计时器是否完成，调用完成回调
			if (timer.IsCompleted)
			{
				timer.OnComplete();
				m_finishedCount++;
				ReleaseTimer(timer.Id);
			}
		}

		/// <summary>
		/// 清理计时器资源（从字典移除 → 回收 → 派发完成事件）
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		private void ReleaseTimer(int timerId)
		{
			if (!m_timerDict.Remove(timerId, out var timerInfo)) return;
			if (timerInfo == null) return;
#if UNITY_EDITOR
			ClearStartStackTrace(timerId);
#endif
			FuLogger.LogInfo($"[TimerModule] 清理计时器{timerId}");
			ReferencePool.Recycle(timerInfo);
			OnTimerFinished?.Invoke(timerId);
		}

		#endregion
	}
}