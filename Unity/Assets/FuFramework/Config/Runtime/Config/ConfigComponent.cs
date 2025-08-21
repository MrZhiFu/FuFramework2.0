using System;
using System.Collections.Concurrent;
using FuFramework.Core.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 全局配置组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Config")]
    public sealed class ConfigComponent : FuComponent
    {
        /// <summary>
        /// 全局配置管理器。
        /// </summary>
        private IConfigManager m_ConfigManager;

        /// <summary>
        /// 全局配置类型与名称字典。key为配置类型，value为配置名称。
        /// </summary>
        private readonly ConcurrentDictionary<Type, string> m_ConfigNameTypeDict = new();

        /// <summary>
        /// 获取全局配置项数量。
        /// </summary>
        public int Count => m_ConfigManager.Count;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            m_ConfigNameTypeDict.Clear();
            ImplComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(IConfigManager);

            base.Awake();

            m_ConfigManager = FuEntry.GetModule<IConfigManager>();
            if (m_ConfigManager == null) Log.Fatal("Config manager is invalid.");
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
            var config     = m_ConfigManager.GetConfig(configName);
            return config != null ? (T)config : default;
        }

        /// <summary>
        /// 检查是否存在指定全局配置项。
        /// </summary>
        /// <returns>指定的全局配置项是否存在。</returns>
        public bool HasConfig<T>() where T : IDataTable
        {
            var configName = GetTypeName<T>();
            return m_ConfigManager.HasConfig(configName);
        }

        /// <summary>
        /// 移除指定全局配置项。
        /// </summary>
        /// <returns>是否移除全局配置项成功。</returns>
        public bool RemoveConfig<T>() where T : IDataTable
        {
            var configName = GetTypeName<T>();
            return m_ConfigManager.RemoveConfig(configName);
        }

        /// <summary>
        /// 清空所有全局配置项。
        /// </summary>
        public void RemoveAllConfigs()
        {
            m_ConfigNameTypeDict.Clear();
            m_ConfigManager.RemoveAllConfigs();
        }

        /// <summary>
        /// 增加
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="dataTable"></param>
        public void Add(string configName, IDataTable dataTable) => m_ConfigManager.AddConfig(configName, dataTable);
    }
}