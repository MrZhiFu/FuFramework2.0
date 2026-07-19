namespace Hotfix.Framework.Localization
{
    /// <summary>
    /// 本地化多语言提供器。
    /// 功能：
    ///     1. 定义获取本地化多语言字符串，由热更代码中的实现类具体实现。
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>
        /// 获取本地化多语言接口
        /// </summary>
        /// <param name="key">多语言key</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        string GetLanguage(string key, params object[] args);
    }
}
