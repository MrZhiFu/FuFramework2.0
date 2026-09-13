using System.Threading;
using Cysharp.Threading.Tasks;
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
        public int Id { get; protected set; }

        /// <summary>
        /// 是否忽略时间缩放
        /// </summary>
        public bool IgnoreTimeScale { get; protected set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; internal set; }

        /// <summary>
        /// 是否需要执行首帧「立即」回调。
        /// 由启动入口（TimerModule.StartIntervalTimer/StartFrameTimer 的 immediate 参数）置位，
        /// 由 TimerModule 在「计时器已入字典、异步链已起」之后调用 RunImmediate 执行一次并复位。
        /// 不在 Create 内执行：Create 发生在入字典之前，此时用户回调抛异常或回调内调 Stop* 都会
        /// 作用在「已 Acquire 但不在字典中」的计时器上（既不回收也停不到，引用池计数永久虚高）。
        /// </summary>
        public bool Immediate { get; internal set; }

        /// <summary>
        /// 取消计时器的令牌
        /// </summary>
        public CancellationTokenSource Cts { get; protected set; }

        /// <summary>
        /// update更新时机类型
        /// </summary>
        public PlayerLoopTiming PlayerLoopTiming { get; protected set; }

        /// <summary>
        /// 计时器名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public abstract bool IsCompleted { get; }

        /// <summary>
        /// 清理计时器
        /// </summary>
        public virtual void Clear()
        {
            Id = -1;

            Cts?.Cancel();
            Cts?.Dispose();
            Cts = null;

            IsPaused        = false;
            Immediate       = false;
            IgnoreTimeScale = false;

            // 复位更新时机为默认值：所有启动入口（TimerModule/TimerRegister）的默认值均为 Update，
            // 不复位会让复用的计时器残留上一次的 update 时机。
            // 全限定枚举名，避免属性名与类型名同名导致的解析歧义。
            PlayerLoopTiming = Cysharp.Threading.Tasks.PlayerLoopTiming.Update;
        }

        /// <summary>
        /// 更新计时器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        /// <param name="deltaFrames">帧增量</param>
        public abstract void Update(float deltaTime, int deltaFrames);

        /// <summary>
        /// 当计时器完成时调用
        /// </summary>
        public virtual void OnComplete() { }

        /// <summary>
        /// 执行首帧「立即」回调（默认无；时间/帧间隔计时器按各自的次数口径覆写）。
        /// 由 TimerModule 在计时器入字典、起链之后调用一次，抛异常由异步链的 finally 统一回收。
        /// </summary>
        public virtual void RunImmediate() { }
    }
}
