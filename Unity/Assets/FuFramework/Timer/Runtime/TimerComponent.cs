using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Runtime
{
    /// <summary>
    /// 计时器组件。
    /// 用于管理计时器任务，提供添加、移除、检查计时器等功能。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Timer")]
    public class TimerComponent : FuComponent
    {
        private ITimerManager m_TimerManager; // 计时器管理器

        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(ITimerManager);

            base.Awake();

            m_TimerManager = FuEntry.GetModule<ITimerManager>();
            if (m_TimerManager == null) Log.Fatal("计时器管理器为空.");
        }

        /// <summary>
        /// 添加一个定时调用的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="repeat">重复次数（0 表示无限重复）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        public void Add(float interval, int repeat, Action<object> callback, object callbackParam = null) =>
            m_TimerManager.Add(interval, repeat, callback, callbackParam);

        /// <summary>
        /// 添加一个只执行一次的任务
        /// </summary>
        /// <param name="interval">间隔时间（以秒为单位）</param>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数（可选）</param>
        public void AddOnce(float interval, Action<object> callback, object callbackParam = null) =>
            m_TimerManager.AddOnce(interval, callback, callbackParam);

        /// <summary>
        /// 添加一个每帧更新执行的任务
        /// </summary>
        /// <param name="callback">要执行的回调函数</param>
        /// <param name="callbackParam">回调函数的参数</param>
        public void AddUpdate(Action<object> callback, object callbackParam = null) => m_TimerManager.AddUpdate(callback, callbackParam);

        /// <summary>
        /// 检查指定的任务是否存在
        /// </summary>
        /// <param name="callback">要检查的回调函数</param>
        /// <returns>存在返回 true，不存在返回 false</returns>
        public bool Exists(Action<object> callback) => m_TimerManager.Exists(callback);

        /// <summary>
        /// 移除指定的任务
        /// </summary>
        /// <param name="callback">要移除的回调函数</param>
        public void Remove(Action<object> callback) => m_TimerManager.Remove(callback);
    }
}