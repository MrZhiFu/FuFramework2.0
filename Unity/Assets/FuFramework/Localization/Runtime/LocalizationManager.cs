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
    [ModuleDependency(typeof(EventManager), typeof(DataSaveManager))]
    public sealed class LocalizationManager : FuModule
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;
        
        private EventManager m_EventManager; // 事件组件
        private DataSaveManager m_dataSaveManager; // Setting组件

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
                m_dataSaveManager.SetString("Language", value.ToString());
                m_dataSaveManager.Save();

                // 发送本地化语言改变事件
                var localizationLanguageChangeEventArgs = LocalizationLanguageChangeEventArgs.Create(oldLanguage, value);
                m_EventManager.Fire(this, localizationLanguageChangeEventArgs);
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
            m_EventManager = ModuleManager.GetModule<EventManager>();
            m_dataSaveManager = ModuleManager.GetModule<DataSaveManager>();
            
            var value = m_dataSaveManager.GetString("Language");
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