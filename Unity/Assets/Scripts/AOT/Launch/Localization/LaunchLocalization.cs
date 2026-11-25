using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace AOT.Launch.Localization
{
	/// <summary>
	/// AOT 阶段本地化多语言门面。
	///     数据按语种拆分：每语种一个 key+value 文件（Resources/LaunchLocalizationText/tblocalizationaot_<语言名>），
	///     启动时按语言注册表定位当前语种文件加载；当前语种译文缺失时二段查询英语兜底文件。
	///     语种清单/系统语言映射/兜底全部来自配置生成的 LaunchLanguageRegistry（删语种零代码改动）。
	///     脱离 UIModule/EventModule 自包含运行（与 LaunchView 同一设计原则）；
	///     热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。
	///     数据解析自包含（json 用 SimpleJSON / bin 用 Luban.ByteBuf，见 LaunchLocalization.Parse 分部）。
	/// </summary>
	public static partial class LaunchLocalization
	{
		/// <summary>
		/// 语言偏好在 PlayerPrefs 中的存储键（值为语言枚举成员名字符串）。
		/// 热更侧切换语言时（LocalizationModule）须以本键同步写入，保证下次启动 AOT 阶段读到相同偏好。
		/// </summary>
		public const string LANGUAGE_PREF_KEY = "AOT_Localization_Language";

		/// <summary>
		/// Resources 下 AOT 本地化拆分文件的目录（不含文件名与扩展名）
		/// </summary>
		private const string DATA_DIRECTORY = "LaunchLocalizationText";

		/// <summary>
		/// 拆分文件名前缀（完整名 = 前缀_语言枚举成员名）
		/// </summary>
		private const string DATA_FILE_PREFIX = "tblocalizationaot_";

		/// <summary>
		/// 当前语种的行数据：key → 译文
		/// </summary>
		private static readonly Dictionary<string, string> sr_rows = new();

		/// <summary>
		/// 英语兜底行数据（当前语言即英语时与 s_Rows 同源，不再重复加载）：key → 译文
		/// </summary>
		private static readonly Dictionary<string, string> sr_englishRows = new();

		/// <summary>
		/// 当前使用的语言（Unspecified 仅存在于加载前；初始化完成后恒为注册表内的有效语种）
		/// </summary>
		public static ELanguage Language { get; private set; } = ELanguage.Unspecified;

		/// <summary>
		/// 初始化：确定当前语言 → 加载当前语种文件（+英语兜底文件）。
		/// 须在启动流程首次展示文本前 await 完成；数据缺失仅记录错误，不阻断启动。
		/// </summary>
		/// <returns>初始化异步流程</returns>
		public static async UniTask InitializeAsync()
		{
			Language = LoadLanguagePreference();

			// AOT 阶段注入 FGUI 声明式多语言解析委托（热更阶段会被 HotfixLauncher 覆盖为热更表版本）
			FairyGUI.GObject.GetL10nText = key => GetL10nText(key);

			sr_rows.Clear();
			sr_englishRows.Clear();

			try
			{
				LoadLanguageRows(Language, sr_rows);

				// 当前语言不是英语时额外加载英语兜底文件；英语文件加载失败仅记录错误（译文缺失时显示 key）
				if (Language != ELanguage.English)
				{
					LoadLanguageRows(ELanguage.English, sr_englishRows);
				}
			}
			catch (Exception e)
			{
				// 数据缺失/解析失败仅记录错误并清空半解析数据，不阻断启动流程
				FuLogger.LogError($"AOT 本地化数据加载失败，AOT 本地化不可用：{e.Message}");
				sr_rows.Clear();
				sr_englishRows.Clear();
			}

			await UniTask.CompletedTask;
		}

		/// <summary>
		/// 加载指定语种的拆分文件到目标字典（同步 Resources.Load：AOT 阶段 YooAsset 未就绪的既有通道）
		/// </summary>
		/// <param name="language">目标语言</param>
		/// <param name="rows">接收行数据的字典</param>
		private static void LoadLanguageRows(ELanguage language, Dictionary<string, string> rows)
		{
			var path      = $"{DATA_DIRECTORY}/{DATA_FILE_PREFIX}{language}";
			var textAsset = Resources.Load<TextAsset>(path);
			if (textAsset == null)
			{
				FuLogger.LogError($"Resources 下未找到 {path}，语种 {language} 数据不可用");
				return;
			}

#if ENABLE_BINARY_CONFIG
			ParseBin(textAsset.bytes, rows);
#else
			ParseJson(textAsset.text, rows);
#endif
		}

		/// <summary>
		/// 获取 AOT 本地化多语言文本：当前语种缺失时二段查询英语兜底（与 Hotfix 版同构）。
		/// </summary>
		/// <param name="key">多语言 key（使用 AOT 版 LaunchL10nKey 静态类字段）</param>
		/// <param name="args">格式化参数</param>
		/// <returns>本地化文本；key 在两个字典都缺失时记录错误并返回空串</returns>
		public static string GetL10nText(string key, params object[] args)
		{
			if (string.IsNullOrEmpty(key)) return string.Empty;

			if (!sr_rows.TryGetValue(key, out var text))
			{
				FuLogger.LogError($"多语言key '{key}' 没找到，请检查AOT多语言配置表!");
				return string.Empty;
			}

			// 当前语种译文为空（未翻译）时回退英语兜底
			if (string.IsNullOrEmpty(text) && Language != ELanguage.English)
			{
				sr_englishRows.TryGetValue(key, out text);
			}

			text ??= string.Empty;

			return args is { Length: > 0 } ? string.Format(text, args) : text;
		}

		/// <summary>
		/// 读取语言偏好：字符串名 → 注册表校验；旧 int 值（历史版本）按枚举值一次性迁移；
		/// 均无效时回落系统语言（再无效由注册表兜底到 English）。
		/// </summary>
		/// <returns>当前语言（恒为注册表内有效语种）</returns>
		private static ELanguage LoadLanguagePreference()
		{
			// 新版：字符串名存储
			var name = PlayerPrefs.GetString(LANGUAGE_PREF_KEY, string.Empty);
			if (!string.IsNullOrEmpty(name) && LaunchLanguageRegistry.TryGetByName(name, out var byName))
			{
				return byName;
			}

			// 旧版迁移：int 值（历史版本按枚举值存储；仅本次改造前存档存在，此后统一写字符串）
			var legacy = PlayerPrefs.GetInt(LANGUAGE_PREF_KEY, 0);
			if (legacy > (int)ELanguage.Unspecified && Enum.IsDefined(typeof(ELanguage), legacy))
			{
				var migrated = (ELanguage)legacy;
				PlayerPrefs.SetString(LANGUAGE_PREF_KEY, migrated.ToString());
				PlayerPrefs.Save();
				return migrated;
			}

			return LaunchLanguageRegistry.FromSystemLanguage(Application.systemLanguage);
		}
	}
}