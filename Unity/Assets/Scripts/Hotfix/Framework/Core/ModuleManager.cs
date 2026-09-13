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
        /// 注意：重启的排水窗口内（Dispose 之后、重新初始化之前）返回的是已释放但保留复用单例的实例，
        /// 其 IsAlive 为 false；调用方若在此窗口内使用，需自行判断 IsAlive。
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
        /// 两条分支的错误处理对齐：OnInit 失败时首次分支回滚出列（不留半初始化实例）、重启分支标记 IsAlive=false，
        /// 两种情况都不会再被帧驱动。
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        public static void RegisterModule<T>() where T : ModuleBase, new()
        {
            var module = GetModule<T>();
            if (module != null)
            {
                // 模块单例已存活，重新初始化，避免重复注册
                FuLogger.LogInfo($"<color=#00FBD5>------重新初始化模块: {typeof(T).Name}</color>");
                try
                {
                    module.OnInit();
                    module.IsAlive = true;
                }
                catch (Exception e)
                {
                    // 重新初始化失败：不得让异常逃逸（否则重启流程停在半初始化且后续模块不再注册），
                    // 标记为非存活使帧驱动跳过它，下次重启可再次尝试重新初始化。
                    module.IsAlive = false;
                    FuLogger.LogError($"重新初始化模块 {typeof(T).Name} 失败: {e.Message}");
                }

                return;
            }

            // 首次启动：编译期创建注册。先入列再 OnInit，保证 OnInit 内 GetModule<T>() 自取（自注册场景）可用。
            var newModule = new T();
            ModuleList.Add(newModule);
            try
            {
                newModule.OnInit();
                newModule.IsAlive = true;
                FuLogger.LogInfo($"<color=#00FBD5>------注册模块 {typeof(T).Name} 成功!</color>");
            }
            catch (Exception e)
            {
                // 半初始化实例回滚出列：留在列内会以「未初始化」状态被每帧驱动，且后续注册会走重新初始化分支用脏实例
                newModule.IsAlive = false;
                ModuleList.Remove(newModule);
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
                if (!module.IsAlive) continue; // 排水窗口内模块已释放但仍在列（见 GetModule），跳过
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
                if (!module.IsAlive) continue;
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
                if (!module.IsAlive) continue;
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
                if (!module.IsAlive) continue;
                module.OnFixedUpdate();
            }
        }

        /// <summary>
        /// 释放框架模块(逆序释放,后注册的先关闭)。
        /// 幂等：已释放（IsAlive=false）的模块跳过，重复调用不会二次 OnDispose；
        /// 模块实例保留在 ModuleList 中，供重启时重新初始化复用（见 RegisterModule）。
        /// </summary>
        public static void Dispose()
        {
            for (var i = ModuleList.Count - 1; i >= 0; i--)
            {
                var module = ModuleList[i];
                if (!module.IsAlive) continue;

                try
                {
                    module.OnDispose();
                    FuLogger.LogInfo($"<color=#00FBD5>------释放模块: {i + 1}.{module.GetType().Name}</color>");
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ModuleManager] 释放模块 {module.GetType().Name} 时出现异常: {e.Message}");
                }
                finally
                {
                    // 无论 OnDispose 是否抛异常都标记为已释放：异常路径若仍标记存活，帧驱动会持续驱动脏实例
                    module.IsAlive = false;
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