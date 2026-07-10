using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 框架模块管理器。
    /// 功能：
    ///     1. 注册和获取框架中的各个模块。
    ///     2. 驱动各个模块生命周期。
    /// </summary>
    public static class ModuleManager
    {
        /// <summary>
        /// 预计的模块数量，用于预分配集合容量
        /// </summary>
        private const int ModuleCount = 25;

        /// <summary>
        /// 记录所有已注册的模块列表
        /// </summary>
        private static readonly List<ModuleBase> ModuleList = new(ModuleCount);

        /// <summary>
        /// 模块根节点
        /// </summary>
        private static Transform m_ModuleRoot;

        /// <summary>
        /// 模块根节点，供需要挂载子 GameObject 的模块使用。
        /// </summary>
        public static Transform ModuleRoot
        {
            get
            {
                if (m_ModuleRoot is not null) return m_ModuleRoot;
                var rootObj = new GameObject("[FrameworkModule]");
                Object.DontDestroyOnLoad(rootObj);
                m_ModuleRoot = rootObj.transform;
                return m_ModuleRoot;
            }
        }

        /// <summary>
        /// 获取游戏框架模块（泛型版本）。
        /// </summary>
        /// <typeparam name="T">要获取的模块类型。</typeparam>
        /// <returns>要获取的模块实例。</returns>
        public static T GetModule<T>() where T : ModuleBase
        {
            var type = typeof(T);
            for (var i = 0; i < ModuleList.Count; i++)
            {
                if (ModuleList[i].GetType() == type)
                    return ModuleList[i] as T;
            }

            return null;
        }

        /// <summary>
        /// 注册游戏框架模块（泛型版本，内部委托给 Type 版本）。
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        public static void RegisterModule<T>() where T : ModuleBase
        {
            RegisterModule(typeof(T));
        }

        /// <summary>
        /// 注册游戏框架模块（Type 版本，支持热更模块）。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        public static void RegisterModule(Type moduleType)
        {
            if (!typeof(ModuleBase).IsAssignableFrom(moduleType))
            {
                FuLogger.LogError($"类型 {moduleType.Name} 不是有效的ModuleBase类型!");
                return;
            }

            try
            {
                // 检查模块是否已存在
                if (GetModule(moduleType) != null)
                {
                    FuLogger.LogWarning($"模块 {moduleType.Name} 已经注册");
                    return;
                }

                // 通过反射创建模块实例
                var module = (ModuleBase)Activator.CreateInstance(moduleType);

                if (module == null)
                {
                    throw new Exception($"注册模块 {moduleType.Name} 失败: 无法创建模块实例!");
                }

                // 按注册顺序添加到列表末尾并初始化
                ModuleList.Add(module);
                module.OnInit();

                FuLogger.LogInfo($"<color=#00FBD5>------注册模块 {moduleType.Name} 成功!</color>");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"注册模块 {moduleType.Name} 失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取游戏框架模块（Type 版本，支持热更模块查询）。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <returns>模块实例，未找到返回 null</returns>
        public static ModuleBase GetModule(Type moduleType)
        {
            for (var i = 0; i < ModuleList.Count; i++)
            {
                if (ModuleList[i].GetType() == moduleType)
                    return ModuleList[i];
            }

            return null;
        }

        /// <summary>
        /// 框架模块帧更新
        /// </summary>
        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            for (var i = 0; i < ModuleList.Count; i++)
            {
                ModuleList[i].OnUpdate(deltaTime, unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 框架模块延迟帧更新
        /// </summary>
        public static void LateUpdate(float deltaTime, float unscaledDeltaTime)
        {
            for (var i = 0; i < ModuleList.Count; i++)
            {
                ModuleList[i].OnLateUpdate(deltaTime, unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 框架模块固定帧更新
        /// </summary>
        public static void FixedUpdate()
        {
            for (var i = 0; i < ModuleList.Count; i++)
            {
                ModuleList[i].OnFixedUpdate();
            }
        }

        /// <summary>
        /// 释放框架模块
        /// </summary>
        public static void Dispose()
        {
            // 逆序释放所有模块(后注册的先关闭)
            for (var i = ModuleList.Count - 1; i >= 0; i--)
            {
                ModuleList[i].OnDispose();
                FuLogger.LogInfo($"<color=#00FBD5>------释放模块: {i + 1}.{ModuleList[i].GetType().Name}</color>");
            }
        }

        /// <summary>
        /// 重新初始化框架模块
        /// </summary>
        public static void ReInit()
        {
            for (var i = 0; i < ModuleList.Count; i++)
            {
                ModuleList[i].OnInit();
                FuLogger.LogInfo($"<color=#00FBD5>------重新初始化模块: {i + 1}.{ModuleList[i].GetType().Name}</color>");
            }
        }
    }
}