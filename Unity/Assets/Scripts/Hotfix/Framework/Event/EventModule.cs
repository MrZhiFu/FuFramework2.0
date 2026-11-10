using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块。
	/// 功能：
	///     1. 管理事件，提供订阅/退订/抛出等便捷方法（原独立事件池 EventPool 已并入本模块）。
	///     2. 支持多播订阅与幂等去重；同一事件的不同处理函数各自独立条目。
	///     3. 事件默认延迟到下一帧统一分发（Broadcast），亦可立即同步分发（BroadcastNow）。
	/// 线程契约：仅允许主线程访问（跨线程产生的事件由网络频道封送到主线程后再 Broadcast）。
	/// </summary>
	public sealed partial class EventModule : ModuleBase
	{
		/// <summary>
		/// 事件队列
		/// </summary>
		private Queue<Event> m_eventQueue;

		/// <summary>
		/// 帧分发批次缓存（复用同一列表，避免每次 OnUpdate 分配新列表产生 GC）
		/// </summary>
		private readonly List<Event> m_cachedEventBatch = new();

		/// <summary>
		/// 事件处理器多值字典，key为事件Id，value为事件处理函数列表
		/// </summary>
		private FuMultiDictionary<string, EventHandler<GameEventArgs>> m_eventHandlerMultiDict;

		/// <summary>
		/// 分发 handler 前的快照缓存（复用同一列表，避免每次分发分配新列表产生 GC）。
		/// 快照用于隔离「分发过程中的订阅与退订」——新订阅的 handler 本轮不调用、已退订的 handler 本轮仍调用完；
		/// 仅非重入分发使用。
		/// </summary>
		private readonly List<EventHandler<GameEventArgs>> m_cachedHandlerSnapshot = new();

		/// <summary>
		/// 是否正在分发 handler（用于识别重入的嵌套分发，避免嵌套时覆盖外层快照）
		/// </summary>
		private bool m_isHandlingEvent;

		/// <summary>
		/// 是否正在执行帧批次分发（用于检测嵌套 OnUpdate——嵌套时内层 batch.Clear() 会清掉
		/// 外层正在遍历的批次，契约上禁止嵌套驱动；检测到即拒绝本次调用，守卫保留进发布版防静默泄漏）。
		/// </summary>
		private bool m_isUpdatingEvents;

		/// <summary>
		/// 构造线程 ID（断言期校验公共入口仅被该线程访问）。
		/// 模块由 ModuleManager 在框架启动（主线程）时构造，捕获时机与原事件池构造一致。
		/// 无条件编译：[Conditional] 语义要求被断言方法及其依赖字段始终存在。
		/// </summary>
		private readonly int m_creatorThreadId = Thread.CurrentThread.ManagedThreadId;

		/// <summary>
		/// 初始化。
		/// </summary>
		protected internal override void OnInit()
		{
			m_eventQueue            = new Queue<Event>();
			m_eventHandlerMultiDict = new FuMultiDictionary<string, EventHandler<GameEventArgs>>();
		}

		/// <summary>
		/// 释放。
		/// </summary>
		protected internal override void OnDispose()
		{
			AssertMainThread();
			Clear();
			m_eventHandlerMultiDict.Clear();
		}

		/// <summary>
		/// 清理事件（私有：破坏性调试操作，不进公开契约，编辑器面板经反射调用）。
		/// 逐个出队并回收池节点：直接 Clear() 会把队列中的 Event 节点（池对象）丢弃，导致 ReferencePool 计数泄漏。
		/// 队列中的节点全部是「已入队、未分发」，其 EventArgs 也由本模块持有，必须一并回收：
		/// Event.Clear() 只置 null 不回收，只回收节点会让事件参数计数跨关停/重启持续累积（ClearAll 保留计数）。
		/// </summary>
		private void Clear()
		{
			AssertMainThread();

			// 逐项隔离回收：ReferencePool.Recycle 一旦抛异常，不得中断整轮排水，
			// 否则其后节点与事件参数全部不再归还（ReferencePool 计数永久漂移）；故各自 try/catch 吞掉并继续。
			while (m_eventQueue.Count > 0)
			{
				var eventNode = m_eventQueue.Dequeue();
				try
				{
					ReferencePool.Recycle(eventNode.EventArgs);
				}
				catch (Exception exception)
				{
					FuLogger.LogError($"清理事件时回收事件参数异常:{exception}");
				}

				try
				{
					ReferencePool.Recycle(eventNode);
				}
				catch (Exception exception)
				{
					FuLogger.LogError($"清理事件时回收事件节点异常:{exception}");
				}
			}
		}

		/// <summary>
		/// 断言当前线程为构造线程（主线程）；违规立即报错定位，而非静默产生数据竞争。
		/// [Conditional("UNITY_ASSERTIONS")]：该符号仅在 Editor/Development 构建定义，
		/// 发布版所有调用点被编译器剥离，零运行时开销。
		/// </summary>
		[Conditional("UNITY_ASSERTIONS")]
		private void AssertMainThread()
		{
			if (Thread.CurrentThread.ManagedThreadId != m_creatorThreadId)
				FuLogger.LogError($"事件模块仅允许主线程访问，检测到跨线程调用（线程 ID:{Thread.CurrentThread.ManagedThreadId}）。");
		}
	}
}
