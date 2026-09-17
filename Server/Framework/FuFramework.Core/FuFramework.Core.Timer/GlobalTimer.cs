using System;
using System.Threading.Tasks;
using FuFramework.Core.Actors;
using FuFramework.Core.Components;
using FuFramework.DataBase;
using FuFramework.Foundation.Logger;
using FuFramework.Utility;
using FuFramework.Utility.Setting;

namespace FuFramework.Core.Timer;

/// <summary>
/// 全局定时器
/// </summary>
public static class GlobalTimer
{
	/// <summary>
	/// 循环任务
	/// </summary>
	private static Task _loopTask;

	/// <summary>
	/// 是否正在工作
	/// </summary>
	public static volatile bool IsWorking;

	/// <summary>
	/// 开始全局定时
	/// </summary>
	public static void Start()
	{
		LogHelper.Debug("初始化全局定时开始...");
		IsWorking = true;
		_loopTask = Task.Run((Func<Task?>)Loop);
		LogHelper.Debug("初始化全局定时完成...");
	}

	/// <summary>
	/// 循环执行的方法
	/// </summary>
	private static async Task Loop()
	{
		long nextSaveTime = NextSaveTime();
		TimeSpan onceDelay = TimeSpan.FromSeconds(5.0);
		while (IsWorking)
		{
			LogHelper.Info($"下次定时回存时间 {nextSaveTime}");
			long num = TimeHelper.UnixTimeMilliseconds();
			while (num < nextSaveTime && IsWorking)
			{
				await Task.Delay(onceDelay);
				num = TimeHelper.UnixTimeMilliseconds();
			}
			if (IsWorking)
			{
				long startTime = TimeHelper.UnixTimeMilliseconds();
				LogHelper.Info($"开始定时回存 时间:{startTime}");
				await StateComponent.TimerSave();
				long num2 = TimeHelper.UnixTimeMilliseconds();
				long value = num2 - startTime;
				LogHelper.Info($"结束定时回存 时间:{num2} 耗时: {value}ms");
				LogHelper.Info($"开始回收空闲Actor 时间:{startTime}");
				await ActorManager.CheckIdle();
				num = TimeHelper.UnixTimeMilliseconds();
				LogHelper.Info($"结束回收空闲Actor 时间:{num}");
				do
				{
					nextSaveTime = NextSaveTime();
				}
				while (num > nextSaveTime);
				continue;
			}
			break;
		}
	}

	/// <summary>
	/// 计算下次回存时间
	/// </summary>
	/// <returns>下次回存时间</returns>
	private static long NextSaveTime()
	{
		return TimeHelper.UnixTimeMilliseconds() + GlobalSettings.CurrentSetting.SaveDataInterval;
	}

	/// <summary>
	/// 停止全局定时
	/// </summary>
	public static async Task Stop()
	{
		LogHelper.Info("停止全局定时开始...");
		IsWorking = false;
		await _loopTask;
		await StateComponent.SaveAll(force: true);
		GameDb.Close();
		LogHelper.Info("停止全局定时完成...");
	}
}
