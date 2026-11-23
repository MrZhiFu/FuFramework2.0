// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Storage
{
	/// <summary>
	/// 数据存储管理模块——常量分部：存储路径与默认文件名（单一事实源）。
	/// </summary>
	public sealed partial class StorageModule
	{
		#region 存储

		/// <summary>
		/// 数据根目录名：全部数据文件写在 persistentDataPath 下的此目录（StorageHelper 与模块共用）。
		/// </summary>
		public const string DIR_ROOT = "GameData";

		/// <summary>
		/// 默认数据存储文件名：存取 API 省略 fileName 参数时的目标文件。
		/// </summary>
		private const string DEFAULT_FILE_NAME = "DefaultData";

		#endregion
	}
}
