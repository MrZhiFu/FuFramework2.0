//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;

namespace GameFrameX.Config.Runtime
{
    /// <summary>
    /// 全局配置管理器。
    /// </summary>
    
    public sealed partial class ConfigManager : FuModule, IConfigManager
    {
        private readonly ConcurrentDictionary<string, IDataTable> m_ConfigDatas;

        /// <summary>
        /// 初始化全局配置管理器的新实例。
        /// </summary>
        
        public ConfigManager()
        {
            m_ConfigDatas = new ConcurrentDictionary<string, IDataTable>(StringComparer.Ordinal);
        }

        /// <summary>
        /// 获取全局配置项数量。
        /// </summary>
        public int Count
        {
            get { return m_ConfigDatas.Count; }
        }


        /// <summary>
        /// 全局配置管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 关闭并清理全局配置管理器。
        /// </summary>
        protected override void Shutdown()
        {
            RemoveAllConfigs();
        }


        /// <summary>
        /// 检查是否存在指定全局配置项。
        /// </summary>
        /// <param name="configName">要检查全局配置项的名称。</param>
        /// <returns>指定的全局配置项是否存在。</returns>
        
        public bool HasConfig(string configName)
        {
            return m_ConfigDatas.TryGetValue(configName, out _);
        }


        /// <summary>
        /// 增加指定全局配置项。
        /// </summary>
        /// <param name="configName">要增加全局配置项的名称。</param>
        /// <param name="configValue">全局配置项的值。</param>
        /// <returns>是否增加全局配置项成功。</returns>
        
        public void AddConfig(string configName, IDataTable configValue)
        {
            bool isExist = m_ConfigDatas.TryGetValue(configName, out var value);
            if (isExist)
            {
                return;
            }

            m_ConfigDatas.TryAdd(configName, configValue);
        }

        /// <summary>
        /// 移除指定全局配置项。
        /// </summary>
        /// <param name="configName">要移除全局配置项的名称。</param>
        
        public bool RemoveConfig(string configName)
        {
            if (!HasConfig(configName))
            {
                return false;
            }

            return m_ConfigDatas.TryRemove(configName, out _);
        }

        /// <summary>
        /// 获取指定全局配置项。
        /// </summary>
        /// <param name="configName">要获取全局配置项的名称。</param>
        /// <returns>要获取全局配置项的全局配置项。</returns>
        
        public IDataTable GetConfig(string configName)
        {
            return m_ConfigDatas.TryGetValue(configName, out var value) ? value : null; //GetConfig()
        }

        /// <summary>
        /// 清空所有全局配置项。
        /// </summary>
        
        public void RemoveAllConfigs()
        {
            m_ConfigDatas.Clear();
        }
    }
}