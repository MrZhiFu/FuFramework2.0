using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Abstractions.Events;
using FuFramework.Core.Actors;
using FuFramework.Core.Hotfix;
using FuFramework.Foundation.Logger;
using FuFramework.Utility.Extensions;

namespace FuFramework.Core.Events;

/// <summary>
/// 事件分发类 - 负责处理游戏中所有事件的分发和处理
/// </summary>
public static class EventDispatcher
{
	/// <summary>
	/// 分发事件到指定的Actor或全局监听器
	/// </summary>
	/// <param name="actorId">目标Actor的唯一标识符，如果为无效值则分发到全局监听器</param>
	/// <param name="eventId">要分发的事件ID</param>
	/// <param name="eventArgs">事件携带的参数数据，可以为null</param>
	public static void Dispatch(long actorId, int eventId, GameEventArgs eventArgs = null)
	{
		Actor actor = ActorManager.GetActor(actorId);
		if (actor != null)
		{
			actor.Tell((Func<Task>)Work, int.MaxValue, default(CancellationToken));
		}
		else
		{
			Task.Run((Func<Task?>)WorkWithoutActor);
		}
		async Task Work()
		{
			List<IEventListener> list = HotfixManager.FindListeners(actor.Type, eventId);
			if (list.IsNullOrEmpty())
			{
				LogHelper.Warn($"事件：{eventId} 没有找到任何监听者");
			}
			else
			{
				foreach (IEventListener listener in list)
				{
					IComponentAgent agent = await actor.GetComponentAgent(listener.AgentType);
					try
					{
						await listener.HandleEvent(agent, eventArgs);
					}
					catch (Exception exception)
					{
						LogHelper.Error(exception);
					}
				}
			}
		}
		async Task WorkWithoutActor()
		{
			List<IEventListener> list2 = HotfixManager.FindListeners(eventId);
			if (list2.IsNullOrEmpty())
			{
				LogHelper.Warn($"事件：{eventId} 没有找到任何监听者");
			}
			else
			{
				foreach (IEventListener item in list2)
				{
					try
					{
						await item.HandleEvent(eventArgs);
					}
					catch (Exception exception2)
					{
						LogHelper.Error(exception2);
					}
				}
			}
		}
	}
}
