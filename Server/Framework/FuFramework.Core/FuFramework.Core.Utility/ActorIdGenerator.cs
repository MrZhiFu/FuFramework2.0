using System;
using FuFramework.Utility;
using FuFramework.Utility.Setting;

namespace FuFramework.Core.Utility;

/// <summary>
/// ActorId 生成器
/// 14   +   7  + 30 +  12   = 63
/// 服务器id 类型 时间戳 自增
/// 玩家
/// 公会
/// 服务器id * 100000 + 全局功能id
/// 全局玩法
/// </summary>
public static class ActorIdGenerator
{
	private static long _genSecond;

	private static long _incrNum;

	private static readonly object LockObj = new object();

	/// <summary>
	/// 根据ActorId获取服务器id
	/// </summary>
	/// <param name="actorId">ActorId</param>
	/// <returns>服务器id</returns>
	public static int GetServerId(long actorId)
	{
		if (actorId < 1000)
		{
			throw new ArgumentOutOfRangeException("actorId", "actorId is less than min server id, min server id is " + 1000);
		}
		if (actorId < 9999000)
		{
			return (int)(actorId / 1000);
		}
		return (int)(actorId >> 49);
	}

	/// <summary>
	/// 根据ActorId获取ActorType
	/// </summary>
	/// <param name="actorId"></param>
	/// <returns></returns>
	/// <exception cref="T:System.ArgumentException"></exception>
	public static ushort GetActorType(long actorId)
	{
		if (actorId < 1000)
		{
			throw new ArgumentOutOfRangeException("actorId", "actorId is less than min server id, min server id is " + 1000);
		}
		if (actorId < 9999000)
		{
			return (ushort)(actorId % 1000);
		}
		return (ushort)((actorId >> 42) & 0xF);
	}

	/// <summary>
	/// 根据ActorType获取ActorId
	/// </summary>
	/// <param name="type"></param>
	/// <param name="serverId"></param>
	/// <returns></returns>
	/// <exception cref="T:System.ArgumentException"></exception>
	public static long GetActorId(ushort type, int serverId = 0)
	{
		if (type == 128)
		{
			throw new ArgumentException($"input actor type error: {type}");
		}
		if (serverId < 0)
		{
			throw new ArgumentException($"serverId negtive when generate id {serverId}");
		}
		if (serverId == 0)
		{
			serverId = GlobalSettings.CurrentSetting.ServerId;
		}
		if (type < 128)
		{
			return GetMultiActorId(type, serverId);
		}
		return GetGlobalActorId(type, serverId);
	}

	/// <summary>
	/// 根据ActorType类型和服务器id获取ActorId
	/// </summary>
	/// <param name="actorType"></param>
	/// <param name="serverId">服务器ID</param>
	/// <returns></returns>
	private static long GetGlobalActorId(ushort actorType, int serverId)
	{
		if (serverId <= 0)
		{
			throw new ArgumentOutOfRangeException("serverId", "serverId is less than 0");
		}
		if (actorType >= 999 || actorType == 128 || actorType == 0)
		{
			throw new ArgumentOutOfRangeException("actorType", "type is invalid");
		}
		return serverId * 1000 + actorType;
	}

	private static long GetMultiActorId(ushort type, int serverId)
	{
		long num = (long)(DateTime.UtcNow - IdGenerator.UtcTimeStart).TotalSeconds;
		lock (LockObj)
		{
			if (num > _genSecond)
			{
				_genSecond = num;
				_incrNum = 0L;
			}
			else if (_incrNum >= 4095)
			{
				_genSecond++;
				_incrNum = 0L;
			}
			else
			{
				_incrNum++;
			}
		}
		return (long)((ulong)((long)serverId << 49) | ((ulong)type << 42) | (ulong)(_genSecond << 12)) | _incrNum;
	}

	/// <summary>
	/// 根据模块获取唯一ID
	/// </summary>
	/// <param name="module">默认最大值.</param>
	/// <returns></returns>
	public static long GetUniqueId(IdModule module = IdModule.Max)
	{
		long num = (long)(DateTime.UtcNow - IdGenerator.UtcTimeStart).TotalSeconds;
		lock (LockObj)
		{
			if (num > _genSecond)
			{
				_genSecond = num;
				_incrNum = 0L;
			}
			else if (_incrNum >= 524287)
			{
				_genSecond++;
				_incrNum = 0L;
			}
			else
			{
				_incrNum++;
			}
		}
		long num2 = (long)module << 49;
		lock (LockObj)
		{
			num2 |= _genSecond << 19;
		}
		return num2 | _incrNum;
	}

	/// <summary>
	/// 根据模块获取唯一ID
	/// </summary>
	/// <param name="module">默认最大值. 最大值不能超过999</param>
	/// <returns></returns>
	public static long GetUniqueIdByModule(ushort module = 999)
	{
		if (module > 999)
		{
			throw new ArgumentOutOfRangeException("module", "module is invalid");
		}
		long num = (long)(DateTime.UtcNow - IdGenerator.UtcTimeStart).TotalSeconds;
		lock (LockObj)
		{
			if (num > _genSecond)
			{
				_genSecond = num;
				_incrNum = 0L;
			}
			else if (_incrNum >= 524287)
			{
				_genSecond++;
				_incrNum = 0L;
			}
			else
			{
				_incrNum++;
			}
		}
		long num2 = (long)((ulong)module << 49);
		lock (LockObj)
		{
			num2 |= _genSecond << 19;
		}
		return num2 | _incrNum;
	}
}
