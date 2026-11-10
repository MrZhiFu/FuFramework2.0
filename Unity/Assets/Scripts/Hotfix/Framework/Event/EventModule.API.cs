using System;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块的公共 API。
	/// 功能：
	///     1. 订阅/退订事件处理函数（幂等去重、即时退订）。
	///     2. 查询订阅状态。
	///     3. 抛出事件（下一帧分发 / 立即分发）。
	/// </summary>
	public sealed partial class EventModule : ModuleBase
	{
		/// <summary>
		/// 订阅事件处理函数。
		/// 幂等去重：同一 (id, handler) 重复订阅只保留首次登记（分发时只调用一次），重复订阅记录警告日志——
		/// 本框架中同一处理函数只有一个所有者（各模块/窗口/组件订阅自己的方法），重复订阅几乎必然是
		/// 「池化对象重复订阅未退订」类误用，静默去重会掩盖问题。同一事件的不同处理函数（多播）各自独立条目。
		/// </summary>
		/// <param name="id">事件类型编号。</param>
		/// <param name="handler">要订阅的事件处理函数。</param>
		public void Subscribe(string id, EventHandler<GameEventArgs> handler)
		{
			AssertMainThread();
			if (handler == null)
			{
				FuLogger.LogError("事件处理函数为空，已忽略本次订阅");
				return;
			}

			if (m_eventHandlerMultiDict.Contains(id, handler))
			{
				FuLogger.LogWarning($"重复订阅事件 '{id}' 的同一处理函数，本次订阅已忽略:{handler}");
				return;
			}

			m_eventHandlerMultiDict.Add(id, handler);
		}

		/// <summary>
		/// 取消订阅事件处理函数：立即从订阅字典移除条目。
		/// 未订阅过的 (id, handler) 直接忽略、不抛异常——FuMultiDictionary.Remove 对缺失条目是安全空操作，
		/// 重复退订（如 UnSubscribeAll 重复账本、退订后再次退订）无需调用方自行判存。
		/// 分发安全：HandleEvent 分发前先快照 handler 列表，分发过程中退订（含 handler 内退订自己/他人）
		/// 不影响本轮快照，被退订的 handler 在本轮仍会收到当次事件，下一轮起不再收到。
		/// </summary>
		/// <param name="id">事件类型编号。</param>
		/// <param name="handler">要取消订阅的事件处理函数。</param>
		public void Unsubscribe(string id, EventHandler<GameEventArgs> handler)
		{
			AssertMainThread();
			if (handler == null)
			{
				FuLogger.LogError("事件处理函数为空，已忽略本次退订");
				return;
			}

			m_eventHandlerMultiDict.Remove(id, handler);
		}

		/// <summary>
		/// 抛出事件，事件会延迟到抛出后的下一帧由主线程分发。
		/// 事件模块仅允许主线程访问（开发期以 UNITY_ASSERTIONS 断言拦截跨线程误用）；
		/// 跨线程产生的事件须先由网络频道等来源封送至主线程，再调用本方法。
		/// 注意：调用后 e 的所有权即移交事件模块（入队持有至分发结束回收），发送者不得再触碰或回收；
		/// 分发结束即回收事件参数并 Clear（见 HandleEvent），处理函数不得转发或缓存收到的 e，
		/// 否则其它处理函数/后续帧会观测到已被清空的数据；需要转发时请新建事件参数对象。
		/// 注意：事件模块关停（OnDispose）之后不得再调用本方法，否则事件将滞留队列永不回收。
		/// </summary>
		/// <param name="sender">事件源。</param>
		/// <param name="e">事件参数。</param>
		public void Broadcast(object sender, GameEventArgs e)
		{
			AssertMainThread();
			if (e == null)
			{
				FuLogger.LogError("事件参数为空，已忽略本次抛出");
				return;
			}

			var tempEvent = Event.Create(sender, e);
			m_eventQueue.Enqueue(tempEvent);
		}

		/// <summary>
		/// 使用事件编号抛出事件，取巧地使用一个空事件包装一个事件编号, 这样可以避免创建过多的无需事件数据的事件对象。
		/// 主线程限制与分发时机同 Broadcast(object, GameEventArgs)：仅限主线程调用，事件会延迟到下一帧主线程分发。
		/// </summary>
		/// <param name="sender">事件发送者。</param>
		/// <param name="eventId">事件编号。</param>
		public void Broadcast(object sender, string eventId)
		{
			if (string.IsNullOrEmpty(eventId))
			{
				FuLogger.LogError("事件编号为空，已忽略本次抛出");
				return;
			}

			Broadcast(sender, EmptyEventArgs.Create(eventId));
		}

		/// <summary>
		/// 立即抛出事件，事件会立刻在主线程分发。
		/// 注意：本方法返回时事件参数已被回收并 Clear，处理函数不得转发或缓存收到的 e（同 Broadcast）。
		/// 事件模块仅允许主线程访问（开发期以 UNITY_ASSERTIONS 断言拦截跨线程误用），故仅限主线程调用。
		/// </summary>
		/// <param name="sender">事件发送者。</param>
		/// <param name="e">事件参数。</param>
		public void BroadcastNow(object sender, GameEventArgs e)
		{
			AssertMainThread();
			if (e == null)
			{
				FuLogger.LogError("事件参数为空，已忽略本次立即分发");
				return;
			}

			HandleEvent(sender, e);
		}
	}
}