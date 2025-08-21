using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Setting.Runtime
{
    /// <summary>
    /// 游戏配置组件。
    /// 功能：通过游戏配置管理器，管理游戏相关的本地存储配置项。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Setting")]
    public sealed class SettingComponent : FuComponent
    {
        /// 游戏配置管理器。
        private ISettingManager m_SettingManager;

        [Header("配置帮助器类型名")]
        [SerializeField] private string m_SettingHelperTypeName = "FuFramework.Setting.Runtime.DefaultSettingHelper";

        [Header("自定义配置帮助器类型名")]
        [SerializeField] private SettingHelperBase m_CustomSettingHelper;

        /// <summary>
        /// 获取游戏配置项数量。
        /// </summary>
        public int Count => m_SettingManager.Count;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(ISettingManager);
            base.Awake();
            m_SettingManager = FuEntry.GetModule<ISettingManager>();
            if (m_SettingManager == null)
            {
                Log.Fatal("配置管理器未找到.");
                return;
            }

            var settingHelper = Helper.CreateHelper(m_SettingHelperTypeName, m_CustomSettingHelper);
            if (!settingHelper)
            {
                Log.Error("创建配置帮助器失败.");
                return;
            }

            settingHelper.name = "SettingHelper";
            var helperTrs = settingHelper.transform;
            helperTrs.SetParent(this.transform);
            helperTrs.localScale = Vector3.one;

            m_SettingManager.SetSettingHelper(settingHelper);
        }

        private void Start()
        {
            if (m_SettingManager.Load()) return;
            Log.Error("加载配置失败.");
        }

        /// <summary>
        /// 保存游戏配置。
        /// </summary>
        public void Save() => m_SettingManager.Save();

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <returns>所有游戏配置项的名称。</returns>
        public string[] GetAllSettingNames() => m_SettingManager.GetAllSettingNames();

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <param name="results">所有游戏配置项的名称。</param>
        public void GetAllSettingNames(List<string> results) => m_SettingManager.GetAllSettingNames(results);

        /// <summary>
        /// 检查是否存在指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要检查游戏配置项的名称。</param>
        /// <returns>指定的游戏配置项是否存在。</returns>
        public bool HasSetting(string settingName) => m_SettingManager.HasSetting(settingName);

        /// <summary>
        /// 移除指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要移除游戏配置项的名称。</param>
        public void RemoveSetting(string settingName) => m_SettingManager.RemoveSetting(settingName);

        /// <summary>
        /// 清空所有游戏配置项。
        /// </summary>
        public void RemoveAllSettings() => m_SettingManager.RemoveAllSettings();

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string settingName) => m_SettingManager.GetBool(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string settingName, bool defaultValue) => m_SettingManager.GetBool(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入布尔值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public void SetBool(string settingName, bool value) => m_SettingManager.SetBool(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string settingName) => m_SettingManager.GetInt(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string settingName, int defaultValue) => m_SettingManager.GetInt(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入整数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public void SetInt(string settingName, int value) => m_SettingManager.SetInt(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string settingName) => m_SettingManager.GetFloat(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string settingName, float defaultValue) => m_SettingManager.GetFloat(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入浮点数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public void SetFloat(string settingName, float value) => m_SettingManager.SetFloat(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string settingName) => m_SettingManager.GetString(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string settingName, string defaultValue) => m_SettingManager.GetString(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入字符串值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public void SetString(string settingName, string value) => m_SettingManager.SetString(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的对象。</returns>
        public T GetObject<T>(string settingName) where T : class, new() => m_SettingManager.GetObject<T>(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的对象。</returns>
        public object GetObject(Type objectType, string settingName) => m_SettingManager.GetObject(objectType, settingName);

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultObj">当指定的游戏配置项不存在时，返回此默认对象。</param>
        /// <returns>读取的对象。</returns>
        public T GetObject<T>(string settingName, T defaultObj) where T : class, new() => m_SettingManager.GetObject(settingName, defaultObj);

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultObj">当指定的游戏配置项不存在时，返回此默认对象。</param>
        /// <returns>读取的对象。</returns>
        public object GetObject(Type objectType, string settingName, object defaultObj) => m_SettingManager.GetObject(objectType, settingName, defaultObj);

        /// <summary>
        /// 向指定游戏配置项写入对象。
        /// </summary>
        /// <typeparam name="T">要写入对象的类型。</typeparam>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public void SetObject<T>(string settingName, T obj) => m_SettingManager.SetObject(settingName, obj);

        /// <summary>
        /// 向指定游戏配置项写入对象。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public void SetObject(string settingName, object obj) => m_SettingManager.SetObject(settingName, obj);
    }
}