using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

namespace Hotfix.ModuleConfig
{
    /// <summary>
    /// 配置管理模块。
    /// 功能：
    ///     1. 存储所有配置表。
    ///     2. 获取配置表。
    ///     3. 移除指定/所有配置表。
    /// </summary>
    public sealed class ConfigModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static ConfigModule Instance { get; private set; }

        /// <summary>
        /// 配置表类型与名称字典。key为配置类型，value为配置名称。
        /// </summary>
        private readonly ConcurrentDictionary<Type, string> m_CfgNameTypeDict = new();

        /// <summary>
        /// 配置表字典。key为配置表名称，value为配置表数据。
        /// </summary>
        private readonly ConcurrentDictionary<string, IDataTable> m_CfgDataDict = new(StringComparer.Ordinal);

        /// <summary>
        /// 获取配置表数量。
        /// </summary>
        public int Count => m_CfgDataDict.Count;

        /// <summary>
        /// 获取所有配置表名称。
        /// </summary>
        public IEnumerable<string> CfgNames => m_CfgDataDict.Keys;


        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            m_CfgNameTypeDict.Clear();
            m_CfgDataDict.Clear();
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            RemoveAllConfigs();
            Instance = null;
        }

        /// <summary>
        /// 获取指定配置表。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>() where T : IDataTable
        {
            if (!HasConfig<T>()) return default;
            var cfgName = GetTypeName<T>();
            var cfg     = GetConfig(cfgName);
            return cfg != null ? (T)cfg : default;
        }

        /// <summary>
        /// 获取指定配置表。
        /// </summary>
        /// <param name="cfgName">要获取配置表的名称。</param>
        /// <returns>要获取配置表的配置表。</returns>
        public IDataTable GetConfig(string cfgName)
        {
            return m_CfgDataDict.GetValueOrDefault(cfgName);
        }

        /// <summary>
        /// 检查是否存在指定配置表。
        /// </summary>
        /// <returns>指定的配置表是否存在。</returns>
        public bool HasConfig<T>() where T : IDataTable
        {
            var cfgName = GetTypeName<T>();
            return HasConfig(cfgName);
        }

        /// <summary>
        /// 检查是否存在指定配置表。
        /// </summary>
        /// <param name="cfgName">要检查配置表的名称。</param>
        /// <returns>指定的配置表是否存在。</returns>
        public bool HasConfig(string cfgName)
        {
            return m_CfgDataDict.TryGetValue(cfgName, out _);
        }

        /// <summary>
        /// 增加指定配置表。
        /// </summary>
        /// <param name="cfgName">要增加配置表的名称。</param>
        /// <param name="cfgValue">配置表的值。</param>
        /// <returns>是否增加配置表成功。</returns>
        public void AddConfig(string cfgName, IDataTable cfgValue)
        {
            var isExist = m_CfgDataDict.TryGetValue(cfgName, out _);
            if (isExist) return;
            m_CfgDataDict.TryAdd(cfgName, cfgValue);
        }

        /// <summary>
        /// 移除指定配置表。
        /// </summary>
        /// <returns>是否移除配置表成功。</returns>
        public bool RemoveConfig<T>() where T : IDataTable
        {
            var cfgName = GetTypeName<T>();
            return RemoveConfig(cfgName);
        }

        /// <summary>
        /// 移除指定配置表。
        /// </summary>
        /// <param name="cfgName">要移除配置表的名称。</param>
        public bool RemoveConfig(string cfgName)
        {
            return HasConfig(cfgName) && m_CfgDataDict.TryRemove(cfgName, out _);
        }

        /// <summary>
        /// 清空所有配置表。
        /// </summary>
        public void RemoveAllConfigs()
        {
            m_CfgNameTypeDict.Clear();
            m_CfgDataDict.Clear();
        }

        /// <summary>
        /// 获取指定类型的配置表名称。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>返回类型名称</returns>
        private string GetTypeName<T>()
        {
            if (m_CfgNameTypeDict.TryGetValue(typeof(T), out var cfgName)) return cfgName;
            cfgName = typeof(T).Name;
            m_CfgNameTypeDict.TryAdd(typeof(T), cfgName);
            return cfgName;
        }
    }
}
