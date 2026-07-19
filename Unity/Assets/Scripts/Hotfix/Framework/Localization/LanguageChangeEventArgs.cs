using Hotfix.Framework.Event;
using Hotfix.Framework.ReferencePools;

namespace Hotfix.Framework.Localization
{
    /// <summary>
    /// 本地化语言改变事件。
    /// </summary>
    public sealed class LanguageChangeEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 本地化语言改变事件编号。
        /// </summary>
        public const string EventId = "Event.Localization.LanguageChange";

        /// <summary>
        /// 当前语言。
        /// </summary>
        public ELanguage ELanguage { get; set; } = ELanguage.Unspecified;

        /// <summary>
        /// 旧的语言。
        /// </summary>
        public ELanguage OldELanguage { get; set; } = ELanguage.Unspecified;


        /// <summary>
        /// 创建本地化语言改变事件。
        /// </summary>
        /// <param name="oldELanguage">旧的语言。</param>
        /// <param name="eLanguage">当前语言。</param>
        /// <returns>创建的本地化语言改变事件。</returns>
        public static LanguageChangeEventArgs Create(ELanguage oldELanguage, ELanguage eLanguage)
        {
            var localizationLanguageChangeEventArgs = ReferencePool.Acquire<LanguageChangeEventArgs>();
            localizationLanguageChangeEventArgs.OldELanguage = oldELanguage;
            localizationLanguageChangeEventArgs.ELanguage    = eLanguage;
            return localizationLanguageChangeEventArgs;
        }

        /// <summary>
        /// 清除事件参数。
        /// </summary>
        public override void Clear()
        {
            OldELanguage = ELanguage.Unspecified;
            ELanguage    = ELanguage.Unspecified;
        }
    }
}
