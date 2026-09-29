using Hotfix.Framework.UI;
using Hotfix.Framework.FSM;
using Hotfix.Framework.Mono;
using Hotfix.Framework.Event;
using Hotfix.Framework.Timer;
using Hotfix.Framework.Asset;
using Hotfix.Framework.ObjectPool;
using Hotfix.Framework.Procedure;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
	/// <summary>
	/// 全局模块类。
	/// 功能：
	///     1. 提供各个框架模块的访问入口，用于在热更代码中通过此类来访问各个模块的接口。
	/// </summary>
	public static class GlobalModule
	{
		private static ObjectPoolModule s_objectPoolModule; // 对象池模块
		private static EventModule      s_eventModule;      // 事件管理模块
		private static AssetModule      s_assetModule;      // 资源管理模块

		private static TimerModule     s_timerModule;     // 计时器管理模块
		private static FsmModule       s_fsmModule;       // 有限状态机管理模块
		private static ProcedureModule s_procedureModule; // 流程管理模块
		private static UIModule        s_uiModule;        // UI管理模块
		private static MonoModule      s_monoModule;      // Mono管理模块

		// private static AdvertisementModule   m_advertisementModule;   // TODO 广告管理模块
		// private static GameAnalyticsModule   m_gameAnalyticsModule;   // TODO 游戏分析管理模块

		/// <summary>
		/// 获取对象池模块。
		/// </summary>
		public static ObjectPoolModule ObjectPoolModule => s_objectPoolModule ??= ModuleManager.GetModule<ObjectPoolModule>();

		/// <summary>
		/// 获取事件管理模块。
		/// </summary>
		public static EventModule EventModule => s_eventModule ??= ModuleManager.GetModule<EventModule>();

		/// <summary>
		/// 获取资源管理模块。
		/// </summary>
		public static AssetModule AssetModule => s_assetModule ??= ModuleManager.GetModule<AssetModule>();


		/// <summary>
		/// 获取计时器管理模块。
		/// </summary>
		public static TimerModule TimerModule => s_timerModule ??= ModuleManager.GetModule<TimerModule>();

		/// <summary>
		/// 获取有限状态机管理模块。
		/// </summary>
		public static FsmModule FsmModule => s_fsmModule ??= ModuleManager.GetModule<FsmModule>();

		/// <summary>
		/// 获取流程管理模块。
		/// </summary>
		public static ProcedureModule ProcedureModule => s_procedureModule ??= ModuleManager.GetModule<ProcedureModule>();

		/// <summary>
		/// 获取UI管理模块。
		/// </summary>
		public static UIModule UIModule => s_uiModule ??= ModuleManager.GetModule<UIModule>();

		/// <summary>
		/// 获取Mono管理模块。
		/// </summary>
		public static MonoModule MonoModule => s_monoModule ??= ModuleManager.GetModule<MonoModule>();

		///// <summary>
		///// 获取广告管理模块。// TODO
		///// </summary>
		// private static AdvertisementModule AdvertisementModule => m_advertisementModule ??= ModuleManager.GetModule<AdvertisementModule>();

		///// <summary>
		///// 获取游戏分析管理模块。// TODO
		///// </summary>
		// private static GameAnalyticsModule GameAnalyticsModule => m_gameAnalyticsModule ?? ModuleManager.GetModule<GameAnalyticsModule>();
	}
}