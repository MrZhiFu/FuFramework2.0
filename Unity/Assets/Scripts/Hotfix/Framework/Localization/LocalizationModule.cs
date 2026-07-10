using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using Hotfix.Storage;

namespace Hotfix.Localization
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
                if (value == ELanguage.Unspecified) throw new FuException("[LocalizationModule]设置本地化语言失败，语言未指定.");
                if (value == m_Language) return;
                var oldLanguage = m_Language;
                m_Language = value;

                // 保存设置
                _storageModule.SetString("Language", value.ToString());
                _storageModule.Save();

                // 发送本地化语言改变事件
                var languageChangeEventArgs = LanguageChangeEventArgs.Create(oldLanguage, value);
                m_EventModule.Broadcast(this, languageChangeEventArgs);
            }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public static ELanguage SystemLanguage
        {
            get
            {
                return Application.systemLanguage switch
                {
                    // @formatter:off
                    UnityEngine.SystemLanguage.Afrikaans          => ELanguage.Afrikaans,
                    UnityEngine.SystemLanguage.Arabic             => ELanguage.Arabic,
                    UnityEngine.SystemLanguage.Basque             => ELanguage.Basque,
                    UnityEngine.SystemLanguage.Belarusian         => ELanguage.Belarusian,
                    UnityEngine.SystemLanguage.Bulgarian          => ELanguage.Bulgarian,
                    UnityEngine.SystemLanguage.Catalan            => ELanguage.Catalan,
                    UnityEngine.SystemLanguage.Chinese            => ELanguage.ChineseSimplified,
                    UnityEngine.SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
                    UnityEngine.SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
                    UnityEngine.SystemLanguage.Czech              => ELanguage.Czech,
                    UnityEngine.SystemLanguage.Danish             => ELanguage.Danish,
                    UnityEngine.SystemLanguage.Dutch              => ELanguage.Dutch,
                    UnityEngine.SystemLanguage.English            => ELanguage.English,
                    UnityEngine.SystemLanguage.Estonian           => ELanguage.Estonian,
                    UnityEngine.SystemLanguage.Faroese            => ELanguage.Faroese,
                    UnityEngine.SystemLanguage.Finnish            => ELanguage.Finnish,
                    UnityEngine.SystemLanguage.French             => ELanguage.French,
                    UnityEngine.SystemLanguage.German             => ELanguage.German,
                    UnityEngine.SystemLanguage.Greek              => ELanguage.Greek,
                    UnityEngine.SystemLanguage.Hebrew             => ELanguage.Hebrew,
                    UnityEngine.SystemLanguage.Hungarian          => ELanguage.Hungarian,
                    UnityEngine.SystemLanguage.Icelandic          => ELanguage.Icelandic,
                    UnityEngine.SystemLanguage.Indonesian         => ELanguage.Indonesian,
                    UnityEngine.SystemLanguage.Italian            => ELanguage.Italian,
                    UnityEngine.SystemLanguage.Japanese           => ELanguage.Japanese,
                    UnityEngine.SystemLanguage.Korean             => ELanguage.Korean,
                    UnityEngine.SystemLanguage.Latvian            => ELanguage.Latvian,
                    UnityEngine.SystemLanguage.Lithuanian         => ELanguage.Lithuanian,
                    UnityEngine.SystemLanguage.Norwegian          => ELanguage.Norwegian,
                    UnityEngine.SystemLanguage.Polish             => ELanguage.Polish,
                    UnityEngine.SystemLanguage.Portuguese         => ELanguage.PortuguesePortugal,
                    UnityEngine.SystemLanguage.Romanian           => ELanguage.Romanian,
                    UnityEngine.SystemLanguage.Russian            => ELanguage.Russian,
                    UnityEngine.SystemLanguage.SerboCroatian      => ELanguage.SerboCroatian,
                    UnityEngine.SystemLanguage.Slovak             => ELanguage.Slovak,
                    UnityEngine.SystemLanguage.Slovenian          => ELanguage.Slovenian,
                    UnityEngine.SystemLanguage.Spanish            => ELanguage.Spanish,
                    UnityEngine.SystemLanguage.Swedish            => ELanguage.Swedish,
                    UnityEngine.SystemLanguage.Thai               => ELanguage.Thai,
                    UnityEngine.SystemLanguage.Turkish            => ELanguage.Turkish,
                    UnityEngine.SystemLanguage.Ukrainian          => ELanguage.Ukrainian,
                    UnityEngine.SystemLanguage.Unknown            => ELanguage.Unspecified,
                    UnityEngine.SystemLanguage.Vietnamese         => ELanguage.Vietnamese,
                    _                                             => ELanguage.Unspecified
                    // @formatter:on
                };
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            Instance = this;

            m_EventModule = ModuleManager.GetModule<EventModule>();
            _storageModule = StorageModule.Instance;

            var value = _storageModule.GetString("Language");
            if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out ELanguage result))
                m_Language = result;
            else
                m_Language = SystemLanguage;
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected override void OnDispose()
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
