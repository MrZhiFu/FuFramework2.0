using MongoDB.Entities;

namespace FuFramework.DataBase.Mongo;

internal sealed class MongoDbContext : DBContext
{
	public MongoDbContext()
	{
		SetGlobalFilterForBaseClass((BaseCacheState m) => m.IsDeleted == false);
	}
}
