using FuFramework.Foundation.Hash;
using FuFramework.Foundation.Logger;
using Standart.Hash.xxHash;

namespace FuFramework.DataBase.Storage;

/// <summary>
/// 数据状态Hash计算处理器
/// </summary>
internal sealed class StateHash
{
	private BaseCacheState State { get; }

	/// <summary>
	/// 缓存的Hash
	/// </summary>
	private uint128 CacheHash { get; set; }

	/// <summary>
	/// 保存的Hash
	/// </summary>
	private uint128 ToSaveHash { get; set; }

	public StateHash(BaseCacheState state, bool isNew = false)
	{
		State = state;
		if (!isNew)
		{
			CacheHash = GetHashAndData(state).md5;
		}
	}

	/// <summary>
	/// 判断是否需要保存
	/// </summary>
	/// <returns></returns>
	public (bool, byte[]) IsChanged()
	{
		(uint128 md5, byte[] data) hashAndData = GetHashAndData(State);
		uint128 item = hashAndData.md5;
		byte[] item2 = hashAndData.data;
		ToSaveHash = item;
		return (XxHashHelper.IsDefault(CacheHash) || !item.Equals(CacheHash), item2);
	}

	/// <summary>
	/// 保存到数据库之后的操作
	/// </summary>
	public void SaveToDbPostHandler()
	{
		if (CacheHash.Equals(ToSaveHash))
		{
			LogHelper.Warn("调用AfterSaveToDB前CacheHash已经等于ToSaveHash " + State.GetType().FullName);
		}
		CacheHash = ToSaveHash;
	}

	private static (uint128 md5, byte[] data) GetHashAndData(BaseCacheState state)
	{
		byte[] array = state.ToBytes();
		return (md5: XxHashHelper.Hash128(array), data: array);
	}
}
