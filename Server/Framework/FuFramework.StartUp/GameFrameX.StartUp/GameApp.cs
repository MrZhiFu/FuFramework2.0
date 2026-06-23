using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using FuFramework.Foundation.Logger;
using FuFramework.StartUp.Abstractions;
using FuFramework.StartUp.Options;
using FuFramework.Utility;
using FuFramework.Utility.Extensions;
using FuFramework.Utility.Setting;
using Mapster;

namespace FuFramework.StartUp;

/// <summary>
/// 程序入口类
/// </summary>
public static class GameApp
{
	private static readonly Dictionary<Type, StartUpTagAttribute> StartUpTypes = new Dictionary<Type, StartUpTagAttribute>();

	private static readonly List<Task> AppStartUpTasks = new List<Task>();

	/// <summary>
	/// 启动入口函数
	/// </summary>
	/// <param name="args">启动参数</param>
	/// <param name="initAction">在启动服务器之前执行,需要外部初始化协议注册</param>
	/// <param name="logConfiguration">初始化日志系统之前回调,可以重写参数</param>
	public static async Task Entry(string[] args, Action initAction, Action<LogOptions> logConfiguration = null)
	{
		List<string> list = new List<string>(args);
		LogHelper.Console("启动参数：" + string.Join(" ", args));
		LogHelper.Console("当前环境变量START---------------------");
		foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
		{
			if (environmentVariable.Value != null && !environmentVariable.Key.ToString().IsNullOrWhiteSpace())
			{
				string item = (environmentVariable.Key.ToString().StartsWith("--") ? environmentVariable.Key.ToString() : ("--" + environmentVariable.Key));
				if (!list.Contains(item))
				{
					list.Add(item);
					list.Add(environmentVariable.Value.ToString());
				}
			}
		}
		LogHelper.Console("当前环境变量END---------------------");
		LogHelper.Console(string.Empty);
		LogHelper.Console(string.Empty);
		LauncherOptions launcherOptions = new Parser(delegate(ParserSettings configuration)
		{
			configuration.IgnoreUnknownArguments = true;
		}).ParseArguments<LauncherOptions>(list).WithParsed(LauncherOptionsValidate)?.Value;
		string text = launcherOptions?.ServerType;
		if (!text.IsNullOrEmpty())
		{
			LogHelper.Console("启动的服务器类型 ServerType: " + text);
		}
		LogOptions.Default.LogType = text;
		logConfiguration?.Invoke(LogOptions.Default);
		LogHandler.Create(LogOptions.Default);
		GlobalSettings.Load("Configs/app_config.json");
		initAction?.Invoke();
		Type[] types = AssemblyHelper.GetTypes();
		if (types != null)
		{
			Type[] array = types;
			foreach (Type type in array)
			{
				if (type.IsClass && type.IsImplWithInterface(typeof(IAppStartUp)) && type.GetCustomAttribute<StartUpTagAttribute>() != null)
				{
					StartUpTagAttribute customAttribute = type.GetCustomAttribute<StartUpTagAttribute>();
					StartUpTypes.Add(type, customAttribute);
				}
			}
		}
		IOrderedEnumerable<KeyValuePair<Type, StartUpTagAttribute>> orderedEnumerable = StartUpTypes.OrderBy((KeyValuePair<Type, StartUpTagAttribute> m) => m.Value.Priority);
		LogHelper.InfoConsole("----------------------------开始启动服务器啦------------------------------");
		List<AppSetting> settings = GlobalSettings.GetSettings();
		if (text != null && Enum.TryParse<ServerType>(text, out var serverTypeValue))
		{
			KeyValuePair<Type, StartUpTagAttribute> keyValuePair = orderedEnumerable.FirstOrDefault((KeyValuePair<Type, StartUpTagAttribute> m) => m.Value.ServerType == serverTypeValue);
			if (keyValuePair.Value != null)
			{
				AppSetting appSetting = settings.FirstOrDefault((AppSetting m) => m.ServerType == serverTypeValue);
				if (appSetting != null)
				{
					LogHelper.InfoConsole($"从配置文件中找到对应的服务器类型的启动配置,将以配置启动=>{keyValuePair.Value.ServerType}");
				}
				else
				{
					LogHelper.WarnConsole($"没有找到对应的服务器类型的启动配置,将以默认配置启动=>{keyValuePair.Value.ServerType}");
					appSetting = launcherOptions.Adapt<AppSetting>();
				}
				Launcher(args, keyValuePair, appSetting);
			}
		}
		else
		{
			foreach (KeyValuePair<Type, StartUpTagAttribute> item2 in orderedEnumerable)
			{
				bool flag = false;
				foreach (AppSetting item3 in settings)
				{
					if (item2.Value.ServerType == item3.ServerType)
					{
						Launcher(args, item2, item3);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					LogHelper.WarnConsole($"没有找到对应的服务器类型的启动配置,将以默认配置启动=>{item2.Value.ServerType}");
					Launcher(args, item2);
					break;
				}
			}
		}
		LogHelper.InfoConsole("----------------------------启动服务器结束啦------------------------------");
		ConsoleHelper.ConsoleLogo();
		await Task.WhenAll(AppStartUpTasks);
	}

	private static void LauncherOptionsValidate(LauncherOptions options)
	{
		if (!options.ServerType.IsNullOrEmpty() && Enum.TryParse<ServerType>(options.ServerType, out var result))
		{
			options.CheckServerId();
			switch (result)
			{
			case ServerType.DataBase:
				options.CheckDataBaseUrl();
				options.CheckDataBaseName();
				options.CheckOuterIp();
				options.CheckOuterPort();
				break;
			case ServerType.Gateway:
				options.CheckOuterIp();
				options.CheckOuterPort();
				break;
			case ServerType.Router:
				options.CheckOuterIp();
				options.CheckOuterPort();
				options.CheckWsPort();
				options.CheckDiscoveryCenterIp();
				options.CheckDiscoveryCenterPort();
				break;
			case ServerType.DiscoveryCenter:
				options.CheckOuterIp();
				options.CheckOuterPort();
				break;
			case ServerType.Game:
				options.CheckDataBaseUrl();
				options.CheckDataBaseName();
				break;
			}
		}
	}

	private static void Launcher(string[] args, KeyValuePair<Type, StartUpTagAttribute> keyValuePair, AppSetting appSetting = null)
	{
		Task item = Start(args, keyValuePair.Key, keyValuePair.Value.ServerType, appSetting);
		AppStartUpTasks.Add(item);
	}

	private static Task Start(string[] args, Type appStartUpType, ServerType serverType, AppSetting setting)
	{
		IAppStartUp appStartUp = (IAppStartUp)Activator.CreateInstance(appStartUpType);
		if (appStartUp == null)
		{
			return Task.CompletedTask;
		}
		if (appStartUp.Init(serverType, setting, args))
		{
			LogHelper.InfoConsole($"----------------------------START-----{serverType}------------------------------");
			LogHelper.InfoConsole("----------------------------配置信息----------------------------------------------");
			LogHelper.InfoConsole(appStartUp.Setting.ToFormatString() ?? "");
			LogHelper.InfoConsole("--------------------------------------------------------------------------------");
			Task result = AppEnter.Entry(appStartUp);
			LogHelper.InfoConsole($"-----------------------------END------{serverType}------------------------------");
			return result;
		}
		return Task.CompletedTask;
	}
}
