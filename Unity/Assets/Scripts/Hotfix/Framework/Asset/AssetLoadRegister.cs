using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePools;
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
        /// 正在加载中的任务去重字典，key为资源路径，value为加载任务。
        /// 同一路径并发加载时共享任务，防止后完成句柄覆盖先完成句柄导致泄漏。
        /// </summary>
        private readonly Dictionary<string, UniTask<AssetHandle>> m_LoadingTasks = new();

        /// <summary>
        /// 创建资源加载器
        /// </summary>
        /// <returns></returns>
        public static AssetLoadRegister Create()
        {
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
            return ReferencePool.Acquire<AssetLoadRegister>();
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            var handle = await LoadAssetHandleAsync(path, () => m_AssetModule.LoadAssetAsync<T>(path));
            return handle.GetAssetObject<T>();
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

            // 并发去重：同一路径正在加载，共享任务（防止后完成句柄覆盖先完成句柄导致泄漏）
            if (m_LoadingTasks.TryGetValue(path, out var loadingTask))
                return await loadingTask;

            // 创建加载任务并注册（async 同步执行到第一个 await 后注册，保证并发请求共享同一任务）
            var task = LoadAssetHandleCoreAsync(path, loadFunc);
            m_LoadingTasks[path] = task;
            try
            {
                return await task;
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
            if (m_AssetModule != null) // 防御：静态模块引用可能被其他实例 Clear 置 null
                m_AssetModule.UnloadAsset(path);

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
            UnloadAll();
            m_AssetModule = null;
        }

        /// <summary>
        /// 将引用归还引用池-释放资源。
        /// 归还前先 UnloadAll 释放所有句柄，保证池对象干净（否则残留句柄导致复用泄漏）。
        /// </summary>
        public void Release()
        {
            UnloadAll();
            ReferencePool.Release(this);
        }
    }
}
