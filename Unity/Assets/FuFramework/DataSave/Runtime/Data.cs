using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.SaveData.Runtime
{
    /// <summary>
    /// 本地存储的数据。
    /// 功能：
    /// 1. 序列化/反序列化本地存储的数据。
    /// 2. 读写本地存储的数据项。
    /// 3. 维护一个本地存储的数据项的字典。
    /// 4. 所有类型都被序列化为字符串。
    /// </summary>
    public sealed class Data
    {
        /// <summary>
        /// 记录本地存储的数据项的字典。key为数据项名称，value为数据项值。
        /// </summary>
        private readonly SortedDictionary<string, string> m_DataDict = new(StringComparer.Ordinal);

        /// <summary>
        /// 获取本地存储的数据项数量。
        /// </summary>
        public int Count => m_DataDict.Count;

        /// <summary>
        /// 序列化数据。
        /// </summary>
        /// <param name="stream">目标流。</param>
        public void Serialize(Stream stream)
        {
            using var binaryWriter = new BinaryWriter(stream, Encoding.UTF8);
            binaryWriter.Write7BitEncodedInt32(m_DataDict.Count);
            foreach (var (key, value) in m_DataDict)
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
            m_DataDict.Clear();
            using var binaryReader = new BinaryReader(stream, Encoding.UTF8);
            var settingCount = binaryReader.Read7BitEncodedInt32();
            for (var i = 0; i < settingCount; i++)
            {
                m_DataDict.Add(binaryReader.ReadString(), binaryReader.ReadString());
            }
        }

        /// <summary>
        /// 获取所有本地存储的数据项的名称。
        /// </summary>
        /// <returns>所有本地存储的数据项的名称。</returns>
        public string[] GetAllDataNames()
        {
            var index = 0;
            var allNames = new string[m_DataDict.Count];
            foreach (var setting in m_DataDict)
            {
                allNames[index++] = setting.Key;
            }

            return allNames;
        }

        /// <summary>
        /// 获取所有本地存储的数据项的名称。
        /// </summary>
        /// <param name="results">所有本地存储的数据项的名称。</param>
        public void GetAllDataNames(List<string> results)
        {
            if (results == null) throw new FuException("[Data] 结果列表不能为空.");
            results.Clear();
            results.AddRange(m_DataDict.Select(setting => setting.Key));
        }

        /// <summary>
        /// 检查是否存在指定本地存储的数据项。
        /// </summary>
        /// <param name="dataName">要检查本地存储的数据项的名称。</param>
        /// <returns>指定的本地存储的数据项是否存在。</returns>
        public bool HasData(string dataName) => m_DataDict.ContainsKey(dataName);

        /// <summary>
        /// 移除指定本地存储的数据项。
        /// </summary>
        /// <param name="dataName">要移除本地存储的数据项的名称。</param>
        /// <returns>是否移除指定本地存储的数据项成功。</returns>
        public bool RemoveData(string dataName) => m_DataDict.Remove(dataName);

        /// <summary>
        /// 清空所有本地存储的数据项。
        /// </summary>
        public void RemoveAllData() => m_DataDict.Clear();

        #region Get

        /// <summary>
        /// 从指定本地存储的数据项中读取布尔值。
        /// </summary>
        /// <param name="dataName">要获取本地存储的数据项的名称。</param>
        /// <param name="defaultValue">当指定的本地存储的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string dataName, bool defaultValue = false)
        {
            if (!m_DataDict.TryGetValue(dataName, out var value)) return defaultValue;
            if (bool.TryParse(value, out var result)) return result;
            throw new FuException($"[Data] 无法将 {value} 转换为布尔值.");
        }

        /// <summary>
        /// 从指定本地存储的数据项中读取整数值。
        /// </summary>
        /// <param name="dataName">要获取本地存储的数据项的名称。</param>
        /// <param name="defaultValue">当指定的本地存储的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string dataName, int defaultValue = 0)
        {
            if (!m_DataDict.TryGetValue(dataName, out var value)) return defaultValue;
            if (int.TryParse(value, out var result)) return result;
            throw new FuException($"[Data] 无法将 {value} 转换为整数值.");
        }
        
        /// <summary>
        /// 从指定本地存储的数据项中读取长整数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        public long GetLong(string dataName, long defaultValue = 0)
        {
            if (!m_DataDict.TryGetValue(dataName, out var value)) return defaultValue;
            if (long.TryParse(value, out var result)) return result;
            throw new FuException($"[Data] 无法将 {value} 转换为长整数值.");
        }

        /// <summary>
        /// 从指定本地存储的数据项中读取浮点数值。
        /// </summary>
        /// <param name="dataName">要获取本地存储的数据项的名称。</param>
        /// <param name="defaultValue">当指定的本地存储的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string dataName, float defaultValue = 0)
        {
            if (!m_DataDict.TryGetValue(dataName, out var value)) return defaultValue;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
            throw new FuException($"[Data] 无法将 {value} 转换为浮点数值.");
        }

        /// <summary>
        /// 从指定本地存储的数据项中读取双精度浮点数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        public double GetDouble(string dataName, double defaultValue = 0)
        {
            if (!m_DataDict.TryGetValue(dataName, out var value)) return defaultValue;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
            throw new FuException($"[Data] 无法将 {value} 转换为双精度浮点数值.");
        }
        
        /// <summary>
        /// 从指定本地存储的数据项中读取字符串值。
        /// </summary>
        /// <param name="dataName">要获取本地存储的数据项的名称。</param>
        /// <param name="defaultValue">当指定的本地存储的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string dataName, string defaultValue = null)
        {
            return m_DataDict.GetValueOrDefault(dataName, defaultValue);
        }

        #endregion

        #region Set

        /// <summary>
        /// 向指定本地存储的数据项写入布尔值。
        /// </summary>
        /// <param name="dataName">要写入本地存储的数据项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public void SetBool(string dataName, bool value) => m_DataDict[dataName] = value.ToString();

        /// <summary>
        /// 向指定本地存储的数据项写入整数值。
        /// </summary>
        /// <param name="dataName">要写入本地存储的数据项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public void SetInt(string dataName, int value) => m_DataDict[dataName] = value.ToString();

        /// <summary>
        /// 向指定本地存储的数据项写入长整数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        public void SetLong(string dataName, long value) => m_DataDict[dataName] = value.ToString();
        
        /// <summary>
        /// 向指定本地存储的数据项写入浮点数值。
        /// </summary>
        /// <param name="dataName">要写入本地存储的数据项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public void SetFloat(string dataName, float value) => m_DataDict[dataName] = value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// 向指定本地存储的数据项写入双精度浮点数值。
        /// </summary>
        public void SetDouble(string dataName, double value) => m_DataDict[dataName] = value.ToString(CultureInfo.InvariantCulture);
        
        /// <summary>
        /// 向指定本地存储的数据项写入字符串值。
        /// </summary>
        /// <param name="dataName">要写入本地存储的数据项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public void SetString(string dataName, string value) => m_DataDict[dataName] = value;

        #endregion
    }
}