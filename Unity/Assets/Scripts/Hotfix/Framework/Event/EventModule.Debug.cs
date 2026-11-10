#if UNITY_EDITOR
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块——调试分部（仅编辑器调试使用）：事件调试面板的数据源。
	/// 提供订阅明细遍历（ForEachHandler）、待分发队列遍历（ForEachEvent）与总数统计
	/// （EventHandlerCount/EventCount）；业务分发路径不依赖本分部任何成员，真机包不编译。
	/// </summary>
	public sealed partial class EventModule
	{
		/// <summary>
		/// 是否正在遍历事件（用于识别重入的嵌套遍历，避免嵌套时清空外层正在遍历的快照列表）
		/// </summary>
		private bool m_isForEachEvent;

		/// <summary>
		/// 是否正在遍历事件处理函数（用于识别重入的嵌套遍历，避免嵌套时清空外层正在遍历的快照列表）
		/// </summary>
		private bool m_isForEachHandler;

		/// <summary>
		/// ForEachHandler 的 (id, handler) 快照缓存（复用同一列表，避免每次遍历分配新列表产生 GC）。
		/// 仅非重入遍历使用（重入识别见 m_isForEachHandler）。
		/// </summary>
		private readonly List<(string id, EventHandler<GameEventArgs> handler)> m_cachedHandlerPairSnapshot = new();

		/// <summary>
		/// ForEachEvent 的 (sender, eventArgs) 快照缓存（复用同一列表，避免每次遍历分配新列表产生 GC）。
		/// 仅非重入遍历使用（重入识别见 m_isForEachEvent）。
		/// </summary>
		private readonly List<(object sender, GameEventArgs eventArgs)> m_cachedEventArgsSnapshot = new();

		/// <summary>
		/// 获取事件数量。
		/// </summary>
		public int EventCount
		{
			get
			{
				AssertMainThread();
				return m_eventQueue.Count;
			}
		}

		/// <summary>
		/// 获取事件处理函数的数量。
		/// 注意：多值字典自身的 Count 是「有订阅的事件 ID 数」，而多播语义下同一事件可挂多个处理函数，
		/// 故此处遍历累加各事件的处理器条目数，保证与命名语义一致；仅供诊断/编辑器面板使用，勿在热路径调用。
		/// </summary>
		public int EventHandlerCount
		{
			get
			{
				AssertMainThread();
				var count = 0;
				foreach (var (_, handlers) in m_eventHandlerMultiDict)
				{
					count += handlers.Count;
				}

				return count;
			}
		}

		/// <summary>
		/// 遍历所有事件处理函数（仅限诊断/调试用途）。
		/// 回调内可重入触发分发/遍历——重入识别见 m_isForEachHandler，重入时使用局部快照互不干扰。
		/// </summary>
		public void ForEachHandler(Action<string, EventHandler<GameEventArgs>> action)
		{
			AssertMainThread();

			// 先快照 (id, handler) 再调用回调：
			// 直接遍历链表时，回调内（同线程重入）新增订阅会以 AddBefore(range.End) 插在尾部被本次遍历再次访问，
			// 与 HandleEvent 同款问题；快照隔离同时避免回调期间集合被修改。
			// 重入识别（写法同 HandleEvent 的 m_isHandlingEvent）：嵌套遍历若复用同一缓存列表，内层 Clear 会清掉
			// 外层正在遍历的快照 → 外层剩余项既不派发也不回收。故非重入复用缓存字段、重入改用局部列表。
			List<(string id, EventHandler<GameEventArgs> handler)> snapshot;
			if (m_isForEachHandler)
			{
				snapshot = new List<(string id, EventHandler<GameEventArgs> handler)>();
			}
			else
			{
				snapshot           = m_cachedHandlerPairSnapshot;
				m_isForEachHandler = true;
			}

			try
			{
				snapshot.Clear();
				foreach (var (id, handlers) in m_eventHandlerMultiDict)
				{
					foreach (var handler in handlers)
					{
						snapshot.Add((id, handler));
					}
				}

				for (var i = 0; i < snapshot.Count; i++)
				{
					action(snapshot[i].id, snapshot[i].handler);
				}
			}
			finally
			{
				if (ReferenceEquals(snapshot, m_cachedHandlerPairSnapshot))
					m_isForEachHandler = false;
			}
		}

		/// <summary>
		/// 遍历所有事件。
		/// 仅限主线程调用；回调内不得修改或回收未分发的事件参数（其所有权仍在事件模块，分发结束后回收）。
		/// </summary>
		public void ForEachEvent(Action<object, GameEventArgs> action)
		{
			AssertMainThread();

			// 先快照 (Sender, EventArgs)，再调用回调：
			// 快照隔离避免回调期间队列被修改，重入识别与 ForEachHandler 一致：
			// 非重入复用缓存字段、重入改用局部列表，且缓存复用下零分配。
			List<(object sender, GameEventArgs eventArgs)> snapshot;
			if (m_isForEachEvent)
			{
				snapshot = new List<(object sender, GameEventArgs eventArgs)>();
			}
			else
			{
				snapshot         = m_cachedEventArgsSnapshot;
				m_isForEachEvent = true;
			}

			try
			{
				snapshot.Clear();
				foreach (var tempEvent in m_eventQueue)
				{
					snapshot.Add((tempEvent.Sender, tempEvent.EventArgs));
				}

				for (var i = 0; i < snapshot.Count; i++)
				{
					action(snapshot[i].sender, snapshot[i].eventArgs);
				}
			}
			finally
			{
				if (ReferenceEquals(snapshot, m_cachedEventArgsSnapshot))
					m_isForEachEvent = false;
			}
		}
	}
}
#endif