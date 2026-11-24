using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Game.Config;
using Hotfix.Framework.Config;
using Hotfix.Framework.Asset;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Localization
{
	/// <summary>
	/// 本地化多语言提供者。
	///   负责根据当前语言环境返回对应的本地化文本。
	///   按语种拆分产物下，当前语言表只含目标语种译文；目标语种缺失（未翻译）时
	///   二段查询英语兜底表（懒加载：首次 miss 触发异步加载，加载完成后后续查询生效）。
	///   拆分产物统一 key+value 两字段，运行时 bean 只有 Key/Value 两属性——取词直接读 Value，语言维度消失。
	/// </summary>
	public class LocalizationProvider : ILocalizationProvider
	{
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
		/// 英语兜底表加载器（由 HotfixLauncher 在表加载完成后注入）。
		/// 不走 LanguageModule.Language 路由（兜底场景语言≠English），直取英语文件。
		/// </summary>
		internal Func<UniTask<TbLocalization>> EnglishTableLoader { get; set; }

		/// <summary>
		/// 获取本地化多语言
		/// </summary>
		/// <param name="key">多语言key(使用静态类L10nKey的多语言字段即可)</param>
		/// <param name="args">参数</param>
		/// <returns>译文；当前语种缺失时回退英语兜底表；两者皆缺返回空串</returns>
		public string GetL10n(string key, params object[] args)
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
		/// 取消令牌挂 AssetModule 模块生命周期（Provider 无独立生命周期，随 Hotfix 程序集存活）。
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
	}
}