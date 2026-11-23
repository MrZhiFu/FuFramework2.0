// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
	/// <summary>
	/// 红点管理模块——常量分部：已读状态持久化参数（单一事实源）。
	/// </summary>
	public partial class RedDotModule
	{
		#region 持久化

		/// <summary>
		/// 已读状态在 StorageModule 存档中的数据键名。
		/// </summary>
		private const string READ_STORAGE_KEY = "ReadSet";

		/// <summary>
		/// 已读状态在 StorageModule 存档中的文件名。
		/// </summary>
		private const string READ_STORAGE_FILE = "RedDotData";

		#endregion
	}
}
