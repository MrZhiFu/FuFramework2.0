using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
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
        private EventModule m_eventModule;

        /// <summary>
        /// 订阅语言切换事件（表加载完成后由持有者调用）
        /// </summary>
        public void SubscribeLanguageChange()
        {
            m_eventModule = ModuleManager.GetModule<EventModule>();
            m_eventModule.Subscribe<LanguageChangeEventArgs>(OnLanguageChanged);
        }

        /// <summary>
        /// 退订语言切换事件（热重启重载前由持有者对旧实例调用，否则旧实例被事件委托引用无法回收）
        /// </summary>
        public void UnsubscribeLanguageChange()
        {
            m_eventModule?.Unsubscribe<LanguageChangeEventArgs>(OnLanguageChanged);
        }

        /// <summary>
        /// 语言切换事件处理：重新翻译全部配置表
        /// </summary>
        /// <param name="e">事件参数</param>
        private void OnLanguageChanged(GameEventArgs e)
        {
            if (IsLoaded)
                RefreshTranslateText();
        }

        /// <summary>
        /// 刷新全部配置表的本地化文本（首次翻译与语言切换刷新共用入口）。
        /// 生成层已剔除无 text 字段的表的调用（Luban 条件生成），此处只遍历含多语言字段（如 Achievement/Item）的表。
        /// 全量同步重译语义保留；text 字段规模上量到万行级（约 2ms/次）再评估分表分帧，十万行级（约 20ms）才有掉帧风险。
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

            var text = LocalizationModule.Instance?.GetL10nText(key);
            return string.IsNullOrEmpty(text) || text == $"[{key}]" ? original : text;
        }

        /// <summary>
        /// 重载本地化表数据（语言切换时由 LocalizationModule 在广播事件前调用）。
        /// 依赖 loader 层按 LocalizationModule 当前语言路由，原地重建 DataList/StrKeyDataDict；
        /// LocalizationModule 缓存的表实例引用不变，无需任何同步操作。
        /// 重载后校验行数非空：损坏数据（解析宽松不抛异常）会导致空表，空表广播事件将让全 UI 译文变占位符——
        /// 视为重载失败返回 false，由 SetLanguageAsync 回滚语言（不广播不存档）。
        /// </summary>
        /// <returns>true = 重载成功且非空；false = 重载失败（表已损坏/为空，语言须回滚）</returns>
        public async UniTask<bool> ReloadLocalizationAsync()
        {
            await TbLocalization.LoadAsync();
            var count = TbLocalization.Count;
            if (count <= 0)
            {
                FuLogger.LogError("本地化表重载失败：拆分文件数据为空或损坏（解析未抛异常但无有效行），语言切换须回滚");
                return false;
            }

            return true;
        }
    }
}