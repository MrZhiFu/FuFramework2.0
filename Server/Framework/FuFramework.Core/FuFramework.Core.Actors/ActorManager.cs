using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Actors.Impl;
using FuFramework.Core.Components;
using FuFramework.Core.Hotfix;
using FuFramework.Core.Timer;
using FuFramework.Core.Utility;
using FuFramework.Foundation.Logger;
using FuFramework.Utility.Extensions;

namespace FuFramework.Core.Actors;

/// <summary>
/// Actor管理器
/// </summary>
public static class ActorManager
{
	private const int WorkerCount = 10;

	private const int OnceSaveCount = 1000;

	private const int CrossDayGlobalWaitSeconds = 60;

	private const int CrossDayNotRoleWaitSeconds = 120;

	private static readonly ConcurrentDictionary<long, Actor> ActorMap;

	private static readonly ConcurrentDictionary<long, DateTime> ActiveTimeDic;

	private static readonly List<WorkerActor> WorkerActors;

	static ActorManager()
	{
		ActorMap = new ConcurrentDictionary<long, Actor>();
		ActiveTimeDic = new ConcurrentDictionary<long, DateTime>();
		WorkerActors = new List<WorkerActor>();
		for (int i = 0; i < 10; i++)
		{
			WorkerActors.Add(new WorkerActor(0L));
		}
	}

	/// <summary>
	/// 根据ActorId获取对应的IComponentAgent对象
	/// </summary>
	/// <param name="actorId">ActorId</param>
	/// <param name="isNew">是否当获取为空的时候默认创建，默认值为true</param>
	/// <typeparam name="T">组件代理类型</typeparam>
	/// <returns>组件代理任务</returns>
	public static async Task<T> GetComponentAgent<T>(long actorId, bool isNew = true) where T : IComponentAgent
	{
		if (isNew)
		{
			return await (await GetOrNew(actorId)).GetComponentAgent<T>();
		}
		Actor actor = Get(actorId);
		if (actor != null)
		{
			return await actor.GetComponentAgent<T>();
		}
		return await Task.FromResult(default(T));
	}

	/// <summary>
	/// 是否存在指定的Actor
	/// </summary>
	/// <param name="actorId">ActorId</param>
	/// <returns>是否存在</returns>
	public static bool HasActor(long actorId)
	{
		return ActorMap.ContainsKey(actorId);
	}

	/// <summary>
	/// 根据ActorId获取对应的Actor
	/// </summary>
	/// <param name="actorId">ActorId</param>
	/// <returns>Actor对象</returns>
	internal static Actor GetActor(long actorId)
	{
		ActorMap.TryGetValue(actorId, out var value);
		return value;
	}

	/// <summary>
	/// 根据ActorId和组件类型获取对应的IComponentAgent数据
	/// </summary>
	/// <param name="actorId">ActorId</param>
	/// <param name="agentType">组件类型</param>
	/// <param name="isNew">是否当获取为空的时候默认创建，默认值为true</param>
	/// <returns>组件代理任务</returns>
	internal static async Task<IComponentAgent> GetComponentAgent(long actorId, Type agentType, bool isNew = true)
	{
		if (isNew)
		{
			return await (await GetOrNew(actorId)).GetComponentAgent(agentType);
		}
		Actor actor = Get(actorId);
		if (actor != null)
		{
			return await actor.GetComponentAgent(agentType);
		}
		return await Task.FromResult<IComponentAgent>(null);
	}

	/// <summary>
	/// 根据ActorId获取对应Actor中所有激活状态的组件代理对象
	/// </summary>
	/// <param name="actorId">要查询的ActorId</param>
	/// <returns>该Actor下所有处于激活状态的组件代理对象列表,如果Actor不存在则返回空列表</returns>
	/// <remarks>
	/// 该方法会返回指定Actor中所有已经被激活的组件代理对象。
	/// 如果指定的ActorId不存在,将返回一个空列表。
	/// 组件的激活状态由Actor内部维护。
	/// </remarks>
	public static List<IComponentAgent> GetActiveComponentAgents(long actorId)
	{
		List<IComponentAgent> result = new List<IComponentAgent>();
		Actor actor = GetActor(actorId);
		if (actor.IsNull())
		{
			return result;
		}
		return actor.GetActiveComponentAgents();
	}

	/// <summary>
	/// 根据组件类型获取对应的IComponentAgent数据
	/// </summary>
	/// <typeparam name="T">组件代理类型</typeparam>
	/// <param name="isNew">是否当获取为空的时候默认创建，默认值为true</param>
	/// <returns>组件代理任务</returns>
	public static Task<T> GetComponentAgent<T>(bool isNew = true) where T : IComponentAgent
	{
		return GetComponentAgent<T>(ActorIdGenerator.GetActorId(ComponentRegister.GetActorType(HotfixManager.GetComponentType(typeof(T)))), isNew);
	}

	/// <summary>
	/// 根据actorId获取对应的actor实例，不存在则新生成一个Actor对象
	/// </summary>
	/// <param name="actorId">actorId</param>
	/// <returns>Actor对象任务</returns>
	internal static async Task<Actor> GetOrNew(long actorId)
	{
		if (ActorIdGenerator.GetActorType(actorId) < 128)
		{
			DateTime now = DateTime.Now;
			if (ActiveTimeDic.TryGetValue(actorId, out var value) && (now - value).TotalMinutes < 10.0 && ActorMap.TryGetValue(actorId, out var value2))
			{
				ActiveTimeDic[actorId] = now;
				return value2;
			}
			return await GetLifeActor(actorId).SendAsync(delegate
			{
				ActiveTimeDic[actorId] = now;
				return ActorMap.GetOrAdd(actorId, (long k) => new Actor(k, ActorIdGenerator.GetActorType(k)));
			});
		}
		return ActorMap.GetOrAdd(actorId, (long k) => new Actor(k, ActorIdGenerator.GetActorType(k)));
	}

	/// <summary>
	/// 根据actorId获取对应的actor实例，不存在则返回空
	/// </summary>
	/// <param name="actorId">actorId</param>
	/// <returns>Actor对象任务</returns>
	private static Actor Get(long actorId)
	{
		Actor value3;
		if (ActorIdGenerator.GetActorType(actorId) < 128)
		{
			DateTime now = DateTime.Now;
			if (ActiveTimeDic.TryGetValue(actorId, out var value) && (now - value).TotalMinutes < 10.0 && ActorMap.TryGetValue(actorId, out var value2))
			{
				ActiveTimeDic[actorId] = now;
				return value2;
			}
			ActorMap.TryGetValue(actorId, out value3);
			return value3;
		}
		ActorMap.TryGetValue(actorId, out value3);
		return value3;
	}

	/// <summary>
	/// 全部完成
	/// </summary>
	/// <returns>任务集合</returns>
	public static Task AllFinish()
	{
		List<Task> list = new List<Task>();
		foreach (Actor value in ActorMap.Values)
		{
			list.Add(value.SendAsync(() => true));
		}
		return Task.WhenAll(list);
	}

	/// <summary>
	/// 根据ActorId 获取玩家
	/// </summary>
	/// <param name="actorId">ActorId</param>
	/// <returns>WorkerActor对象</returns>
	private static WorkerActor GetLifeActor(long actorId)
	{
		return WorkerActors[(int)(actorId % 10)];
	}

	/// <summary>
	/// 检查并回收空闲的Actor
	/// </summary>
	/// <returns>任务</returns>
	public static Task CheckIdle()
	{
		foreach (Actor value3 in ActorMap.Values)
		{
			Actor actor = value3;
			if (actor.AutoRecycle)
			{
				actor.Tell((Func<Task>)Func, int.MaxValue, default(CancellationToken));
			}
			async Task Func()
			{
				if (actor.AutoRecycle && (DateTime.Now - ActiveTimeDic[actor.Id]).TotalMinutes > 15.0)
				{
					await GetLifeActor(actor.Id).SendAsync((Func<Task<bool>>)Work, int.MaxValue, default(CancellationToken));
				}
			}
			async Task<bool> Work()
			{
				if (ActiveTimeDic.TryGetValue(actor.Id, out var _) && (DateTime.Now - ActiveTimeDic[actor.Id]).TotalMinutes > 15.0)
				{
					if (actor.ReadyToDeActive)
					{
						await actor.Inactive();
						ActorMap.TryRemove(actor.Id, out var _);
						LogHelper.Debug($"actor回收 id:{actor.Id} type:{actor.Type}");
					}
					else
					{
						ActiveTimeDic[actor.Id] = DateTime.Now;
					}
				}
				return true;
			}
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// 保存所有数据
	/// </summary>
	/// <returns>任务</returns>
	public static async Task SaveAll()
	{
		try
		{
			DateTime begin = DateTime.Now;
			List<Task> list = new List<Task>();
			foreach (Actor value2 in ActorMap.Values)
			{
				Actor actor = value2;
				list.Add(actor.SendAsync((Action)Save));
				async void Save()
				{
					await actor.SaveAllState();
				}
			}
			await Task.WhenAll(list);
			LogHelper.Info($"save all state, use: {(DateTime.Now - begin).TotalMilliseconds}ms");
		}
		catch (Exception value)
		{
			LogHelper.Error($"save all state error \n{value}");
			throw;
		}
	}

	/// <summary>
	/// 定时回存所有数据
	/// </summary>
	/// <returns>任务</returns>
	public static async Task TimerSave()
	{
		_ = 1;
		try
		{
			int num = 0;
			List<Task> list = new List<Task>();
			foreach (Actor value in ActorMap.Values)
			{
				Actor actor = value;
				if (!GlobalTimer.IsWorking)
				{
					return;
				}
				if (num < 1000)
				{
					list.Add(actor.SendAsync((Action)Work));
					num++;
					continue;
				}
				await Task.WhenAll(list);
				await Task.Delay(1000);
				list = new List<Task>();
				num = 0;
				async void Work()
				{
					await actor.SaveAllState();
				}
			}
		}
		catch (Exception ex)
		{
			LogHelper.Info("timer save state error");
			LogHelper.Error(ex.ToString());
		}
	}

	/// <summary>
	/// 角色跨天
	/// </summary>
	/// <param name="openServerDay">开服天数</param>
	/// <returns>任务</returns>
	public static Task RoleCrossDay(int openServerDay)
	{
		foreach (Actor actor in ActorMap.Values)
		{
			if (actor.Type < 128)
			{
				actor.Tell(() => actor.CrossDay(openServerDay));
			}
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// 跨天
	/// </summary>
	/// <param name="openServerDay">开服天数</param>
	/// <param name="driverActorType">驱动Actor类型</param>
	/// <returns>任务</returns>
	public static async Task CrossDay(int openServerDay, ushort driverActorType)
	{
		long actorId = ActorIdGenerator.GetActorId(driverActorType);
		await ActorMap[actorId].CrossDay(openServerDay);
		DateTime begin = DateTime.Now;
		int a = 0;
		int b = 0;
		foreach (Actor value2 in ActorMap.Values)
		{
			Actor actor = value2;
			if (actor.Type > 128 && actor.Type != driverActorType)
			{
				b++;
				actor.Tell((Func<Task>)Work, int.MaxValue, default(CancellationToken));
			}
			async Task Work()
			{
				LogHelper.Info($"全局Actor：{actor.Type}执行跨天");
				await actor.CrossDay(openServerDay);
				Interlocked.Increment(ref a);
			}
		}
		while (a < b)
		{
			if ((DateTime.Now - begin).TotalSeconds > 60.0)
			{
				LogHelper.Warn($"全局comp跨天耗时过久，不阻止其他comp跨天，当前已过{60}秒");
				break;
			}
			await Task.Delay(TimeSpan.FromMilliseconds(10.0));
		}
		double globalCost = (DateTime.Now - begin).TotalMilliseconds;
		LogHelper.Info($"全局comp跨天完成 耗时：{globalCost:f4}ms");
		a = 0;
		b = 0;
		foreach (Actor value3 in ActorMap.Values)
		{
			Actor actor2 = value3;
			if (actor2.Type > 128)
			{
				b++;
				actor2.Tell((Func<Task>)Work, int.MaxValue, default(CancellationToken));
			}
			async Task Work()
			{
				await actor2.CrossDay(openServerDay);
				Interlocked.Increment(ref a);
			}
		}
		while (a < b)
		{
			if ((DateTime.Now - begin).TotalSeconds > 120.0)
			{
				LogHelper.Warn($"非玩家comp跨天耗时过久，不阻止玩家comp跨天，当前已过{120}秒");
				break;
			}
			await Task.Delay(TimeSpan.FromMilliseconds(10.0));
		}
		double value = (DateTime.Now - begin).TotalMilliseconds - globalCost;
		LogHelper.Info($"非玩家comp跨天完成 耗时：{value:f4}ms");
	}

	/// <summary>
	/// 删除所有actor
	/// </summary>
	/// <returns>任务</returns>
	public static async Task RemoveAll()
	{
		List<Task> list = new List<Task>();
		foreach (Actor value in ActorMap.Values)
		{
			list.Add(value.Inactive());
		}
		await Task.WhenAll(list);
	}

	/// <summary>
	/// 删除actor
	/// </summary>
	/// <param name="actorId">actorId</param>
	/// <returns>任务</returns>
	public static Task Remove(long actorId)
	{
		if (ActorMap.Remove(actorId, out var value))
		{
			value.Tell((Func<Task>)value.Inactive, int.MaxValue, default(CancellationToken));
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// 遍历所有actor
	/// </summary>
	/// <param name="action">遍历回调</param>
	public static void ActorForEach(Action<IActor> action)
	{
		foreach (Actor value in ActorMap.Values)
		{
			try
			{
				action(value);
			}
			catch (Exception exception)
			{
				LogHelper.Error(exception);
			}
		}
	}

	/// <summary>
	/// 遍历所有actor
	/// </summary>
	/// <param name="func">遍历actor回调</param>
	/// <typeparam name="T">组件代理类型</typeparam>
	public static void ActorForEach<T>(Func<T, Task> func) where T : IComponentAgent
	{
		ushort actorType = ComponentRegister.GetActorType(HotfixManager.GetComponentType(typeof(T)));
		foreach (Actor value in ActorMap.Values)
		{
			Actor actor = value;
			if (actor.Type == actorType)
			{
				actor.Tell((Func<Task>)Work, int.MaxValue, default(CancellationToken));
			}
			async Task Work()
			{
				T arg = await actor.GetComponentAgent<T>();
				await func(arg);
			}
		}
	}

	/// <summary>
	/// 遍历所有actor
	/// </summary>
	/// <param name="action">遍历actor回调</param>
	/// <typeparam name="T">组件代理类型</typeparam>
	public static void ActorForEach<T>(Action<T> action) where T : IComponentAgent
	{
		ushort actorType = ComponentRegister.GetActorType(HotfixManager.GetComponentType(typeof(T)));
		foreach (Actor value in ActorMap.Values)
		{
			Actor actor = value;
			if (actor.Type == actorType)
			{
				actor.Tell((Func<Task>)Work, int.MaxValue, default(CancellationToken));
			}
			async Task Work()
			{
				T obj = await actor.GetComponentAgent<T>();
				action(obj);
			}
		}
	}

	/// <summary>
	/// 清除所有agent
	/// </summary>
	public static void ClearAgent()
	{
		foreach (Actor value in ActorMap.Values)
		{
			value.Tell((Action)value.ClearAgent, int.MaxValue, default(CancellationToken));
		}
	}
}
