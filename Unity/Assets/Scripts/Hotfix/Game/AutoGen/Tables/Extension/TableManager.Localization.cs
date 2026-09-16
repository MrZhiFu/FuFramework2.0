// ReSharper disable once CheckNamespace

namespace Hotfix.Game.Config
{
    /// <summary>
    /// TableManager 本地化分部：为配置表接入本地化模块的翻译适配。
    /// 语言切换刷新的事件订阅由 HotfixLauncher 负责，本类保持纯数据容器职责。
    /// </summary>
    public partial class TableManager
    {
        /// <summary>
        /// 刷新全部配置表的本地化文本（首次翻译与语言切换刷新共用入口）
        /// </summary>
        public void RefreshTranslateText()
        {
            SetTranslateText(Translate);
        }

        /// <summary>
        /// 翻译委托：按多语言 key 从本地化模块取译文。
        /// </summary>
        /// <param name="key">多语言 key</param>
        /// <param name="original">原始文本（取不到译文时的保底）</param>
        /// <returns>译文；key 为空或查不到时返回原始文本</returns>
        private string Translate(string key, string original)
        {
            if (string.IsNullOrEmpty(key)) return original;

            var text = Framework.Localization.LocalizationModule.Instance?.GetLanguageText(key);
            return string.IsNullOrEmpty(text) || text == $"[{key}]" ? original : text;
        }
    }
}