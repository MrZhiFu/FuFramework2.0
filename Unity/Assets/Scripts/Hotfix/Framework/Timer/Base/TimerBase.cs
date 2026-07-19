using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.ReferencePools;

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
            IgnoreTimeScale = false;
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
    }
}
