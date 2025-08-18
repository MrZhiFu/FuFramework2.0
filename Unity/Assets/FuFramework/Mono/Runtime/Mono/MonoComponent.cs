using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Mono.Runtime
{
    /// <summary>
    /// Mono 组件。
    /// 管理游戏中 MonoBehaviour 的生命周期事件，例如 FixedUpdate、LateUpdate、OnDestroy等，并提供了添加和移除这些事件的监听。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Mono")]
    public class MonoComponent : FuComponent
    {
        private IMonoManager m_MonoManager; // Mono管理器
        private EventComponent m_EventComponent; // 事件管理器

        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(IMonoManager);

            base.Awake();

            m_MonoManager = FuEntry.GetModule<IMonoManager>();
            if (m_MonoManager == null)
            {
                Log.Fatal("Mono管理器为空.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("事件管理器为空.");
            }
        }

        /// <summary>
        /// 在固定的帧率下调用。
        /// </summary>
        private void FixedUpdate() => m_MonoManager.FixedUpdate();

        /// <summary>
        /// 在所有 Update 函数调用后每帧调用。
        /// </summary>
        private void LateUpdate() => m_MonoManager.LateUpdate();

        /// <summary>
        /// 当 MonoBehaviour 将被销毁时调用。
        /// </summary>
        private void OnDestroy() => m_MonoManager.OnDestroy();


        /// <summary>
        /// 当应用程序失去或获得焦点时调用。
        /// </summary>
        /// <param name="focusStatus">应用程序的焦点状态</param>
        private void OnApplicationFocus(bool focusStatus)
        {
            m_MonoManager.OnApplicationFocus(focusStatus);
            if (m_EventComponent)
                m_EventComponent.Fire(this, OnApplicationFocusChangedEventArgs.Create(focusStatus));
        }

        /// <summary>
        /// 当应用程序暂停或恢复时调用。
        /// </summary>
        /// <param name="pauseStatus">应用程序的暂停状态</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            m_MonoManager.OnApplicationPause(pauseStatus);
            if (m_EventComponent)
                m_EventComponent.Fire(this, OnApplicationPauseChangedEventArgs.Create(pauseStatus));
        }

        /// <summary>
        /// 添加 LateUpdate 监听器
        /// </summary>
        /// <param name="fun">要添加的 LateUpdate 监听器回调函数</param>
        public void AddLateUpdateListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.AddLateUpdateListener(fun);
        }

        /// <summary>
        /// 移除 LateUpdate 监听器
        /// </summary>
        /// <param name="fun">要移除的 LateUpdate 监听器回调函数</param>
        public void RemoveLateUpdateListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.RemoveLateUpdateListener(fun);
        }

        /// <summary>
        /// 添加 OnApplicationFocus 监听器
        /// </summary>
        /// <param name="fun">要添加的 OnApplicationFocus 监听器回调函数</param>
        public void AddFixedUpdateListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.AddFixedUpdateListener(fun);
        }

        /// <summary>
        /// 移除 OnApplicationFocus 监听器
        /// </summary>
        /// <param name="fun">要移除的 OnApplicationFocus 监听器回调函数</param>
        public void RemoveFixedUpdateListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.RemoveFixedUpdateListener(fun);
        }

        /// <summary>
        /// 添加 Update 监听器
        /// </summary>
        /// <param name="fun">要添加的 Update 监听器回调函数</param>
        public void AddUpdateListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.AddUpdateListener(fun);
        }

        /// <summary>
        /// 移除 Update 监听器
        /// </summary>
        /// <param name="fun">要移除的 Update 监听器回调函数</param>
        public void RemoveUpdateListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.RemoveUpdateListener(fun);
        }

        /// <summary>
        /// 添加 Destroy 监听器
        /// </summary>
        /// <param name="fun">要添加的 Destroy 监听器回调函数</param>
        public void AddDestroyListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.AddDestroyListener(fun);
        }

        /// <summary>
        /// 移除 Destroy 监听器
        /// </summary>
        /// <param name="fun">要移除的 Destroy 监听器回调函数</param>
        public void RemoveDestroyListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.RemoveDestroyListener(fun);
        }

        /// <summary>
        /// 添加 OnApplicationPause 监听器
        /// </summary>
        /// <param name="fun">要添加的 OnApplicationPause 监听器回调函数</param>
        public void AddOnApplicationPauseListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.AddOnApplicationPauseListener(fun);
        }

        /// <summary>
        /// 移除 OnApplicationPause 监听器
        /// </summary>
        /// <param name="fun">要移除的 OnApplicationPause 监听器回调函数</param>
        public void RemoveOnApplicationPauseListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.RemoveOnApplicationPauseListener(fun);
        }

        /// <summary>
        /// 添加 OnApplicationFocus 监听器
        /// </summary>
        /// <param name="fun">要添加的 OnApplicationFocus 监听器回调函数</param>
        public void AddOnApplicationFocusListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.AddOnApplicationFocusListener(fun);
        }

        /// <summary>
        /// 移除 OnApplicationFocus 监听器
        /// </summary>
        /// <param name="fun">要移除的 OnApplicationFocus 监听器回调函数</param>
        public void RemoveOnApplicationFocusListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "回调函数无效.");
                return;
            }

            m_MonoManager.RemoveOnApplicationFocusListener(fun);
        }
    }
}