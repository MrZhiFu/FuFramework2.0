// 本文件整体仅 Editor 编译（发布构建不存在）：内容为调试面板使用的信息/统计快照 DTO，
// 与 TimerModule.Debug.cs 成对（BuildTimerInfo/GetStatistics 的返回类型定义）。
#if UNITY_EDITOR
// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Timer
{
	/// <summary>
	/// 计时器类型。
	/// </summary>
	public enum ETimerType
	{
		/// <summary>
		/// 一次性倒计时计时器。
		/// </summary>
		Countdown = 0,

		/// <summary>
		/// 时间间隔计时器。
		/// </summary>
		Interval = 1,

		/// <summary>
		/// 帧间隔计时器。
		/// </summary>
		Frame = 2,
	}

	/// <summary>
	/// 计时器信息快照（供调试面板等观察者读取；由 GetAllTimerInfos 每次生成新副本，不持有计时器实例）。
	/// 不适用的字段按注释口径填 -1/false。
	/// </summary>
	public struct TimerInfo
	{
		/// <summary>
		/// 计时器ID。
		/// </summary>
		public int Id { get; internal set; }

		/// <summary>
		/// 计时器类型。
		/// </summary>
		public ETimerType Type { get; internal set; }

		/// <summary>
		/// 是否暂停。
		/// </summary>
		public bool IsPaused { get; internal set; }

		/// <summary>
		/// 是否忽略时间缩放。
		/// </summary>
		public bool IgnoreTimeScale { get; internal set; }

		/// <summary>
		/// 首帧「立即」回调是否尚未执行。
		/// </summary>
		public bool ImmediatePending { get; internal set; }

		/// <summary>
		/// 已执行次数（倒计时无此概念恒 0）。
		/// </summary>
		public int ExecutedCount { get; internal set; }

		/// <summary>
		/// 最大执行次数（-1 表示无限或不适用）。
		/// </summary>
		public int MaxCount { get; internal set; }

		/// <summary>
		/// 剩余秒数（仅倒计时适用，其余 -1）。
		/// </summary>
		public float RemainingTime { get; internal set; }

		/// <summary>
		/// 总时长秒（仅倒计时适用，其余 -1）。
		/// </summary>
		public float DurationTime { get; internal set; }

		/// <summary>
		/// 间隔值（时间间隔为秒，帧间隔为帧数，倒计时 -1）。
		/// </summary>
		public float Interval { get; internal set; }

		/// <summary>
		/// 累计进度（AccumulatedTime 或 AccumulatedFrames，倒计时 -1）。
		/// </summary>
		public float Accumulated { get; internal set; }

		/// <summary>
		/// 启动来源堆栈（仅 Editor 有值，其余 null）。
		/// </summary>
		public string StartStackTrace { get; internal set; }
	}

	/// <summary>
	/// 计时器模块生命周期统计快照（由 GetStatistics 每次生成新副本）。
	/// </summary>
	public struct TimerModuleStatistics
	{
		/// <summary>
		/// 累计启动数。
		/// </summary>
		public int StartedCount { get; internal set; }

		/// <summary>
		/// 累计自然完成数（倒计时到点/次数打满）。
		/// </summary>
		public int FinishedCount { get; internal set; }

		/// <summary>
		/// 累计被主动停止数（StopTimer/StopAllTimers）。
		/// </summary>
		public int StoppedCount { get; internal set; }

		/// <summary>
		/// 累计异常终止数（回调抛异常被隔离回收）。
		/// </summary>
		public int ErrorCount { get; internal set; }
	}
}
#endif
