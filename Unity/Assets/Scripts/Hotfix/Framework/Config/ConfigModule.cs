using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Config
{
    /// <summary>
    /// 配置管理模块。
    /// 功能：
    ///     1. 存储所有配置表。
    ///     2. 配置表在启动期一次性加载，加载后只读。
    /// </summary>
    public sealed partial class ConfigModule : ModuleBase
    {
        /// <summary>
        /// 配置表字典。key为配置表名称，value为配置表数据。
        /// 配置在启动期一次性加载、加载后只读，故使用普通 Dictionary 保证读取路径最快。
        /// </summary>
        private readonly Dictionary<string, IDataTable> m_CfgDataDict = new(StringComparer.Ordinal);

        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
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
    }
}
