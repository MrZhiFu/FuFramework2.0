// ReSharper disable once CheckNamespace

namespace FuFramework.Localization.Runtime
{
    /// <summary>
    /// 获取本地化多语言接口
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