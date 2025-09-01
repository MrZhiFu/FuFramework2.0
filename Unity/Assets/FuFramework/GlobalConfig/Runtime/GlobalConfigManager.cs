using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace FuFramework.GlobalConfig.Runtime
{
    /// <summary>
    /// 全局配置组件。
    /// 功能：从服务器获取并提供游戏全局配置信息。包括：
    ///    - 检测App版本地址接口
    ///    - 检测资源版本地址接口
    ///    - 主机服务地址
    ///    - AOT代码列表
    ///    - AOT补充元数据列表
    ///    - 附加内容
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GlobalConfigManager : FuComponent
    {
        /// <summary>
        /// 检测App版本地址接口
        /// </summary>
        [SerializeField] private string m_CheckAppVersionUrl = string.Empty;

        /// <summary>
        /// 检测资源版本地址接口
        /// </summary>
        [SerializeField] private string m_CheckResourceVersionUrl = string.Empty;

        /// <summary>
        /// 主机服务地址
        /// </summary>
        [SerializeField] private string m_HostServerUrl = string.Empty;

        /// <summary>
        /// AOT代码列表
        /// </summary>
        [SerializeField] private string m_AOTCodeList = string.Empty;

        /// <summary>
        /// AOT补充元数据列表
        /// </summary>
        [SerializeField] private List<string> m_AOTCodeLists = new();

        /// <summary>
        /// 附加内容
        /// </summary>
        [SerializeField] private string m_Content = string.Empty;

        /// <summary>
        /// 检测App版本地址接口
        /// </summary>
        public string CheckAppVersionUrl
        {
            get => m_CheckAppVersionUrl;
            set => m_CheckAppVersionUrl = value;
        }

        /// <summary>
        /// 检测资源版本地址接口
        /// </summary>
        public string CheckResourceVersionUrl
        {
            get => m_CheckResourceVersionUrl;
            set => m_CheckResourceVersionUrl = value;
        }

        /// <summary>
        /// 补充元数据列表
        /// </summary>
        public List<string> AOTCodeLists => m_AOTCodeLists;

        /// <summary>
        /// AOT代码列表
        /// </summary>
        public string AOTCodeList
        {
            get => m_AOTCodeList;
            set
            {
                m_AOTCodeList = value;
                try
                {
                    m_AOTCodeLists = Utility.Json.ToObject<List<string>>(value);
                }
                catch (Exception e)
                {
                    Log.Fatal(e);
                }
            }
        }

        /// <summary>
        /// 附加内容
        /// </summary>
        public string Content
        {
            get => m_Content;
            set => m_Content = value;
        }

        /// <summary>
        /// 主机服务地址
        /// </summary>
        public string HostServerUrl
        {
            get => m_HostServerUrl;
            set => m_HostServerUrl = value;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType) { }
    }
}