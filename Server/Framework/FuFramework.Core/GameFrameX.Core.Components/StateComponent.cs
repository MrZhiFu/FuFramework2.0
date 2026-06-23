using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuFramework.Core.Actors;
using FuFramework.Core.Timer;
using FuFramework.Core.Utility;
using FuFramework.DataBase;
using FuFramework.DataBase.Mongo;
using FuFramework.Foundation.Logger;
using FuFramework.Utility.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FuFramework.Core.Components;

/// <summary>
/// 数据状态组件
/// </summary>
public sealed class StateComponent
{
	private static readonly ConcurrentBag<Func<bool, bool, Task>> SaveFuncMap = new ConcurrentBag<Func<bool, bool, Task>>();

	/// <summary>
	/// 统计工具
	/// </summary>
	public static readonly StatisticsTool StatisticsTool = new StatisticsTool();

	/// <summary>
	/// 注册回存
	/// </summary>
	/// <param name="shutdown"></param>
	public static void AddShutdownSaveFunc(Func<bool, bool, Task> shutdown)
	{
		SaveFuncMap.Add(shutdown);
	}

	/// <summary>
	/// 当游戏出现异常，导致无法正常回存，才需要将force=true
	/// 由后台http指令调度
	/// </summary>
	/// <param name="force"></param>
	/// <returns></returns>
	public static async Task SaveAll(bool force = false)
	{
		try
		{
			DateTime begin = DateTime.Now;
			List<Task> list = new List<Task>();
			foreach (Func<bool, bool, Task> item in SaveFuncMap)
			{
				list.Add(item(arg1: true, force));
			}
			await Task.WhenAll(list);
			LogHelper.Info($"save all state, use: {(DateTime.Now - begin).TotalMilliseconds}ms");
		}
		catch (Exception value)
		{
			LogHelper.Error($"save all state error \n{value}");
		}
	}

	/// <summary>
	/// 定时回存所有数据
	/// </summary>
	public static async Task TimerSave()
	{
		try
		{
			foreach (Func<bool, bool, Task> item in SaveFuncMap)
			{
				await item(arg1: false, arg2: false);
				if (!GlobalTimer.IsWorking)
				{
					return;
				}
			}
		}
		catch (Exception ex)
		{
			LogHelper.Info("timer save state error");
			LogHelper.Error(ex.ToString());
		}
	}
}
/// <summary>
/// 数据状态组件
/// </summary>
/// <typeparam name="TState"></typeparam>
public abstract class StateComponent<TState> : BaseComponent where TState : BaseCacheState, new()
{
	private static readonly ConcurrentDictionary<long, TState> StateDic;

	/// <summary>
	/// 单次批量保存的最大数量
	/// </summary>
	private const int ONCE_SAVE_COUNT = 500;

	/// <summary>
	/// 数据对象
	/// </summary>
	public TState State { get; private set; }

	/// <summary>
	/// 判断组件是否准备好进入非激活状态
	/// 当State为空或State未被修改时返回true,表示可以进入非激活状态
	/// </summary>
	internal override bool ReadyToInactive
	{
		get
		{
			if (State != null)
			{
				return !State.IsModify();
			}
			return true;
		}
	}

	/// <summary>
	/// 是否创建默认数据
	/// </summary>
	protected virtual bool IsCreateDefaultState { get; set; } = true;

	static StateComponent()
	{
		StateDic = new ConcurrentDictionary<long, TState>();
		StateComponent.AddShutdownSaveFunc(SaveAll);
	}

	/// <summary>
	/// 激活状态的时候异步读取数据
	/// </summary>
	/// <returns>返回查询的数据结果对象，没有数据返回null</returns>
	protected virtual Task<TState> ActiveReadStateAsync()
	{
		return Task.FromResult<TState>(null);
	}

	/// <summary>
	/// 准备并读取状态数据
	/// 子类不要重写该函数，而是重写ActiveReadStateAsync函数
	/// </summary>
	/// <returns>异步任务</returns>
	public override async Task ReadStateAsync()
	{
		TState value;
		try
		{
			value = (State = await ActiveReadStateAsync());
		}
		catch (Exception exception)
		{
			LogHelper.Error(exception);
		}
		if (State.IsNull())
		{
			value = (State = await GameDb.FindAsync<TState>(base.ActorId, null, IsCreateDefaultState));
		}
		if (State.IsNotNull())
		{
			StateDic.TryRemove(State.Id, out value);
			StateDic.TryAdd(State.Id, State);
		}
	}

	/// <summary>
	/// 激活组件，如果状态为空则读取状态数据
	/// </summary>
	/// <returns>异步任务</returns>
	public override async Task Active()
	{
		await base.Active();
		if (State == null)
		{
			await ReadStateAsync();
		}
	}

	/// <summary>
	/// 反激活组件，从状态字典中移除当前Actor的状态
	/// </summary>
	/// <returns>异步任务</returns>
	public override Task Inactive()
	{
		StateDic.TryRemove(base.ActorId, out var _);
		return base.Inactive();
	}

	/// <summary>
	/// 保存状态到数据库
	/// </summary>
	/// <returns>异步任务</returns>
	protected async Task SaveState()
	{
		try
		{
			if (State.IsNotNull())
			{
				await GameDb.UpdateAsync(State);
			}
		}
		catch (Exception value)
		{
			LogHelper.Fatal($"StateComp.SaveState.Failed.StateId:{State.Id},{value}");
		}
	}

	/// <summary>
	/// 异步写入状态到数据库
	/// </summary>
	/// <returns>异步任务</returns>
	public override async Task WriteStateAsync()
	{
		await SaveState();
	}

	/// <summary>
	/// 保存所有状态数据到数据库
	/// </summary>
	/// <param name="shutdown">是否为关服保存</param>
	/// <param name="force">是否强制保存所有数据</param>
	/// <returns>异步任务</returns>
	public static async Task SaveAll(bool shutdown, bool force = false)
	{
		List<long> idList = new List<long>();
		List<ReplaceOneModel<BsonDocument>> writeList = new List<ReplaceOneModel<BsonDocument>>();
		if (shutdown)
		{
			foreach (TState value3 in StateDic.Values)
			{
				if (value3.IsModify())
				{
					BsonDocument replacement = value3.ToBsonDocument();
					lock (writeList)
					{
						FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", value3.Id);
						writeList.Add(new ReplaceOneModel<BsonDocument>(filter, replacement)
						{
							IsUpsert = true
						});
						idList.Add(value3.Id);
					}
				}
			}
		}
		else
		{
			List<Task> list = new List<Task>();
			foreach (TState state in StateDic.Values)
			{
				Actor actor = ActorManager.GetActor(state.Id);
				if (actor == null)
				{
					continue;
				}
				list.Add(actor.SendAsync(delegate
				{
					if (!force && !state.IsModify())
					{
						return;
					}
					BsonDocument replacement2 = state.ToBsonDocument();
					lock (writeList)
					{
						FilterDefinition<BsonDocument> filter2 = Builders<BsonDocument>.Filter.Eq("_id", state.Id);
						writeList.Add(new ReplaceOneModel<BsonDocument>(filter2, replacement2)
						{
							IsUpsert = true
						});
						idList.Add(state.Id);
					}
				}));
			}
			await Task.WhenAll(list);
		}
		if (writeList.IsNullOrEmpty())
		{
			return;
		}
		string name = typeof(TState).Name;
		StateComponent.StatisticsTool.Count(name, writeList.Count);
		LogHelper.Debug($"[StateComp] 状态回存 {name} count:{writeList.Count}");
		IMongoDatabase currentDatabase = GameDb.As<MongoDbService>().CurrentDatabase;
		IMongoCollection<BsonDocument> collection = currentDatabase.GetCollection<BsonDocument>(name);
		for (int idx = 0; idx < writeList.Count; idx += 500)
		{
			List<ReplaceOneModel<BsonDocument>> range = writeList.GetRange(idx, Math.Min(500, writeList.Count - idx));
			List<long> ids = idList.GetRange(idx, range.Count);
			bool save = false;
			try
			{
				if ((await collection.BulkWriteAsync(range, MongoDbService.BulkWriteOptions)).IsAcknowledged)
				{
					foreach (long item in ids)
					{
						StateDic.TryGetValue(item, out var value);
						value?.SaveToDbPostHandler();
					}
					save = true;
				}
				else
				{
					LogHelper.Error("保存数据失败，类型:" + typeof(TState).FullName);
				}
			}
			catch (Exception value2)
			{
				LogHelper.Error($"保存数据异常，类型:{typeof(TState).FullName}，{value2}");
			}
			if (!save && shutdown)
			{
				LogHelper.Error("保存数据失败，类型:" + typeof(TState).FullName);
			}
		}
	}
}
