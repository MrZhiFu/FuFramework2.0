using System;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块的公共 API。
	/// 功能：
	///     1. 订阅/退订事件处理函数（幂等去重、即时退订；类型键：事件参数类型即事件标识）。
	///     2. 抛出事件（下一帧分发 / 立即分发；无 sender 形态）。
	/// </summary>
	public sealed partial class EventModule : ModuleBase
	{
		/// <summary>
		/// 订阅事件处理函数（类型键：事件参数类型即事件标识，见 BaseEventArgs.Id）。
		/// 幂等去重：同一事件同一处理函数重复订阅只保留首次登记（分发时只调用一次），重复订阅记录警告日志——
		/// 本框架中同一处理函数只有一个所有者（各模块/窗口/组件订阅自己的方法），重复订阅几乎必然是
		/// 「池化对象重复订阅未退订」类误用，静默去重会掩盖问题。同一事件的不同处理函数（多播）各自独立条目。
		/// </summary>
		/// <typeparam name="T">事件参数类型（事件标识）。</typeparam>
		/// <param name="handler">要订阅的事件处理函数。</param>
		public void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
			=> Subscribe(typeof(T).FullName, handler);

		/// <summary>
		/// 订阅事件处理函数（运行时类型形态：事件类型编译期未知时使用，身份仍为事件参数类型 FullName）。
		/// 语义与泛型形态完全一致；典型场景如红点触发事件集合（运行时 Type 数组）。
		/// </summary>
		/// <param name="eventType">事件参数类型（事件标识）。</param>
		/// <param name="handler">要订阅的事件处理函数。</param>
		public void Subscribe(Type eventType, Action<GameEventArgs> handler)
			=> Subscribe(eventType.FullName, handler);

		/// <summary>
		/// 框架簿记专用订阅入口（挂起/重放/账本遍历按 (id, handler) 对调用，泛型无法从运行时 id 反推）。
		/// 业务订阅走 <see cref="Subscribe{T}(Action{GameEventArgs})"/> 泛型形态，勿直接调用本方法。
		/// 幂等去重：同一 (id, handler) 重复订阅只保留首次登记（分发时只调用一次），重复订阅记录警告日志。
		/// </summary>
		/// <param name="id">事件类型编号（事件参数类型 FullName）。</param>
		/// <param name="handler">要订阅的事件处理函数。</param>
		internal void Subscribe(string id, Action<GameEventArgs> handler)
		{
			AssertMainThread();
			if (handler == null)
			{
				FuLogger.LogError("事件处理函数为空，已忽略本次订阅");
				return;
			}

			if (m_actionHandlerMultiDict.Contains(id, handler))
			{
				FuLogger.LogWarning($"重复订阅事件 '{id}' 的同一处理函数，本次订阅已忽略:{handler}");
				return;
			}

			m_actionHandlerMultiDict.Add(id, handler);
		}

		/// <summary>
		/// 取消订阅事件处理函数（类型键：事件参数类型即事件标识，见 BaseEventArgs.Id）。
		/// 未订阅过的条目直接忽略、不抛异常——FuMultiDictionary.Remove 对缺失条目是安全空操作，
		/// 重复退订（如 UnSubscribeAll 重复账本、退订后再次退订）无需调用方自行判存。
		/// 分发安全：HandleEvent 分发前先快照 handler 列表，分发过程中退订（含 handler 内退订自己/他人）
		/// 不影响本轮快照，被退订的 handler 在本轮仍会收到当次事件，下一轮起不再收到。
		/// </summary>
		/// <typeparam name="T">事件参数类型（事件标识）。</typeparam>
		/// <param name="handler">要取消订阅的事件处理函数。</param>
		public void Unsubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
			=> Unsubscribe(typeof(T).FullName, handler);

		/// <summary>
		/// 取消订阅事件处理函数（运行时类型形态：事件类型编译期未知时使用，身份仍为事件参数类型 FullName）。
		/// </summary>
		/// <param name="eventType">事件参数类型（事件标识）。</param>
		/// <param name="handler">要取消订阅的事件处理函数。</param>
		public void Unsubscribe(Type eventType, Action<GameEventArgs> handler)
			=> Unsubscribe(eventType.FullName, handler);

		/// <summary>
		/// 框架簿记专用退订入口（挂起/重放/账本遍历按 (id, handler) 对调用）。
		/// 业务退订走 <see cref="Unsubscribe{T}(Action{GameEventArgs})"/> 泛型形态，勿直接调用本方法。
		/// </summary>
		/// <param name="id">事件类型编号（事件参数类型 FullName）。</param>
		/// <param name="handler">要取消订阅的事件处理函数。</param>
		internal void Unsubscribe(string id, Action<GameEventArgs> handler)
		{
			AssertMainThread();
			if (handler == null)
			{
				FuLogger.LogError("事件处理函数为空，已忽略本次退订");
				return;
			}

			m_actionHandlerMultiDict.Remove(id, handler);
		}

		/// <summary>
		/// 抛出事件，事件会延迟到抛出后的下一帧由主线程分发。
		/// 事件模块仅允许主线程访问（开发期以 UNITY_ASSERTIONS 断言拦截跨线程误用）；
		/// 跨线程产生的事件须先由网络频道等来源封送至主线程，再调用本方法。
		/// 注意：调用后 e 的所有权即移交事件模块（入队持有至分发结束回收），发送者不得再触碰或回收；
		/// 分发结束即回收事件参数并 Clear（见 HandleEvent），处理函数不得转发、缓存或修改收到的 e，
		/// 否则其它处理函数/后续帧会观测到已被清空的数据；需要转发时请新建事件参数对象。
		/// 注意：事件模块关停（OnDispose）之后不得再调用本方法，否则事件将滞留队列永不回收。
		/// </summary>
		/// <param name="e">事件参数。</param>
		public void Broadcast(GameEventArgs e)
		{
			AssertMainThread();
			if (e == null)
			{
				FuLogger.LogError("事件参数为空，已忽略本次抛出");
				return;
			}

			var tempEvent = Event.Create(e);
			m_eventQueue.Enqueue(tempEvent);
		}

		/// <summary>
		/// 立即抛出事件，事件会立刻在主线程分发。
		/// 注意：本方法返回时事件参数已被回收并 Clear，处理函数不得转发、缓存或修改收到的 e（同 Broadcast）。
		/// 事件模块仅允许主线程访问（开发期以 UNITY_ASSERTIONS 断言拦截跨线程误用），故仅限主线程调用。
		/// </summary>
		/// <param name="e">事件参数。</param>
		public void BroadcastNow(GameEventArgs e)
		{
			AssertMainThread();
			if (e == null)
			{
				FuLogger.LogError("事件参数为空，已忽略本次立即分发");
				return;
			}

			HandleEvent(e);
		}
	}
}
