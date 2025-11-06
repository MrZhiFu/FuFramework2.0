using System;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Timer.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 计时器订阅器-界面的计时器管理
    /// </summary>
    public abstract partial class ViewBase
    {
        /// <summary>
        /// 界面计时器订阅器。
        /// </summary>
        private TimerRegister TimerRegister { get; set; }


        /// <summary>
        /// 启动一个计时器
        /// </summary>
        /// <param name="duration">持续时间(以秒为单位)</param>
        /// <param name="updateCallBack">每帧/每秒回调(如果loopTiming为Update，则每帧回调；如果loopTiming为TimeUpdate，则每秒回调)</param>
        /// <param name="playerLoopTiming">计时器执行时机</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <param name="finishCallBack">计时器执行回调</param>
        /// <returns>计时器ID，用于停止指定计时器</returns>
        public void StartTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            TimerRegister?.StartTimer(duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);
        }

        /// <summary>
        /// 启动一个只执行一次的计时器(即延时操作)
        /// </summary>
        /// <param name="duration">持续时间（以秒为单位）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimerOnce(float duration, Action callback, bool ignoreTimeScale = false)
        {
            TimerRegister?.StartTimerOnce(duration, callback, ignoreTimeScale);
        }

        /// <summary>
        /// 启动一个指定时间间隔的循环计时器
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="intervalCallback">每次间隔时间执行的回调函数</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimerInterval(float interval, Action intervalCallback, bool ignoreTimeScale = false)
        {
            TimerRegister?.StartTimerInterval(interval, intervalCallback, ignoreTimeScale);
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void PauseTimer(int timerId) => TimerRegister?.PauseTimer(timerId);

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void ResumeTimer(int timerId) => TimerRegister?.ResumeTimer(timerId);

        /// <summary>
        /// 停止计时器
        /// </summary>
        /// <param name="timerId"></param>
        public void StopTimer(int timerId) => TimerRegister.StopTimer(timerId);

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers() => TimerRegister?.PauseAllTimers();

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers() => TimerRegister?.ResumeAllTimers();

        /// <summary>
        /// 停止所有计时器
        /// </summary>
        public void StopAllTimers() => TimerRegister?.StopAllTimers();
        
        /// <summary>
        /// 检查计时器是否存在
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerExist(int timerId) => TimerRegister != null && TimerRegister.IsTimerExist(timerId);

        /// <summary>
        /// 检查计时器是否处于暂停状态
        /// </summary>
        /// <param name="timerId"></param>
        /// <returns></returns>
        public bool IsTimerPaused(int timerId) => TimerRegister != null && TimerRegister.IsTimerPaused(timerId);


        /// <summary>
        /// 释放事件注册器
        /// </summary>
        private void ReleaseTimerRegister()
        {
            TimerRegister.Release();
            TimerRegister = null;
        }
    }
}