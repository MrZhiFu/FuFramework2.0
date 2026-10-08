using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Timer
{
	/// <summary>
	/// 计时器基类。
	/// 功能：
	///     1. 提供计时器的基本属性和方法。
	///     2. 实现引用池接口，提供计时器的初始化、清理方法。
	/// </summary>
	internal abstract class TimerBase : IReference
	{
		/// <summary>
		/// 计时器ID
		/// </summary>
		public int Id { get; private set; }

		/// <summary>
		/// 是否忽略时间缩放
		/// </summary>
		public bool IgnoreTimeScale { get; private set; }

		/// <summary>
		/// 是否暂停
		/// </summary>
		public bool IsPaused { get; internal set; }

		/// <summary>
		/// 是否需要执行首帧「立即」回调。
		/// 由启动入口（TimerModule.StartIntervalTimer/StartFrameTimer 的 immediate 参数）置位，
		/// 由 TimerModule 在「计时器已入字典、被帧驱动遍历到」时执行一次并复位。
		/// 不在 Create 内执行：Create 发生在入字典之前，此时用户回调抛异常或回调内调 Stop* 都会
		/// 作用在「已 Acquire 但不在字典中」的计时器上（既不回收也停不到，引用池计数永久虚高）。
		/// </summary>
		public bool Immediate { get; internal set; }

		/// <summary>
		/// 计时器名称
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// 是否已完成
		/// </summary>
		public abstract bool IsCompleted { get; }

		/// <summary>
		/// 初始化各计时器共有的基础字段（由各 Create 工厂在 Acquire 之后调用）
		/// </summary>
		/// <param name="timerId">计时器ID</param>
		/// <param name="ignoreTimeScale">是否忽略时间缩放</param>
		protected void InitCommon(int timerId, bool ignoreTimeScale)
		{
			Id              = timerId;
			IgnoreTimeScale = ignoreTimeScale;
			IsPaused        = false;
		}

		/// <summary>
		/// 帧更新计时器
		/// </summary>
		/// <param name="deltaTime">时间增量</param>
		/// <param name="deltaFrames">帧增量</param>
		public abstract void OnUpdate(float deltaTime, int deltaFrames);

		/// <summary>
		/// 清理计时器
		/// </summary>
		public virtual void Clear()
		{
			Id              = -1;
			IsPaused        = false;
			Immediate       = false;
			IgnoreTimeScale = false;
		}

		/// <summary>
		/// 重置计时器进度（仅清零进度，不重发 immediate，不改暂停状态）。
		/// 基类无进度字段故为空实现；有进度的计时器按各自口径覆写。
		/// </summary>
		public virtual void Reset() { }

		/// <summary>
		/// 当计时器完成时调用
		/// </summary>
		public virtual void OnComplete() { }

		/// <summary>
		/// 执行首帧「立即」回调（默认无；时间/帧间隔计时器按各自的次数口径覆写）。
		/// 由 TimerModule 在计时器入字典、被帧驱动遍历到时调用一次，抛异常由该次遍历的 catch 统一回收。
		/// </summary>
		public virtual void RunImmediate() { }
	}
}