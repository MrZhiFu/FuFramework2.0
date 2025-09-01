using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 全局配置管理器。
    /// </summary>
    public sealed class ConfigManager : FuComponent
    {
        /// <summary>
        /// 全局配置类型与名称字典。key为配置类型，value为配置名称。
        /// </summary>
        private readonly ConcurrentDictionary<Type, string> m_ConfigNameTypeDict = new();

        /// <summary>
        /// 全局配置表字典。key为配置表名称，value为配置表数据。
        /// </summary>
        private readonly ConcurrentDictionary<string, IDataTable> m_ConfigDataDict = new(StringComparer.Ordinal);

        /// <summary>
        /// 获取全局配置项数量。
        /// </summary>
        public int Count => m_ConfigDataDict.Count;

        /// <summary>
        /// 全局配置管理器初始化。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        protected override void OnInit()
        {
            m_ConfigNameTypeDict.Clear();
            m_ConfigDataDict.Clear();
        }

        /// <summary>
        /// 全局配置管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 全局配置管理器关闭。
        /// </summary>
        /// <param name="shutdownType"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            RemoveAllConfigs();
        }

        /// <summary>
        /// 获取指定类型的全局配置项名称。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>返回类型名称</returns>
        private string GetTypeName<T>()
        {
            if (m_ConfigNameTypeDict.TryGetValue(typeof(T), out var configName)) return configName;
            configName = typeof(T).Name;
            m_ConfigNameTypeDict.TryAdd(typeof(T), configName);
            return configName;
        }

        /// <summary>
        /// 获取指定全局配置项。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>() where T : IDataTable
        {
            if (!HasConfig<T>()) return default;
            var configName = GetTypeName<T>();
            var config = GetConfig(configName);
            return config != null ? (T)config : default;
        }

        /// <summary>
        /// 获取指定全局配置项。
        /// </summary>
        /// <param name="configName">要获取全局配置项的名称。</param>
        /// <returns>要获取全局配置项的全局配置项。</returns>
        public IDataTable GetConfig(string configName) => m_ConfigDataDict.GetValueOrDefault(configName);

        /// <summary>
        /// 检查是否存在指定全局配置项。
        /// </summary>
        /// <returns>指定的全局配置项是否存在。</returns>
        public bool HasConfig<T>() where T : IDataTable
        {
            var configName = GetTypeName<T>();
            return HasConfig(configName);
        }

        /// <summary>
        /// 检查是否存在指定全局配置项。
        /// </summary>
        /// <param name="configName">要检查全局配置项的名称。</param>
        /// <returns>指定的全局配置项是否存在。</returns>
        public bool HasConfig(string configName)
        {
            return m_ConfigDataDict.TryGetValue(configName, out _);
        }

        /// <summary>
        /// 增加指定全局配置项。
        /// </summary>
        /// <param name="configName">要增加全局配置项的名称。</param>
        /// <param name="configValue">全局配置项的值。</param>
        /// <returns>是否增加全局配置项成功。</returns>
        public void AddConfig(string configName, IDataTable configValue)
        {
            var isExist = m_ConfigDataDict.TryGetValue(configName, out _);
            if (isExist) return;
            m_ConfigDataDict.TryAdd(configName, configValue);
        }

        /// <summary>
        /// 移除指定全局配置项。
        /// </summary>
        /// <returns>是否移除全局配置项成功。</returns>
        public bool RemoveConfig<T>() where T : IDataTable
        {
            var configName = GetTypeName<T>();
            return RemoveConfig(configName);
        }

        /// <summary>
        /// 移除指定全局配置项。
        /// </summary>
        /// <param name="configName">要移除全局配置项的名称。</param>
        public bool RemoveConfig(string configName)
        {
            return HasConfig(configName) && m_ConfigDataDict.TryRemove(configName, out _);
        }

        /// <summary>
        /// 清空所有全局配置项。
        /// </summary>
        public void RemoveAllConfigs()
        {
            m_ConfigNameTypeDict.Clear();
            m_ConfigDataDict.Clear();
        }
    }
}