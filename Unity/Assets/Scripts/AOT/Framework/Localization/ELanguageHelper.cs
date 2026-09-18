using UnityEngine;
using AOT.Launch.Localization;

namespace AOT.Framework.Localization
{
    /// <summary>
    /// 语言类型辅助函数集。
    ///     语言枚举 ELanguage 为配置生成（__enums__.xlsx 语言 sheet），成员集与 Hotfix 侧一致。
    /// </summary>
    public static class ELanguageHelper
    {
        /// <summary>
        /// 将 Unity 系统语言映射为本地化语言类型（映射项与 Hotfix 侧 LocalizationModule.GetSystemLanguage 保持一致）。
        /// </summary>
        /// <param name="systemLanguage">Unity 系统语言</param>
        /// <returns>对应的本地化语言类型（无法识别时返回 Unspecified）</returns>
        public static ELanguage FromSystemLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
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
    }
}
