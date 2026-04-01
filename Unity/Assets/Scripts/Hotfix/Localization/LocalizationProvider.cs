using FuFramework.Entry.Runtime;
using FuFramework.Localization.Runtime;
using Hotfix.Config.Local;

namespace Hotfix.Localization
{
    /// <summary>
    /// 本地化多语言提供着
    /// </summary>
    public class LocalizationProvider : ILocalizationProvider
    {
        /// <summary>
        /// 获取本地化多语言
        /// </summary>
        /// <param name="key">多语言key(使用静态类LanguageKey的多语言字段即可)</param>
        /// <returns></returns>
        public string GetLanguage(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            var tbLocalization = GlobalModule.ConfigModule.GetConfig<TbLocalization>();

            var localization = tbLocalization?.Get(key);
            if (localization is null) return "";

            var language = GlobalModule.LocalizationModule.Language;
            return language switch
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
                _                            => localization.English, // 所有其他未支持的语言，统一使用英语
                // @formatter:on
            };
        }
    }
}