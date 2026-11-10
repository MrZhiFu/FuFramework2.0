using System;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件注册器。
	/// 功能：
	///     1. 订阅事件。
	///     2. 取消订阅事件。
	///     3. 派发事件。
	/// 错误契约：全部守卫以 FuLogger 错误日志表达，不抛异常——未绑定事件模块的订阅器上的
	/// 所有操作均记日志并忽略，调用流程不中断（失效可观测但不致命）。
	/// </summary>
	public sealed class EventRegister : IReference
	{
		/// 事件管理模块（实例字段：每个订阅器持有自己的引用，池化对象 Clear 置 null 不影响其他实例。
		/// 原为 static——单实例 Clear 会清空共享引用，后续实例的 UnSubscribeAll 访问 null 抛 NRE）
		private EventModule m_eventModule;

		/// <summary>
		/// 事件处理多值字典，key为事件ID，value为事件处理对象列表。
		/// </summary>
		private readonly FuMultiDictionary<string, EventHandler<GameEventArgs>> m_eventHandlerDict = new();

		/// <summary>
		/// 创建事件订阅器。
		/// 创建即校验 EventModule 可用（已注册且存活）：不可用时记错误日志并返回「未绑定订阅器」——
		/// 其上的订阅/退订/派发均记日志并忽略。不返回 null：调用点均不判空，null 只会变成延迟 NRE；
		/// 静默放行则订阅写进已关停的旧池而失效，未绑定设计让每步操作显式报错，失效可观测。
		/// </summary>
		/// <returns>事件订阅器（EventModule 不可用时为未绑定订阅器）。</returns>
		public static EventRegister Create()
		{
			var eventModule = ModuleManager.GetModule<EventModule>();
			if (eventModule == null)
			{
				FuLogger.LogError("[EventRegister] EventModule 尚未注册，本次创建的订阅器未绑定事件模块（其上操作均被忽略）.");
			}
			else if (!eventModule.IsAlive)
			{
				FuLogger.LogError("[EventRegister] EventModule 未初始化或已释放（重启排水窗口），本次创建的订阅器未绑定事件模块（其上操作均被忽略）.");
				eventModule = null;
			}

			var register = ReferencePool.Acquire<EventRegister>();
			register.m_eventModule = eventModule;
			return register;
		}

		/// <summary>
		/// 订阅事件
		/// </summary>
		/// <param name="id">消息ID</param>
		/// <param name="handler">处理对象</param>
		public void Subscribe(string id, EventHandler<GameEventArgs> handler)
		{
			if (handler == null)
			{
				FuLogger.LogError("[EventRegister]事件处理对象为空，已忽略本次订阅.");
				return;
			}

			if (m_eventModule == null)
			{
				FuLogger.LogError("[EventRegister]订阅器未绑定事件模块，已忽略本次订阅.");
				return;
			}

			m_eventHandlerDict.Add(id, handler);
			m_eventModule.Subscribe(id, handler);
		}

		/// <summary>
		/// 取消订阅事件
		/// </summary>
		/// <param name="id">消息ID</param>
		/// <param name="handler">处理对象</param>
		public void UnSubscribe(string id, EventHandler<GameEventArgs> handler)
		{
			if (m_eventModule == null)
			{
				FuLogger.LogError("[EventRegister]订阅器未绑定事件模块，已忽略本次退订.");
				return;
			}

			if (!m_eventHandlerDict.Remove(id, handler))
			{
				FuLogger.LogError($"[EventRegister]事件订阅器中不存在指定消息ID '{id}' 的处理对象，已忽略本次退订.");
				return;
			}

			m_eventModule.Unsubscribe(id, handler);
		}

		/// <summary>
		/// 取消所有订阅
		/// </summary>
		public void UnSubscribeAll()
		{
			if (m_eventHandlerDict.Count == 0) return;

			foreach (var (id, eventHandlers) in m_eventHandlerDict)
			{
				foreach (var eventHandler in eventHandlers)
				{
					m_eventModule.Unsubscribe(id, eventHandler);
				}
			}

			m_eventHandlerDict.Clear();
		}

		/// <summary>
		/// 派发事件(事件会在抛出后的下一帧分发)。
		/// </summary>
		/// <param name="sender">事件发送者。</param>
		/// <param name="eventArgs">消息对象</param>
		public void Broadcast(object sender, GameEventArgs eventArgs)
		{
			if (m_eventModule == null)
			{
				FuLogger.LogError("[EventRegister]订阅器未绑定事件模块，已忽略本次派发.");
				return;
			}

			m_eventModule.Broadcast(sender, eventArgs);
		}

		/// <summary>
		/// 派发事件(事件会在抛出后的下一帧分发)。
		/// </summary>
		/// <param name="sender">事件发送者。</param>
		/// <param name="eventId">事件编号。</param>
		public void Broadcast(object sender, string eventId)
		{
			if (m_eventModule == null)
			{
				FuLogger.LogError("[EventRegister]订阅器未绑定事件模块，已忽略本次派发.");
				return;
			}

			m_eventModule.Broadcast(sender, eventId);
		}

		/// <summary>
		/// 立即抛出事件(事件会立刻分发)。
		/// </summary>
		/// <param name="sender">事件发送者。</param>
		/// <param name="eventArgs">事件内容。</param>
		public void BroadcastNow(object sender, GameEventArgs eventArgs)
		{
			if (m_eventModule == null)
			{
				FuLogger.LogError("[EventRegister]订阅器未绑定事件模块，已忽略本次派发.");
				return;
			}

			m_eventModule.BroadcastNow(sender, eventArgs);
		}

		/// <summary>
		/// 清理
		/// </summary>
		public void Clear()
		{
			UnSubscribeAll();
			m_eventModule = null;
		}

		/// <summary>
		/// 将引用归还引用池-释放资源
		/// </summary>
		public void Release() => ReferencePool.Recycle(this);
	}
}
