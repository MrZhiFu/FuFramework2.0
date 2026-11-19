using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using AOT.Launch.Localization;
using Hotfix.Framework.Event;
using Hotfix.Framework.Storage;
using ELanguage = Hotfix.Game.Config.ELanguage;

namespace Hotfix.Framework.Localization
{
	/// <summary>
	/// 本地化管理模块。
	/// 功能：
	///     1. 获取/设置当前使用的语言。
	///     2. 配合数据保存模块，保存当前使用的语言设置。
	///     3. 配合事件管理模块，发送本地化语言改变事件。
	///     4. 使用指定的具体本地化多语言提供器获取本地化多语言字符串。
	/// </summary>
	public sealed class LocalizationModule : ModuleBase
	{
		/// <summary>
		/// 模块单例
		/// </summary>
		public static LocalizationModule Instance { get; private set; }

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
		/// 本地化多语言提供者
		/// </summary>
		public ILocalizationProvider LocalizationProvider { get; set; }

		/// <summary>
		/// 取消栅栏：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
		/// OnDispose 时 Cancel，语言切换等在途异步操作随之取消；经 CancelAsync 等待取消清理完成。
		/// </summary>
		private CancellationBarrier m_barrier = new();

		/// <summary>
		/// 本地化表重填回调（由 HotfixLauncher 在表加载完成后注入，指向 TableManager.ReloadLocalizationAsync）。
		/// 语言切换先重填表、后广播事件的时序由本回调保证；返回 false 表示重填失败（空表/损坏数据），SetLanguageAsync 须回滚。
		/// </summary>
		internal Func<UniTask<bool>> LocalizationReloader { get; set; }

		/// <summary>
		/// 取消令牌：模块销毁（OnDispose）后触发，语言切换等在途异步操作观察它并中止。
		/// </summary>
		public CancellationToken Token => m_barrier.Token;

		/// <summary>
		/// 触发取消并等待在途操作完成清理后才返回。供框架重启取消清理。
		/// </summary>
		public UniTask CancelAsync() => m_barrier.CancelAsync();

		/// <summary>
		/// 获取或设置当前使用的语言（同步切换：仅改标记/存档/广播，不加载资源——按语种拆分后资源加载走 SetLanguageAsync）。
		/// </summary>
		public ELanguage Language
		{
			get => m_language;
			set
			{
				if (value == ELanguage.Unspecified)
				{
					FuLogger.LogError("设置本地化语言失败，语言为 Unspecified（未指定），已忽略本次设置");
					return;
				}

				if (value == m_language) return;
				var oldLanguage = m_language;
				m_language = value;

				ApplyLanguageChange(oldLanguage, value);
			}
		}

		/// <summary>
		/// 从系统语言转换出本地化语言类型（枚举为配置生成，映射项与 AOT 侧 LaunchLocalization 保持一致）。
		/// </summary>
		/// <returns>本地化语言类型（无法识别时返回 Unspecified）</returns>
		private static ELanguage FromSystemLanguage()
		{
			return Application.systemLanguage switch
			{
				// @formatter:off
				SystemLanguage.Chinese            => ELanguage.ChineseSimplified,
				SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
				SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
				SystemLanguage.English            => ELanguage.English,
				SystemLanguage.French             => ELanguage.French,
				SystemLanguage.German             => ELanguage.German,
				SystemLanguage.Indonesian         => ELanguage.Indonesian,
				SystemLanguage.Italian            => ELanguage.Italian,
				SystemLanguage.Japanese           => ELanguage.Japanese,
				SystemLanguage.Korean             => ELanguage.Korean,
				SystemLanguage.Portuguese         => ELanguage.PortuguesePortugal,
				SystemLanguage.Russian            => ELanguage.Russian,
				SystemLanguage.Spanish            => ELanguage.Spanish,
				SystemLanguage.Thai               => ELanguage.Thai,
				SystemLanguage.Vietnamese         => ELanguage.Vietnamese,
				SystemLanguage.Unknown            => ELanguage.Unspecified,
				_                                 => ELanguage.Unspecified
				// @formatter:on
			};
		}

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

			// 启动收敛：把本侧最终语言回写 PlayerPrefs，修正 AOT 偏好缺失/漂移（如旧版本仅存档无偏好）的情况
			PlayerPrefs.SetInt(LaunchLocalization.LANGUAGE_PREF_KEY, (int)m_language);
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
		/// 获取当前语言下的多语言文本
		/// </summary>
		/// <param name="key">多语言key</param>
		/// <param name="args">参数</param>
		/// <returns></returns>
		public string GetLanguageText(string key, params object[] args)
		{
			if (LocalizationProvider is null)
			{
				FuLogger.LogWarning("本地化多语言提供者未设置，请先设置");
				return $"[{key}]";
			}

			var result = LocalizationProvider.GetLanguage(key, args);
			return result.IsNullOrEmpty() ? $"[{key}]" : result;
		}

		/// <summary>
		/// 当前在途切换链路的取消源（防抖：新切换先取消旧链路，以最新目标为准；null = 无在途切换）。
		/// 访问须在主线程（切换链路的发起/收尾都在主线程同步段），无并发写风险。
		/// </summary>
		private CancellationTokenSource m_switchCts;

		/// <summary>
		/// 异步切换语言：确保目标语种资源就绪（必要时下载）→ 重填本地化表 → 才持久化并广播语言切换事件。
		/// 加载/下载中再次切换会取消旧链路（旧调用抛 OperationCanceledException），以最新目标为准；
		/// 失败回滚内存语言并返回 false（不广播不存档）。
		/// </summary>
		/// <param name="language">目标语言（Unspecified 视为非法调用）</param>
		/// <param name="token">取消令牌（必传，调用方生命周期所有者）</param>
		/// <returns>true = 切换完成（表已重填、事件已广播）；false = 切换失败（语言已回滚，原因见日志）；取消抛 OperationCanceledException</returns>
		public async UniTask<bool> SetLanguageAsync(ELanguage language, CancellationToken token)
		{
			if (language == ELanguage.Unspecified)
			{
				FuLogger.LogError("切换本地化语言失败，语言为 Unspecified（未指定），已忽略本次切换");
				return false;
			}

			if (language == m_language) return true; // 同语言幂等直过

			m_barrier.Token.ThrowIfCancellationRequested(); // 模块已销毁则拒绝
			token.ThrowIfCancellationRequested();         // 调用方已取消则拒绝

			// 防抖：取消在途旧链路（其 catch OCE 回滚内存语言后抛出，不影响本链路）
			if (m_switchCts != null)
			{
				m_switchCts.Cancel();
				m_switchCts.Dispose();
				m_switchCts = null;
			}

			var oldLanguage = m_language;
			m_language = language; // 先改内存语言：loader 路由与 AssetModule 按 tag 下载都读它（失败/取消路径须回滚）
			m_switchCts = new CancellationTokenSource();
			var switchCts = m_switchCts; // 本链路凭据：finally 清理时识别自己是否仍是最新链路
			using (m_barrier.Register())
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_barrier.Token, token, switchCts.Token))
			{
				try
				{
					// 1. 下载目标语种 bundle（本地已有则直过；失败回滚返回）
					if (!await GlobalModule.AssetModule.EnsureDownloadByTagAsync(language.ToString(), linkedCts.Token))
					{
						m_language = oldLanguage;
						return false;
					}

					// 2. 重填本地化表（loader 此时路由到新语言文件）；重填失败（含空表/损坏数据）同样回滚
					if (LocalizationReloader == null)
					{
						FuLogger.LogError("本地化表重填回调未注入，切换语言失败（HotfixLauncher 表加载完成后须注入 LocalizationReloader）");
						m_language = oldLanguage;
						return false;
					}
					if (!await LocalizationReloader())
					{
						m_language = oldLanguage;
						return false;
					}

					// 3. 表就绪后才持久化 + 广播（TableManager 订阅者此时重译拿到的已是新表）
					ApplyLanguageChange(oldLanguage, language);
					return true;
				}
				catch (OperationCanceledException)
				{
					m_language = oldLanguage; // 取消链路回滚内存语言（数据未换，必须还原）
					throw;
				}
				catch (Exception e)
				{
					FuLogger.LogError($"切换本地化语言 {language} 失败：{e.Message}");
					m_language = oldLanguage;
					return false;
				}
				finally
				{
					// 链路收尾：仅当自己仍是最新链路时清空凭据（被防抖取消的旧链路不动新链路的 CTS）
					if (m_switchCts == switchCts)
					{
						m_switchCts.Dispose();
						m_switchCts = null;
					}
				}
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
			PlayerPrefs.SetInt(LaunchLocalization.LANGUAGE_PREF_KEY, (int)newLanguage);
			PlayerPrefs.Save();

			// 发送本地化语言改变事件（订阅者如 TableManager 在事件回调中重译全表）
			var languageChangeEventArgs = LanguageChangeEventArgs.Create(oldLanguage, newLanguage);
			m_eventModule.Broadcast(languageChangeEventArgs);
		}
	}
}