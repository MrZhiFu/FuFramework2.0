#if UNITY_EDITOR
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// Hotfix 程序集反射工具（仅 Editor 调试面板使用）。
	/// 功能：
	///     1. 集中登记全部 Hotfix 类型全名——改热更类型名 / 命名空间只需修改本文件内对应属性的字面量，勿在各调试窗口写字面量；
	///     2. 提供模块实例获取的两种标准模式：经 ModuleManager.GetModule&lt;T&gt;（无静态单例的模块）与经静态 Instance 属性；
	///     3. 统一 "类型全名, Hotfix" 的程序集限定解析。
	/// 注意：
	///     1. 本命名空间下存在同名工具类 <c>FuFramework.Core.Editor.Type</c>（Common/Misc/Type.cs），
	///        故本文件内 System.Type 一律写全限定名，避免解析到该工具类；
	///     2. 跨命名空间引用本类须用 using 别名（如 using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;）。
	/// </summary>
	public static class HotfixReflection
	{
		/// <summary>
		/// 热更程序集名称
		/// </summary>
		private const string HOTFIX_ASSEMBLY_NAME = "Hotfix";

		#region 类型解析

		/// <summary>
		/// 按类型全名解析 Hotfix 程序集中的类型（未找到返回 null）
		/// </summary>
		/// <param name="typeFullName">Hotfix 类型全名（不含程序集限定）。</param>
		/// <returns>解析到的类型，未找到为 null。</returns>
		private static System.Type Resolve(string typeFullName)
		{
			return string.IsNullOrEmpty(typeFullName) ? null : System.Type.GetType($"{typeFullName}, {HOTFIX_ASSEMBLY_NAME}");
		}

		/// <summary>
		/// Hotfix.Framework.Core.ModuleManager
		/// </summary>
		public static System.Type ModuleManager => Resolve("Hotfix.Framework.Core.ModuleManager");

		/// <summary>
		/// Hotfix.Framework.Core.ReferencePool（静态基座）
		/// </summary>
		public static System.Type ReferencePool => Resolve("Hotfix.Framework.Core.ReferencePool");

		/// <summary>
		/// Hotfix.Framework.Core.ReferencePoolInfo
		/// </summary>
		public static System.Type ReferencePoolInfo => Resolve("Hotfix.Framework.Core.ReferencePoolInfo");

		/// <summary>
		/// Hotfix.Framework.Event.EventModule
		/// </summary>
		public static System.Type EventModule => Resolve("Hotfix.Framework.Event.EventModule");

		/// <summary>
		/// Hotfix.Framework.Event.GameEventArgs
		/// </summary>
		public static System.Type GameEventArgs => Resolve("Hotfix.Framework.Event.GameEventArgs");

		/// <summary>
		/// Hotfix.Framework.Config.ConfigModule
		/// </summary>
		public static System.Type ConfigModule => Resolve("Hotfix.Framework.Config.ConfigModule");

		/// <summary>
		/// Hotfix.Framework.RedDot.RedDotModule
		/// </summary>
		public static System.Type RedDotModule => Resolve("Hotfix.Framework.RedDot.RedDotModule");

		/// <summary>
		/// Hotfix.Framework.RedDot.RedDotNode
		/// </summary>
		public static System.Type RedDotNode => Resolve("Hotfix.Framework.RedDot.RedDotNode");

		/// <summary>
		/// Hotfix.Framework.RedDot.RedDotKey
		/// </summary>
		public static System.Type RedDotKey => Resolve("Hotfix.Framework.RedDot.RedDotKey");

		/// <summary>
		/// Hotfix.Framework.ObjectPool.ObjectPoolModule
		/// </summary>
		public static System.Type ObjectPoolModule => Resolve("Hotfix.Framework.ObjectPool.ObjectPoolModule");

		/// <summary>
		/// Hotfix.Framework.ObjectPool.ObjectPoolBase
		/// </summary>
		public static System.Type ObjectPoolBase => Resolve("Hotfix.Framework.ObjectPool.ObjectPoolBase");

		/// <summary>
		/// Hotfix.Framework.ObjectPool.ObjectInfo
		/// </summary>
		public static System.Type ObjectInfo => Resolve("Hotfix.Framework.ObjectPool.ObjectInfo");

		/// <summary>
		/// Hotfix.Framework.Web.WebModule
		/// </summary>
		public static System.Type WebModule => Resolve("Hotfix.Framework.Web.WebModule");

		/// <summary>
		/// Hotfix.Framework.Web.WebModuleDebugInfo
		/// </summary>
		public static System.Type WebModuleDebugInfo => Resolve("Hotfix.Framework.Web.WebModuleDebugInfo");

		/// <summary>
		/// Hotfix.Framework.Web.WebLiveRequestInfo
		/// </summary>
		public static System.Type WebLiveRequestInfo => Resolve("Hotfix.Framework.Web.WebLiveRequestInfo");

		/// <summary>
		/// Hotfix.Framework.Web.WebLogEntry
		/// </summary>
		public static System.Type WebLogEntry => Resolve("Hotfix.Framework.Web.WebLogEntry");

		/// <summary>
		/// Hotfix.Framework.Web.WebJsonDataBase
		/// </summary>
		public static System.Type WebJsonDataBase => Resolve("Hotfix.Framework.Web.WebJsonDataBase");

		/// <summary>
		/// Hotfix.Framework.Web.WebPbData
		/// </summary>
		public static System.Type WebPbData => Resolve("Hotfix.Framework.Web.WebPbData");

		#endregion

		#region 模块实例获取

		/// <summary>
		/// 经 ModuleManager.GetModule&lt;T&gt; 泛型方法获取热更模块实例（无静态单例的模块，如 EventModule / ObjectPoolModule）
		/// </summary>
		/// <param name="moduleType">模块类型。</param>
		/// <returns>模块实例，获取失败为 null。</returns>
		public static object GetModuleInstance(System.Type moduleType)
		{
			if (moduleType == null) return null;

			var moduleManagerType = ModuleManager;
			var getModuleMethod   = moduleManagerType?.GetMethod("GetModule", BindingFlags.Public | BindingFlags.Static);
			return getModuleMethod?.MakeGenericMethod(moduleType).Invoke(null, null);
		}

		/// <summary>
		/// 经静态 Instance 属性获取热更模块实例（有静态单例的模块，如 ConfigModule / RedDotModule / WebModule）
		/// </summary>
		/// <param name="moduleType">模块类型。</param>
		/// <returns>模块实例，获取失败为 null。</returns>
		public static object GetStaticInstance(System.Type moduleType)
		{
			if (moduleType == null) return null;

			var instanceProperty = moduleType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
			return instanceProperty?.GetValue(null);
		}

		#endregion
	}
}
#endif
