namespace FairyGUI
{
    /// <summary>
    /// GTextField 的 L10n 扩展：声明式文本写入 text 属性。
    /// </summary>
    public partial class GTextField
    {
        /// <summary>
        /// GTextField 支持 L10n。
        /// </summary>
        protected override bool ShouldHandleL10nGear() => true;

        /// <summary>
        /// 把解析出的文本写入 text。
        /// </summary>
        protected override void OnApplyL10nText(string key)
        {
            text = ResolveL10nText(key);
        }
    }
}