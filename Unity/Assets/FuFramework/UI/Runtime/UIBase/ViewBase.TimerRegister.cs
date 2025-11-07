using System;
using Cysharp.Threading.Tasks;
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
        /// 启动一个基础计时器
        /// </summary>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="finishCallBack">计时器结束回调</param>
        /// <param name="updateCallBack">计时器更新回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimer(float duration, Action finishCallBack = null, Action updateCallBack = null, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update, bool ignoreTimeScale = false)
        {
            TimerRegister?.StartTimer(duration, finishCallBack, updateCallBack, playerLoopTiming, ignoreTimeScale);
        }

        /// <summary>
        /// 启动一个基础的一次性计时器
        /// </summary>
        /// <param name="interval">计时器间隔时间</param>
        /// <param name="intervalCallback">计时器每次间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        public void StartTimerInterval(float interval, Action intervalCallback, int repeatCount = -1, bool immediate = false, bool ignoreTimeScale = false)
        {
            TimerRegister?.StartTimerInterval(interval, intervalCallback, repeatCount, immediate, ignoreTimeScale);
        }

        /// <summary>
        /// 启动一个帧间隔计时器
        /// </summary>
        /// <param name="frameInterval">计时器帧间隔</param>
        /// <param name="intervalCallback">计时器每次帧间隔回调</param>
        /// <param name="repeatCount">计时器重复次数，-1表示无限循环</param>
        /// <param name="immediate">是否立即执行第一次回调</param>
        /// <param name="playerLoopTiming">计时器所在的更新时间点类型</param>
        public void StartTimerFrameInterval(int frameInterval, Action intervalCallback, int repeatCount = -1, bool immediate = false, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update)
        {
            TimerRegister?.StartTimerFrameInterval(frameInterval, intervalCallback, repeatCount, immediate, playerLoopTiming);
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