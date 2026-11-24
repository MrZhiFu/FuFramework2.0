namespace Hotfix.Framework.Localization
{
	/// <summary>
	/// 本地化多语言提供器：定义多语言字符串获取，具体实现由热更代码提供。
	/// </summary>
	public interface ILocalizationProvider
	{
		/// <summary>
		/// 获取本地化多语言接口
		/// </summary>
		/// <param name="key">多语言key</param>
		/// <param name="args">参数</param>
		string GetL10n(string key, params object[] args);
	}
}