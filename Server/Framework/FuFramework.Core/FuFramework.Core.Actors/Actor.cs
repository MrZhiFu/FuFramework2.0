using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Abstractions.Timer;
using FuFramework.Core.Actors.Impl;
using FuFramework.Core.Components;
using FuFramework.Foundation.Logger;

namespace FuFramework.Core.Actors;

/// <summary>
/// Actor类,用于管理和协调组件的生命周期、消息传递等核心功能
/// </summary>
public sealed class Actor : IActor, IWorker
{
	/// <summary>
	/// 默认超时时长,使用int最大值表示无限等待
	/// </summary>
	public const int TimeOut = int.MaxValue;

	/// <summary>
	/// 组件映射字典,用于存储当前Actor下的所有组件实例
	/// </summary>
	private readonly ConcurrentDictionary<Type, BaseComponent> _componentsMap = new ConcurrentDictionary<Type, BaseComponent>();

	/// <summary>
	/// 判断Actor是否准备好进行反激活操作
	/// </summary>
	internal bool ReadyToDeActive => _componentsMap.Values.All((BaseComponent item) => item.ReadyToInactive);

	/// <summary>
	/// Actor的唯一标识符
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	/// 订阅的定时器任务ID集合
	/// </summary>
	public HashSet<long> ScheduleIdSet { get; } = new HashSet<long>();

	/// <summary>
	/// Actor的类型标识,用于区分不同种类的Actor
	/// </summary>
	public ushort Type { get; set; }

	/// <summary>
	/// 工作者Actor实例,负责具体的任务执行
	/// </summary>
	public IWorkerActor WorkerActor { get; init; }

	/// <summary>
	/// 标识Actor是否启用自动回收机制
	/// </summary>
	public bool AutoRecycle { get; private set; }

	/// <summary>
	/// Actor构造函数
	/// </summary>
	/// <param name="id">Actor的唯一标识符</param>
	/// <param name="type">Actor的类型标识</param>
	public Actor(long id, ushort type)
	{
		Id = id;
		Type = type;
		WorkerActor = new WorkerActor(id);
		if (type < 128)
		{
			Tell(delegate
			{
				SetAutoRecycle(autoRecycle: true);
			});
		}
		else
		{
			Tell(() => ComponentRegister.ActiveComponents(this));
		}
	}

	/// <summary>
	/// 设置Actor的自动回收状态
	/// </summary>
	/// <param name="autoRecycle">是否启用自动回收,true表示启用,false表示禁用</param>
	public void SetAutoRecycle(bool autoRecycle)
	{
		Tell(delegate
		{
			AutoRecycle = autoRecycle;
		});
	}

	/// <summary>
	/// 获取指定类型的组件代理实例
	/// </summary>
	/// <typeparam name="T">组件代理类型</typeparam>
	/// <param name="isNew">当组件不存在时是否创建新实例,默认为true</param>
	/// <returns>返回指定类型的组件代理实例</returns>
	public async Task<T> GetComponentAgent<T>(bool isNew = true) where T : IComponentAgent
	{
		return (T)(await GetComponentAgent(typeof(T), isNew));
	}

	/// <summary>
	/// 获取所有已激活的组件代理实例
	/// </summary>
	/// <remarks>
	/// 遍历组件映射字典(_componentsMap),筛选出所有处于激活状态(IsActive=true)的组件,
	/// 并获取它们对应的代理实例。这个方法通常用于需要批量处理或遍历所有活跃组件的场景。
	/// </remarks>
	/// <returns>返回包含所有已激活组件代理实例的列表</returns>
	public List<IComponentAgent> GetActiveComponentAgents()
	{
		List<IComponentAgent> list = new List<IComponentAgent>();
		foreach (KeyValuePair<Type, BaseComponent> item in _componentsMap)
		{
			if (item.Value.IsActive)
			{
				list.Add(item.Value.GetAgent());
			}
		}
		return list;
	}

	/// <summary>
	/// 根据代理类型获取组件代理实例
	/// </summary>
	/// <param name="agentType">代理类型</param>
	/// <param name="isNew">当组件不存在时是否创建新实例,默认为true</param>
	/// <returns>返回指定类型的组件代理实例</returns>
	public async Task<IComponentAgent> GetComponentAgent(Type agentType, bool isNew = true)
	{
		Type key = agentType.BaseType.GetGenericArguments()[0];
		BaseComponent comp = null!;
		IComponentAgent agent;
		if (isNew)
		{
			comp = _componentsMap.GetOrAdd(key, GetOrAddFactory);
			agent = comp.GetAgent(agentType);
			if (!comp.IsActive)
			{
				await SendAsyncWithoutCheck(Worker);
			}
			return agent;
		}
		if (!_componentsMap.TryGetValue(key, out var component))
		{
			return null;
		}
		agent = component.GetAgent(agentType);
		if (!component.IsActive)
		{
			await SendAsyncWithoutCheck(Worker2);
		}
		return agent;
		async Task Worker()
		{
			try
			{
				await comp.Active();
			}
			catch (Exception exception)
			{
				LogHelper.Fatal(exception);
			}
			try
			{
				await agent.Active();
			}
			catch (Exception exception2)
			{
				LogHelper.Fatal(exception2);
			}
		}
		async Task Worker2()
		{
			try
			{
				await component.Active();
			}
			catch (Exception exception3)
			{
				LogHelper.Fatal(exception3);
			}
			try
			{
				await agent.Active();
			}
			catch (Exception exception4)
			{
				LogHelper.Fatal(exception4);
			}
		}
	}

	/// <summary>
	/// 处理跨天逻辑,遍历所有组件并执行跨天操作
	/// </summary>
	/// <param name="openServerDay">开服天数</param>
	public async Task CrossDay(int openServerDay)
	{
		LogHelper.Debug($"actor跨天 id:{Id} type:{Type}");
		foreach (BaseComponent value2 in _componentsMap.Values)
		{
			IComponentAgent agent = value2.GetAgent();
			if (agent is ICrossDay crossDay)
			{
				try
				{
					await crossDay.OnCrossDay(openServerDay);
				}
				catch (Exception value)
				{
					LogHelper.Error($"{agent.GetType().FullName}跨天失败 actorId:{Id} actorType:{Type} 异常：\n{value}");
				}
			}
		}
	}

	/// <summary>
	/// 反激活所有组件,使其进入非活动状态
	/// </summary>
	public async Task Inactive()
	{
		foreach (BaseComponent value in _componentsMap.Values)
		{
			await value.Inactive();
		}
	}

	/// <summary>
	/// 清理所有组件的缓存代理实例
	/// </summary>
	public void ClearAgent()
	{
		foreach (BaseComponent value in _componentsMap.Values)
		{
			value.ClearCacheAgent();
		}
	}

	/// <summary>
	/// 创建或获取指定类型的组件实例
	/// </summary>
	/// <param name="type">组件类型</param>
	/// <returns>返回基础组件实例</returns>
	private BaseComponent GetOrAddFactory(Type type)
	{
		return ComponentRegister.CreateComponent(this, type);
	}

	/// <summary>
	/// 保存所有组件的当前状态
	/// </summary>
	internal async Task SaveAllState()
	{
		foreach (KeyValuePair<Type, BaseComponent> item in _componentsMap)
		{
			await item.Value.WriteStateAsync();
		}
	}

	/// <summary>
	/// 重写ToString方法,返回Actor的标识信息
	/// </summary>
	/// <returns>返回包含类型和ID的字符串表示</returns>
	public override string ToString()
	{
		return $"{base.ToString()}_{Type}_{Id}";
	}

	/// <summary>
	/// 发送无返回值的工作指令到Actor队列
	/// </summary>
	/// <param name="work">要执行的工作内容</param>
	/// <param name="timeOut">执行超时时间,默认为TimeOut常量值</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	public void Tell(Action work, int timeOut = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
	{
		WorkerActor.Tell(work, timeOut, cancellationToken);
	}

	/// <summary>
	/// 发送异步工作指令到Actor队列
	/// </summary>
	/// <param name="work">要执行的异步工作内容</param>
	/// <param name="timeOut">执行超时时间,默认为TimeOut常量值</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	public void Tell(Func<Task> work, int timeOut = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
	{
		WorkerActor.Tell(work, timeOut, cancellationToken);
	}

	/// <summary>
	/// 发送无返回值的异步工作指令
	/// </summary>
	/// <param name="work">要执行的工作内容</param>
	/// <returns>返回表示异步操作的Task</returns>
	public Task SendAsync(Action work)
	{
		return WorkerActor.SendAsync(work);
	}

	/// <summary>
	/// 发送带超时的异步工作指令
	/// </summary>
	/// <param name="work">要执行的工作内容</param>
	/// <param name="timeout">执行超时时间（毫秒），默认为int.MaxValue</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	/// <returns>返回表示异步操作的Task</returns>
	public Task SendAsync(Action work, int timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WorkerActor.SendAsync(work, timeout, cancellationToken);
	}

	/// <summary>
	/// 发送带返回值的异步工作指令
	/// </summary>
	/// <typeparam name="T">返回值类型</typeparam>
	/// <param name="work">要执行的工作内容</param>
	/// <param name="timeout">超时时间,默认为TimeOut常量值</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	/// <returns>返回指定类型的异步操作结果</returns>
	public Task<T> SendAsync<T>(Func<T> work, int timeout = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WorkerActor.SendAsync(work, timeout, cancellationToken);
	}

	/// <summary>
	/// 发送带锁检查的异步工作指令
	/// </summary>
	/// <param name="work">要执行的异步工作内容</param>
	/// <param name="timeout">超时时间,默认为TimeOut常量值</param>
	/// <param name="checkLock">是否检查锁,默认为true</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	/// <returns>返回表示异步操作的Task</returns>
	public Task SendAsync(Func<Task> work, int timeout = int.MaxValue, bool checkLock = true, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WorkerActor.SendAsync(work, timeout, checkLock, cancellationToken);
	}

	/// <summary>
	/// 发送不检查锁的异步工作指令
	/// </summary>
	/// <param name="work">要执行的异步工作内容</param>
	/// <param name="timeout">超时时间,默认为TimeOut常量值</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	/// <returns>返回表示异步操作的Task</returns>
	public Task SendAsyncWithoutCheck(Func<Task> work, int timeout = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WorkerActor.SendAsync(work, timeout, checkLock: false, cancellationToken);
	}

	/// <summary>
	/// 发送带返回值的异步工作指令
	/// </summary>
	/// <typeparam name="T">返回值类型</typeparam>
	/// <param name="work">要执行的异步工作内容</param>
	/// <param name="timeout">超时时间,默认为TimeOut常量值</param>
	/// <param name="cancellationToken">取消操作的令牌</param>
	/// <returns>返回指定类型的异步操作结果</returns>
	public Task<T> SendAsync<T>(Func<Task<T>> work, int timeout = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WorkerActor.SendAsync(work, timeout, cancellationToken);
	}
}
