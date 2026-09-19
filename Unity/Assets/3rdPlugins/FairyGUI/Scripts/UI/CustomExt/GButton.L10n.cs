namespace FairyGUI
{
    /// <summary>
    /// GButton 的 L10n 扩展：声明式文本写入 title 属性。
    /// </summary>
    public partial class GButton
    {
        /// <summary>
        /// GButton 支持 L10n。
        /// </summary>
        protected override bool ShouldHandleL10nGear() => true;

        /// <summary>
        /// 把解析出的文本写入 title。
        /// </summary>
        protected override void OnApplyL10nText(string key)
        {
            title = ResolveL10nText(key);
        }
    }
}