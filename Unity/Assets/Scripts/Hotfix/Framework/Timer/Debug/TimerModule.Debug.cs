// 本文件整体仅 Editor 编译（发布构建不存在）：内容为调试面板专用的信息快照、生命周期统计
// 与启动堆栈记录。主文件的挂钩调用点各自 #if UNITY_EDITOR 包裹，故此处方法无需保留空实现。
#if UNITY_EDITOR
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
	/// <summary>
	/// TimerModule 的 Editor 调试分部类：信息快照、生命周期统计与启动堆栈记录（仅 Editor 存在）。
	/// 与主文件（帧驱动与操作）分离；发布构建中本文件整体不参与编译。
	/// </summary>
	public sealed partial class TimerModule
	{
		/// <summary>
		/// 计时器 Id → 启动来源堆栈（启动时固定记录，回收时移除；约 1-4KB/条，仅存活计时器持有）。
		/// </summary>
		private readonly Dictionary<int, string> m_startStackTraces = new();

		#region 信息快照

		/// <summary>
		/// 获取所有计时器的信息快照（供调试面板等观察者使用；每次调用生成新数组，不暴露计时器实例）。
		/// </summary>
		/// <returns>按字典序（即启动顺序）排列的计时器信息快照。</returns>
		public TimerInfo[] GetAllTimerInfos()
		{
			var infos = new TimerInfo[m_timerDict.Count];
			var index = 0;
			foreach (var (_, timer) in m_timerDict)
			{
				infos[index] = BuildTimerInfo(timer);
				index++;
			}

			return infos;
		}

		/// <summary>
		/// 组装单个计时器的信息快照（不适用的字段填 -1）。
		/// </summary>
		private TimerInfo BuildTimerInfo(TimerBase timer)
		{
			var info = new TimerInfo
			{
				Id               = timer.Id,
				IsPaused         = timer.IsPaused,
				IgnoreTimeScale  = timer.IgnoreTimeScale,
				ImmediatePending = timer.Immediate,
				MaxCount         = -1,
				RemainingTime    = -1f,
				DurationTime     = -1f,
				Interval         = -1f,
				Accumulated      = -1f,
			};

			switch (timer)
			{
				case CountdownTimer countdown:
					info.Type          = ETimerType.Countdown;
					info.RemainingTime = countdown.RemainingTime;
					info.DurationTime  = countdown.DurationTime;
					break;

				case IntervalTimer interval:
					info.Type          = ETimerType.Interval;
					info.Interval      = interval.Interval;
					info.Accumulated   = interval.AccumulatedTime;
					info.ExecutedCount = interval.ExecutedCount;
					info.MaxCount      = interval.MaxCount == int.MaxValue ? -1 : interval.MaxCount;
					break;

				case FrameTimer frame:
					info.Type          = ETimerType.Frame;
					info.Interval      = frame.FrameInterval;
					info.Accumulated   = frame.AccumulatedFrames;
					info.ExecutedCount = frame.ExecutedCount;
					info.MaxCount      = frame.MaxCount == int.MaxValue ? -1 : frame.MaxCount;
					break;
			}

			info.StartStackTrace = m_startStackTraces.GetValueOrDefault(timer.Id);
			return info;
		}

		#endregion

		#region 生命周期统计

		/// <summary>
		/// 获取模块生命周期统计快照（累计启动/自然完成/被停止/异常终止）。
		/// </summary>
		public TimerModuleStatistics GetStatistics()
		{
			return new TimerModuleStatistics
			{
				StartedCount  = m_startedCount,
				FinishedCount = m_finishedCount,
				StoppedCount  = m_stoppedCount,
				ErrorCount    = m_errorCount,
			};
		}

		#endregion

		#region 启动堆栈记录

		/// <summary>
		/// 记录启动来源堆栈。
		/// </summary>
		/// <param name="timerId">计时器 Id。</param>
		private void CaptureStartStackTrace(int timerId) => m_startStackTraces[timerId] = Environment.StackTrace;

		/// <summary>
		/// 清除指定计时器的启动来源堆栈（回收路径统一调用，防止字典滞留失效 Id）。
		/// </summary>
		/// <param name="timerId">计时器 Id。</param>
		private void ClearStartStackTrace(int timerId) => m_startStackTraces.Remove(timerId);

		/// <summary>
		/// 清空全部启动来源堆栈（模块释放路径调用——OnDispose 不经 ReleaseTimer，防止字典滞留失效条目）。
		/// </summary>
		private void ClearAllStartStackTraces() => m_startStackTraces.Clear();

		#endregion
	}
}
#endif
