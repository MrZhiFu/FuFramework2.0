using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.SaveData.Runtime
{
    /// <summary>
    /// 本地存储数据管理器。
    /// 功能：负责管理游戏的本地存档数据，允许您保存和获取各种类型的本地数据。
    /// </summary>
    public sealed class SaveManager : FuModule
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Core;

        /// <summary>
        /// 数据文件后缀
        /// </summary>
        public const string Suffix = ".dat";

        /// <summary>
        /// 数据根目录
        /// </summary>
        public const string DirRoot = "GameData";

        /// <summary>
        /// 默认游戏设置存档数据文件名
        /// </summary>
        public const string GameSettingName = "GameSetting" + Suffix;

        /// <summary>
        /// 所有辅助器字典，key为文件名，value为数据辅助器实例
        /// </summary>
        private readonly Dictionary<string, SaveHelper> m_Helpers = new();

        /// <summary>
        /// 获取所有本地存储数据项的数量。
        /// </summary>
        public int Count => m_Helpers.Count;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit() => LoadAll();

        /// <summary>
        /// 关闭并清理数据本地存储管理器
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType) => SaveAll();

        /// <summary>
        /// 获取或创建指定文件的数据辅助器
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>数据辅助器实例</returns>
        public SaveHelper GetOrCreateHelper(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("[SavaManager] 文件名不能为空");
            if (m_Helpers.TryGetValue(fileName, out var helper)) return helper;

            // 创建新的辅助器实例
            var helperGo = new GameObject($"SaveHelper_{fileName}");
            helperGo.transform.SetParent(transform);
            helperGo.transform.localScale = Vector3.one;

            helper = helperGo.AddComponent<SaveHelper>();
            helper.Init(fileName);

            m_Helpers[fileName] = helper;
            return helper;
        }

        /// <summary>
        /// 获取指定文件的本地存储数据辅助器
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>数据辅助器</returns>
        public SaveHelper GetHelper(string fileName) => m_Helpers.GetValueOrDefault(fileName);

        /// <summary>
        /// 获取所有本地存储数据辅助器
        /// </summary>
        /// <returns>辅助器字典</returns>
        public Dictionary<string, SaveHelper> GetAllHelpers() => m_Helpers;

        /// <summary>
        /// 加载本地存储数据。
        /// </summary>
        /// <returns>是否加载数据本地存储成功。</returns>
        public bool Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            var helper = GetOrCreateHelper(fileName); 
            return helper.Load();
        }

        /// <summary>
        /// 保存本地存储数据。
        /// </summary>
        /// <returns>是否保存数据本地存储成功。</returns>
        public bool Save(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            var helper = GetHelper(fileName);
            return helper != null && helper.Save();
        }

        /// <summary>
        /// 加载所有本地存储数据
        /// </summary>
        public void LoadAll()
        {
            // 从PersistentDataPath/GameData 目录下找到所有数据的文件名，并创建对应的辅助器实例
            var path = Path.Combine(Application.persistentDataPath, DirRoot);

            // 检查目录是否存在
            if (!Directory.Exists(path)) return;

            // 加载所有.dat数据文件
            var files = Directory.GetFiles(path, $"*{Suffix}", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                if (m_Helpers.ContainsKey(fileName)) continue;

                var helper = GetOrCreateHelper(fileName);
                helper.Load();
            }
        }

        /// <summary>
        /// 保存所有本地存储数据
        /// </summary>
        public void SaveAll()
        {
            foreach (var helper in m_Helpers.Values)
            {
                helper.Save();
            }
        }

        /// <summary>
        /// 获取所有数据本地存储项的名称。
        /// </summary>
        /// <returns>所有数据本地存储项的名称。</returns>
        public string[] GetAllHelperNames()
        {
            var results = new List<string>();
            GetAllHelperNames(results);
            return results.ToArray();
        }

        /// <summary>
        /// 获取所有数据本地存储项的名称。
        /// </summary>
        /// <param name="results">所有数据本地存储项的名称。</param>
        public void GetAllHelperNames(List<string> results)
        {
            foreach (var helper in m_Helpers)
            {
                results.Add(helper.Key);
            }
        }

        /// <summary>
        /// 移除指定文件的数据辅助器
        /// </summary>
        /// <param name="fileName">文件名</param>
        public void RemoveHelper(string fileName)
        {
            if (!m_Helpers.TryGetValue(fileName, out var helper)) return;
            helper.RemoveAllData();

            // 删除数据文件
            var dataPath = Path.Combine(Application.persistentDataPath, DirRoot, fileName);
            if (Directory.Exists(dataPath))
                Utility.File.Delete(dataPath);

            DestroyImmediate(helper.gameObject);
            m_Helpers.Remove(fileName);
        }

        /// <summary>
        /// 清空所有辅助器
        /// </summary>
        public void RemoveAllHelper()
        {
            foreach (var helper in m_Helpers.Values)
            {
                helper.RemoveAllData();
                DestroyImmediate(helper.gameObject);
            }

            // 删除整个数据文件夹GameData
            var dataPath = Path.Combine(Application.persistentDataPath, DirRoot);
            if (Directory.Exists(dataPath))
                Utility.File.DeleteDir(dataPath);

            m_Helpers.Clear();
        }

        /// <summary>
        /// 检查是否存在指定本地存储项。
        /// </summary>
        /// <param name="dataName">要检查数据本地存储项的名称。</param>
        /// <param name="fileName">要检查数据本地存储项的文件名。</param>
        /// <returns>指定的数据本地存储项是否存在。</returns>
        public bool HasData(string fileName, string dataName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) && helper.HasData(dataName);
        }

        /// <summary>
        /// 移除指定数据本地存储项。
        /// </summary>
        /// <param name="fileName">要移除数据本地存储项的文件名。</param>
        /// <param name="dataName">要移除数据本地存储项的名称。</param>
        /// <returns>是否移除指定数据本地存储项成功。</returns>
        public bool RemoveData(string fileName, string dataName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) && helper.RemoveData(dataName);
        }

        /// <summary>
        /// 清空所有数据本地存储项。
        /// </summary>
        public void RemoveAllData()
        {
            foreach (var helper in m_Helpers.Values)
            {
                helper.RemoveAllData();
            }
        }

        #region Get

        /// <summary>
        /// 从指定本地存储项中读取布尔值。
        /// </summary>
        /// <param name="fileName">要获取数据本地存储项的文件名。</param>
        /// <param name="dataName">要获取数据本地存储项的名称。</param>
        /// <param name="defaultValue">当指定的数据本地存储项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public bool GetBool(string fileName, string dataName, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) && helper.GetBool(dataName, defaultValue);
        }

        /// <summary>
        /// 从指定本地存储项中读取整数值。
        /// </summary>
        /// <param name="fileName">要获取数据本地存储项的文件名。</param>
        /// <param name="dataName">要获取数据本地存储项的名称。</param>
        /// <param name="defaultValue">当指定的数据本地存储项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public int GetInt(string fileName, string dataName, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetInt(dataName, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 从指定本地存储项中读取长整数值。
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        public long GetLong(string fileName, string dataName, long defaultValue = 0)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetLong(dataName, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 从指定本地存储项中读取浮点数值。
        /// </summary>
        /// <param name="fileName">要获取数据本地存储项的文件名。</param>
        /// <param name="dataName">要获取数据本地存储项的名称。</param>
        /// <param name="defaultValue">当指定的数据本地存储项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public float GetFloat(string fileName, string dataName, float defaultValue = 0)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetFloat(dataName, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 从指定本地存储项中读取双精度浮点数值。
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        public double GetDouble(string fileName, string dataName, double defaultValue = 0)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetDouble(dataName, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 从指定本地存储项中读取字符串值。
        /// </summary>
        /// <param name="dataName">要获取数据本地存储项的名称。</param>
        /// <param name="defaultValue">当指定的数据本地存储项不存在时，返回此默认值。</param>
        /// <param name="fileName">要获取数据本地存储项的文件名。</param>
        /// <returns>读取的字符串值。</returns>
        public string GetString(string fileName, string dataName, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetString(dataName, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 从指定本地存储项中读取对象。
        /// </summary>
        /// <param name="fileName">要获取数据本地存储项的文件名。</param>
        /// <param name="dataName">要获取数据本地存储项的名称。</param>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <returns>读取的对象。</returns>
        public T GetObject<T>(string fileName, string dataName) where T : class, new()
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetObject<T>(dataName) : null;
        }

        /// <summary>
        /// 从指定本地存储项中读取对象。
        /// </summary>
        /// <param name="fileName">要获取数据本地存储项的文件名。</param>
        /// <param name="dataName">要获取数据本地存储项的名称。</param>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <returns>读取的对象。</returns>
        public object GetObject(string fileName, string dataName, Type objectType)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (objectType is null) throw new FuException("要存储的数据对象不能为空.");
            return m_Helpers.TryGetValue(fileName, out var helper) ? helper.GetObject(objectType, dataName) : null;
        }

        #endregion

        #region Set

        /// <summary>
        /// 向指定本地存储项写入布尔值。
        /// </summary>
        /// <param name="fileName">要写入数据本地存储项的文件名。</param>
        /// <param name="dataName">要写入数据本地存储项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public void SetBool(string fileName, string dataName, bool value)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetBool(dataName, value);
        }

        /// <summary>
        /// 向指定本地存储项写入整数值。
        /// </summary>
        /// <param name="fileName">要写入数据本地存储项的文件名。</param>
        /// <param name="dataName">要写入数据本地存储项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public void SetInt(string fileName, string dataName, int value)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetInt(dataName, value);
        }

        /// <summary>
        /// 向指定本地存储项写入长整数值。
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        /// <exception cref="FuException"></exception>
        public void SetLong(string fileName, string dataName, long value)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetLong(dataName, value);
        }

        /// <summary>
        /// 向指定本地存储项写入浮点数值。
        /// </summary>
        /// <param name="fileName">要写入数据本地存储项的文件名。</param>
        /// <param name="dataName">要写入数据本地存储项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public void SetFloat(string fileName, string dataName, float value)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetFloat(dataName, value);
        }

        /// <summary>
        /// 向指定本地存储项写入双精度浮点数值。
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataName"></param>
        /// <param name="value"></param>
        /// <exception cref="FuException"></exception>
        public void SetDouble(string fileName, string dataName, double value)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetDouble(dataName, value);
        }

        /// <summary>
        /// 向指定本地存储项写入字符串值。
        /// </summary>
        /// <param name="fileName">要写入数据本地存储项的文件名。</param>
        /// <param name="dataName">要写入数据本地存储项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public void SetString(string fileName, string dataName, string value)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetString(dataName, value);
        }


        /// <summary>
        /// 向指定本地存储项写入对象。
        /// </summary>
        /// <param name="fileName">要写入数据本地存储项的文件名。</param>
        /// <param name="dataName">要写入数据本地存储项的名称。</param>
        /// <typeparam name="T">要写入对象的类型。</typeparam>
        /// <param name="obj">要写入的对象。</param>
        public void SetObject<T>(string fileName, string dataName, T obj) where T : class, new()
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetObject(dataName, obj);
        }

        /// <summary>
        /// 向指定本地存储项写入对象。
        /// </summary>
        /// <param name="fileName">要写入数据本地存储项的文件名。</param>
        /// <param name="dataName">要写入数据本地存储项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public void SetObject(string fileName, string dataName, object obj)
        {
            if (string.IsNullOrEmpty(fileName)) throw new FuException("数据文件名不能为空.");
            if (string.IsNullOrEmpty(dataName)) throw new FuException("数据名称不能为空.");
            if (!m_Helpers.TryGetValue(fileName, out var helper)) helper = GetOrCreateHelper(fileName);
            helper.SetObject(dataName, obj);
        }

        #endregion
    }
}