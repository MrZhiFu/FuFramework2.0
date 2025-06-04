using System;
using System.Collections.Generic;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.GlobalConfig.Runtime
{
    /// <summary>
    /// 全局配置组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Global Config")]
    public sealed class GlobalConfigComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 检测App版本地址接口
        /// </summary>
        [SerializeField] private string m_CheckAppVersionUrl = string.Empty;

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
        [SerializeField] private string m_CheckResourceVersionUrl = string.Empty;

        /// <summary>
        /// 检测资源版本地址接口
        /// </summary>
        public string CheckResourceVersionUrl
        {
            get => m_CheckResourceVersionUrl;
            set => m_CheckResourceVersionUrl = value;
        }

        /// <summary>
        /// AOT代码列表
        /// </summary>
        [SerializeField] private string m_AOTCodeList = string.Empty;

        /// <summary>
        /// AOT补充元数据列表
        /// </summary>
        [SerializeField] private List<string> m_AOTCodeLists = new List<string>();

        /// <summary>
        /// 补充元数据列表
        /// </summary>
        public List<string> AOTCodeLists
        {
            get { return m_AOTCodeLists; }
        }

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
        [SerializeField] private string m_Content = string.Empty;

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
        [SerializeField] private string m_HostServerUrl = string.Empty;

        /// <summary>
        /// 主机服务地址
        /// </summary>
        public string HostServerUrl
        {
            get => m_HostServerUrl;
            set => m_HostServerUrl = value;
        }

        protected override void Awake()
        {
            IsAutoRegister = false;
            base.Awake();
        }
    }
}