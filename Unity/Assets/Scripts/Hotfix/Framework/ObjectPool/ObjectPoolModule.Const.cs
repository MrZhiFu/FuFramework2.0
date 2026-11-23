// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
	/// <summary>
	/// 对象池管理模块——常量分部：创建对象池的默认参数（单一事实源）。
	/// 无参重载 CreateObjectPool(poolName) 以此为准，全参重载不受影响。
	/// </summary>
	public sealed partial class ObjectPoolModule
	{
		#region 默认参数

		/// <summary>
		/// 对象池默认容量：int.MaxValue 表示不限制容量。
		/// </summary>
		private const int DEFAULT_CAPACITY = int.MaxValue;

		/// <summary>
		/// 对象池默认自动销毁检查间隔秒数：float.MaxValue 表示默认不检查自动销毁。
		/// </summary>
		private const float DEFAULT_AUTO_DISPOSE_CHECK_INTERVAL = float.MaxValue;

		/// <summary>
		/// 对象池默认过期时间（秒）：float.MaxValue 表示默认不会过期。
		/// </summary>
		private const float DEFAULT_EXPIRE_TIME = float.MaxValue;

		/// <summary>
		/// 对象池默认优先级。
		/// </summary>
		internal const int DEFAULT_PRIORITY = 0;

		#endregion
	}
}
