using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.Localization.Runtime
{
    /// <summary>
    /// 本地化语言改变事件。
    /// </summary>
    public sealed class LocalizationLanguageChangeEventArgs : GameEventArgs
    {
        /// <summary>
        /// 本地化语言改变事件编号。
        /// </summary>
        public static readonly string EventId = typeof(LocalizationLanguageChangeEventArgs).FullName;

        /// <summary>
        /// 当前语言。
        /// </summary>
        public Language Language { get; set; }

        /// <summary>
        /// 旧的语言。
        /// </summary>
        public Language OldLanguage { get; set; }

        /// <summary>
        /// 初始化本地化语言改变事件的新实例。
        /// </summary>
        public LocalizationLanguageChangeEventArgs()
        {
            OldLanguage = Language.Unspecified;
            Language = Language.Unspecified;
        }

        /// <summary>
        /// 创建本地化语言改变事件。
        /// </summary>
        /// <param name="oldLanguage">旧的语言。</param>
        /// <param name="language">当前语言。</param>
        /// <returns>创建的本地化语言改变事件。</returns>
        public static LocalizationLanguageChangeEventArgs Create(Language oldLanguage, Language language)
        {
            LocalizationLanguageChangeEventArgs localizationLanguageChangeEventArgs = ReferencePool.Acquire<LocalizationLanguageChangeEventArgs>();
            localizationLanguageChangeEventArgs.OldLanguage = oldLanguage;
            localizationLanguageChangeEventArgs.Language = language;
            return localizationLanguageChangeEventArgs;
        }

        /// <summary>
        /// 清除事件参数。
        /// </summary>
        public override void Clear()
        {
            OldLanguage = Language.Unspecified;
            Language = Language.Unspecified;
        }

        /// <summary>
        /// 获取事件编号。
        /// </summary>
        /// <returns>事件编号。</returns>
        public override string Id
        {
            get { return EventId; }
        }
    }
}