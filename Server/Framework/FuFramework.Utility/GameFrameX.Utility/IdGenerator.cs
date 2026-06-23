using System;
using System.Threading;
using Yitter.IdGenerator;

namespace FuFramework.Utility;

/// <summary>
/// ID生成器，提供多种生成唯一标识符的方法，包括整数ID、长整数ID和字符串ID
/// </summary>
public static class IdGenerator
{
	/// <summary>
	/// 全局UTC起始时间，用作计数器的基准时间点
	/// 设置为2020年1月1日0时0分0秒(UTC)
	/// </summary>
	public static readonly DateTime UtcTimeStart;

	/// <summary>
	/// 共享计数器，初始值为当前UTC时间与起始时间的秒数差值
	/// 用于生成递增的整数ID
	/// </summary>
	private static int _intCounter;

	/// <summary>
	/// 使用Interlocked.Increment生成唯一的整数ID
	/// 通过原子操作确保线程安全
	/// </summary>
	/// <returns>返回下一个唯一的整数ID，保证递增且不重复</returns>
	public static int GetNextUniqueIntId()
	{
		return Interlocked.Increment(ref _intCounter);
	}

	/// <summary>
	/// 使用雪花算法生成唯一的长整数ID
	/// 基于Yitter.IdGenerator实现，提供分布式环境下的唯一ID生成
	/// </summary>
	/// <returns>返回下一个唯一的长整数ID，保证全局唯一性</returns>
	public static long GetNextUniqueId()
	{
		return YitIdHelper.NextId();
	}

	/// <summary>
	/// 生成一个全局唯一的GUID字符串
	/// 移除了GUID中的连字符，返回32位的十六进制字符串
	/// </summary>
	/// <returns>返回一个32位的十六进制字符串格式的GUID，不包含连字符</returns>
	public static string GetUniqueIdString()
	{
		return Guid.NewGuid().ToString("N");
	}

	/// <summary>
	/// 静态构造函数，初始化雪花算法的ID生成器
	/// </summary>
	static IdGenerator()
	{
		UtcTimeStart = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		_intCounter = (int)(DateTime.UtcNow - UtcTimeStart).TotalSeconds;
		YitIdHelper.SetIdGenerator(new IdGeneratorOptions(0));
	}
}
