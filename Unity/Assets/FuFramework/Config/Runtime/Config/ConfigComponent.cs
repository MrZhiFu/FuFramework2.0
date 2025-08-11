//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using FuFramework.Core.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

namespace GameFrameX.Config.Runtime
{
    /// <summary>
    /// 全局配置组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Config")]
    public sealed class ConfigComponent : FuComponent
    {
        private IConfigManager m_ConfigManager = null;
        private ConcurrentDictionary<Type, string> m_ConfigNameTypeMap = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// 获取全局配置项数量。
        /// </summary>
        public int Count
        {
            get { return m_ConfigManager.Count; }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            m_ConfigNameTypeMap.Clear();
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(IConfigManager);
            base.Awake();
            m_ConfigManager = FuEntry.GetModule<IConfigManager>();
            if (m_ConfigManager == null)
            {
                Log.Fatal("Config manager is invalid.");
                return;
            }
        }

        /// <summary>
        /// 获取指定类型的全局配置项名称。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>返回类型名称</returns>
        private string GetTypeName<T>()
        {
            if (m_ConfigNameTypeMap.TryGetValue(typeof(T), out var configName))
            {
                return configName;
            }

            configName = typeof(T).Name;
            m_ConfigNameTypeMap.TryAdd(typeof(T), configName);

            return configName;
        }

        /// <summary>
        /// 获取指定全局配置项。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>() where T : IDataTable
        {
            if (HasConfig<T>())
            {
                var configName = GetTypeName<T>();
                var config = m_ConfigManager.GetConfig(configName);
                if (config != null)
                {
                    return (T)config;
                }
            }

            return default;
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
            m_ConfigNameTypeMap.Clear();
            m_ConfigManager.RemoveAllConfigs();
        }

        /// <summary>
        /// 增加
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="dataTable"></param>
        public void Add(string configName, IDataTable dataTable)
        {
            m_ConfigManager.AddConfig(configName, dataTable);
        }
    }
}