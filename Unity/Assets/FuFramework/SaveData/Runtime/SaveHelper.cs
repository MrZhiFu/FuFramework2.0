using System;
using System.IO;
using UnityEngine;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.SaveData.Runtime
{
    /// <summary>
    /// 数据存储辅助器。
    /// 功能：
    /// 1. 加载/保存数据。
    /// 2. 获取/数据数据。
    /// 3. 序列化/反序列化数据。
    /// 注意：每个实例对应一个特定的数据文件。
    /// </summary>
    public class SaveHelper : MonoBehaviour
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// 数据存储文件路径。
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// 待保存的数据。
        /// </summary>
        public Data Data { get; private set; }

        /// <summary>
        /// 数据序列化器。
        /// </summary>
        public FuSerializer<Data> Serializer { get; private set; }

        /// <summary>
        /// 获取数据项数量。
        /// </summary>
        public int Count => Data?.Count ?? 0;

        /// <summary>
        /// 初始化数据辅助器
        /// </summary>
        /// <param name="fileName">文件名</param>
        public void Init(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("[SaveHelper] 文件名不能为空");
            FileName = fileName;
            var path = Path.Combine(Application.persistentDataPath, SaveManager.DirRoot, fileName);
            FilePath = Utility.Path.GetRegularPath(path);
            Data = new Data();
            Serializer = new DataSerializer();
            Serializer.RegisterSerializeCallback(0, DefaultSerializeCallback);
            Serializer.RegisterDeserializeCallback(0, DefaultDeserializeCallback);
        }

        /// <summary>
        /// 默认序列化数据回调函数。
        /// </summary>
        private bool DefaultSerializeCallback(Stream stream, Data data)
        {
            Data.Serialize(stream);
            return true;
        }

        /// <summary>
        /// 默认反序列化数据回调函数。
        /// </summary>
        private Data DefaultDeserializeCallback(Stream stream)
        {
            Data.Deserialize(stream);
            return Data;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <returns>是否加载数据成功。</returns>
        public bool Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    FuLog.Warning($"[SaveHelper] 加载数据文件失败，文件不存在: {FileName}");
                    return false;
                }

                using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                Serializer.Deserialize(fileStream);
                FuLog.Info($"[SaveHelper] 加载数据文件成功: {FileName}");
                return true;
            }
            catch (Exception exception)
            {
                FuLog.Warning($"[SaveHelper] 加载数据文件失败 {FileName}：'{exception}'.");
                return false;
            }
        }

        /// <summary>
        /// 保存数据。
        /// </summary>
        /// <returns>是否保存数据成功。</returns>
        public bool Save()
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
                var result = Serializer.Serialize(fileStream, Data);
                if (result)
                {
                    FuLog.Info($"[SaveHelper] 保存数据文件成功: {FileName}");
                }

                return result;
            }
            catch (Exception exception)
            {
                FuLog.Warning($"[SaveHelper] 保存数据文件失败 {FileName}：'{exception}'.");
                return false;
            }
        }

        /// <summary>
        /// 获取所有数据项的名称。
        /// </summary>
        /// <returns>所有数据项的名称。</returns>
        public string[] GetAllDataNames() => Data.GetAllDataNames();

        /// <summary>
        /// 获取所有数据项的名称。
        /// </summary>
        /// <param name="results">所有数据项的名称。</param>
        public void GetAllDataNames(List<string> results) => Data.GetAllDataNames(results);

        /// <summary>
        /// 检查是否存在指定数据项。
        /// </summary>
        /// <param name="dataName">要检查数据项的名称。</param>
        /// <returns>指定的数据项是否存在。</returns>
        public bool HasData(string dataName) => Data.HasData(dataName);

        /// <summary>
        /// 移除指定数据项。
        /// </summary>
        /// <param name="dataName">要移除数据项的名称。</param>
        /// <returns>是否移除指定数据项成功。</returns>
        public bool RemoveData(string dataName)
        {
            var result = Data.RemoveData(dataName);
            Save();
            return result;
        }

        /// <summary>
        /// 清空所有数据项。
        /// </summary>
        public void RemoveAllData()
        {
            Data.RemoveAllData();
            Save();
        }

        #region Get

        /// <summary>
        /// 从指定数据项中读取布尔值。
        /// </summary>
        /// <param name="dataName">要获取数据项的名称。</param>
        /// <param name="defaultValue">当指定的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string dataName, bool defaultValue = false) => Data.GetBool(dataName, defaultValue);

        /// <summary>
        /// 从指定数据项中读取整数值。
        /// </summary>
        /// <param name="dataName">要获取数据项的名称。</param>
        /// <param name="defaultValue">当指定的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string dataName, int defaultValue = 0) => Data.GetInt(dataName, defaultValue);

        /// <summary>
        /// 从指定数据项中读取长整数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public long GetLong(string dataName, long defaultValue = 0) => Data.GetLong(dataName, defaultValue);

        /// <summary>
        /// 从指定数据项中读取浮点数值。
        /// </summary>
        /// <param name="dataName">要获取数据项的名称。</param>
        /// <param name="defaultValue">当指定的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string dataName, float defaultValue = 0) => Data.GetFloat(dataName, defaultValue);

        /// <summary>
        /// 从指定数据项中读取双精度浮点数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public double GetDouble(string dataName, double defaultValue = 0) => Data.GetDouble(dataName, defaultValue);

        /// <summary>
        /// 从指定数据项中读取字符串值。
        /// </summary>
        /// <param name="dataName">要获取数据项的名称。</param>
        /// <param name="defaultValue">当指定的数据项不存在时，返回此默认值。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string dataName, string defaultValue = null) => Data.GetString(dataName, defaultValue);

        /// <summary>
        /// 从指定数据项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="dataName">要获取数据项的名称。</param>
        /// <returns>读取的对象。</returns>
        public T GetObject<T>(string dataName)
        {
            var json = GetString(dataName);
            if (json.IsNullOrWhiteSpace()) return default;
            return Utility.Json.ToObject<T>(json);
        }

        /// <summary>
        /// 从指定数据项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="dataName">要获取数据项的名称。</param>
        /// <returns>读取的对象。</returns>
        public object GetObject(Type objectType, string dataName)
        {
            var json = GetString(dataName);
            if (json.IsNullOrWhiteSpace()) return null;
            return Utility.Json.ToObject(objectType, json);
        }

        #endregion

        #region Set

        /// <summary>
        /// 向指定数据项写入布尔值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public void SetBool(string dataName, bool value) => Data.SetBool(dataName, value);

        /// <summary>
        /// 向指定数据项写入整数值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public void SetInt(string dataName, int value) => Data.SetInt(dataName, value);

        /// <summary>
        /// 向指定数据项写入长整数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        public void SetLong(string dataName, long value) => Data.SetLong(dataName, value);

        /// <summary>
        /// 向指定数据项写入浮点数值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public void SetFloat(string dataName, float value) => Data.SetFloat(dataName, value);

        /// <summary>
        /// 向指定数据项写入双精度浮点数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        public void SetDouble(string dataName, double value) => Data.SetDouble(dataName, value);

        /// <summary>
        /// 向指定数据项写入字符串值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public void SetString(string dataName, string value) => Data.SetString(dataName, value);

        /// <summary>
        /// 向指定数据项写入对象。
        /// </summary>
        /// <typeparam name="T">要写入对象的类型。</typeparam>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public void SetObject<T>(string dataName, T obj)
        {
            var json = Utility.Json.ToJson(obj);
            SetString(dataName, json);
        }

        /// <summary>
        /// 向指定数据项写入对象。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public void SetObject(string dataName, object obj)
        {
            var json = Utility.Json.ToJson(obj);
            SetString(dataName, json);
        }

        #endregion
    }
}