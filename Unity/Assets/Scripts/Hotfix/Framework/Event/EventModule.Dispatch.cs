using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
	/// <summary>
	/// 事件管理模块分部：帧分发机关（OnUpdate 批次排水 + HandleEvent 单事件分发）。
	/// </summary>
	public sealed partial class EventModule
	{
		/// <summary>
		/// 帧更新（仅由 ModuleManager 每帧驱动一次，禁止嵌套驱动）。
		/// </summary>
		/// <param name="deltaTime">帧间隔时间。</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
		protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
		{
			AssertMainThread();

			if (m_isUpdatingEvents)
			{
				FuLogger.LogError("检测到嵌套 OnUpdate：禁止在事件处理函数内手动驱动帧更新，本次调用已拒绝。");
				return;
			}

			m_isUpdatingEvents = true;

			try
			{
				var count = m_eventQueue.Count;
				if (count <= 0) return;

				m_cachedEventBatch.Clear();
				for (var i = 0; i < count; i++)
				{
					m_cachedEventBatch.Add(m_eventQueue.Dequeue());
				}

				// 逐条分发并回收；handler 抛异常时未处理的批次事件仍占用池对象，用 finally 兜底回收
				var processed = 0;
				try
				{
					for (; processed < m_cachedEventBatch.Count; processed++)
					{
						var tempEvent = m_cachedEventBatch[processed];
						HandleEvent(tempEvent.Sender, tempEvent.EventArgs);

						try
						{
							ReferencePool.Recycle(tempEvent);
						}
						catch (Exception exception)
						{
							FuLogger.LogError($"回收已分发事件节点异常:{exception}");
						}
					}
				}
				finally
				{
					for (var i = processed; i < m_cachedEventBatch.Count; i++)
					{
						var unhandledEvent = m_cachedEventBatch[i];

						// batch[processed] 的参数已由 HandleEvent 的 finally 回收，此处只补收节点；
						// 其余「出队未分发」节点的参数仍由本模块持有，须与节点一并回收
						if (i > processed)
						{
							try
							{
								ReferencePool.Recycle(unhandledEvent.EventArgs);
							}
							catch (Exception exception)
							{
								FuLogger.LogError($"兜底回收未分发事件参数异常:{exception}");
							}
						}

						try
						{
							ReferencePool.Recycle(unhandledEvent);
						}
						catch (Exception exception)
						{
							FuLogger.LogError($"兜底回收事件节点异常:{exception}");
						}
					}
				}
			}
			finally
			{
				m_isUpdatingEvents = false;
			}
		}

		/// <summary>
		/// 处理事件结点。
		/// 本方法无条件拥有 e：返回前（含异常路径）必定回收并 Clear 它，故处理函数不得转发或缓存收到的 e，
		/// 否则转发目标会观测到已清空的数据，且与「回池后可能被复用」形成脏读。需要转发时请新建事件参数对象。
		/// </summary>
		private void HandleEvent(object sender, GameEventArgs e)
		{
			try
			{
				var eventId = e.Id;
				if (string.IsNullOrEmpty(eventId))
				{
					FuLogger.LogError($"事件参数 {e.GetType().Name} 的 Id 为空，已跳过本次分发（防异常穿透分发链路）");
					return;
				}

				// 分发前先快照再逐个调用：直接遍历链表时，回调内新订阅会插到尾部被本轮再次调用，可致同帧无限循环；
				// 快照同时隔离分发过程中的订阅/退订
				List<EventHandler<GameEventArgs>> snapshot;
				if (m_isHandlingEvent)
				{
					// 重入（handler 内 BroadcastNow 触发嵌套分发）：复用外层快照会被清空，单独分配
					snapshot = new List<EventHandler<GameEventArgs>>();
				}
				else
				{
					snapshot = m_cachedHandlerSnapshot;
					snapshot.Clear();
					m_isHandlingEvent = true;
				}

				try
				{
					var hasHandlers = m_eventHandlerMultiDict.TryGetValue(eventId, out var handlerRange);

					// 无处理器：静默丢弃（原宽松模式语义，现为唯一语义）。
					if (!hasHandlers) return;

					for (var currentNode = handlerRange.First; currentNode != null && currentNode != handlerRange.End; currentNode = currentNode.Next)
					{
						snapshot.Add(currentNode.Value);
					}

					// 逐个 handler 独立 try/catch：异常只记录不穿透，否则逃到 ModuleManager.Update 当帧后续模块停更
					foreach (var handler in snapshot)
					{
						try
						{
							handler.Invoke(sender, e);
						}
						catch (Exception exception)
						{
							FuLogger.LogError($"处理事件 '{eventId}' 的处理函数时发生异常:{exception}");
						}
					}
				}
				finally
				{
					if (ReferenceEquals(snapshot, m_cachedHandlerSnapshot))
						m_isHandlingEvent = false;
				}
			}
			finally
			{
				// 回收异常吞掉不重抛：重抛会打断 OnUpdate 的兜底回收链，漏回收其后未分发节点（ReferencePool 计数漂移）
				try
				{
					ReferencePool.Recycle(e);
				}
				catch (Exception exception)
				{
					FuLogger.LogWarning($"回收事件参数异常（疑似重复归还误用）:{exception}");
				}
			}
		}
	}
}