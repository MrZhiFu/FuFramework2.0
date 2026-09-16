using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.Localization;

// ReSharper disable once CheckNamespace
namespace Hotfix.Game.Config
{
    /// <summary>
    /// TableManager 本地化分部：为配置表接入本地化模块的翻译适配，并支持订阅语言切换事件自动刷新表内文本。
    /// 订阅/退订由实例持有者（HotfixLauncher）在表加载完成后/热重启重载前驱动调用。
    /// </summary>
    public partial class TableManager
    {
        /// <summary>
        /// 事件管理模块
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// 订阅语言切换事件（表加载完成后由持有者调用）
        /// </summary>
        public void SubscribeLanguageChange()
        {
            m_EventModule = ModuleManager.GetModule<EventModule>();
            m_EventModule.Subscribe(LanguageChangeEventArgs.EventId, OnLanguageChanged);
        }

        /// <summary>
        /// 退订语言切换事件（热重启重载前由持有者对旧实例调用，否则旧实例被事件委托引用无法回收）
        /// </summary>
        public void UnsubscribeLanguageChange()
        {
            m_EventModule?.Unsubscribe(LanguageChangeEventArgs.EventId, OnLanguageChanged);
        }

        /// <summary>
        /// 语言切换事件处理：重新翻译全部配置表
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnLanguageChanged(object sender, GameEventArgs e)
        {
            if (IsLoaded)
                RefreshTranslateText();
        }

        /// <summary>
        /// 刷新全部配置表的本地化文本（首次翻译与语言切换刷新共用入口）
        /// </summary>
        public void RefreshTranslateText() => SetTranslateText(Translate);

        /// <summary>
        /// 翻译委托：按多语言 key 从本地化模块取译文。
        /// </summary>
        /// <param name="key">多语言 key</param>
        /// <param name="original">原始文本（取不到译文时的保底）</param>
        /// <returns>译文；key 为空或查不到时返回原始文本</returns>
        private string Translate(string key, string original)
        {
            if (string.IsNullOrEmpty(key)) return original;

            var text = LocalizationModule.Instance?.GetLanguageText(key);
            return string.IsNullOrEmpty(text) || text == $"[{key}]" ? original : text;
        }
    }
}