using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Asset;
using ELanguage = Hotfix.Game.Config.ELanguage;

namespace Hotfix.Framework.Localization
{
	/// <summary>
	/// 本地化管理模块公共 API：单例/取消令牌、当前语言读写、多语言取词、异步切换语言。
	/// </summary>
	public sealed partial class LocalizationModule : ModuleBase
	{
		/// <summary>
		/// 模块单例
		/// </summary>
		public static LocalizationModule Instance { get; private set; }

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
		/// 获取当前语言下的多语言文本（查询型消费门面）。
		/// 取不到（key 缺失且英语兜底也缺失）时返回 [key] 占位符，便于发现漏翻。
		/// </summary>
		/// <param name="key">多语言key（建议用 L10nKey 常量）</param>
		/// <param name="args">格式化参数</param>
		/// <returns>译文；取不到时返回 [key]</returns>
		public string GetL10nText(string key, params object[] args)
		{
			var result = GetL10n(key, args);
			return result.IsNullOrEmpty() ? $"[{key}]" : result;
		}

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
			token.ThrowIfCancellationRequested();           // 调用方已取消则拒绝

			// 防抖：取消在途旧链路（其 catch OCE 回滚内存语言后抛出，不影响本链路）
			if (m_switchCts != null)
			{
				m_switchCts.Cancel();
				m_switchCts.Dispose();
				m_switchCts = null;
			}

			var oldLanguage = m_language;
			m_language  = language; // 先改内存语言：loader 路由与 AssetModule 按 tag 下载都读它（失败/取消路径须回滚）
			m_switchCts = new CancellationTokenSource();
			var switchCts = m_switchCts; // 本链路凭据：finally 清理时识别自己是否仍是最新链路
			using (m_barrier.Register())
			{
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
		}
	}
}
