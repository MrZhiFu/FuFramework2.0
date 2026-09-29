using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Config
{
	/// <summary>
	/// 配置管理模块的公共 API。
	/// 功能：
	///     1. 获取配置表。
	///     2. 检查配置表是否存在。
	///     3. 增加/移除配置表。
	/// </summary>
	public sealed partial class ConfigModule : ModuleBase
	{
		/// <summary>
		/// 模块单例
		/// </summary>
		public static ConfigModule Instance { get; private set; }

		/// <summary>
		/// 获取配置表数量。
		/// </summary>
		public int Count => m_cfgDataDict.Count;

		/// <summary>
		/// 获取所有配置表名称。
		/// </summary>
		public string[] CfgNames
		{
			get
			{
				var names = new string[m_cfgDataDict.Count];
				m_cfgDataDict.Keys.CopyTo(names, 0);
				return names;
			}
		}

		/// <summary>
		/// 获取指定配置表。
		/// 键解析：优先类型全名（跨命名空间同名表可区分），未命中回退类名（兼容以 nameof(TbXxx) 注册的表管理器）。
		/// </summary>
		/// <typeparam name="T">配置表类型</typeparam>
		/// <returns>配置表，不存在或键上的表类型非 T 时返回 default</returns>
		public T GetConfig<T>() where T : IDataTable
		{
			var type = typeof(T);

			if (type.FullName != null && m_cfgDataDict.TryGetValue(type.FullName, out var fullNameCfg) && fullNameCfg is T fullNameTyped)
				return fullNameTyped;

			// 类型不匹配时返回 default 而非强制转换：跨命名空间同名表在注册期会被短名键覆盖，强转必抛 InvalidCastException
			return m_cfgDataDict.TryGetValue(type.Name, out var cfg) && cfg is T typed ? typed : default;
		}

		/// <summary>
		/// 获取指定配置表。
		/// </summary>
		/// <param name="cfgName">配置表名称</param>
		/// <returns>配置表，不存在时返回 null</returns>
		public IDataTable GetConfig(string cfgName)
		{
			cfgName.NotNullOrEmpty(nameof(cfgName));
			return m_cfgDataDict.GetValueOrDefault(cfgName);
		}

		/// <summary>
		/// 检查是否存在指定配置表。
		/// 键解析与 <see cref="GetConfig{T}"/> 一致（全名优先、类名回退），并要求键上的表类型确为 T。
		/// </summary>
		/// <typeparam name="T">配置表类型</typeparam>
		/// <returns>是否存在</returns>
		public bool HasConfig<T>() where T : IDataTable
		{
			var type = typeof(T);

			if (type.FullName != null && m_cfgDataDict.TryGetValue(type.FullName, out var fullNameCfg) && fullNameCfg is T)
				return true;

			return m_cfgDataDict.TryGetValue(type.Name, out var cfg) && cfg is T;
		}

		/// <summary>
		/// 检查是否存在指定配置表。
		/// </summary>
		/// <param name="cfgName">配置表名称</param>
		/// <returns>是否存在</returns>
		public bool HasConfig(string cfgName)
		{
			cfgName.NotNullOrEmpty(nameof(cfgName));
			return m_cfgDataDict.ContainsKey(cfgName);
		}

		/// <summary>
		/// 增加指定配置表。
		/// </summary>
		/// <param name="cfgName">配置表名称</param>
		/// <param name="cfgValue">配置表数据</param>
		/// <returns>是否增加成功</returns>
		public bool AddConfig(string cfgName, IDataTable cfgValue)
		{
			cfgName.NotNullOrEmpty(nameof(cfgName));
			cfgValue.NotNull(nameof(cfgValue));
			if (m_cfgDataDict.ContainsKey(cfgName))
			{
				FuLogger.LogWarning($"[ConfigModule] 配置表 '{cfgName}' 已存在，忽略重复添加。");
				return false;
			}

			m_cfgDataDict.Add(cfgName, cfgValue);
			return true;
		}

		/// <summary>
		/// 移除指定配置表。
		/// 键解析与 <see cref="GetConfig{T}"/> 一致（全名优先、类名回退），并要求键上的表类型确为 T。
		/// </summary>
		/// <typeparam name="T">配置表类型</typeparam>
		/// <returns>是否移除成功</returns>
		public bool RemoveConfig<T>() where T : IDataTable
		{
			var type = typeof(T);

			if (type.FullName != null && m_cfgDataDict.TryGetValue(type.FullName, out var fullNameCfg) && fullNameCfg is T)
				return m_cfgDataDict.Remove(type.FullName);

			return m_cfgDataDict.TryGetValue(type.Name, out var cfg) && cfg is T && m_cfgDataDict.Remove(type.Name);
		}

		/// <summary>
		/// 移除指定配置表。
		/// </summary>
		/// <param name="cfgName">配置表名称</param>
		/// <returns>是否移除成功</returns>
		public bool RemoveConfig(string cfgName)
		{
			cfgName.NotNullOrEmpty(nameof(cfgName));
			return m_cfgDataDict.Remove(cfgName);
		}

		/// <summary>
		/// 清空所有配置表。
		/// </summary>
		public void RemoveAllConfigs()
		{
			m_cfgDataDict.Clear();
		}
	}
}
