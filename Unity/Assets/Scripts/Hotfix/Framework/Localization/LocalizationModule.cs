using System;
using UnityEngine;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using AOT.Framework.Localization;
using Hotfix.Framework.Event;
using Hotfix.Framework.Storage;

namespace Hotfix.Framework.Localization
{
    /// <summary>
    /// 本地化管理模块。
    /// 功能：
    ///     1. 获取/设置当前使用的语言。
    ///     2. 配合数据保存模块，保存当前使用的语言设置。
    ///     3. 配合事件管理模块，发送本地化语言改变事件。
    ///     4. 使用指定的具体本地化多语言提供器获取本地化多语言字符串。
    /// </summary>
    public sealed class LocalizationModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static LocalizationModule Instance { get; private set; }

        /// <summary>
        /// 事件管理模块
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// 数据保存模块
        /// </summary>
        private StorageModule _storageModule;

        /// <summary>
        /// 当前使用的语言
        /// </summary>
        private ELanguage m_Language;

        /// <summary>
        /// 本地化多语言提供者
        /// </summary>
        public ILocalizationProvider LocalizationProvider { get; set; }

        /// <summary>
        /// 获取或设置当前使用的语言。
        /// </summary>
        public ELanguage Language
        {
            get => m_Language;
            set
            {
                if (value == ELanguage.Unspecified) throw new InvalidOperationException("[LocalizationModule]设置本地化语言失败，语言未指定.");
                if (value == m_Language) return;
                var oldLanguage = m_Language;
                m_Language = value;

                // 保存设置（数据保存模块缺失时跳过保存，语言切换本身仍生效）
                if (_storageModule != null)
                {
                    _storageModule.SetString("Language", value.ToString());
                    _storageModule.Save();
                }

                // 发送本地化语言改变事件
                var languageChangeEventArgs = LanguageChangeEventArgs.Create(oldLanguage, value);
                m_EventModule.Broadcast(this, languageChangeEventArgs);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;

            m_EventModule = ModuleManager.GetModule<EventModule>();
            _storageModule = StorageModule.Instance;

            // 数据保存模块缺失（模块注册顺序调整/初始化失败）时退化为系统语言，不能直接解引用（否则 NRE 中断本模块初始化）
            if (_storageModule == null)
            {
                FuLogger.LogError("[LocalizationModule] 初始化失败，数据保存模块未找到，语言设置将无法读取与保存!");
                m_Language = ELanguageHelper.FromSystemLanguage(Application.systemLanguage);
                return;
            }

            var value = _storageModule.GetString("Language");
            if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out ELanguage result))
                m_Language = result;
            else
                m_Language = ELanguageHelper.FromSystemLanguage(Application.systemLanguage);
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            Instance = null;
        }

        /// <summary>
        /// 获取当前语言下的多语言文本
        /// </summary>
        /// <param name="key">多语言key</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public string GetLanguageText(string key, params object[] args)
        {
            if (LocalizationProvider is null)
            {
                FuLogger.LogWarning("[LocalizationModule] 本地化多语言提供者未设置，请先设置");
                return $"[{key}]";
            }

            var result = LocalizationProvider.GetLanguage(key, args);
            return result.IsNullOrEmpty() ? $"[{key}]" : result;
        }
    }
}
