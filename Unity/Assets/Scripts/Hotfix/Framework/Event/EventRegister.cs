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
	/// 生命周期契约：窗口关闭时 Suspend（总线退订、账本保留即重放清单），打开时 Resume（按账本重放）；
	/// 挂起态下的新订阅只记账本不进总线。
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
		/// 是否已挂起（窗口关闭态）：挂起时账本仍登记新订阅、总线侧保持退订，Resume 按账本统一重放。
		/// 寄存器经 ReferencePool 回池复用且 Acquire 不重置任何字段，Clear 必须复位本标志——
		/// 漏复位会让挂起态寄存器被新窗口领走后订阅全部只进账本不进总线（事件静默失效）。
		/// </summary>
		private bool m_suspended;

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
				FuLogger.LogError("EventModule尚未注册，本次创建的订阅器未绑定事件模块（其上操作均被忽略）.");
			}
			else if (!eventModule.IsAlive)
			{
				FuLogger.LogError("EventModule未初始化或已释放（重启排水窗口），本次创建的订阅器未绑定事件模块（其上操作均被忽略）.");
				eventModule = null;
			}

			var register = ReferencePool.Acquire<EventRegister>();
			register.m_eventModule = eventModule;
			return register;
		}

		/// <summary>
		/// 订阅事件
		/// 幂等去重：同一 (id, handler) 重复登记警告+忽略——与 EventModule.Subscribe 的总线级去重对齐，
		/// 保证账本与总线条目严格 1:1，Suspend/Resume 按账本增删总线时严格对称（否则重复订阅会造成
		/// 账本 N 份、总线 1 条的失配，挂起重放对账错位、退订自检失效）。
		/// </summary>
		/// <param name="id">消息ID</param>
		/// <param name="handler">处理对象</param>
		public void Subscribe(string id, EventHandler<GameEventArgs> handler)
		{
			if (handler == null)
			{
				FuLogger.LogError("事件处理对象为空，已忽略本次订阅.");
				return;
			}

			if (m_eventModule == null)
			{
				FuLogger.LogError("订阅器未绑定事件模块，已忽略本次订阅.");
				return;
			}

			if (m_eventHandlerDict.Contains(id, handler))
			{
				FuLogger.LogWarning($"重复登记事件 '{id}' 的同一处理函数，本次已忽略（账本与总线条目保持 1:1，防挂起重放对账错位）:{handler}");
				return;
			}

			m_eventHandlerDict.Add(id, handler);

			// 挂起态只记账不进总线：总线侧当前整体退订中，重开时由 Resume 按账本统一重放
			if (m_suspended) return;

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
				FuLogger.LogError("订阅器未绑定事件模块，已忽略本次退订.");
				return;
			}

			if (!m_eventHandlerDict.Remove(id, handler))
			{
				FuLogger.LogError($"事件订阅器中不存在指定消息ID '{id}' 的处理对象，已忽略本次退订.");
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
				FuLogger.LogError("订阅器未绑定事件模块，已忽略本次派发.");
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
				FuLogger.LogError("订阅器未绑定事件模块，已忽略本次派发.");
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
				FuLogger.LogError("订阅器未绑定事件模块，已忽略本次派发.");
				return;
			}

			m_eventModule.BroadcastNow(sender, eventArgs);
		}

		/// <summary>
		/// 挂起：窗口关闭时调用。按账本逐条从总线退订（模块侧对缺失条目安全空操作），账本保留不清空——账本即重放清单。
		/// 幂等：已挂起直接返回。internal：仅框架生命周期（WinBase._OnClose）可调，业务勿手动挂起。
		/// </summary>
		internal void Suspend()
		{
			if (m_suspended) return;

			m_suspended = true;
			if (m_eventModule == null) return;

			foreach (var (id, eventHandlers) in m_eventHandlerDict)
			{
				foreach (var eventHandler in eventHandlers)
				{
					m_eventModule.Unsubscribe(id, eventHandler);
				}
			}
		}

		/// <summary>
		/// 恢复：窗口打开时调用（须先于业务 OnOpen，保证其执行期间订阅已就绪）。按账本逐条向总线重放订阅。
		/// 幂等：未挂起直接返回。internal：仅框架生命周期（WinBase._OnOpen）可调，业务勿手动重放。
		/// </summary>
		internal void Resume()
		{
			if (!m_suspended) return;

			m_suspended = false;
			if (m_eventModule == null) return;

			foreach (var (id, eventHandlers) in m_eventHandlerDict)
			{
				foreach (var eventHandler in eventHandlers)
				{
					m_eventModule.Subscribe(id, eventHandler);
				}
			}
		}

		/// <summary>
		/// 清理
		/// </summary>
		public void Clear()
		{
			UnSubscribeAll();
			m_suspended   = false; // 复位挂起标志：寄存器回池复用且 Acquire 不重置字段，漏复位会让新窗口的订阅只进账本不进总线
			m_eventModule = null;
		}

		/// <summary>
		/// 将引用归还引用池-释放资源
		/// </summary>
		public void Release() => ReferencePool.Recycle(this);
	}
}