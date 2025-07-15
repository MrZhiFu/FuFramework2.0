using System;
using GameFrameX.Runtime;
using GameFrameX.Timer.Runtime;

namespace GameFrameX.UI.Runtime
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
        /// 添加一个定时调用的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="repeat">重复次数（0 表示无限重复）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        protected void AddTimer(float interval, int repeat, Action<object> callback, object callbackParam = null)
        {
            GameFrameworkGuard.NotNull(callback, nameof(callback));
            GameFrameworkGuard.NotNull(TimerRegister, "计时器订阅器为空, 请先初始化TimerRegister.");
            TimerRegister.AddTimer(interval, repeat,callback, callbackParam);
        }

        /// <summary>
        /// 添加一个只执行一次的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        protected void AddTimerOnce(float interval, Action<object> callback, object callbackParam = null)
        {
            GameFrameworkGuard.NotNull(callback,      nameof(callback));
            GameFrameworkGuard.NotNull(TimerRegister, "计时器订阅器为空, 请先初始化TimerRegister.");
            TimerRegister.AddTimerOnce(interval, callback, callbackParam);
        }

        /// <summary>
        /// 添加一个每帧更新执行的任务
        /// </summary>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数</param>
        protected void AddTimerUpdate(Action<object> callback, object callbackParam = null)
        {
            GameFrameworkGuard.NotNull(callback, nameof(callback));
            GameFrameworkGuard.NotNull(TimerRegister, "计时器订阅器为空, 请先初始化TimerRegister.");
            TimerRegister.AddTimerUpdate(callback, callbackParam);
        }

        /// <summary>
        /// 移除指定的任务
        /// </summary>
        /// <param name="callback">要移除的回调函数</param>
        protected void RemoveTimer(Action<object> callback)
        {
            GameFrameworkGuard.NotNull(callback,      nameof(callback));
            GameFrameworkGuard.NotNull(TimerRegister, "计时器订阅器为空, 请先初始化TimerRegister.");
            TimerRegister.RemoveTimer(callback);
        }

        /// <summary>
        /// 移除所有计时任务
        /// </summary>
        protected void RemoveAllTimer()
        {
            GameFrameworkGuard.NotNull(TimerRegister, "计时器订阅器为空, 请先初始化TimerRegister.");
            TimerRegister.RemoveAllTimer();
        }
        
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