using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using FuFramework.Foundation.Logger;
using FuFramework.StartUp.Abstractions;
using FuFramework.Utility;
using FuFramework.Utility.Setting;

namespace FuFramework.StartUp;

/// <summary>
/// </summary>
internal static class AppExitHandler
{
	private static Action<string> _existCallBack;

	private static AppSetting _setting;

	private static PosixSignalRegistration _exitSignalRegistration;

	private static bool _isKill;

	private static readonly List<IFetalExceptionExitHandler> FetalExceptionExitHandlers = new List<IFetalExceptionExitHandler>();

	/// <summary>
	/// </summary>
	/// <param name="existCallBack">退出回调</param>
	/// <param name="setting">启动设置</param>
	public static void Init(Action<string> existCallBack, AppSetting setting)
	{
		_isKill = false;
		_setting = setting;
		_existCallBack = existCallBack;
		foreach (Type runtimeImplementTypeName in AssemblyHelper.GetRuntimeImplementTypeNames<IFetalExceptionExitHandler>())
		{
			IFetalExceptionExitHandler item = (IFetalExceptionExitHandler)Activator.CreateInstance(runtimeImplementTypeName);
			FetalExceptionExitHandlers.Add(item);
		}
		_exitSignalRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ExitSignalRegistrationHandler);
		AppDomain.CurrentDomain.ProcessExit += delegate
		{
			_existCallBack?.Invoke("process exit");
		};
		AssemblyLoadContext.Default.Unloading += DefaultOnUnloading;
		AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
		{
			HandleFetalException("AppDomain.CurrentDomain.UnhandledException", e.ExceptionObject);
		};
		TaskScheduler.UnobservedTaskException += delegate(object? s, UnobservedTaskExceptionEventArgs e)
		{
			HandleFetalException("TaskScheduler.UnobservedTaskException", e.Exception);
		};
		Console.CancelKeyPress += delegate
		{
			_existCallBack?.Invoke("ctrl+c exit");
		};
	}

	private static void ExitSignalRegistrationHandler(PosixSignalContext posixSignalContext)
	{
		LogHelper.Info("PosixSignalRegistration SIGTERM....");
		_existCallBack?.Invoke("SIGTERM exit");
	}

	private static void DefaultOnUnloading(AssemblyLoadContext obj)
	{
		HandleFetalException("AssemblyLoadContext.Default.Unloading", obj.ToString());
	}

	/// <summary>
	/// 关闭程序
	/// </summary>
	public static void Kill()
	{
		_isKill = true;
	}

	/// <summary>
	/// 程序发生内部异常导致程序终止
	/// </summary>
	/// <param name="tag"></param>
	/// <param name="e"></param>
	private static void HandleFetalException(string tag, object e)
	{
		if (_isKill)
		{
			return;
		}
		List<IFetalExceptionExitHandler> fetalExceptionExitHandlers = FetalExceptionExitHandlers;
		if (fetalExceptionExitHandlers != null && fetalExceptionExitHandlers.Count > 0)
		{
			foreach (IFetalExceptionExitHandler fetalExceptionExitHandler in FetalExceptionExitHandlers)
			{
				fetalExceptionExitHandler.Run(tag, _setting, e?.ToString());
			}
		}
		LogHelper.Error("get unhandled exception Tag:" + tag);
		if (e is IEnumerable enumerable)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (object item in enumerable)
			{
				stringBuilder.Append(item);
			}
			LogHelper.Error($"Unhandled Exception:{stringBuilder}");
			_existCallBack?.Invoke("all Unhandled Exception:" + stringBuilder);
		}
		else
		{
			LogHelper.Error($"Unhandled Exception:{e}");
			_existCallBack?.Invoke($"Unhandled Exception:{e}");
		}
	}
}
