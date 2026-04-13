using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Model.Runtime
{
    /// <summary>
    /// 数据模型管理模块。
    /// 功能：
    ///     1.将所有的Model存到字典里统一管理。
    ///     2.提供获取、删除指定Model的方法。
    /// </summary>
    public class ModelModule : FuModule
    {
        /// <summary>
        /// 存储所有的Model字典。Key：Model类型， value：Model实例
        /// </summary>
        private readonly Dictionary<Type, BaseModel> m_ModelDict = new();

        /// <summary>
        /// 初始化。
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 释放。
        /// </summary>
        protected override void OnDispose() => Clear();

        /// <summary>
        /// 获取指定类型的Model
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <returns></returns>
        public T GetModel<T>() where T : BaseModel, new()
        {
            var key = typeof(T);
            if (m_ModelDict.TryGetValue(key, out var model)) return model as T;
            return CreateModel<T>();
        }

        /// <summary>
        /// 移除指定的Model
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        public void RemoveModel<T>()
        {
            var key = typeof(T);
            if (!m_ModelDict.ContainsKey(key)) FuLogger.LogError($"[ModelModule] 删除Model失败! '{key.Name}' 不存在");

            var model = m_ModelDict[key];
            if (m_ModelDict.Remove(key))
                model.Dispose();
        }

        /// <summary>
        /// 清理所有的Model(一般在游戏登出时才调用)
        /// </summary>
        private void Clear()
        {
            foreach (var (_, model) in m_ModelDict)
            {
                model.Dispose();
            }

            m_ModelDict.Clear();
        }

        /// <summary>
        /// 创建指定类型的Model
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <returns></returns>
        private T CreateModel<T>() where T : BaseModel, new()
        {
            var key = typeof(T);
            if (m_ModelDict.ContainsKey(key)) FuLogger.LogError($"[ModelModule] 创建Model失败! '{key.Name}' 已存在");

            var model = new T();
            model.Init();

            m_ModelDict.Add(key, model);
            return model;
        }
    }
}