using System;
using System.IO;
using UnityEngine;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Setting.Runtime
{
    /// <summary>
    /// 默认游戏配置辅助器。
    /// 功能：
    /// 1. 加载/保存游戏配置。
    /// 2. 获取/设置游戏配置项。
    /// 3. 序列化/反序列化游戏配置。
    /// 注意：对象的序列化和反序列化使用Json字符串作为Value保存。
    /// </summary>
    public class DefaultSettingHelper : SettingHelperBase
    {
        /// <summary>
        /// Setting配置文件名。
        /// </summary>
        private const string SettingFileName = "GameFrameworkSetting.dat";

        /// <summary>
        /// 获取游戏配置存储文件路径。
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// 获取游戏配置。
        /// </summary>
        public DefaultSetting Setting { get; private set; }

        /// <summary>
        /// 获取游戏配置序列化器。
        /// </summary>
        public DefaultSettingSerializer Serializer { get; private set; }

        /// <summary>
        /// 获取游戏配置项数量。
        /// </summary>
        public override int Count => Setting?.Count ?? 0;

        private void Awake()
        {
            FilePath   = Utility.Path.GetRegularPath(Path.Combine(Application.persistentDataPath, SettingFileName));
            Setting    = new DefaultSetting();
            Serializer = new DefaultSettingSerializer();
            Serializer.RegisterSerializeCallback(0, SerializeDefaultSettingCallback);
            Serializer.RegisterDeserializeCallback(0, DeserializeDefaultSettingCallback);
        }

        /// <summary>
        /// 加载游戏配置。
        /// </summary>
        /// <returns>是否加载游戏配置成功。</returns>
        public override bool Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return true;
                using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                Serializer.Deserialize(fileStream);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("加载配置失败：'{0}'.", exception);
                return false;
            }
        }

        /// <summary>
        /// 保存游戏配置。
        /// </summary>
        /// <returns>是否保存游戏配置成功。</returns>
        public override bool Save()
        {
            try
            {
                using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
                return Serializer.Serialize(fileStream, Setting);
            }
            catch (Exception exception)
            {
                Log.Warning("保存配置失败：'{0}'.", exception);
                return false;
            }
        }

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <returns>所有游戏配置项的名称。</returns>
        public override string[] GetAllSettingNames() => Setting.GetAllSettingNames();

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <param name="results">所有游戏配置项的名称。</param>
        public override void GetAllSettingNames(List<string> results) => Setting.GetAllSettingNames(results);

        /// <summary>
        /// 检查是否存在指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要检查游戏配置项的名称。</param>
        /// <returns>指定的游戏配置项是否存在。</returns>
        public override bool HasSetting(string settingName) => Setting.HasSetting(settingName);

        /// <summary>
        /// 移除指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要移除游戏配置项的名称。</param>
        /// <returns>是否移除指定游戏配置项成功。</returns>
        public override bool RemoveSetting(string settingName) => Setting.RemoveSetting(settingName);

        /// <summary>
        /// 清空所有游戏配置项。
        /// </summary>
        public override void RemoveAllSettings() => Setting.RemoveAllSettings();

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的布尔值。</returns>
        public override bool GetBool(string settingName) => Setting.GetBool(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public override bool GetBool(string settingName, bool defaultValue) => Setting.GetBool(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入布尔值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public override void SetBool(string settingName, bool value) => Setting.SetBool(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的整数值。</returns>
        public override int GetInt(string settingName) => Setting.GetInt(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public override int GetInt(string settingName, int defaultValue) => Setting.GetInt(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入整数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public override void SetInt(string settingName, int value) => Setting.SetInt(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的浮点数值。</returns>
        public override float GetFloat(string settingName) => Setting.GetFloat(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public override float GetFloat(string settingName, float defaultValue) => Setting.GetFloat(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入浮点数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public override void SetFloat(string settingName, float value) => Setting.SetFloat(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的字符串值。</returns>
        public override string GetString(string settingName) => Setting.GetString(settingName);

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的字符串值。</returns>
        public override string GetString(string settingName, string defaultValue) => Setting.GetString(settingName, defaultValue);

        /// <summary>
        /// 向指定游戏配置项写入字符串值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public override void SetString(string settingName, string value) => Setting.SetString(settingName, value);

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的对象。</returns>
        public override T GetObject<T>(string settingName) => Utility.Json.ToObject<T>(GetString(settingName));

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的对象。</returns>
        public override object GetObject(Type objectType, string settingName) => Utility.Json.ToObject(objectType, GetString(settingName));

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultObj">当指定的游戏配置项不存在时，返回此默认对象。</param>
        /// <returns>读取的对象。</returns>
        public override T GetObject<T>(string settingName, T defaultObj)
        {
            var json = GetString(settingName, null);
            return json.IsNullOrWhiteSpace() ? defaultObj : Utility.Json.ToObject<T>(json);
        }

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultObj">当指定的游戏配置项不存在时，返回此默认对象。</param>
        /// <returns>读取的对象。</returns>
        public override object GetObject(Type objectType, string settingName, object defaultObj)
        {
            var json = GetString(settingName, null);
            return json.IsNullOrWhiteSpace() ? defaultObj : Utility.Json.ToObject(objectType, json);
        }

        /// <summary>
        /// 向指定游戏配置项写入对象。
        /// </summary>
        /// <typeparam name="T">要写入对象的类型。</typeparam>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public override void SetObject<T>(string settingName, T obj) => SetString(settingName, Utility.Json.ToJson(obj));

        /// <summary>
        /// 向指定游戏配置项写入对象。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public override void SetObject(string settingName, object obj) => SetString(settingName, Utility.Json.ToJson(obj));

        /// <summary>
        /// 序列化默认配置回调函数。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="defaultSetting"></param>
        /// <returns></returns>
        private bool SerializeDefaultSettingCallback(Stream stream, DefaultSetting defaultSetting)
        {
            Setting.Serialize(stream);
            return true;
        }

        /// <summary>
        /// 反序列化默认配置回调函数。
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private DefaultSetting DeserializeDefaultSettingCallback(Stream stream)
        {
            Setting.Deserialize(stream);
            return Setting;
        }
    }
}