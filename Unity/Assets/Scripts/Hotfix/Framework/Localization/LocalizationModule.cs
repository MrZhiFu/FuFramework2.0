using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using AOT.Launch.Localization;
using Hotfix.Framework.Event;
using Hotfix.Framework.Storage;
using Hotfix.Game.Config;
using Hotfix.Framework.Config;
using ELanguage = Hotfix.Game.Config.ELanguage;

namespace Hotfix.Framework.Localization
{
	/// <summary>
	/// 本地化管理模块（生命周期与内部实现分部）。
	///     当前语言的读取/设置/持久化（经 StorageModule）、语言变更事件广播（经 EventModule）、
	///     取词实现（当前语种表 → 英语兜底表二段查询，英语表懒加载）。
	///     公共 API 见 LocalizationModule.API.cs。
	/// </summary>
	public sealed partial class LocalizationModule : ModuleBase
	{
		/// <summary>
		/// 事件管理模块
		/// </summary>
		private EventModule m_eventModule;

		/// <summary>
		/// 数据保存模块
		/// </summary>
		private StorageModule m_storageModule;

		/// <summary>
		/// 当前使用的语言
		/// </summary>
		private ELanguage m_language;

		/// <summary>
		/// 取消栅栏：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
		/// OnDispose 时 Cancel，语言切换等在途异步操作随之取消；经 CancelAsync 等待取消清理完成。
		/// </summary>
		private CancellationBarrier m_barrier = new();

		/// <summary>
		/// 当前在途切换链路的取消源（防抖：新切换先取消旧链路，以最新目标为准；null = 无在途切换）。
		/// 访问须在主线程（切换链路的发起/收尾都在主线程同步段），无并发写风险。
		/// </summary>
		private CancellationTokenSource m_switchCts;

		/// <summary>
		/// 本地化表重填回调（由 HotfixLauncher 在表加载完成后注入，指向 TableManager.ReloadLocalizationAsync）。
		/// 语言切换先重填表、后广播事件的时序由本回调保证；返回 false 表示重填失败（空表/损坏数据），SetLanguageAsync 须回滚。
		/// </summary>
		internal Func<UniTask<bool>> LocalizationReloader { get; set; }

		/// <summary>
		/// 英语兜底表加载器（由 HotfixLauncher 在表加载完成后注入）。
		/// 不走语言路由（兜底场景语言≠English），直取英语文件。
		/// </summary>
		internal Func<UniTask<TbLocalization>> EnglishTableLoader { get; set; }

		/// <summary>
		/// 缓存多语言配置表，避免每次调用都从配置表中获取
		/// </summary>
		private TbLocalization m_tbLocalization;

		/// <summary>
		/// 英语兜底表（懒加载持有）：目标语种查不到时二段查询；null = 尚未加载
		/// </summary>
		private TbLocalization m_tbEnglishFallback;

		/// <summary>
		/// 英语表在途加载的完成源（并发防抖共享等待点）。
		/// 用 UniTaskCompletionSource 而非 UniTask 字段：UniTask 是单次消费的（重复 await 抛
		/// "Already continuation registered"），切语言时全表批量翻译会产生大量并发 miss，
		/// 必须允许多个等待者共享同一次加载。
		/// </summary>
		private UniTaskCompletionSource<TbLocalization> m_englishLoadTcs;

		/// <summary>
		/// 初始化
		/// </summary>
		protected internal override void OnInit()
		{
			Instance = this;

			// 新生命周期 = 新 Token：旧 Token 已被 OnDispose 取消，在途旧任务据此识别中止
			m_barrier = new CancellationBarrier();

			m_eventModule   = ModuleManager.GetModule<EventModule>();
			m_storageModule = StorageModule.Instance;

			// 数据保存模块缺失（模块注册顺序调整/初始化失败）时退化为系统语言，不能直接解引用（否则 NRE 中断本模块初始化）
			if (m_storageModule == null)
			{
				FuLogger.LogError("初始化失败，数据保存模块未找到，语言设置将无法读取与保存!");
				m_language = FromSystemLanguage();
				return;
			}

			var value = m_storageModule.GetString("Language");
			if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out ELanguage result))
				m_language = result;
			else
				m_language = FromSystemLanguage();

			// 启动收敛：把本侧最终语言回写 PlayerPrefs（字符串名），修正 AOT 偏好缺失/漂移（如旧版本仅存档无偏好）的情况
			PlayerPrefs.SetString(LaunchLocalization.LANGUAGE_PREF_KEY, m_language.ToString());
			PlayerPrefs.Save();
		}

		/// <summary>
		/// 释放
		/// </summary>
		protected internal override void OnDispose()
		{
			m_barrier.Cancel(); // 随模块销毁取消语言切换等在途异步操作

			// 在途切换凭据随生命周期终结（barrier.Cancel 已令其链路取消，此处防泄漏）
			m_switchCts?.Dispose();
			m_switchCts = null;

			Instance = null;
		}

		/// <summary>
		/// 按 key 取当前语言译文（取词实现）：当前语种表直读 → 空则二段查询英语兜底。
		/// </summary>
		/// <param name="key">多语言key(使用静态类L10nKey的多语言字段即可)</param>
		/// <param name="args">参数</param>
		/// <returns>译文；当前语种缺失时回退英语兜底表；两者皆缺返回空串</returns>
		private string GetL10n(string key, params object[] args)
		{
			if (string.IsNullOrEmpty(key)) return string.Empty;

			// 获取多语言配置表并缓存
			m_tbLocalization ??= ConfigModule.Instance.GetConfig<TbLocalization>();

			var localization = m_tbLocalization?.Get(key);
			if (localization == null)
			{
				FuLogger.LogError($"多语言key '{key}' 没找到，请检查多语言配置表!");
				return string.Empty;
			}

			// 拆分产物每文件只含当前语种，直接取 Value（与 AOT 侧 LaunchLocalization 同构）
			var text = localization.Value;

			// 二段查询英语兜底：目标语种缺失（拆分产物该语种键缺失→null / 或翻译为空）时查英语表
			if (text.IsNullOrEmpty())
			{
				text = TryGetEnglish(key);
			}

			// 如果有参数，则返回格式化参数后的文本，否则直接返回文本
			return args is { Length: > 0 } ? string.Format(text, args) : text;
		}

		/// <summary>
		/// 查英语兜底表；未加载时同步短路返回空串并触发异步加载（下次查询生效，不阻塞当前查询）
		/// </summary>
		/// <param name="key">多语言key</param>
		/// <returns>英语译文；英语表未就绪或 key 也缺失时返回空串</returns>
		private string TryGetEnglish(string key)
		{
			if (m_tbEnglishFallback == null)
			{
				TriggerEnglishLoadAsync().Forget();
				return string.Empty;
			}

			var englishRow = m_tbEnglishFallback.Get(key);
			// 拆分产物统一 key+value：英语文件的 value 即英语译文
			var english = englishRow?.Value;
			return string.IsNullOrEmpty(english) ? string.Empty : english;
		}

		/// <summary>
		/// 触发英语兜底表异步加载（并发防抖：在途加载共享同一完成源，多个等待者安全复用；
		/// 失败/取消清空完成源允许下次重试）。
		/// 取消令牌挂本模块取消栅栏（随 OnDispose 取消）。
		/// </summary>
		private async UniTaskVoid TriggerEnglishLoadAsync()
		{
			if (EnglishTableLoader == null)
			{
				FuLogger.LogError("英语兜底表加载器未注入，英语兜底不可用（HotfixLauncher 表加载完成后须注入 EnglishTableLoader）");
				return;
			}

			// 已有在途加载：共享等待（UniTaskCompletionSource.Task 支持多消费者）
			if (m_englishLoadTcs != null)
			{
				try
				{
					await m_englishLoadTcs.Task;
				}
				catch (Exception)
				{
					// 共享等待者忽略结果：主加载链负责记录错误与清空完成源
				}

				return;
			}

			var tcs = new UniTaskCompletionSource<TbLocalization>();
			m_englishLoadTcs = tcs;
			try
			{
				var table = await EnglishTableLoader();
				m_tbEnglishFallback = table;
				tcs.TrySetResult(table);
				FuLogger.LogInfo("英语兜底表加载完成（懒加载触发）");
			}
			catch (OperationCanceledException)
			{
				tcs.TrySetCanceled();
				m_englishLoadTcs = null; // 模块生命周期取消：允许新生命周期重试
			}
			catch (Exception e)
			{
				tcs.TrySetException(e);
				m_englishLoadTcs = null; // 失败清空允许重试
				FuLogger.LogError($"英语兜底表加载失败：{e.Message}");
			}
		}

		/// <summary>
		/// 语言切换落地三步：存档 + 同步 PlayerPrefs 偏好 + 广播语言切换事件。
		/// 供 Language setter（同步切换）与 SetLanguageAsync（异步切换就绪后）共用；调用前 m_language 须已更新。
		/// </summary>
		private void ApplyLanguageChange(ELanguage oldLanguage, ELanguage newLanguage)
		{
			// 保存设置（数据保存模块缺失时跳过保存，语言切换本身仍生效）
			if (m_storageModule != null)
			{
				m_storageModule.SetString("Language", newLanguage.ToString());
				m_storageModule.Save();
			}

			// 同步语言偏好到 PlayerPrefs：下次启动 AOT 阶段（LaunchLocalization）从 PlayerPrefs 读取，
			// 两阶段偏好不同源会导致 AOT 界面文本语言与本侧不一致
			PlayerPrefs.SetString(LaunchLocalization.LANGUAGE_PREF_KEY, newLanguage.ToString());
			PlayerPrefs.Save();

			// 发送本地化语言改变事件（订阅者如 TableManager 在事件回调中重译全表）
			var languageChangeEventArgs = LanguageChangeEventArgs.Create(oldLanguage, newLanguage);
			m_eventModule.Broadcast(languageChangeEventArgs);
		}

		/// <summary>
		/// 从系统语言转换出本地化语言类型（映射走配置生成的语言注册表，与 AOT 侧同一事实源；
		/// 未覆盖的系统语言由注册表兜底到 English）。
		/// </summary>
		/// <returns>本地化语言类型</returns>
		private static ELanguage FromSystemLanguage()
		{
			var aotLanguage = LaunchLanguageRegistry.FromSystemLanguage(Application.systemLanguage);
			// AOT 与 Hotfix 各持一份配置生成的 ELanguage（数值一致）；跨程序集传递须显式按名转换
			return (ELanguage)Enum.Parse(typeof(ELanguage), aotLanguage.ToString());
		}
	}
}
