using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Setting.Runtime
{
    /// <summary>
    /// 默认游戏配置。
    /// </summary>
    public sealed class DefaultSetting
    {
        /// <summary>
        /// 记录所有游戏配置项的字典。key为配置项名称，value为配置项值。
        /// </summary>
        private readonly SortedDictionary<string, string> m_SettingDict = new(StringComparer.Ordinal);

        /// <summary>
        /// 获取游戏配置项数量。
        /// </summary>
        public int Count => m_SettingDict.Count;

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <returns>所有游戏配置项的名称。</returns>
        public string[] GetAllSettingNames()
        {
            var index           = 0;
            var allSettingNames = new string[m_SettingDict.Count];
            foreach (var setting in m_SettingDict)
            {
                allSettingNames[index++] = setting.Key;
            }

            return allSettingNames;
        }

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <param name="results">所有游戏配置项的名称。</param>
        public void GetAllSettingNames(List<string> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            results.AddRange(m_SettingDict.Select(setting => setting.Key));
        }

        /// <summary>
        /// 检查是否存在指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要检查游戏配置项的名称。</param>
        /// <returns>指定的游戏配置项是否存在。</returns>
        public bool HasSetting(string settingName) => m_SettingDict.ContainsKey(settingName);

        /// <summary>
        /// 移除指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要移除游戏配置项的名称。</param>
        /// <returns>是否移除指定游戏配置项成功。</returns>
        public bool RemoveSetting(string settingName) => m_SettingDict.Remove(settingName);

        /// <summary>
        /// 清空所有游戏配置项。
        /// </summary>
        public void RemoveAllSettings() => m_SettingDict.Clear();

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string settingName)
        {
            if (m_SettingDict.TryGetValue(settingName, out var value)) return int.Parse(value) != 0;
            Log.Warning("配置项 '{0}' 不存在!", settingName);
            return false;
        }

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string settingName, bool defaultValue)
        {
            return m_SettingDict.TryGetValue(settingName, out var value) ? int.Parse(value) != 0 : defaultValue;
        }

        /// <summary>
        /// 向指定游戏配置项写入布尔值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public void SetBool(string settingName, bool value) => m_SettingDict[settingName] = value ? "1" : "0";

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string settingName)
        {
            if (m_SettingDict.TryGetValue(settingName, out var value)) return int.Parse(value);
            Log.Warning("配置项 '{0}' 不存在!", settingName);
            return 0;
        }

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string settingName, int defaultValue)
        {
            return m_SettingDict.TryGetValue(settingName, out var value) ? int.Parse(value) : defaultValue;
        }

        /// <summary>
        /// 向指定游戏配置项写入整数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public void SetInt(string settingName, int value) => m_SettingDict[settingName] = value.ToString();

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string settingName)
        {
            if (m_SettingDict.TryGetValue(settingName, out var value)) return float.Parse(value);
            Log.Warning("配置项 '{0}' 不存在!", settingName);
            return 0f;
        }

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string settingName, float defaultValue)
        {
            return m_SettingDict.TryGetValue(settingName, out var value) ? float.Parse(value) : defaultValue;
        }

        /// <summary>
        /// 向指定游戏配置项写入浮点数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public void SetFloat(string settingName, float value)
        {
            m_SettingDict[settingName] = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string settingName)
        {
            if (m_SettingDict.TryGetValue(settingName, out var value)) return value;
            Log.Warning("配置项 '{0}' 不存在!", settingName);
            return null;
        }

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string settingName, string defaultValue)
        {
            return m_SettingDict.GetValueOrDefault(settingName, defaultValue);
        }

        /// <summary>
        /// 向指定游戏配置项写入字符串值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public void SetString(string settingName, string value) => m_SettingDict[settingName] = value;

        /// <summary>
        /// 序列化数据。
        /// </summary>
        /// <param name="stream">目标流。</param>
        public void Serialize(Stream stream)
        {
            using var binaryWriter = new BinaryWriter(stream, Encoding.UTF8);
            binaryWriter.Write7BitEncodedInt32(m_SettingDict.Count);
            foreach (var (key, value) in m_SettingDict)
            {
                binaryWriter.Write(key);
                binaryWriter.Write(value);
            }
        }

        /// <summary>
        /// 反序列化数据。
        /// </summary>
        /// <param name="stream">指定流。</param>
        public void Deserialize(Stream stream)
        {
            m_SettingDict.Clear();
            using var binaryReader = new BinaryReader(stream, Encoding.UTF8);
            var       settingCount = binaryReader.Read7BitEncodedInt32();
            for (var i = 0; i < settingCount; i++)
            {
                m_SettingDict.Add(binaryReader.ReadString(), binaryReader.ReadString());
            }
        }
    }
}