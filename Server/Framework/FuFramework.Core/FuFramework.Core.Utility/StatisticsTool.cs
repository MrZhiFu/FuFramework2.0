using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.Core.Actors.Impl;
using FuFramework.Utility.Extensions;

namespace FuFramework.Core.Utility;

/// <summary>
/// 统计工具
/// </summary>
public sealed class StatisticsTool
{
	private const string Format = "yyyy-MM-dd HH:mm";

	private readonly Dictionary<string, Dictionary<string, int>> countDic = new Dictionary<string, Dictionary<string, int>>();

	private readonly WorkerActor workerActor = new WorkerActor(0L);

	/// <summary>
	/// 统计
	/// </summary>
	/// <param name="limit"></param>
	/// <returns></returns>
	public async Task<string> CountRecord(int limit = 10)
	{
		return await workerActor.SendAsync(delegate
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, Dictionary<string, int>> item in countDic)
			{
				string key = item.Key;
				foreach (KeyValuePair<string, int> item2 in item.Value)
				{
					string key2 = item2.Key;
					int value = item2.Value;
					if (value >= limit)
					{
						stringBuilder.Append('\t').Append(key).Append('\t')
							.Append(value)
							.Append('\t')
							.Append(key2)
							.Append('\n');
					}
				}
			}
			return stringBuilder.ToString();
		});
	}

	/// <summary>
	/// 清理统计
	/// </summary>
	public void ClearCount()
	{
		workerActor.Tell((Action)countDic.Clear, int.MaxValue, default(CancellationToken));
	}

	/// <summary>
	/// 清理统计
	/// </summary>
	/// <param name="time"></param>
	public void ClearCount(DateTime time)
	{
		workerActor.Tell(delegate
		{
			string timeStr = time.ToString("yyyy-MM-dd HH:mm");
			countDic.RemoveIf((string k, Dictionary<string, int> v) => k.CompareTo(timeStr) < 0);
		});
	}

	/// <summary>
	/// 记录统计
	/// </summary>
	/// <param name="key"></param>
	/// <param name="num"></param>
	public void Count(string key, int num = 1)
	{
		if (num <= 0)
		{
			return;
		}
		workerActor.Tell(delegate
		{
			string key2 = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
			if (!countDic.TryGetValue(key2, out var value))
			{
				value = new Dictionary<string, int>();
				countDic[key2] = value;
			}
			int valueOrDefault = value.GetValueOrDefault(key, 0);
			value[key] = valueOrDefault + num;
		});
	}
}
