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
    public class DataSaveHelper : MonoBehaviour
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
        /// 数据是否被修改（脏数据标记）
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        public bool EnableAutoSave { get; set; } = true;

        /// <summary>
        /// 自动保存间隔时间（秒）
        /// </summary>
        public float AutoSaveInterval { get; set; } = 300f; // 默认5分钟

        /// <summary>
        /// 是否启用加密
        /// </summary>
        public bool EnableEncryption { get; private set; }

        /// <summary>
        /// 加密密钥
        /// </summary>
        public string EncryptKey { get; private set; }

        /// <summary>
        /// 最后保存时间
        /// </summary>
        private float m_LastSaveTime;

        /// <summary>
        /// 获取数据项数量。
        /// </summary>
        public int Count => Data?.Count ?? 0;

        /// <summary>
        /// 初始化数据辅助器
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="enableAutoSave">是否启用自动保存</param>
        /// <param name="autoSaveInterval">自动保存间隔时间（秒）</param>
        /// <param name="enableEncryption">是否启用加密</param>
        /// <param name="encryptKey">加密密钥</param>
        public void Init(string fileName, bool enableAutoSave, float autoSaveInterval, bool enableEncryption = false, string encryptKey = null)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("[[DataSaveHelper] 文件名不能为空");

            FileName = fileName;
            var path = Path.Combine(Application.persistentDataPath, DataSaveManager.DirRoot, fileName);
            FilePath = Utility.Path.GetRegularPath(path);

            Data = new Data();

            Serializer = new DataSerializer();
            Serializer.RegisterSerializeCallback(0, DefaultSerializeCallback);
            Serializer.RegisterDeserializeCallback(0, DefaultDeserializeCallback);

            m_LastSaveTime   = Time.realtimeSinceStartup;
            IsDirty          = false;
            EnableAutoSave   = enableAutoSave;
            AutoSaveInterval = autoSaveInterval;
            EnableEncryption = enableEncryption;
            EncryptKey       = encryptKey;
        }

        /// <summary>
        /// 更新自动保存逻辑
        /// </summary>
        public void OnUpdate()
        {
            if (!EnableAutoSave || !IsDirty) return;

            var currentTime = Time.realtimeSinceStartup;
            if (currentTime - m_LastSaveTime >= AutoSaveInterval)
            {
                FuLog.Info($"[[DataSaveHelper] 自动保存数据文件: {FileName}");
                Save();
            }
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
                    FuLog.Warning($"[[DataSaveHelper] 加载数据文件失败，文件不存在: {FileName}");
                    return false;
                }

                using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

                // 如果启用加密，先解密数据
                if (EnableEncryption && !string.IsNullOrEmpty(EncryptKey))
                {
                    // 读取加密的字节数据
                    using var memoryStream = new MemoryStream();
                    fileStream.CopyTo(memoryStream);
                    var encryptedBytes = memoryStream.ToArray();

                    // 解密数据
                    var decryptedBytes = Utility.Encryption.Aes.AesDecrypt(encryptedBytes, EncryptKey);

                    // 使用解密后的数据创建新的流进行反序列化
                    using var decryptedStream = new MemoryStream(decryptedBytes);
                    Serializer.Deserialize(decryptedStream);
                }
                else
                {
                    // 未启用加密，直接反序列化
                    Serializer.Deserialize(fileStream);
                }

                FuLog.Info($"[[DataSaveHelper] 加载数据文件成功: {FileName}");
                return true;
            }
            catch (Exception exception)
            {
                FuLog.Error($"[[DataSaveHelper] 加载数据文件失败 {FileName}：'{exception}'.");
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
                if (!IsDirty) return true;

                // 确保目录存在
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 如果启用加密，先序列化到内存流，然后加密
                if (EnableEncryption && !string.IsNullOrEmpty(EncryptKey))
                {
                    // 先序列化到内存流
                    using var memoryStream = new MemoryStream();
                    var       result       = Serializer.Serialize(memoryStream, Data);
                    if (!result)
                    {
                        FuLog.Warning($"[[DataSaveHelper] 序列化数据失败: {FileName}");
                        return false;
                    }

                    // 获取序列化后的字节数据
                    var dataBytes = memoryStream.ToArray();

                    // 加密数据
                    var encryptedBytes = Utility.Encryption.Aes.AesEncrypt(dataBytes, EncryptKey);

                    // 将加密后的数据写入文件
                    using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
                    fileStream.Write(encryptedBytes, 0, encryptedBytes.Length);

                    FuLog.Info($"[[DataSaveHelper] 保存数据文件成功（已加密）: {FileName}");
                    IsDirty        = false;                     // 清除脏数据标记
                    m_LastSaveTime = Time.realtimeSinceStartup; // 更新最后保存时间
                    return true;
                }
                else
                {
                    // 未启用加密，直接保存
                    using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
                    var       result     = Serializer.Serialize(fileStream, Data);
                    if (result)
                    {
                        FuLog.Info($"[[DataSaveHelper] 保存数据文件成功: {FileName}");
                        IsDirty        = false;                     // 清除脏数据标记
                        m_LastSaveTime = Time.realtimeSinceStartup; // 更新最后保存时间
                    }

                    return result;
                }
            }
            catch (Exception exception)
            {
                FuLog.Warning($"[[DataSaveHelper] 保存数据文件失败 {FileName}：'{exception}'.");
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
            if (result)
                IsDirty = true;
            return result;
        }

        /// <summary>
        /// 清空所有数据项。
        /// </summary>
        public void RemoveAllData()
        {
            Data.RemoveAllData();
            IsDirty = true;
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
        public void SetBool(string dataName, bool value)
        {
            var oldValue = Data.GetBool(dataName);
            if (oldValue == value) return;
            Data.SetBool(dataName, value);
            IsDirty = true;
        }

        /// <summary>
        /// 向指定数据项写入整数值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public void SetInt(string dataName, int value)
        {
            var oldValue = Data.GetInt(dataName);
            if (oldValue == value) return;
            Data.SetInt(dataName, value);
            IsDirty = true;
        }

        /// <summary>
        /// 向指定数据项写入长整数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        public void SetLong(string dataName, long value)
        {
            var oldValue = Data.GetLong(dataName);
            if (oldValue == value) return;
            Data.SetLong(dataName, value);
            IsDirty = true;
        }

        /// <summary>
        /// 向指定数据项写入浮点数值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public void SetFloat(string dataName, float value)
        {
            var oldValue = Data.GetFloat(dataName);
            if (Math.Abs(oldValue - value) < float.Epsilon) return;
            Data.SetFloat(dataName, value);
            IsDirty = true;
        }

        /// <summary>
        /// 向指定数据项写入双精度浮点数值。
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        public void SetDouble(string dataName, double value)
        {
            var oldValue = Data.GetDouble(dataName);
            if (Math.Abs(oldValue - value) < double.Epsilon) return;
            Data.SetDouble(dataName, value);
            IsDirty = true;
        }

        /// <summary>
        /// 向指定数据项写入字符串值。
        /// </summary>
        /// <param name="dataName">要写入数据项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public void SetString(string dataName, string value)
        {
            var oldValue = Data.GetString(dataName);
            if (oldValue == value) return;
            Data.SetString(dataName, value);
            IsDirty = true;
        }

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

        #endregion
    }
}