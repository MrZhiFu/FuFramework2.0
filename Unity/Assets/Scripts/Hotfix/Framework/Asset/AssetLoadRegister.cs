using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePool;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源加载注册器。
    /// 功能：
    ///     1.异步加载资源。
    ///     2.记录加载过的资源句柄，避免重复加载。
    ///     3.异步实例化游戏物体。
    ///     4.卸载资源。
    /// </summary>
    public class AssetLoadRegister : IReference
    {
        /// <summary>
        /// 资源管理模块
        /// </summary>
        private static AssetModule m_AssetModule;

        /// <summary>
        /// 缓存已经加载的资源句柄，key为资源路径，value为资源句柄
        /// </summary>
        private readonly Dictionary<string, AssetHandle> m_HandleDict = new();

        /// <summary>
        /// 正在加载中的任务去重字典，key为资源路径，value为共享完成源。
        /// 同一路径并发加载时共享完成源（UniTaskCompletionSource.Task 可被多个调用者 await），
        /// 防止后完成句柄覆盖先完成句柄导致泄漏，也避免 async UniTask 重复 await 报错。
        /// </summary>
        private readonly Dictionary<string, UniTaskCompletionSource<AssetHandle>> m_LoadingTasks = new();

        /// <summary>
        /// 是否已归还对象池。防止归还后在途的加载任务把句柄写回缓存（复用泄漏）。
        /// </summary>
        private bool m_Released;

        /// <summary>
        /// 创建资源加载器
        /// </summary>
        /// <returns></returns>
        public static AssetLoadRegister Create()
        {
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
            var register = GlobalModule.ReferencePoolModule.Acquire<AssetLoadRegister>();
            register.m_Released = false;
            return register;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            var handle = await LoadAssetHandleAsync(path, () => m_AssetModule.LoadAssetAsync<T>(path));

            // GetAssetObject<T> 用 as 转换，类型不匹配时返回 null，需显式报错（否则调用方 NRE 难排查）
            var result = handle.GetAssetObject<T>();
            if (result == null)
                throw new InvalidOperationException($"[AssetLoadRegister]资源{path}类型不匹配，期望类型: {typeof(T)}");

            return result;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public async UniTask<Object> LoadAsync(string path, Type type)
        {
            var handle = await LoadAssetHandleAsync(path, () => m_AssetModule.LoadAssetAsync(path, type));
            return handle.AssetObject;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<Object> LoadAsync(string path)
        {
            var handle = await LoadAssetHandleAsync(path, () => m_AssetModule.LoadAssetAsync(path));
            return handle.AssetObject;
        }

        /// <summary>
        /// 异步实例化实体。
        /// <param name="path">资源路径</param>
        /// </summary>
        /// <returns>实例化后的实体。</returns>
        public async UniTask<GameObject> InstantiateAsync(string path)
        {
            var assetHandle          = await LoadAssetHandleAsync(path, () => m_AssetModule.LoadAssetAsync(path));
            var instantiateOperation = assetHandle.InstantiateAsync();
            await instantiateOperation;
            if (instantiateOperation.Result == null)
                throw new InvalidOperationException($"[AssetLoadRegister]实例化资源{path}失败（资源可能不是 GameObject 或加载异常）");
            return instantiateOperation.Result;
        }

        /// <summary>
        /// 加载资源句柄的通用逻辑。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <param name="loadFunc">实际加载资源的异步函数。</param>
        /// <returns>资源句柄。</returns>
        private async UniTask<AssetHandle> LoadAssetHandleAsync(string path, Func<UniTask<AssetHandle>> loadFunc)
        {
            // 已归还对象池后禁止继续使用
            if (m_Released)
                throw new ObjectDisposedException(nameof(AssetLoadRegister));

            // 检查是否已加载
            if (m_HandleDict.TryGetValue(path, out var existingHandle))
            {
                // 验证资源是否仍然有效
                if (existingHandle.AssetObject != null)
                    return existingHandle;

                // 资源已失效，清理后重新加载
                m_HandleDict.Remove(path);
                existingHandle.Release();
            }

            // 并发去重：同一路径正在加载，共享完成源（UniTaskCompletionSource.Task 可多次 await）
            if (m_LoadingTasks.TryGetValue(path, out var sharedSource))
                return await sharedSource.Task;

            // 创建共享完成源并注册，多个并发调用者 await 同一 Task
            var taskSource = new UniTaskCompletionSource<AssetHandle>();
            m_LoadingTasks[path] = taskSource;
            try
            {
                var handle = await LoadAssetHandleCoreAsync(path, loadFunc);
                taskSource.TrySetResult(handle);
                return handle;
            }
            catch (Exception e)
            {
                taskSource.TrySetException(e); // 失败：所有 await 此 Task 的调用者抛异常
                throw;
            }
            finally
            {
                m_LoadingTasks.Remove(path);
            }
        }

        /// <summary>
        /// 实际加载资源句柄并缓存。并发请求经 LoadAssetHandleAsync 去重后共享此任务。
        /// </summary>
        private async UniTask<AssetHandle> LoadAssetHandleCoreAsync(string path, Func<UniTask<AssetHandle>> loadFunc)
        {
            AssetHandle assetHandle = null;
            try
            {
                assetHandle = await loadFunc();
                if (assetHandle == null || assetHandle.AssetObject == null)
                {
                    throw new InvalidOperationException($"[AssetLoadRegister]资源{path}加载失败");
                }

                // 加载期间被 Release 归还对象池：句柄已不归本实例，释放并阻止写回（否则复用泄漏）
                if (m_Released)
                {
                    assetHandle.Release();
                    assetHandle = null; // 置空避免 catch 二次释放
                    throw new ObjectDisposedException($"{nameof(AssetLoadRegister)}已归还对象池");
                }

                // 保存资源句柄
                m_HandleDict[path] = assetHandle;
                FuLogger.LogInfo($"[AssetLoadRegister]加载{path}资源完成");
                return assetHandle;
            }
            catch
            {
                assetHandle?.Release();
                throw;
            }
        }

        /// <summary>
        /// 卸载已经加载的指定资源。
        /// 1.释放资源句柄，即减少引用计数。
        /// 2.尝试卸载资源，即引用计数为零时，才会真正卸载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public void Unload(string path)
        {
            if (!m_HandleDict.TryGetValue(path, out var handle)) return;

            // 释放资源句柄，即减少引用计数
            handle.Release();

            // 尝试卸载资源，即引用计数为零时，才会真正卸载资源
            m_AssetModule?.UnloadAsset(path);

            m_HandleDict.Remove(path);
            FuLogger.LogInfo($"[AssetLoadRegister]释放{path}资源完成.");
        }

        /// <summary>
        /// 卸载所有已经加载的资源。
        /// </summary>
        public void UnloadAll()
        {
            // 先复制路径列表，避免遍历时集合被修改
            var paths = new List<string>(m_HandleDict.Keys);

            foreach (var path in paths)
            {
                Unload(path);
            }

            m_HandleDict.Clear();
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public void Clear()
        {
            m_Released = true;
            UnloadAll();
            m_AssetModule = null;
        }

        /// <summary>
        /// 将引用归还引用池-释放资源。
        /// 归还前先 UnloadAll 释放所有句柄、清空加载中去重字典，保证池对象干净
        /// （否则残留句柄/去重任务导致复用泄漏）。
        /// </summary>
        public void Release()
        {
            m_Released = true;
            UnloadAll();
            m_LoadingTasks.Clear();
            GlobalModule.ReferencePoolModule.Recycle(this);
        }
    }
}