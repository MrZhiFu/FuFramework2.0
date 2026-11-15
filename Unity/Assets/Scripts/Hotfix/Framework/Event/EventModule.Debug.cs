#if UNITY_EDITOR
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块——调试分部（仅编辑器调试使用）：事件调试面板的数据源。
	/// 提供订阅明细遍历（ForEachHandler）、待分发队列遍历（ForEachEvent）、总数统计
	/// （EventHandlerCount/EventCount）与广播统计（RecordBroadcast/RecordSubscribe 埋点的实现与快照输出）；
	/// 业务分发路径不依赖本分部任何成员，真机包不编译。
	/// </summary>
	public sealed partial class EventModule
	{
		/// <summary>
		/// 单个事件的广播统计。
		/// </summary>
		public sealed class BroadcastStat
		{
			/// <summary>
			/// 累计广播次数
			/// </summary>
			public int Count;

			/// <summary>
			/// 最近一次广播的时间（Time.realtimeSinceStartup）
			/// </summary>
			public float LastBroadcastTime;
		}

		/// <summary>
		/// 广播统计快照（面板展示行：按事件 ID 聚合的双桶口径）。
		/// </summary>
		public readonly struct BroadcastStatSnapshot
		{
			/// <summary>事件 ID。</summary>
			public readonly string Id;

			/// <summary>累计广播次数。</summary>
			public readonly int Count;

			/// <summary>最近一次广播时间。</summary>
			public readonly float LastBroadcastTime;

			/// <summary>无监听广播次数——「从未订阅」桶（真孤儿：拼写错/漏订阅，面板重点警示）。</summary>
			public readonly int NoListenerNeverSubscribedCount;

			/// <summary>无监听广播次数——「有历史订阅但当前无监听」桶（挂起中窗口或已退订，预期态）。</summary>
			public readonly int NoListenerEverSubscribedCount;

			/// <summary>当前订阅的 handler 数。</summary>
			public readonly int CurrentHandlerCount;

			/// <summary>
			/// 构造快照。
			/// </summary>
			public BroadcastStatSnapshot(string id, int count, float lastBroadcastTime,
				int noListenerNeverSubscribedCount, int noListenerEverSubscribedCount, int currentHandlerCount)
			{
				Id                             = id;
				Count                          = count;
				LastBroadcastTime              = lastBroadcastTime;
				NoListenerNeverSubscribedCount = noListenerNeverSubscribedCount;
				NoListenerEverSubscribedCount  = noListenerEverSubscribedCount;
				CurrentHandlerCount            = currentHandlerCount;
			}
		}

		/// <summary>
		/// 每事件 ID 的广播统计（次数/最近时间）。
		/// </summary>
		private readonly Dictionary<string, BroadcastStat> m_broadcastStats = new();

		/// <summary>
		/// 历史订阅 ID 集合（单调只增、永不清理）：无监听广播的双桶口径依据——
		/// 不在集合中 = 从未有人订阅（真孤儿）；在集合中 = 订阅过但当前无监听（挂起中窗口或已退订，预期态）。
		/// 不做「感知挂起态」的精确区分：需 EventRegister 跨层上报并处理挂起中退订的簿记，复杂度不成比例（设计 §7.1）。
		/// </summary>
		private readonly HashSet<string> m_everSubscribedIds = new();

		/// <summary>
		/// 广播入口埋点实现：计数 + 最近时间；无监听时按双桶口径累加。
		/// 调用点位于 Broadcast/BroadcastNow（API 分部），发布版随 partial 无实现整体剥除。
		/// </summary>
		partial void RecordBroadcast(string eventId)
		{
			if (string.IsNullOrEmpty(eventId)) return;

			if (!m_broadcastStats.TryGetValue(eventId, out var stat))
			{
				stat                        = new BroadcastStat();
				m_broadcastStats[eventId]   = stat;
			}

			stat.Count++;
			stat.LastBroadcastTime = UnityEngine.Time.realtimeSinceStartup;

			// 无监听判定：只查键存在（O(1)），不用 (key, value) 重载
			if (!m_actionHandlerMultiDict.Contains(eventId))
			{
				if (m_everSubscribedIds.Contains(eventId))
				{
					// 有历史订阅但当前无监听：挂起中窗口或已退订（预期态桶）
					m_noListenerEverSubscribed[eventId] = m_noListenerEverSubscribed.TryGetValue(eventId, out var v) ? v + 1 : 1;
				}
				else
				{
					m_noListenerNeverSubscribed[eventId] = m_noListenerNeverSubscribed.TryGetValue(eventId, out var v) ? v + 1 : 1;
				}
			}
		}

		/// <summary>
		/// 订阅入口埋点实现：登记历史订阅 ID（双桶口径依据）。
		/// 调用点位于 internal Subscribe 成功路径，发布版随 partial 无实现整体剥除。
		/// </summary>
		partial void RecordSubscribe(string eventId)
		{
			m_everSubscribedIds.Add(eventId);
		}

		/// <summary>
		/// 无监听广播计数——「从未订阅」桶。
		/// </summary>
		private readonly Dictionary<string, int> m_noListenerNeverSubscribed = new();

		/// <summary>
		/// 无监听广播计数——「有历史订阅但当前无监听」桶。
		/// </summary>
		private readonly Dictionary<string, int> m_noListenerEverSubscribed = new();

		/// <summary>
		/// 取全部广播统计快照（面板数据源；每面板刷新调用一次，非热路径）。
		/// </summary>
		public List<BroadcastStatSnapshot> ForEachBroadcastStat()
		{
			AssertMainThread();

			var list = new List<BroadcastStatSnapshot>(m_broadcastStats.Count);
			foreach (var (id, stat) in m_broadcastStats)
			{
				m_noListenerNeverSubscribed.TryGetValue(id, out var neverCount);
				m_noListenerEverSubscribed.TryGetValue(id, out var everCount);
				var handlerCount = 0;
				if (m_actionHandlerMultiDict.TryGetValue(id, out var range))
				{
					handlerCount = range.Count;
				}

				list.Add(new BroadcastStatSnapshot(id, stat.Count, stat.LastBroadcastTime,
					neverCount, everCount, handlerCount));
			}

			return list;
		}

		/// <summary>
		/// 清零全部广播统计（面板清零按钮；破坏性调试操作，不进公开契约）。
		/// </summary>
		public void ClearBroadcastStats()
		{
			AssertMainThread();
			m_broadcastStats.Clear();
			m_noListenerNeverSubscribed.Clear();
			m_noListenerEverSubscribed.Clear();
		}

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
		private readonly List<(string id, Action<GameEventArgs> handler)> m_cachedHandlerPairSnapshot = new();
		/// <summary>
		/// ForEachEvent 的 eventArgs 快照缓存（复用同一列表，避免每次遍历分配新列表产生 GC）。
		/// 仅非重入遍历使用（重入识别见 m_isForEachEvent）。
		/// </summary>
		private readonly List<GameEventArgs> m_cachedEventArgsSnapshot = new();

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
				foreach (var (_, handlers) in m_actionHandlerMultiDict)
				{
					count += handlers.Count;
				}

				return count;
			}
		}

		/// <summary>
		/// 遍历所有事件处理函数（仅限诊断/调试用途；并存期遍历新旧双字典，统一以 Action 形态输出）。
		/// 回调内可重入触发分发/遍历——重入识别见 m_isForEachHandler，重入时使用局部快照互不干扰。
		/// </summary>
		public void ForEachHandler(Action<string, Action<GameEventArgs>> action)
		{
			AssertMainThread();

			// 先快照 (id, handler) 再调用回调：
			// 直接遍历链表时，回调内（同线程重入）新增订阅会以 AddBefore(range.End) 插在尾部被本次遍历再次访问，
			// 与 HandleEvent 同款问题；快照隔离同时避免回调期间集合被修改。
			// 重入识别（写法同 HandleEvent 的 m_isHandlingEvent）：嵌套遍历若复用同一缓存列表，内层 Clear 会清掉
			// 外层正在遍历的快照 → 外层剩余项既不派发也不回收。故非重入复用缓存字段、重入改用局部列表。
			List<(string id, Action<GameEventArgs> handler)> snapshot;
			if (m_isForEachHandler)
			{
				snapshot = new List<(string id, Action<GameEventArgs> handler)>();
			}
			else
			{
				snapshot           = m_cachedHandlerPairSnapshot;
				m_isForEachHandler = true;
			}

			try
			{
				snapshot.Clear();
				foreach (var (id, handlers) in m_actionHandlerMultiDict)
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
		public void ForEachEvent(Action<GameEventArgs> action)
		{
			AssertMainThread();

			// 先快照 EventArgs 再调用回调：
			// 快照隔离避免回调期间队列被修改，重入识别与 ForEachHandler 一致：
			// 非重入复用缓存字段、重入改用局部列表，且缓存复用下零分配。
			List<GameEventArgs> snapshot;
			if (m_isForEachEvent)
			{
				snapshot = new List<GameEventArgs>();
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
					snapshot.Add(tempEvent.EventArgs);
				}

				for (var i = 0; i < snapshot.Count; i++)
				{
					action(snapshot[i]);
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