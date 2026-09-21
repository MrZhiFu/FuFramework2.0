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
    ///     从 Resources 加载 AOT 本地化表数据（YooAsset 未就绪阶段的既有加载通道），
    ///     脱离 UIModule/EventModule 自包含运行（与 LaunchView 同一设计原则）；
    ///     热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。
    ///     数据解析自包含（json 用 SimpleJSON / bin 用 Luban.ByteBuf，见 LaunchLocalization.Parse 分部），
    ///     不依赖热更侧配置框架。
    /// </summary>
    public static partial class LaunchLocalization
    {
        /// <summary>
        /// 语言偏好在 PlayerPrefs 中的存储键（值为 (int)ELanguage）。
        /// 热更侧切换语言时（LocalizationModule）须以本键同步写入，保证下次启动 AOT 阶段读到相同偏好。
        /// </summary>
        public const string LanguagePrefKey = "AOT_Localization_Language";

        /// <summary>
        /// Resources 下 AOT 本地化表数据的加载路径（不含扩展名）。
        /// </summary>
        private const string DataPath = "LaunchLocalizationText/tblocalizationaot";

        /// <summary>
        /// 多语言行数据字典。key 为多语言 key，value 为行数据。
        /// </summary>
        private static readonly Dictionary<string, LocalizationAOT> s_Rows = new();

        /// <summary>
        /// 当前使用的语言。
        /// </summary>
        public static ELanguage Language { get; private set; } = ELanguage.Unspecified;

        /// <summary>
        /// 初始化：加载 AOT 本地化表数据并确定当前语言。
        /// 须在启动流程首次展示文本前 await 完成；数据缺失仅记录错误，不阻断启动。
        /// </summary>
        /// <returns>初始化异步流程</returns>
        public static async UniTask InitializeAsync()
        {
            Language = LoadLanguagePreference();

            // AOT 阶段注入 FGUI 声明式多语言解析委托（热更阶段会被 HotfixLauncher 覆盖为热更表版本）
            FairyGUI.GObject.GetLanguageText = key => GetLanguage(key);

            var textAsset = Resources.Load<TextAsset>(DataPath);
            if (textAsset == null)
            {
                FuLogger.LogError($"[LaunchLocalization] Resources 下未找到 {DataPath}，AOT 本地化不可用!");
                return;
            }

            s_Rows.Clear();

            try
            {
#if ENABLE_BINARY_CONFIG
                ParseBin(textAsset.bytes);
#else
                ParseJson(textAsset.text);
#endif
            }
            catch (Exception e)
            {
                // 数据解析失败（变体错配/数据损坏）仅记录错误并清空半解析数据，不阻断启动流程
                FuLogger.LogError($"[LaunchLocalization] AOT 本地化数据解析失败，AOT 本地化不可用：{e.Message}");
                s_Rows.Clear();
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 获取 AOT 本地化多语言文本。
        /// </summary>
        /// <param name="key">多语言 key（使用 AOT 版 LaunchL10nKey 静态类字段）</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化文本；key 未找到时记录错误并返回空串</returns>
        public static string GetLanguage(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            if (!s_Rows.TryGetValue(key, out var row))
            {
                FuLogger.LogError($"[LaunchLocalization] 多语言key '{key}' 没找到，请检查AOT多语言配置表!");
                return string.Empty;
            }

            var text = Language switch
            {
                // @formatter:off
                ELanguage.ChineseSimplified  => row.ChineseSimplified,
                ELanguage.ChineseTraditional => row.ChineseTraditional,
                ELanguage.English            => row.English,
                ELanguage.Japanese           => row.Japanese,
                ELanguage.Korean             => row.Korean,
                ELanguage.Thai               => row.Thai,
                ELanguage.Indonesian         => row.Indonesian,
                ELanguage.French             => row.French,
                ELanguage.German             => row.German,
                ELanguage.Italian            => row.Italian,
                ELanguage.PortuguesePortugal => row.PortuguesePortugal,
                ELanguage.PortugueseBrazil   => row.PortugueseBrazil,
                ELanguage.Spanish            => row.Spanish,
                ELanguage.Vietnamese         => row.Vietnamese,
                ELanguage.Russian            => row.Russian,
                _                            => row.English, // 语言类型未支持时，统一使用英语
                // @formatter:on
            };

            // 目标语言字段为空时回退英语（与 Hotfix 版同构）
            if (string.IsNullOrEmpty(text))
            {
                text = row.English;
            }

            text ??= string.Empty;

            return args is { Length: > 0 } ? string.Format(text, args) : text;
        }

        /// <summary>
        /// 从系统语言转换出本地化语言类型（枚举为配置生成，映射项与 Hotfix 侧 LocalizationModule 保持一致）。
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
        /// 读取语言偏好：用户持久化选择优先，无记录时回退系统语言。
        /// </summary>
        /// <returns>当前语言</returns>
        private static ELanguage LoadLanguagePreference()
        {
            var value = PlayerPrefs.GetInt(LanguagePrefKey, (int)ELanguage.Unspecified);
            if (value > (int)ELanguage.Unspecified && value <= (int)ELanguage.Vietnamese)
            {
                return (ELanguage)value;
            }

            return FromSystemLanguage();
        }
    }
}