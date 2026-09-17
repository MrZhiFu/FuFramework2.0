using System.Collections.Concurrent;
using System.Collections.Generic;
using FuFramework.Core.Abstractions;
using FuFramework.Core.Utility;
using FuFramework.Foundation.Logger;

namespace FuFramework.Core.Actors.Impl;

/// <summary>
/// 判断Actor交叉死锁
/// </summary>
public static class ActorLimit
{
	/// <summary>
	/// 可以按需扩展检查规则
	/// </summary>
	public enum RuleType
	{
		/// <summary>
		/// 不检查
		/// </summary>
		None,
		/// <summary>
		/// 禁止双向调用
		/// </summary>
		NoBidirectionCall
	}

	private interface IRule
	{
		bool AllowCall(long target);
	}

	private class ByLevelRule : IRule
	{
		/// <summary>
		/// 判断是否允许调用
		/// </summary>
		/// <param name="target">目标</param>
		/// <returns></returns>
		bool IRule.AllowCall(long target)
		{
			long currentActor = RuntimeContext.CurrentActor;
			if (currentActor == 0L)
			{
				return true;
			}
			ushort actorType = ActorIdGenerator.GetActorType(currentActor);
			ushort actorType2 = ActorIdGenerator.GetActorType(target);
			if (LevelDic.TryGetValue(actorType2, out var value) && LevelDic.TryGetValue(actorType, out var value2) && value2 > value)
			{
				LogHelper.Error($"不合法的调用路径:{actorType}==>{actorType2}");
				return false;
			}
			return true;
		}
	}

	private class NoBidirectionCallRule : IRule
	{
		private readonly ConcurrentDictionary<long, ConcurrentDictionary<long, bool>> _crossDic = new ConcurrentDictionary<long, ConcurrentDictionary<long, bool>>();

		/// <summary>
		/// 是否允许调用
		/// </summary>
		/// <param name="target">目标</param>
		/// <returns>返回是否调用</returns>
		public bool AllowCall(long target)
		{
			long currentActor = RuntimeContext.CurrentActor;
			if (currentActor == 0L)
			{
				return true;
			}
			return AllowCall(currentActor, target);
		}

		private bool AllowCall(long self, long target)
		{
			if (self == target)
			{
				return true;
			}
			if (_crossDic.TryGetValue(target, out var value) && value.ContainsKey(self))
			{
				LogHelper.Error($"发生交叉死锁，ActorId1:{self} ActorType1:{ActorIdGenerator.GetActorType(self)} ActorId2:{target} ActorType2:{ActorIdGenerator.GetActorType(target)}");
				return false;
			}
			_crossDic.GetOrAdd(self, (long k) => new ConcurrentDictionary<long, bool>()).TryAdd(target, value: false);
			return true;
		}
	}

	private static IRule _rule;

	private static readonly Dictionary<ushort, int> LevelDic = new Dictionary<ushort, int>(128);

	/// <summary>
	/// 初始化
	/// </summary>
	/// <param name="type"> 检查规则 </param>
	public static void Init(RuleType type)
	{
		switch (type)
		{
		case RuleType.NoBidirectionCall:
			_rule = new NoBidirectionCallRule();
			return;
		case RuleType.None:
			return;
		}
		LogHelper.Error($"不支持的rule类型:{type}");
	}

	/// <summary>
	/// 是否允许调用
	/// </summary>
	/// <param name="target">目标</param>
	/// <returns>返回是否调用</returns>
	public static bool AllowCall(long target)
	{
		if (_rule != null)
		{
			return _rule.AllowCall(target);
		}
		return true;
	}
}
