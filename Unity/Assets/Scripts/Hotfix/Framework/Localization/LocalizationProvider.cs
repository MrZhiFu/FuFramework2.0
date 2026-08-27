using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Config.Local;
using Hotfix.Game.Proto;
using Hotfix.Framework.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Localization
{
    /// <summary>
    /// 本地化多语言提供者。
    ///   负责根据当前语言环境返回对应的本地化文本
    /// </summary>
    public class LocalizationProvider : ILocalizationProvider
    {
        /// <summary>
        /// 缓存多语言配置表，避免每次调用都从配置表中获取
        /// </summary>
        private TbLocalization m_TbLocalization;

        /// <summary>
        /// 获取本地化多语言
        /// </summary>
        /// <param name="key">多语言key(使用静态类LanguageKey的多语言字段即可)</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public string GetLanguage(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            // 获取多语言配置表并缓存
            m_TbLocalization ??= ConfigModule.Instance.GetConfig<TbLocalization>();

            var localization = m_TbLocalization?.Get(key);
            if (localization == null)
            {
                FuLogger.LogError($"[LocalizationProvider] 多语言key '{key}' 没找到，请检查多语言配置表!");
                return string.Empty;
            }

            var eLanguage = LocalizationModule.Instance.Language;

            var text = eLanguage switch
            {
                // @formatter:off
                ELanguage.ChineseSimplified  => localization.ChineseSimplified,
                ELanguage.ChineseTraditional => localization.ChineseTraditional,
                ELanguage.English            => localization.English,
                ELanguage.Japanese           => localization.Japanese,
                ELanguage.Korean             => localization.Korean,
                ELanguage.Thai               => localization.Thai,
                ELanguage.Indonesian         => localization.Indonesian,
                ELanguage.French             => localization.French,
                ELanguage.German             => localization.German,
                ELanguage.Italian            => localization.Italian,
                ELanguage.PortuguesePortugal => localization.PortuguesePortugal,
                ELanguage.Spanish            => localization.Spanish,
                ELanguage.Vietnamese         => localization.Vietnamese,
                ELanguage.PortugueseBrazil   => localization.PortuguesePortugal,
                ELanguage.Russian            => localization.Russian,
                ELanguage.Belarusian         => localization.Russian,
                ELanguage.Ukrainian          => localization.Russian,
                _                            => localization.English, // 语言类型未支持时，统一使用英语
                // @formatter:on
            };

            // 如果目标语言字段为空，则使用英语
            if (text.IsNullOrEmpty())
            {
                text = localization.English;
            }

            // 如果有参数，则返回格式化参数后的文本，否则直接返回文本
            return args is { Length: > 0 } ? string.Format(text, args) : text;
        }
    }
}
