using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.Localization
{
    /// <summary>
    /// 本地化语言类型。
    /// </summary>
    public enum ELanguage : byte
    {
        /// <summary>
        /// 未指定。
        /// </summary>
        Unspecified = 0,

        Afrikaans,
        Albanian,
        Arabic,
        Basque,
        Belarusian,
        Bulgarian,
        Catalan,
        ChineseSimplified,
        ChineseTraditional,
        Croatian,
        Czech,
        Danish,
        Dutch,
        English,
        Estonian,
        Faroese,
        Finnish,
        French,
        Georgian,
        German,
        Greek,
        Hebrew,
        Hungarian,
        Icelandic,
        Indonesian,
        Italian,
        Japanese,
        Korean,
        Latvian,
        Lithuanian,
        Macedonian,
        Malayalam,
        Norwegian,
        Persian,
        Polish,
        PortugueseBrazil,
        PortuguesePortugal,
        Romanian,
        Russian,
        SerboCroatian,
        SerbianCyrillic,
        SerbianLatin,
        Slovak,
        Slovenian,
        Spanish,
        Swedish,
        Thai,
        Turkish,
        Ukrainian,
        Vietnamese
    }

    /// <summary>
    /// 语言类型辅助函数集。
    /// </summary>
    public static class ELanguageHelper
    {
        /// <summary>
        /// 将 Unity 系统语言映射为本地化语言类型。
        /// </summary>
        /// <param name="systemLanguage">Unity 系统语言</param>
        /// <returns>对应的本地化语言类型（无法识别时返回 Unspecified）</returns>
        public static ELanguage FromSystemLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
                // @formatter:off
                SystemLanguage.Afrikaans          => ELanguage.Afrikaans,
                SystemLanguage.Arabic             => ELanguage.Arabic,
                SystemLanguage.Basque             => ELanguage.Basque,
                SystemLanguage.Belarusian         => ELanguage.Belarusian,
                SystemLanguage.Bulgarian          => ELanguage.Bulgarian,
                SystemLanguage.Catalan            => ELanguage.Catalan,
                SystemLanguage.Chinese            => ELanguage.ChineseSimplified,
                SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
                SystemLanguage.Czech              => ELanguage.Czech,
                SystemLanguage.Danish             => ELanguage.Danish,
                SystemLanguage.Dutch              => ELanguage.Dutch,
                SystemLanguage.English            => ELanguage.English,
                SystemLanguage.Estonian           => ELanguage.Estonian,
                SystemLanguage.Faroese            => ELanguage.Faroese,
                SystemLanguage.Finnish            => ELanguage.Finnish,
                SystemLanguage.French             => ELanguage.French,
                SystemLanguage.German             => ELanguage.German,
                SystemLanguage.Greek              => ELanguage.Greek,
                SystemLanguage.Hebrew             => ELanguage.Hebrew,
                SystemLanguage.Hungarian          => ELanguage.Hungarian,
                SystemLanguage.Icelandic          => ELanguage.Icelandic,
                SystemLanguage.Indonesian         => ELanguage.Indonesian,
                SystemLanguage.Italian            => ELanguage.Italian,
                SystemLanguage.Japanese           => ELanguage.Japanese,
                SystemLanguage.Korean             => ELanguage.Korean,
                SystemLanguage.Latvian            => ELanguage.Latvian,
                SystemLanguage.Lithuanian         => ELanguage.Lithuanian,
                SystemLanguage.Norwegian          => ELanguage.Norwegian,
                SystemLanguage.Polish             => ELanguage.Polish,
                SystemLanguage.Portuguese         => ELanguage.PortuguesePortugal,
                SystemLanguage.Romanian           => ELanguage.Romanian,
                SystemLanguage.Russian            => ELanguage.Russian,
                SystemLanguage.SerboCroatian      => ELanguage.SerboCroatian,
                SystemLanguage.Slovak             => ELanguage.Slovak,
                SystemLanguage.Slovenian          => ELanguage.Slovenian,
                SystemLanguage.Spanish            => ELanguage.Spanish,
                SystemLanguage.Swedish            => ELanguage.Swedish,
                SystemLanguage.Thai               => ELanguage.Thai,
                SystemLanguage.Turkish            => ELanguage.Turkish,
                SystemLanguage.Ukrainian          => ELanguage.Ukrainian,
                SystemLanguage.Unknown            => ELanguage.Unspecified,
                SystemLanguage.Vietnamese         => ELanguage.Vietnamese,
                _                                 => ELanguage.Unspecified
                // @formatter:on
            };
        }
    }
}
