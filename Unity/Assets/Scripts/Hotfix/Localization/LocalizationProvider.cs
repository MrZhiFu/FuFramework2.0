using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Localization.Runtime;
using Hotfix.Config.Local;

namespace Hotfix.Localization
{
    /// <summary>
    /// 本地化多语言提供着
    /// 负责根据当前语言环境返回对应的本地化文本
    /// </summary>
    public class LocalizationProvider : ILocalizationProvider
    {
        /// <summary>
        /// 缓存配置表
        /// </summary>
        private TbLocalization m_TbLocalization;

        /// <summary>
        /// 获取本地化多语言
        /// </summary>
        /// <param name="key">多语言key(使用静态类LanguageKey的多语言字段即可)</param>
        /// <returns></returns>
        public string GetLanguage(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            m_TbLocalization ??= GlobalModule.ConfigModule.GetConfig<TbLocalization>();

            var localization = m_TbLocalization?.Get(key);
            if (localization == null)
            {
                FuLogger.LogWarning($"多语言key '{key}' 没找到，请检查多语言配置表!");
                return string.Empty;
            }

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