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
    public sealed class ConfigManager : FuModule, IConfigManager
    {
        /// <summary>
        /// 全局配置表字典。key为配置表名称，value为配置表数据。
        /// </summary>
        private readonly ConcurrentDictionary<string, IDataTable> m_ConfigDataDict;

        /// <summary>
        /// 初始化全局配置管理器的新实例。
        /// </summary>
        public ConfigManager()
        {
            m_ConfigDataDict = new ConcurrentDictionary<string, IDataTable>(StringComparer.Ordinal);
        }

        /// <summary>
        /// 获取全局配置项数量。
        /// </summary>
        public int Count => m_ConfigDataDict.Count;


        /// <summary>
        /// 全局配置管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 关闭并清理全局配置管理器。
        /// </summary>
        protected override void Shutdown() => RemoveAllConfigs();

        /// <summary>
        /// 获取指定全局配置项。
        /// </summary>
        /// <param name="configName">要获取全局配置项的名称。</param>
        /// <returns>要获取全局配置项的全局配置项。</returns>
        public IDataTable GetConfig(string configName) => m_ConfigDataDict.GetValueOrDefault(configName);

        /// <summary>
        /// 检查是否存在指定全局配置项。
        /// </summary>
        /// <param name="configName">要检查全局配置项的名称。</param>
        /// <returns>指定的全局配置项是否存在。</returns>
        public bool HasConfig(string configName) => m_ConfigDataDict.TryGetValue(configName, out _);

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
        /// <param name="configName">要移除全局配置项的名称。</param>
        public bool RemoveConfig(string configName) => HasConfig(configName) && m_ConfigDataDict.TryRemove(configName, out _);

        /// <summary>
        /// 清空所有全局配置项。
        /// </summary>
        public void RemoveAllConfigs() => m_ConfigDataDict.Clear();
    }
}