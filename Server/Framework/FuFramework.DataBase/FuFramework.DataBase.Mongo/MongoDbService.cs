using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.DataBase.Abstractions;
using FuFramework.Foundation.Logger;
using FuFramework.Utility;
using FuFramework.Utility.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

namespace FuFramework.DataBase.Mongo;

/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
/// <summary>
/// MongoDB服务连接类，实现了
/// <see>
///     <cref>IDatabaseService</cref>
/// </see>
/// 接口。
/// </summary>
public sealed class MongoDbService : IDatabaseService
{
	private sealed class MongoIndexModel
	{
		public string Name { get; set; }

		public bool Unique { get; set; }

		internal MongoIndexModel(bool unique, string name)
		{
			Unique = unique;
			Name = name;
		}
	}

	/// <summary>
	/// 批量写入选项，用于批量写入文档。设置
	/// <see>
	///     <cref>IsOrdered</cref>
	/// </see>
	/// 属性为 false 可以并行执行写入操作。
	/// </summary>
	public static readonly BulkWriteOptions BulkWriteOptions = new BulkWriteOptions
	{
		IsOrdered = false
	};

	private MongoDbContext _mongoDbContext;

	private readonly ConcurrentDictionary<string, List<MongoIndexModel>> _indexCache = new ConcurrentDictionary<string, List<MongoIndexModel>>();

	/// <summary>
	/// 获取或设置当前使用的MongoDB数据库。
	/// </summary>
	public IMongoDatabase CurrentDatabase { get; private set; }

	/// <summary>
	/// 增加一条数据
	/// </summary>
	/// <param name="state"></param>
	/// <typeparam name="TState"></typeparam>
	/// <returns>返回修改的条数</returns>
	public async Task AddAsync<TState>(TState state) where TState : BaseCacheState, new()
	{
		state.CreateTime = TimeHelper.UnixTimeMilliseconds();
		state.UpdateTime = state.CreateTime;
		await _mongoDbContext.SaveAsync(state);
	}

	/// <summary>
	/// 增加一个列表数据
	/// </summary>
	/// <param name="states"></param>
	/// <typeparam name="TState"></typeparam>
	public async Task AddListAsync<TState>(IEnumerable<TState> states) where TState : BaseCacheState, new()
	{
		List<TState> list = states.ToList();
		foreach (TState item in list)
		{
			item.CreateTime = TimeHelper.UnixTimeMilliseconds();
			item.UpdateTime = item.CreateTime;
		}
		await _mongoDbContext.SaveAsync((IEnumerable<TState>)list, default(CancellationToken));
	}

	/// <summary>
	/// 链接数据库
	/// </summary>
	/// <param name="dbOptions">数据库配置选项</param>
	/// <returns>返回数据库是否初始化成功</returns>
	public async Task<bool> Open(DbOptions dbOptions)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(dbOptions.ConnectionString, "ConnectionString");
			ArgumentNullException.ThrowIfNull(dbOptions.Name, "Name");
			MongoClientSettings settings = MongoClientSettings.FromConnectionString(dbOptions.ConnectionString);
			await DB.InitAsync(dbOptions.Name, settings);
			_mongoDbContext = new MongoDbContext();
			CurrentDatabase = DB.Database(dbOptions.Name);
			LogHelper.Info("初始化MongoDB服务完成 Url:" + dbOptions.ConnectionString + " DbName:" + dbOptions.Name);
			return true;
		}
		catch (Exception exception)
		{
			LogHelper.Fatal(exception);
			LogHelper.Error("初始化MongoDB服务失败 Url:" + dbOptions.ConnectionString + " DbName:" + dbOptions.Name);
			return false;
		}
	}

	/// <summary>
	/// 关闭MongoDB连接。
	/// </summary>
	public void Close()
	{
		_mongoDbContext?.Session?.Dispose();
	}

	/// <summary>
	/// 获取指定类型的MongoDB集合。
	/// </summary>
	/// <typeparam name="TState">文档的类型。</typeparam>
	/// <param name="settings">集合的设置。</param>
	/// <returns>指定类型的MongoDB集合。</returns>
	private IMongoCollection<TState> GetCollection<TState>(MongoCollectionSettings settings = null) where TState : class, ICacheState, new()
	{
		string name = typeof(TState).Name;
		IMongoCollection<TState> collection = CurrentDatabase.GetCollection<TState>(name, settings);
		CreateIndexes(collection);
		return collection;
	}

	/// <summary>
	/// 获取指定类型的MongoDB集合。
	/// </summary>
	/// <param name="collectionName">集合名称。</param>
	/// <param name="settings">集合的设置。</param>
	/// <returns>指定类型的MongoDB集合。</returns>
	private IMongoCollection<BsonDocument> GetCollection(string collectionName, MongoCollectionSettings settings = null)
	{
		return CurrentDatabase.GetCollection<BsonDocument>(collectionName, settings);
	}

	/// <summary>
	/// 根据条件删除单条数据(软删除)
	/// </summary>
	/// <param name="filter">查询条件表达式</param>
	/// <typeparam name="TState">数据类型,必须继承自BaseCacheState</typeparam>
	/// <returns>返回修改的记录数</returns>
	public async Task<long> DeleteAsync<TState>(Expression<Func<TState, bool>> filter) where TState : BaseCacheState, new()
	{
		TState state = await FindAsync(filter);
		state.DeleteTime = TimeHelper.UnixTimeMilliseconds();
		state.IsDeleted = true;
		return (await _mongoDbContext.Update<TState>().Match((TState m) => m.Id == state.Id).Modify((TState x) => x.IsDeleted, state.IsDeleted)
			.Modify((TState x) => x.DeleteTime, state.DeleteTime)
			.ExecuteAsync()).ModifiedCount;
	}

	/// <summary>
	/// 根据条件批量删除数据(软删除)
	/// </summary>
	/// <param name="filter">查询条件表达式</param>
	/// <typeparam name="TState">数据类型,必须继承自BaseCacheState</typeparam>
	/// <returns>返回修改的记录数</returns>
	public async Task<long> DeleteListAsync<TState>(Expression<Func<TState, bool>> filter) where TState : BaseCacheState, new()
	{
		Update<TState> bulkUpdate = _mongoDbContext.Update<TState>();
		List<TState> obj = await FindListAsync(filter);
		long deleteTime = TimeHelper.UnixTimeMilliseconds();
		foreach (TState item in obj)
		{
			item.DeleteTime = deleteTime;
			item.IsDeleted = true;
			bulkUpdate.MatchID(item.Id).Modify((TState x) => x.IsDeleted, item.IsDeleted).Modify((TState x) => x.DeleteTime, item.DeleteTime)
				.AddToQueue();
		}
		return (await bulkUpdate.ExecuteAsync()).ModifiedCount;
	}

	/// <summary>
	/// 根据ID列表批量删除数据(软删除)
	/// </summary>
	/// <param name="ids">要删除的ID列表</param>
	/// <typeparam name="TState">数据类型,必须继承自BaseCacheState</typeparam>
	/// <returns>返回修改的记录数</returns>
	public async Task<long> DeleteListIdAsync<TState>(IEnumerable<long> ids) where TState : BaseCacheState, new()
	{
		Update<TState> update = _mongoDbContext.Update<TState>();
		long value = TimeHelper.UnixTimeMilliseconds();
		foreach (long id in ids)
		{
			update.MatchID(id).Modify((TState x) => x.IsDeleted, value: true).Modify((TState x) => x.DeleteTime, value)
				.AddToQueue();
		}
		return (await update.ExecuteAsync()).ModifiedCount;
	}

	/// <summary>
	/// 删除指定对象(软删除)
	/// </summary>
	/// <param name="state">要删除的对象</param>
	/// <typeparam name="TState">数据类型,必须继承自BaseCacheState</typeparam>
	/// <returns>返回修改的记录数</returns>
	public async Task<long> DeleteAsync<TState>(TState state) where TState : BaseCacheState, new()
	{
		state.DeleteTime = TimeHelper.UnixTimeMilliseconds();
		state.IsDeleted = true;
		return (await _mongoDbContext.Update<TState>().Match((TState m) => m.Id == state.Id).Modify((TState x) => x.IsDeleted, state.IsDeleted)
			.Modify((TState x) => x.DeleteTime, state.DeleteTime)
			.ExecuteAsync()).ModifiedCount;
	}

	private static bool AreIndexesConsistent<T>(List<CreateIndexModel<T>> toBeCreatedIndexes, List<BsonDocument> createdIndexes)
	{
		List<string> list = toBeCreatedIndexes.Select((CreateIndexModel<T> i) => i.Options.Name).ToList();
		List<string> list2 = createdIndexes.Select((BsonDocument i) => i["name"].AsString).ToList();
		if (list.Count != list2.Count)
		{
			return false;
		}
		foreach (CreateIndexModel<T> indexInfo in toBeCreatedIndexes)
		{
			BsonDocument bsonDocument = createdIndexes.FirstOrDefault((BsonDocument i) => i["name"].AsString == indexInfo.Options.Name);
			if (bsonDocument == null)
			{
				return false;
			}
			if (indexInfo.Options.Unique != bsonDocument["unique"].AsBoolean)
			{
				return false;
			}
		}
		return true;
	}

	private void CreateIndexes<T>(IMongoCollection<T> collection)
	{
		Type typeFromHandle = typeof(T);
		if (_indexCache.TryGetValue(typeFromHandle.Name, out var value))
		{
			return;
		}
		value = new List<MongoIndexModel>();
		_indexCache.TryAdd(typeFromHandle.Name, value);
		PropertyInfo[] properties = typeFromHandle.GetProperties();
		List<BsonDocument> createdIndexes = collection.Indexes.List().ToList();
		List<CreateIndexModel<T>> list = new List<CreateIndexModel<T>>();
		PropertyInfo[] array = properties;
		foreach (PropertyInfo propertyInfo in array)
		{
			MongoIndexAttribute customAttribute = propertyInfo.GetCustomAttribute<MongoIndexAttribute>();
			if (customAttribute != null)
			{
				CreateIndexModel<T> item = new CreateIndexModel<T>(customAttribute.IsAscending ? Builders<T>.IndexKeys.Ascending(propertyInfo.Name) : Builders<T>.IndexKeys.Descending(propertyInfo.Name), new CreateIndexOptions
				{
					Unique = customAttribute.Unique,
					Name = customAttribute.Name
				});
				list.Add(item);
				MongoIndexModel item2 = new MongoIndexModel(customAttribute.Unique, customAttribute.Name);
				value.Add(item2);
			}
		}
		if (list.Count > 0 && !AreIndexesConsistent(list, createdIndexes))
		{
			collection.Indexes.CreateMany(list);
		}
	}

	/// <summary>
	/// 创建索引
	/// </summary>
	/// <param name="collectionName">集合名</param>
	/// <param name="index">索引键</param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public string CreateIndex(string collectionName, string index, bool asc = true)
	{
		IMongoIndexManager<BsonDocument> indexes = GetCollection(collectionName).Indexes;
		IAsyncCursor<BsonDocument> asyncCursor = indexes.List();
		while (asyncCursor.MoveNext())
		{
			if (!asyncCursor.Current.Any((BsonDocument doc) => doc["name"].AsString.StartsWith(index)))
			{
				return indexes.CreateOne(new CreateIndexModel<BsonDocument>(asc ? Builders<BsonDocument>.IndexKeys.Ascending((BsonDocument doc) => doc[index]) : Builders<BsonDocument>.IndexKeys.Descending((BsonDocument doc) => doc[index])));
			}
		}
		return string.Empty;
	}

	/// <summary>
	/// 创建索引
	/// </summary>
	/// <param name="collectionName">集合名</param>
	/// <param name="index">索引键</param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public async Task<string> CreateIndexAsync(string collectionName, string index, bool asc = true)
	{
		IMongoIndexManager<BsonDocument> mgr = GetCollection(collectionName).Indexes;
		IAsyncCursor<BsonDocument> list = await mgr.ListAsync();
		while (await list.MoveNextAsync())
		{
			if (!list.Current.Any((BsonDocument doc) => doc["name"].AsString.StartsWith(index)))
			{
				return await mgr.CreateOneAsync(new CreateIndexModel<BsonDocument>(asc ? Builders<BsonDocument>.IndexKeys.Ascending((BsonDocument doc) => doc[index]) : Builders<BsonDocument>.IndexKeys.Descending((BsonDocument doc) => doc[index])));
			}
		}
		return string.Empty;
	}

	/// <summary>
	/// 更新索引
	/// </summary>
	/// <param name="collectionName">集合名</param>
	/// <param name="index">索引键</param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public string UpdateIndex(string collectionName, string index, bool asc = true)
	{
		return GetCollection(collectionName).Indexes.CreateOne(new CreateIndexModel<BsonDocument>(asc ? Builders<BsonDocument>.IndexKeys.Ascending((BsonDocument doc) => doc[index]) : Builders<BsonDocument>.IndexKeys.Descending((BsonDocument doc) => doc[index])));
	}

	/// <summary>
	/// 更新索引
	/// </summary>
	/// <param name="collectionName">集合名</param>
	/// <param name="index">索引键</param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public async Task<string> UpdateIndexAsync(string collectionName, string index, bool asc = true)
	{
		return await GetCollection(collectionName).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(asc ? Builders<BsonDocument>.IndexKeys.Ascending((BsonDocument doc) => doc[index]) : Builders<BsonDocument>.IndexKeys.Descending((BsonDocument doc) => doc[index])));
	}

	/// <summary>
	/// 删除索引
	/// </summary>
	/// <param name="collectionName">集合名</param>
	/// <param name="index">索引键</param>
	/// <returns></returns>
	public void DropIndex(string collectionName, string index)
	{
		GetCollection(collectionName).Indexes.DropOne(index);
	}

	/// <summary>
	/// 删除索引
	/// </summary>
	/// <param name="collectionName">集合名</param>
	/// <param name="index">索引键</param>
	/// <returns></returns>
	public Task DropIndexAsync(string collectionName, string index)
	{
		return GetCollection(collectionName).Indexes.DropOneAsync(index);
	}

	/// <summary>
	/// 创建索引
	/// </summary>
	/// <param name="index">索引键</param>
	/// <param name="key"></param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public string CreateIndex<TState>(string index, Expression<Func<TState, object>> key, bool asc = true) where TState : class, ICacheState, new()
	{
		IMongoIndexManager<TState> indexes = GetCollection<TState>().Indexes;
		IAsyncCursor<BsonDocument> asyncCursor = indexes.List();
		while (asyncCursor.MoveNext())
		{
			if (!asyncCursor.Current.Any((BsonDocument doc) => doc["name"].AsString.StartsWith(index)))
			{
				return indexes.CreateOne(new CreateIndexModel<TState>(asc ? Builders<TState>.IndexKeys.Ascending(key) : Builders<TState>.IndexKeys.Descending(key)));
			}
		}
		return string.Empty;
	}

	/// <summary>
	/// 创建索引
	/// </summary>
	/// <param name="index">索引键</param>
	/// <param name="key"></param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public async Task<string> CreateIndexAsync<TState>(string index, Expression<Func<TState, object>> key, bool asc = true) where TState : class, ICacheState, new()
	{
		IMongoIndexManager<TState> mgr = GetCollection<TState>().Indexes;
		IAsyncCursor<BsonDocument> list = await mgr.ListAsync();
		while (await list.MoveNextAsync())
		{
			if (!list.Current.Any((BsonDocument doc) => doc["name"].AsString.StartsWith(index)))
			{
				return await mgr.CreateOneAsync(new CreateIndexModel<TState>(asc ? Builders<TState>.IndexKeys.Ascending(key) : Builders<TState>.IndexKeys.Descending(key)));
			}
		}
		return string.Empty;
	}

	/// <summary>
	/// 更新索引
	/// </summary>
	/// <param name="key"></param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public string UpdateIndex<TState>(Expression<Func<TState, object>> key, bool asc = true) where TState : class, ICacheState, new()
	{
		return GetCollection<TState>().Indexes.CreateOne(new CreateIndexModel<TState>(asc ? Builders<TState>.IndexKeys.Ascending(key) : Builders<TState>.IndexKeys.Descending(key)));
	}

	/// <summary>
	/// 更新索引
	/// </summary>
	/// <param name="key"></param>
	/// <param name="asc"></param>
	/// <returns></returns>
	public async Task<string> UpdateIndexAsync<TState>(Expression<Func<TState, object>> key, bool asc = true) where TState : class, ICacheState, new()
	{
		return await GetCollection<TState>().Indexes.CreateOneAsync(new CreateIndexModel<TState>(asc ? Builders<TState>.IndexKeys.Ascending(key) : Builders<TState>.IndexKeys.Descending(key)));
	}

	/// <summary>
	/// 增加或更新数据
	/// </summary>
	/// <param name="state">数据对象</param>
	/// <typeparam name="TState">数据类型</typeparam>
	/// <returns>返回增加或更新的条数</returns>
	public async Task<TState> AddOrUpdateAsync<TState>(TState state) where TState : BaseCacheState, new()
	{
		if (await InnerFindAsync<TState>(state.Id) == null)
		{
			await AddAsync(state);
			return state;
		}
		return await UpdateAsync(state);
	}

	/// <summary>
	/// 异步加载指定ID的缓存状态。
	/// 此方法尝试从MongoDB中查找与给定ID匹配的缓存状态。
	/// 如果找到状态，则返回该状态；如果未找到，则创建一个新的状态实例。
	/// </summary>
	/// <typeparam name="TState">缓存状态的类型，必须是BaseCacheState的子类，并具有无参数构造函数。</typeparam>
	/// <param name="id">要加载的缓存状态的ID。</param>
	/// <param name="filter">可选的过滤器，用于进一步限制查询结果的条件。</param>
	/// <param name="isCreateIfNotExists">是否创建不存在的文档</param>
	/// <returns>加载的缓存状态，如果未找到则返回新创建的状态。</returns>
	public async Task<TState> FindAsync<TState>(long id, Expression<Func<TState, bool>> filter = null, bool isCreateIfNotExists = true) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		TState val = await _mongoDbContext.Find<TState>().Match(defaultFindExpression).OneAsync(id);
		if (!isCreateIfNotExists)
		{
			return val;
		}
		bool isNew = val == null;
		if (val == null)
		{
			val = new TState
			{
				Id = id,
				CreateTime = TimeHelper.TimeMilliseconds()
			};
		}
		val.LoadFromDbPostHandler(isNew);
		return val;
	}

	/// <summary>
	/// 异步查找满足指定条件的缓存状态。
	/// 如果没有找到满足条件的状态，则会创建一个新的状态实例。
	/// </summary>
	/// <typeparam name="TState">缓存状态的类型，必须是BaseCacheState的子类，并具有无参数构造函数。</typeparam>
	/// <param name="filter">查询条件，用于限制查找的结果。</param>
	/// <param name="isCreateIfNotExists">是否创建不存在的文档</param>
	/// <returns>满足条件的缓存状态，如果未找到则返回新创建的状态。</returns>
	public async Task<TState> FindAsync<TState>(Expression<Func<TState, bool>> filter, bool isCreateIfNotExists = true) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		TState val = await _mongoDbContext.Queryable<TState>().Where(defaultFindExpression).SingleOrDefaultAsync();
		if (!isCreateIfNotExists)
		{
			return val;
		}
		bool isNew = val == null;
		if (val == null)
		{
			val = new TState
			{
				Id = IdGenerator.GetNextUniqueId(),
				CreateTime = TimeHelper.TimeMilliseconds()
			};
		}
		val.LoadFromDbPostHandler(isNew);
		return val;
	}

	/// <summary>
	/// 异步加载指定ID的缓存状态。
	/// 此方法尝试从MongoDB中查找与给定ID匹配的缓存状态。
	/// 如果未找到状态，将返回null。
	/// </summary>
	/// <typeparam name="TState">缓存状态的类型，必须是BaseCacheState的子类，并具有无参数构造函数。</typeparam>
	/// <param name="id">要加载的缓存状态的唯一标识符。</param>
	/// <param name="filter">可选的过滤器，用于进一步限制查询结果的条件。</param>
	/// <returns>加载的缓存状态，如果未找到则返回null。</returns>
	private async Task<TState> InnerFindAsync<TState>(long id, Expression<Func<TState, bool>> filter = null) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		return await _mongoDbContext.Find<TState>().Match(defaultFindExpression).OneAsync(id);
	}

	/// <summary>
	/// 异步查找满足指定条件的缓存状态列表。
	/// </summary>
	/// <typeparam name="TState">缓存状态的类型。</typeparam>
	/// <param name="filter">查询条件。</param>
	/// <returns>满足条件的缓存状态列表。</returns>
	public async Task<List<TState>> FindListAsync<TState>(Expression<Func<TState, bool>> filter) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		List<TState> list = await _mongoDbContext.Queryable<TState>().Where(defaultFindExpression).ToListAsync();
		foreach (TState item in list)
		{
			item?.LoadFromDbPostHandler();
		}
		return list;
	}

	/// <summary>
	/// 以升序方式查找符合条件的第一个元素。
	/// </summary>
	/// <typeparam name="TState">实现ICacheState接口的类型。</typeparam>
	/// <param name="filter">过滤表达式。</param>
	/// <param name="sortExpression">排序字段表达式。</param>
	/// <returns>符合条件的第一个元素。</returns>
	public async Task<TState> FindSortAscendingFirstOneAsync<TState>(Expression<Func<TState, bool>> filter, Expression<Func<TState, object>> sortExpression) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		TState obj = await _mongoDbContext.Find<TState>().Match(defaultFindExpression).Sort(sortExpression, Order.Ascending)
			.Limit(1)
			.ExecuteSingleAsync();
		obj?.LoadFromDbPostHandler();
		return obj;
	}

	/// <summary>
	/// 以降序方式查找符合条件的第一个元素。
	/// </summary>
	/// <typeparam name="TState">实现ICacheState接口的类型。</typeparam>
	/// <param name="filter">过滤表达式。</param>
	/// <param name="sortExpression">排序字段表达式。</param>
	/// <returns>符合条件的第一个元素。</returns>
	public async Task<TState> FindSortDescendingFirstOneAsync<TState>(Expression<Func<TState, bool>> filter, Expression<Func<TState, object>> sortExpression) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		TState obj = await _mongoDbContext.Find<TState>().Match(defaultFindExpression).Sort(sortExpression, Order.Descending)
			.Limit(1)
			.ExecuteSingleAsync();
		obj?.LoadFromDbPostHandler();
		return obj;
	}

	/// <summary>
	/// 以降序方式查找符合条件的元素并进行分页。
	/// </summary>
	/// <typeparam name="TState">实现ICacheState接口的类型。</typeparam>
	/// <param name="filter">过滤表达式。</param>
	/// <param name="sortExpression">排序字段表达式。</param>
	/// <param name="pageIndex">页码，从0开始。</param>
	/// <param name="pageSize">每页数量，默认为10。</param>
	/// <returns>符合条件的元素列表。</returns>
	public async Task<List<TState>> FindSortDescendingAsync<TState>(Expression<Func<TState, bool>> filter, Expression<Func<TState, object>> sortExpression, int pageIndex = 0, int pageSize = 10) where TState : BaseCacheState, new()
	{
		if (pageIndex < 0)
		{
			pageIndex = 0;
		}
		if (pageSize <= 0)
		{
			pageSize = 10;
		}
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		List<TState> list = await _mongoDbContext.Find<TState>().Match(defaultFindExpression).Sort(sortExpression, Order.Descending)
			.Skip(pageIndex * pageSize)
			.Limit(pageSize)
			.ExecuteAsync();
		foreach (TState item in list)
		{
			item?.LoadFromDbPostHandler();
		}
		return list;
	}

	/// <summary>
	/// 以升序方式查找符合条件的元素并进行分页。
	/// </summary>
	/// <typeparam name="TState">实现ICacheState接口的类型。</typeparam>
	/// <param name="filter">过滤表达式。</param>
	/// <param name="sortExpression">排序字段表达式。</param>
	/// <param name="pageIndex">页码，从0开始。</param>
	/// <param name="pageSize">每页数量，默认为10。</param>
	/// <returns>符合条件的元素列表。</returns>
	public async Task<List<TState>> FindSortAscendingAsync<TState>(Expression<Func<TState, bool>> filter, Expression<Func<TState, object>> sortExpression, int pageIndex = 0, int pageSize = 10) where TState : BaseCacheState, new()
	{
		if (pageIndex < 0)
		{
			pageIndex = 0;
		}
		if (pageSize <= 0)
		{
			pageSize = 10;
		}
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		List<TState> list = await _mongoDbContext.Find<TState>().Match(defaultFindExpression).Sort(sortExpression, Order.Ascending)
			.Skip(pageIndex * pageSize)
			.Limit(pageSize)
			.ExecuteAsync();
		foreach (TState item in list)
		{
			item?.LoadFromDbPostHandler();
		}
		return list;
	}

	/// <summary>
	/// 查询数据长度
	/// </summary>
	/// <param name="filter">查询条件</param>
	/// <typeparam name="TState"></typeparam>
	/// <returns></returns>
	public async Task<long> CountAsync<TState>(Expression<Func<TState, bool>> filter) where TState : BaseCacheState, new()
	{
		Expression<Func<TState, bool>> defaultFindExpression = GetDefaultFindExpression(filter);
		return await _mongoDbContext.CountAsync(defaultFindExpression);
	}

	/// <summary>
	/// 获取默认的查询表达式。
	/// </summary>
	/// <typeparam name="TState">缓存状态的类型。</typeparam>
	/// <param name="filter">自定义查询表达式。</param>
	/// <returns>默认的查询表达式。</returns>
	private static Expression<Func<TState, bool>> GetDefaultFindExpression<TState>(Expression<Func<TState, bool>> filter) where TState : BaseCacheState
	{
		Expression<Func<TState, bool>> expression = (TState m) => m.IsDeleted == false;
		if (filter != null)
		{
			expression = expression.And(filter);
		}
		return expression;
	}

	/// <summary>
	/// 判断是否存在符合条件的数据
	/// </summary>
	/// <param name="filter">条件</param>
	/// <returns></returns>
	public async Task<bool> AnyAsync<TState>(Expression<Func<TState, bool>> filter) where TState : BaseCacheState, new()
	{
		filter = GetDefaultFindExpression(filter);
		return await _mongoDbContext.Queryable<TState>().AnyAsync(filter);
	}

	/// <summary>
	/// 保存数据
	/// </summary>
	/// <param name="state"></param>
	/// <typeparam name="TState"></typeparam>
	/// <returns></returns>
	public async Task<TState> UpdateAsync<TState>(TState state) where TState : BaseCacheState, new()
	{
		if (state.IsModify())
		{
			state.UpdateTime = TimeHelper.UnixTimeMilliseconds();
			state.UpdateCount++;
			if ((await _mongoDbContext.Update<TState>().MatchID(state.Id).ModifyExcept((TState m) => new { m.CreateId, m.CreateTime, m.Id, m.IsDeleted, m.DeleteTime }, state)
				.ExecuteAsync()).IsAcknowledged)
			{
				state.SaveToDbPostHandler();
			}
		}
		return state;
	}

	/// <summary>
	/// 保存多条数据
	/// </summary>
	/// <param name="stateList">数据列表对象</param>
	/// <returns>返回更新成功的数量</returns>
	public async Task<long> UpdateAsync<TState>(IEnumerable<TState> stateList) where TState : BaseCacheState, new()
	{
		long resultCount = 0L;
		Update<TState> update = _mongoDbContext.Update<TState>();
		TState[] cacheStates = (stateList as TState[]) ?? stateList.ToArray();
		TState[] array = cacheStates;
		for (int i = 0; i < array.Length; i++)
		{
			TState val = array[i];
			if (val.IsModify())
			{
				val.UpdateTime = TimeHelper.UnixTimeMilliseconds();
				val.UpdateCount++;
				update.MatchID(val.Id).ModifyExcept((TState m) => new { m.CreateId, m.CreateTime, m.Id, m.IsDeleted, m.DeleteTime }, val).AddToQueue();
				resultCount++;
			}
		}
		if ((await update.ExecuteAsync()).IsAcknowledged)
		{
			array = cacheStates;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SaveToDbPostHandler();
			}
		}
		return resultCount;
	}
}
