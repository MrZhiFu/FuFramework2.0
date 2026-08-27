using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
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
        /// 获取游戏框架模块（泛型版本）。
        /// </summary>
        /// <typeparam name="T">要获取的模块类型。</typeparam>
        /// <returns>要获取的模块实例。</returns>
        public static T GetModule<T>() where T : ModuleBase
        {
            foreach (var module in ModuleList)
            {
                if (module is T result)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 注册游戏框架模块
        /// 首次启动创建注册；重启时模块单例已存活，直接重新初始化（OnInit）。
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        public static void RegisterModule<T>() where T : ModuleBase, new()
        {
            var module = GetModule<T>();
            if (module != null)
            {
                // 模块单例已存活，重新初始化，避免重复注册
                FuLogger.LogInfo($"<color=#00FBD5>------重新初始化模块: {typeof(T).Name}</color>");
                module.OnInit();
                return;
            }

            try
            {
                // 首次启动：编译期创建注册
                var newModule = new T();
                ModuleList.Add(newModule);
                newModule.OnInit();
                FuLogger.LogInfo($"<color=#00FBD5>------注册模块 {typeof(T).Name} 成功!</color>");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"注册模块 {typeof(T).Name} 失败: {e.Message}");
            }
        }

        /// <summary>
        /// 框架模块帧更新
        /// </summary>
        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var module in ModuleList)
            {
                module.OnUpdate(deltaTime, unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 框架模块延迟帧更新
        /// </summary>
        public static void LateUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var module in ModuleList)
            {
                module.OnLateUpdate(deltaTime, unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 框架模块每秒更新
        /// </summary>
        public static void PerSecondUpdate()
        {
            foreach (var module in ModuleList)
            {
                module.OnPerSecondUpdate();
            }
        }

        /// <summary>
        /// 框架模块固定帧更新
        /// </summary>
        public static void FixedUpdate()
        {
            foreach (var module in ModuleList)
            {
                module.OnFixedUpdate();
            }
        }

        /// <summary>
        /// 释放框架模块(逆序释放,后注册的先关闭)
        /// </summary>
        public static void Dispose()
        {
            for (var i = ModuleList.Count - 1; i >= 0; i--)
            {
                var module = ModuleList[i];
                try
                {
                    module.OnDispose();
                    FuLogger.LogInfo($"<color=#00FBD5>------释放模块: {i + 1}.{module.GetType().Name}</color>");
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ModuleManager] 释放模块 {module.GetType().Name} 时出现异常: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 取消所有实现了ICancelAsync接口的模块的异步任务，并等待其在途任务清理完毕，保证旧生命周期无在途异步任务残留。
        /// </summary>
        public static async UniTask CancelAllAsync()
        {
            foreach (var module in ModuleList)
            {
                if (module is ICancelAsync cancellable)
                {
                    await cancellable.CancelAsync();
                }
            }
        }
    }
}