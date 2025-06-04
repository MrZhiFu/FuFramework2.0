//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using GameFrameX.Asset.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using GameFrameX.Setting.Runtime;
using UnityEngine;

namespace GameFrameX.Localization.Runtime
{
    /// <summary>
    /// 本地化组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Localization")]
    public sealed class LocalizationComponent : GameFrameworkComponent
    {
        private ILocalizationManager m_LocalizationManager = null;

        private EventComponent m_EventComponent = null;
        private SettingComponent m_SettingComponent = null;

        [SerializeField] private Language m_EditorLanguage = Language.Unspecified;

        [SerializeField] private Language m_DefaultLanguage = Language.Unspecified;

        [SerializeField] private bool m_EnableLoadDictionaryUpdateEvent = false;
        [SerializeField] private bool m_IsEnableEditorMode = false;

        [SerializeField] private string m_LocalizationHelperTypeName = "GameFrameX.Localization.Runtime.DefaultLocalizationHelper";

        [SerializeField] private LocalizationHelperBase m_CustomLocalizationHelper = null;


        /// <summary>
        /// 获取或设置编辑器语言（仅编辑器内有效）。
        /// </summary>
        public Language EditorLanguage
        {
            get { return m_EditorLanguage; }
            set { m_EditorLanguage = value; }
        }

        /// <summary>
        /// 获取或设置 默认本地化语言。当加载本地化失败时使用。
        /// </summary>
        public Language DefaultLanguage
        {
            get
            {
                if (m_LocalizationManager.DefaultLanguage == Language.Unspecified)
                {
                    var value = m_SettingComponent.GetString(nameof(LocalizationComponent) + "." + nameof(DefaultLanguage));
                    if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out Language result))
                    {
                        m_LocalizationManager.DefaultLanguage = result;
                    }
                }

                return m_LocalizationManager.DefaultLanguage;
            }
            set
            {
                if (m_LocalizationManager.DefaultLanguage != value)
                {
                    m_LocalizationManager.DefaultLanguage = value;
                    m_SettingComponent.SetString(nameof(LocalizationComponent) + "." + nameof(DefaultLanguage), value.ToString());
                    m_SettingComponent.Save();
                }
            }
        }

        /// <summary>
        /// 获取或设置本地化语言。
        /// </summary>
        public Language Language
        {
            get
            {
                if (m_LocalizationManager.Language == Language.Unspecified)
                {
                    var value = m_SettingComponent.GetString(nameof(LocalizationComponent) + "." + nameof(Language));
                    if (value.IsNotNullOrWhiteSpace() && Enum.TryParse(value, true, out Language result))
                    {
                        m_LocalizationManager.Language = result;
                    }
                }

                return m_LocalizationManager.Language;
            }
            set
            {
                if (m_LocalizationManager.Language != value)
                {
                    var oldLanguage = m_LocalizationManager.Language;
                    m_LocalizationManager.Language = value;
                    m_SettingComponent.SetString(nameof(LocalizationComponent) + "." + nameof(Language), value.ToString());
                    m_SettingComponent.Save();
                    var localizationLanguageChangeEventArgs = LocalizationLanguageChangeEventArgs.Create(oldLanguage, value);
                    m_EventComponent.Fire(this, localizationLanguageChangeEventArgs);
                }
            }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public Language SystemLanguage
        {
            get { return m_LocalizationManager.SystemLanguage; }
        }

        /// <summary>
        /// 获取字典数量。
        /// </summary>
        public int DictionaryCount
        {
            get { return m_LocalizationManager.DictionaryCount; }
        }

        /// <summary>
        /// 获取缓冲二进制流的大小。
        /// </summary>
        // public int CachedBytesSize
        // {
        //     get { return m_LocalizationManager.CachedBytesSize; }
        // }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(ILocalizationManager);
            base.Awake();
            m_LocalizationManager = GameFrameworkEntry.GetModule<ILocalizationManager>();
            if (m_LocalizationManager == null)
            {
                Log.Fatal("Localization manager is invalid.");
                return;
            }

            /*m_LocalizationManager.ReadDataSuccess += OnReadDataSuccess;
            m_LocalizationManager.ReadDataFailure += OnReadDataFailure;

            if (m_EnableLoadDictionaryUpdateEvent)
            {
                m_LocalizationManager.ReadDataUpdate += OnReadDataUpdate;
            }*/
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            m_SettingComponent = GameEntry.GetComponent<SettingComponent>();
            if (m_SettingComponent == null)
            {
                Log.Fatal("Setting component is invalid.");
                return;
            }

            m_LocalizationManager.SetAssetManager(GameFrameworkEntry.GetModule<IAssetManager>());

            LocalizationHelperBase localizationHelper = Helper.CreateHelper(m_LocalizationHelperTypeName, m_CustomLocalizationHelper);
            if (localizationHelper == null)
            {
                Log.Error("Can not create localization helper.");
                return;
            }

            localizationHelper.name = "Localization Helper";
            Transform localizationHelperTransform = localizationHelper.transform;
            localizationHelperTransform.SetParent(this.transform);
            localizationHelperTransform.localScale = Vector3.one;

            m_LocalizationManager.SetLocalizationHelper(localizationHelper);
#if UNITY_EDITOR
            if (m_IsEnableEditorMode)
            {
                Language = EditorLanguage != Language.Unspecified ? EditorLanguage : m_LocalizationManager.SystemLanguage;
            }
#else
            if (m_LocalizationManager.Language == Language.Unspecified)
            {
                Language = m_LocalizationManager.SystemLanguage;
            }
#endif
            DefaultLanguage = m_DefaultLanguage != Language.Unspecified ? m_DefaultLanguage : m_LocalizationManager.SystemLanguage;
        }

        /*/// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key)
        {
            return m_LocalizationManager.GetString(key);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T">字典参数的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg">字典参数。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T>(string key, T arg)
        {
            return m_LocalizationManager.GetString(key, arg);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2>(string key, T1 arg1, T2 arg2)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3>(string key, T1 arg1, T2 arg2, T3 arg3)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6, T7 arg7)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6, T7 arg7, T8 arg8)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string key, T1 arg1, T2 arg2, T3 arg3,
            T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10,
                                                   arg11);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string key, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10,
                                                   arg11, arg12);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string key, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10,
                                                   arg11, arg12, arg13);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <typeparam name="T14">字典参数 14 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <param name="arg14">字典参数 14。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string key, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12,
            T13 arg13, T14 arg14)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10,
                                                   arg11, arg12, arg13, arg14);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <typeparam name="T14">字典参数 14 的类型。</typeparam>
        /// <typeparam name="T15">字典参数 15 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <param name="arg14">字典参数 14。</param>
        /// <param name="arg15">字典参数 15。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string key, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12,
            T13 arg13, T14 arg14, T15 arg15)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10,
                                                   arg11, arg12, arg13, arg14, arg15);
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <typeparam name="T14">字典参数 14 的类型。</typeparam>
        /// <typeparam name="T15">字典参数 15 的类型。</typeparam>
        /// <typeparam name="T16">字典参数 16 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <param name="arg14">字典参数 14。</param>
        /// <param name="arg15">字典参数 15。</param>
        /// <param name="arg16">字典参数 16。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string key,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11,
            T12 arg12, T13 arg13, T14 arg14,
            T15 arg15, T16 arg16)
        {
            return m_LocalizationManager.GetString(key, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10,
                                                   arg11, arg12, arg13, arg14, arg15, arg16);
        }

        /// <summary>
        /// 是否存在字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否存在字典。</returns>
        public bool HasRawString(string key)
        {
            return m_LocalizationManager.HasRawString(key);
        }

        /// <summary>
        /// 根据字典主键获取字典值。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>字典值。</returns>
        public string GetRawString(string key)
        {
            return m_LocalizationManager.GetRawString(key);
        }

        /// <summary>
        /// 移除字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否移除字典成功。</returns>
        public bool RemoveRawString(string key)
        {
            return m_LocalizationManager.RemoveRawString(key);
        }

        /// <summary>
        /// 清空所有字典。
        /// </summary>
        public void RemoveAllRawStrings()
        {
            m_LocalizationManager.RemoveAllRawStrings();
        }*/

        /*
        private void OnReadDataSuccess(object sender, ReadDataSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, LoadDictionarySuccessEventArgs.Create(e));
        }

        private void OnReadDataFailure(object sender, ReadDataFailureEventArgs e)
        {
            Log.Warning("Load dictionary failure, asset name '{0}', error message '{1}'.", e.DataAssetName,
                e.ErrorMessage);
            m_EventComponent.Fire(this, LoadDictionaryFailureEventArgs.Create(e));
        }

        private void OnReadDataUpdate(object sender, ReadDataUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, LoadDictionaryUpdateEventArgs.Create(e));
        }*/
    }
}