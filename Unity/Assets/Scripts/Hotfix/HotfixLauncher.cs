using FairyGUI;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Launch;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
using AOT.Framework.Core.Utility;
using AOT.Launch.Localization;
using Hotfix.Framework.Config;
using Hotfix.Framework.Guide;
using Hotfix.Framework.RedDot;
using Hotfix.Framework.Network;
using Hotfix.Framework.Core;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Timer;
using Hotfix.Framework.Mono;
using Hotfix.Framework.Event;
using Hotfix.Framework.FSM;
using Hotfix.Framework.UI;
using Hotfix.Framework.Procedure;
using Hotfix.Game.Network;
using Hotfix.Framework.ObjectPool;
using Hotfix.Framework.Localization;
using Hotfix.Framework.Model;
using Hotfix.Framework.Scene;
using Hotfix.Framework.Storage;
using Hotfix.Framework.Sound;
using Hotfix.Framework.Web;
using Hotfix.Framework.Download;
using Hotfix.Framework.Entity;
using Hotfix.Game.UI;
using Hotfix.Game.Config;

#if ENABLE_BINARY_CONFIG
using Luban;
#else
using SimpleJSON;
#endif

namespace Hotfix
{
	/// <summary>
	/// 热更代码启动入口
	/// </summary>
	public static class HotfixLauncher
	{
		/// <summary>
		/// 表管理器实例（语言切换刷新用，每次启动/热重启由 LoadConfigAsync 覆盖）
		/// </summary>
		private static TableManager s_tableManager;

		/// <summary>
		/// 本地化表名（loader 路由拦截键：该表按当前语言加载拆分文件，其余表走整表原路径）
		/// </summary>
		private const string LOCALIZATION_TABLE_NAME = "tblocalization";

		/// <summary>
		/// 启动入口
		/// </summary>
		/// <param name="launchView">AOT 启动加载界面句柄，登录界面打开后关闭。</param>
		public static async UniTask MainAsync(ILaunchView launchView)
		{
			FuLogger.LogInfo("------热更逻辑完毕，进入热更后的代码逻辑入口------".Color(LogColorEx.Green));

			// 初始化 FairyGUI相关：注册自定义加载器（CustomLoader）与自定义组件绑定（CustomCompBind）。
			InitFGUI();

			// 将 ModuleManager 生命周期方法挂接到 GameDriven 委托
			HookGameDriven();

			// 注册基础模块(不依赖配置数据)
			RegisterBaseModules();

			// 配置表必须在依赖它的模块（Sound、Entity 等）初始化前加载
			await InitDependenciesAsync(launchView);

			// 功能模块：首次启动注册 / 重启重新初始化（配置已加载）
			RegisterFeatureModules();

			// 进入游戏
			EnterGame(launchView);
		}

		/// <summary>
		/// 初始化 FairyGUI相关：注册自定义加载器（CustomLoader）与自定义组件绑定（CustomCompBind）。
		/// </summary>
		private static void InitFGUI()
		{
			UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));
			CustomCompBind.BindAll();
		}

		/// <summary>
		/// 将 ModuleManager 生命周期方法挂接到 GameDriven 委托
		/// </summary>
		private static void HookGameDriven()
		{
			GameDriven.Instance.m_OnUpdate          = ModuleManager.Update;
			GameDriven.Instance.m_OnLateUpdate      = ModuleManager.LateUpdate;
			GameDriven.Instance.m_OnFixedUpdate     = ModuleManager.FixedUpdate;
			GameDriven.Instance.m_OnPerSecondUpdate = ModuleManager.PerSecondUpdate;
			GameDriven.Instance.m_DisposeModules    = ModuleManager.Dispose;
		}

		/// <summary>
		/// 注册基础模块（配置表加载前，不依赖配置数据）。
		/// </summary>
		private static void RegisterBaseModules()
		{
			ModuleManager.RegisterModule<ConfigModule>();
			ModuleManager.RegisterModule<FsmModule>();
			ModuleManager.RegisterModule<ProcedureModule>();
			ModuleManager.RegisterModule<EventModule>();
			ModuleManager.RegisterModule<ObjectPoolModule>();
			ModuleManager.RegisterModule<MonoModule>();
			ModuleManager.RegisterModule<TimerModule>();
			ModuleManager.RegisterModule<AssetModule>();
			ModuleManager.RegisterModule<UIModule>();
			ModuleManager.RegisterModule<StorageModule>();
			ModuleManager.RegisterModule<LocalizationModule>();
		}

		/// <summary>
		/// 注册功能模块（配置表加载后，可能依赖配置数据）。
		/// </summary>
		private static void RegisterFeatureModules()
		{
			ModuleManager.RegisterModule<RedDotModule>();
			ModuleManager.RegisterModule<GuideModule>();
			ModuleManager.RegisterModule<ModelModule>();
			ModuleManager.RegisterModule<SceneModule>();
			ModuleManager.RegisterModule<SoundModule>();
			ModuleManager.RegisterModule<EntityModule>();
			ModuleManager.RegisterModule<DownloadModule>();
			ModuleManager.RegisterModule<WebModule>();
			ModuleManager.RegisterModule<NetworkModule>();
		}

		/// <summary>
		/// 初始化框架依赖：加载配置表与公共 UI 资源包、设置多语言 Provider。
		/// 须在依赖配置数据的功能模块（Sound/Entity 等）注册或重新初始化之前调用。
		/// </summary>
		private static async UniTask InitDependenciesAsync(ILaunchView launchView)
		{
			// 加载配置表
			launchView.SetTip(LaunchLocalization.GetL10nText(LaunchL10nKey.aot_res_loading_config));
			var tableManager = await LoadConfigAsync();

			// 加载初始必要的公共UI资源包
			launchView.SetTip(LaunchLocalization.GetL10nText(LaunchL10nKey.aot_res_loading_init_res));
			await LoadCommonUIAsync();

			// 设置本地化多语言提供者
			LocalizationModule.Instance.LocalizationProvider = new LocalizationProvider();

			// 注入英语兜底表加载器：Provider 目标语种缺失时二段查英语表（懒加载）——
			// 须在 Provider 创建之后注入（LoadConfigAsync 阶段的 Provider 尚未创建，注入会被本处 new 覆盖）
			if (LocalizationModule.Instance.LocalizationProvider is LocalizationProvider provider)
			{
				provider.EnglishTableLoader = LoadEnglishTableAsync;
			}

			// 注入 FGUI 声明式多语言的文本解析委托（覆盖 AOT 阶段的 AOT 表版本）
			GObject.GetL10nText = key => LocalizationModule.Instance.GetL10nText(key);

			// 多语言提供者就绪后翻译配置表
			tableManager.RefreshTranslateText();
		}

		/// <summary>
		/// 进入游戏：打开登录界面，关闭启动界面，启动新手引导
		/// </summary>
		private static void EnterGame(ILaunchView launchView)
		{
			GlobalModule.UIModule.Open<WinLogin>();

			// 网络重连编排：订阅断线事件（仅在进游戏后生效；登录期失联由按钮兜底）
			ReconnectManager.Instance.Initialize();
			launchView.Close();

			// 卸载Launcher界面资源包, 防止Launcher包常驻内存
			UIPackage.RemovePackage("UI/Launcher");

			// 引导
			if (GameSetting.Instance.OpenGuide)
			{
				GuideModule.Instance.GuideAction = new GuideActionImpl();
				GuideModule.Instance.StartFirstGuide();
			}
		}

		/// <summary>
		/// 加载配置表
		/// </summary>
		/// <returns>加载完成的表管理器实例</returns>
		private static async UniTask<TableManager> LoadConfigAsync()
		{
			// 热重启：退订旧实例的语言切换事件，避免重复刷新与委托引用导致旧实例无法回收
			s_tableManager?.UnsubscribeLanguageChange();

			var tableManager = new TableManager();
			tableManager.Init(ConfigModule.Instance);

			// 启动期保证当前语种 bundle 在本地：AOT 首启按 PlayerPrefs 语言下载，存档语言可能漂移（跨设备迁移存档），
			// 漂移时目标 bundle 未下载会导致 loader 加载失败——统一在此幂等补齐（本地已有则零开销直过；EditorSimulate/Offline 由 AssetModule 内部短路）
			var langTag = LocalizationModule.Instance.Language.ToString();
			if (!await GlobalModule.AssetModule.EnsureDownloadByTagAsync(langTag, GlobalModule.AssetModule.Token))
				FuLogger.LogError($"[HotfixLauncher] 启动期下载语言资源失败：{langTag}，配置表加载可能失败");

#if ENABLE_BINARY_CONFIG
			// 使用二进制配置表
			await tableManager.LoadAsync(BinaryJsonConfigLoader);
#else
			// 使用JSON配置表
			await tableManager.LoadAsync(JsonConfigLoader);
#endif

			// 订阅语言切换事件并登记实例（退订/覆盖次序固定，勿在别处重复登记）
			tableManager.SubscribeLanguageChange();

			// 注入语言切换重填回调：SetLanguageAsync 先重填表后广播事件的时序依赖此钩子
			LocalizationModule.Instance.LocalizationReloader = tableManager.ReloadLocalizationAsync;

			s_tableManager = tableManager;
			return tableManager;
		}

		/// <summary>
		/// 英语兜底表显式加载器（注入 LocalizationProvider.EnglishTableLoader）。
		/// 不走 LanguageModule.Language 路由（兜底场景语言≠English），直取英语文件路径。
		/// 返回新构造的 TbLocalization 实例（不注册 ConfigModule，Provider 私有兜底）；
		/// 失败抛异常（调用方捕获清空在途任务允许重试）。
		/// </summary>
		private static async UniTask<TbLocalization> LoadEnglishTableAsync()
		{
			// 英语 bundle 幂等补齐（本地已有零开销直过；EditorSimulate/Offline 由 AssetModule 内部短路）。
			// 兜底语种是业务不变量（英语兜底），tag 名取注册表 FallbackName（与文件后缀/资源标签同一事实源）
			if (!await GlobalModule.AssetModule.EnsureDownloadByTagAsync(LaunchLanguageRegistry.FallbackName, GlobalModule.AssetModule.Token))
			{
				throw new InvalidOperationException("[HotfixLauncher] 英语兜底表 bundle 下载失败");
			}

			var englishPath = UtilityAOT.AssetPath.GetLocalizationPath($"{LOCALIZATION_TABLE_NAME}_{LaunchLanguageRegistry.FallbackName}", ".bytes");
			var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(englishPath, GlobalModule.AssetModule.Token);
			try
			{
				var textAsset = assetHandle.GetAssetObject<TextAsset>();
				if (textAsset == null)
					throw new InvalidOperationException($"[HotfixLauncher] 英语兜底表加载失败：{englishPath}");

				// json 变体（ENABLE_BINARY_CONFIG 未定义）：内容是 json 文本，JSON.Parse；
				// bin 变体：内容是 ByteBuf，ByteBuf.Wrap——产物扩展名统一 .bytes，内容格式随宏
				var loadFunc = CreateEnglishTableLoadFunc(textAsset);
				var table    = new TbLocalization(loadFunc);
				await table.LoadAsync();
				return table;
			}
			finally
			{
				// 一次性加载，解析后释放句柄并卸载，避免 provider 引用残留
				assetHandle.Release();
				GlobalModule.AssetModule.UnloadAsset(englishPath);
			}
		}

#if ENABLE_BINARY_CONFIG
		/// <summary>
		/// 构造英语兜底表取数委托（bin 变体：ByteBuf 包装）
		/// </summary>
		private static Func<UniTask<ByteBuf>> CreateEnglishTableLoadFunc(TextAsset textAsset)
		{
			var bytes = textAsset.bytes;
			return () => UniTask.FromResult(ByteBuf.Wrap(bytes));
		}
#else
		/// <summary>
		/// 构造英语兜底表取数委托（json 变体：SimpleJSON 解析）
		/// </summary>
		private static Func<UniTask<JSONNode>> CreateEnglishTableLoadFunc(TextAsset textAsset)
		{
			var node = JSON.Parse(textAsset.text);
			return () => UniTask.FromResult(node);
		}
#endif

		/// <summary>
		/// 加载初始必要的公共UI资源包
		/// </summary>
		private static UniTask LoadCommonUIAsync()
		{
			return GlobalModule.UIModule.PkgManager.LoadPkgAsync("Common");
		}

#if ENABLE_BINARY_CONFIG
		/// <summary>
		/// 加载二进制配置表
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private static async UniTask<ByteBuf> BinaryJsonConfigLoader(string file)
		{
			// 本地化表路由：tblocalization 按当前语言加载拆分文件（表名拦截点全仓唯一: TableManager.cs:88）
			// 语言来源: LocalizationModule.OnInit 先于本加载执行（RegisterBaseModules 先于 LoadConfigAsync），恒已确定
			if (file == LOCALIZATION_TABLE_NAME)
			{
				var lang = LocalizationModule.Instance.Language.ToString();
				var localPath = UtilityAOT.AssetPath.GetLocalizationPath($"{LOCALIZATION_TABLE_NAME}_{lang}", ".bytes");
				var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(localPath, GlobalModule.AssetModule.Token);
				try
				{
					var textAsset = assetHandle.GetAssetObject<TextAsset>();
					if (textAsset == null)
						throw new InvalidOperationException($"[HotfixLauncher] 本地化表加载失败：{localPath}");
					return ByteBuf.Wrap(textAsset.bytes);
				}
				finally
				{
					assetHandle.Release();
					GlobalModule.AssetModule.UnloadAsset(localPath);
				}
			}

			var configPath = UtilityAOT.AssetPath.GetConfigPath(file);
			var handle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(configPath, GlobalModule.AssetModule.Token);
			try
			{
				var textAsset = handle.GetAssetObject<TextAsset>();
				if (textAsset == null)
					throw new InvalidOperationException($"[HotfixLauncher] 配置文件加载失败：{configPath}");
				return ByteBuf.Wrap(textAsset.bytes);
			}
			finally
			{
				// 启动一次性加载，解析后释放句柄，避免 provider 引用残留
				// AutoUnload=false 下显式卸载，否则配置 bundle 常驻内存
				handle.Release();
				GlobalModule.AssetModule.UnloadAsset(configPath);
			}
		}
#else
		/// <summary>
		/// 加载json配置表
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private static async UniTask<JSONNode> JsonConfigLoader(string file)
		{
			// 本地化表路由：tblocalization 按当前语言加载拆分文件（表名拦截点全仓唯一: TableManager.cs:88）
			// 语言来源: LocalizationModule.OnInit 先于本加载执行（RegisterBaseModules 先于 LoadConfigAsync），恒已确定
			// 扩展名恒为 .bytes（产物统一扩展名裁决：json 变体内容是 json 文本、bin 变体是 ByteBuf，同为 TextAsset）
			if (file == LOCALIZATION_TABLE_NAME)
			{
				var lang        = LocalizationModule.Instance.Language.ToString();
				var localPath   = UtilityAOT.AssetPath.GetLocalizationPath($"{LOCALIZATION_TABLE_NAME}_{lang}", ".bytes");
				var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(localPath, GlobalModule.AssetModule.Token);
				try
				{
					var textAsset = assetHandle.GetAssetObject<TextAsset>();
					if (textAsset == null)
						throw new InvalidOperationException($"[HotfixLauncher] 本地化表加载失败：{localPath}");
					return JSON.Parse(textAsset.text);
				}
				finally
				{
					assetHandle.Release();
					GlobalModule.AssetModule.UnloadAsset(localPath);
				}
			}

			var cfgPath = UtilityAOT.AssetPath.GetConfigPath(file, ".json");
			var handle  = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(cfgPath, GlobalModule.AssetModule.Token);
			try
			{
				var textAsset = handle.GetAssetObject<TextAsset>();
				if (textAsset == null)
					throw new InvalidOperationException($"[HotfixLauncher] 配置文件加载失败：{cfgPath}");
				return JSON.Parse(handle.GetAssetObject<TextAsset>().text);
			}
			finally
			{
				// 解析后释放句柄，避免 provider 引用残留
				// AutoUnload=false 下显式卸载，否则配置 bundle 常驻内存
				handle.Release();
				GlobalModule.AssetModule.UnloadAsset(cfgPath);
			}
		}
#endif
	}
}