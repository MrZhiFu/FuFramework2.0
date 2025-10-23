using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Model.Runtime
{
    /// <summary>
    /// Model管理器
    /// 1.将所有的Model存到字典里统一管理
    /// 2.提供创建、获取、删除指定Model的方法
    /// </summary>
    [ModuleDependency(typeof(EventManager))]
    public class ModelManager : FuModule
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;

        /// 存储所有的Model字典。Key：Model类型， value：Model
        private readonly Dictionary<Type, BaseModel> m_ModelDic = new();

        /// <summary>
        /// 初始化。
        /// </summary>
        protected override void OnInit() { }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            Clear();
        }

        /// <summary>
        /// 清理所有的Model(一般在游戏登出时才调用)
        /// </summary>
        public void Clear()
        {
            foreach (var (_, model) in m_ModelDic)
            {
                model.OnDispose();
            }

            m_ModelDic.Clear();
        }

        /// <summary>
        /// 获取指定类型的Model
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <returns></returns>
        public T GetModel<T>() where T : BaseModel, new()
        {
            var key = typeof(T);

            if (m_ModelDic.TryGetValue(key, out var model))
                return model as T;
            return CreateModel<T>();
        }

        /// <summary>
        /// 删除指定的Model
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        public void DeleteModel<T>()
        {
            var key = typeof(T);

            if (!m_ModelDic.ContainsKey(key))
                FuLog.Error($"删除Model失败! '{key.Name}' 不存在");

            var model = m_ModelDic[key];

            if (m_ModelDic.Remove(key))
                model.OnDispose();
        }

        /// <summary>
        /// 创建指定类型的Model，并注入指定的View
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <returns></returns>
        private T CreateModel<T>() where T : BaseModel, new()
        {
            var key = typeof(T);

            if (m_ModelDic.ContainsKey(key))
                FuLog.Error($"创建Model失败! '{key.Name}' 已存在");

            var model = new T();
            model.Init();

            m_ModelDic.Add(key, model);
            return model;
        }
    }
}