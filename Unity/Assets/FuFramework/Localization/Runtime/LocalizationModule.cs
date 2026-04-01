using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.SaveData.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Runtime
{
    /// <summary>
    /// 本地化管理器。
    /// </summary>
    [ModuleDependency(typeof(EventModule), typeof(DataSaveModule))]
    public sealed class LocalizationModule : FuModule
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;

        /// <summary>
        /// 事件组件
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// Setting组件
        /// </summary>
        private DataSaveModule m_DataSaveModule;

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
                m_DataSaveModule.SetString("Language", value.ToString());
                m_DataSaveModule.Save();

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
            m_EventModule    = ModuleManager.GetModule<EventModule>();
            m_DataSaveModule = ModuleManager.GetModule<DataSaveModule>();

            var value = m_DataSaveModule.GetString("Language");
            if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out ELanguage result))
                m_Language = result;
            else
                m_Language = SystemLanguage;
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected override void OnDispose() { }

        /// <summary>
        /// 获取当前语言下的多语言文本
        /// </summary>
        /// <param name="key">多语言key</param>
        /// <returns></returns>
        public string GetLanguageText(string key)
        {
            return LocalizationProvider is null ? "" : LocalizationProvider.GetLanguage(key);
        }
    }
}