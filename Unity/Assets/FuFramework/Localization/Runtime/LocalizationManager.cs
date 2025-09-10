using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Setting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Runtime
{
    /// <summary>
    /// 本地化管理器。
    /// </summary>
    public sealed class LocalizationManager : FuComponent
    {
        private EventManager m_EventComponent; // 事件组件
        private SettingComponent m_SettingComponent; // Setting组件

        private ELanguage m_Language; // 本地化语言

        /// <summary>
        /// 获取或设置本地化语言。
        /// </summary>
        public ELanguage Language
        {
            get => m_Language;
            set
            {
                if (value == ELanguage.Unspecified) throw new FuException("设置本地化语言失败，语言未指定.");
                var oldLanguage = m_Language;
                m_Language = value;

                // 保存设置
                m_SettingComponent.SetString("Language", value.ToString());
                m_SettingComponent.Save();

                // 发送本地化语言改变事件
                var localizationLanguageChangeEventArgs = LocalizationLanguageChangeEventArgs.Create(oldLanguage, value);
                m_EventComponent.Fire(this, localizationLanguageChangeEventArgs);
            }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public ELanguage SystemELanguage
        {
            get
            {
                return Application.systemLanguage switch
                {
                    SystemLanguage.Afrikaans => ELanguage.Afrikaans,
                    SystemLanguage.Arabic => ELanguage.Arabic,
                    SystemLanguage.Basque => ELanguage.Basque,
                    SystemLanguage.Belarusian => ELanguage.Belarusian,
                    SystemLanguage.Bulgarian => ELanguage.Bulgarian,
                    SystemLanguage.Catalan => ELanguage.Catalan,
                    SystemLanguage.Chinese => ELanguage.ChineseSimplified,
                    SystemLanguage.ChineseSimplified => ELanguage.ChineseSimplified,
                    SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
                    SystemLanguage.Czech => ELanguage.Czech,
                    SystemLanguage.Danish => ELanguage.Danish,
                    SystemLanguage.Dutch => ELanguage.Dutch,
                    SystemLanguage.English => ELanguage.English,
                    SystemLanguage.Estonian => ELanguage.Estonian,
                    SystemLanguage.Faroese => ELanguage.Faroese,
                    SystemLanguage.Finnish => ELanguage.Finnish,
                    SystemLanguage.French => ELanguage.French,
                    SystemLanguage.German => ELanguage.German,
                    SystemLanguage.Greek => ELanguage.Greek,
                    SystemLanguage.Hebrew => ELanguage.Hebrew,
                    SystemLanguage.Hungarian => ELanguage.Hungarian,
                    SystemLanguage.Icelandic => ELanguage.Icelandic,
                    SystemLanguage.Indonesian => ELanguage.Indonesian,
                    SystemLanguage.Italian => ELanguage.Italian,
                    SystemLanguage.Japanese => ELanguage.Japanese,
                    SystemLanguage.Korean => ELanguage.Korean,
                    SystemLanguage.Latvian => ELanguage.Latvian,
                    SystemLanguage.Lithuanian => ELanguage.Lithuanian,
                    SystemLanguage.Norwegian => ELanguage.Norwegian,
                    SystemLanguage.Polish => ELanguage.Polish,
                    SystemLanguage.Portuguese => ELanguage.PortuguesePortugal,
                    SystemLanguage.Romanian => ELanguage.Romanian,
                    SystemLanguage.Russian => ELanguage.Russian,
                    SystemLanguage.SerboCroatian => ELanguage.SerboCroatian,
                    SystemLanguage.Slovak => ELanguage.Slovak,
                    SystemLanguage.Slovenian => ELanguage.Slovenian,
                    SystemLanguage.Spanish => ELanguage.Spanish,
                    SystemLanguage.Swedish => ELanguage.Swedish,
                    SystemLanguage.Thai => ELanguage.Thai,
                    SystemLanguage.Turkish => ELanguage.Turkish,
                    SystemLanguage.Ukrainian => ELanguage.Ukrainian,
                    SystemLanguage.Unknown => ELanguage.Unspecified,
                    SystemLanguage.Vietnamese => ELanguage.Vietnamese,
                    _ => ELanguage.Unspecified
                };
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            m_EventComponent = ModuleManager.GetModule<EventManager>();
            m_SettingComponent = ModuleManager.GetModule<SettingComponent>();
            
            var value = m_SettingComponent.GetString("Language");
            if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out ELanguage result))
                m_Language = result;
            else
                m_Language = SystemELanguage;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType) { }
    }
}