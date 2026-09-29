using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Mono
{
	/// <summary>
	/// Mono管理模块。
	/// 功能：
	///     1.管理游戏中 MonoBehaviour 的生命周期事件，例如 FixedUpdate、LateUpdate、OnDestroy等。
	///     2.提供简便的方式来添加和移除这些事件的监听。
	/// </summary>
	public class MonoModule : ModuleBase
	{
		/// <summary>
		/// 等待执行的 Update 回调列表
		/// </summary>
		private readonly List<Action> m_waitUpdateList = new();

		/// <summary>
		/// 正在执行的 Update 回调列表
		/// </summary>
		private readonly List<Action> m_doingUpdateList = new();


		/// <summary>
		/// 等待执行的 FixedUpdate 回调列表
		/// </summary>
		private readonly List<Action> m_waitFixedUpdateList = new();

		/// <summary>
		/// 正在执行的 FixedUpdate 回调列表
		/// </summary>
		private readonly List<Action> m_doingFixedUpdateList = new();


		/// <summary>
		/// 等待执行的 LateUpdate 回调列表
		/// </summary>
		private readonly List<Action> m_waitLateUpdateList = new();

		/// <summary>
		/// 正在执行的 LateUpdate 回调列表
		/// </summary>
		private readonly List<Action> m_doingLateUpdateList = new();


		/// <summary>
		/// 等待执行的 Destroy 回调列表
		/// </summary>
		private readonly List<Action> m_waitDestroyList = new();

		/// <summary>
		/// 正在执行的 Destroy 回调列表
		/// </summary>
		private readonly List<Action> m_doingDestroyList = new();


		/// <summary>
		/// 等待执行的 OnApplicationPause 回调列表
		/// </summary>
		private List<Action<bool>> m_waitOnApplicationPauseList = new();

		/// <summary>
		/// 正在执行的 OnApplicationPause 回调列表
		/// </summary>
		private List<Action<bool>> m_doOnApplicationPauseList = new();


		/// <summary>
		/// 等待执行的 OnApplicationFocus 回调列表
		/// </summary>
		private List<Action<bool>> m_waitOnApplicationFocusList = new();

		/// <summary>
		/// 正在执行的 OnApplicationFocus 回调列表
		/// </summary>
		private List<Action<bool>> m_doOnApplicationFocusList = new();


		/// <summary>
		/// 初始化
		/// </summary>
		protected internal override void OnInit() { }

		/// <summary>
		/// 帧更新。
		/// </summary>
		/// <param name="deltaTime">帧间隔时间。</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
		protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
		{
			QueueInvoking(m_doingUpdateList, m_waitUpdateList);
		}

		/// <summary>
		/// 固定帧更新
		/// </summary>
		protected internal override void OnFixedUpdate()
		{
			QueueInvoking(m_doingFixedUpdateList, m_waitFixedUpdateList);
		}

		/// <summary>
		/// 延迟帧更新
		/// </summary>
		/// <param name="deltaTime">帧间隔时间。</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
		protected internal override void OnLateUpdate(float deltaTime, float unscaledDeltaTime)
		{
			QueueInvoking(m_doingLateUpdateList, m_waitLateUpdateList);
		}

		/// <summary>
		/// 释放。
		/// </summary>
		protected internal override void OnDispose()
		{
			QueueInvoking(m_doingDestroyList, m_waitDestroyList);

			m_waitUpdateList.Clear();
			m_waitDestroyList.Clear();
			m_waitFixedUpdateList.Clear();
			m_waitLateUpdateList.Clear();
			m_waitOnApplicationFocusList.Clear();
			m_waitOnApplicationPauseList.Clear();
		}

		/// <summary>
		/// 当应用程序失去或获得焦点时调用。
		/// </summary>
		/// <param name="focusStatus">应用程序的焦点状态</param>
		public void OnApplicationFocus(bool focusStatus)
		{
			QueueInvoking(ref m_doOnApplicationFocusList, ref m_waitOnApplicationFocusList, focusStatus);
		}

		/// <summary>
		/// 当应用程序暂停或恢复时调用。
		/// </summary>
		/// <param name="pauseStatus">应用程序的暂停状态</param>
		public void OnApplicationPause(bool pauseStatus)
		{
			QueueInvoking(ref m_doOnApplicationPauseList, ref m_waitOnApplicationPauseList, pauseStatus);
		}


		/// <summary>
		/// 添加一个在 Update 期间调用的监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void AddUpdateListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitUpdateList.Add(action);
		}

		/// <summary>
		/// 添加一个在 LateUpdate 期间调用的监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void AddLateUpdateListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitLateUpdateList.Add(action);
		}

		/// <summary>
		/// 从 LateUpdate 中移除一个监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void RemoveLateUpdateListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitLateUpdateList.Remove(action);
		}

		/// <summary>
		/// 添加一个在 FixedUpdate 期间调用的监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void AddFixedUpdateListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitFixedUpdateList.Add(action);
		}

		/// <summary>
		/// 从 FixedUpdate 中移除一个监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void RemoveFixedUpdateListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitFixedUpdateList.Remove(action);
		}

		/// <summary>
		/// 从 Update 中移除一个监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void RemoveUpdateListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitUpdateList.Remove(action);
		}


		/// <summary>
		/// 添加一个在 Destroy 期间调用的监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void AddDestroyListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitDestroyList.Add(action);
		}

		/// <summary>
		/// 从 Destroy 中移除一个监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void RemoveDestroyListener(Action action)
		{
			action.NotNull(nameof(action));
			m_waitDestroyList.Remove(action);
		}

		/// <summary>
		/// 添加一个在 OnApplicationPause 期间调用的监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void AddOnApplicationPauseListener(Action<bool> action)
		{
			action.NotNull(nameof(action));
			m_waitOnApplicationPauseList.Add(action);
		}

		/// <summary>
		/// 从 OnApplicationPause 中移除一个监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void RemoveOnApplicationPauseListener(Action<bool> action)
		{
			action.NotNull(nameof(action));
			m_waitOnApplicationPauseList.Remove(action);
		}

		/// <summary>
		/// 添加一个在 OnApplicationFocus 期间调用的监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void AddOnApplicationFocusListener(Action<bool> action)
		{
			action.NotNull(nameof(action));
			m_waitOnApplicationFocusList.Add(action);
		}

		/// <summary>
		/// 从 OnApplicationFocus 中移除一个监听器。
		/// </summary>
		/// <param name="action">监听器函数</param>
		public void RemoveOnApplicationFocusListener(Action<bool> action)
		{
			action.NotNull(nameof(action));
			m_waitOnApplicationFocusList.Remove(action);
		}

		/// <summary>
		/// 使用交互引用的形式实现队列调用效果，确保在多线程环境下安全，在执行回调函数时不会发生竞态条件:
		/// 1. 先将 invokeList 与 waitInvokeList 进行交换引用，这样 invokeList 就指向waitInvokeList，而 waitInvokeList指向了invokeList.
		/// 2. 交换后，waitInvokeList可以继续收集新的回调函数，为下一次执行做准备。
		/// 3. 遍历 invokeList，调用其中的函数.
		/// </summary>
		/// <param name="invokeList"></param>
		/// <param name="waitInvokeList"></param>
		private static void QueueInvoking(List<Action> invokeList, List<Action> waitInvokeList)
		{
			Utility.Object.Swap(ref invokeList, ref waitInvokeList);

			foreach (var action in invokeList)
			{
				try
				{
					action.Invoke();
				}
				catch (Exception e)
				{
					FuLogger.LogError(e);
				}
			}
		}

		/// <summary>
		/// 使用交互引用的形式实现队列调用效果，确保在多线程环境下安全，在执行回调函数时不会发生竞态条件:
		/// 1. 先将 invokeList 与 waitInvokeList 进行交换引用，这样 invokeList 就指向waitInvokeList，而 waitInvokeList指向了invokeList.
		/// 2. 交换后，waitInvokeList可以继续收集新的回调函数，为下一次执行做准备。
		/// 3. 遍历 invokeList，调用其中的函数.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="value"></param>
		private static void QueueInvoking(ref List<Action<bool>> a, ref List<Action<bool>> b, bool value)
		{
			Utility.Object.Swap(ref a, ref b);

			foreach (var action in a)
			{
				try
				{
					action.Invoke(value);
				}
				catch (Exception e)
				{
					FuLogger.LogError(e);
				}
			}
		}
	}
}
