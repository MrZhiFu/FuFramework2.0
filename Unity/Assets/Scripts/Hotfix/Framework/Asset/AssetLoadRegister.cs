using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源加载注册器（纯实例类，非池化）。
    /// 功能：
    ///     1.异步加载资源。
    ///     2.记录加载过的资源句柄，避免重复加载。
    ///     3.异步实例化游戏物体。
    ///     4.卸载资源。
    /// 作为「逻辑分组的资源装载器」使用（如 UI 包：包打开时加载、包关闭时 UnloadAll 整组释放）。
    /// 不使用引用池——每个实例归单一调用方持有，用完 UnloadAll 后弃引用由 GC 回收，无复用竞态，无需生命周期版本计数。
    /// </summary>
    public class AssetLoadRegister
    {
        /// <summary>
        /// 资源管理模块（实例持有，构造时获取）。
        /// </summary>
        private readonly AssetModule m_AssetModule;

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
        /// 创建资源加载器。
        /// </summary>
        /// <returns>资源加载注册器。</returns>
        public static AssetLoadRegister Create() => new();

        /// <summary>
        /// 资源加载注册器。纯实例类：每次创建独立装载器，调用方持有，用完 UnloadAll 后弃引用由 GC 回收。
        /// </summary>
        public AssetLoadRegister()
        {
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            var handle = await LoadAssetHandleAsync(path, m_AssetModule.LoadAssetAsync<T>);
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
            var handle = await LoadAssetHandleAsync(path, p => m_AssetModule.LoadAssetAsync(p, type));
            return handle.AssetObject;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<Object> LoadAsync(string path)
        {
            var handle = await LoadAssetHandleAsync(path, m_AssetModule.LoadAssetAsync);
            return handle.AssetObject;
        }

        /// <summary>
        /// 异步实例化实体。
        /// <param name="path">资源路径</param>
        /// </summary>
        /// <returns>实例化后的实体。</returns>
        public async UniTask<GameObject> InstantiateAsync(string path)
        {
            var assetHandle          = await LoadAssetHandleAsync(path, m_AssetModule.LoadAssetAsync);
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
        /// <param name="loadFunc">以路径发起加载的异步函数。</param>
        /// <returns>资源句柄。</returns>
        private async UniTask<AssetHandle> LoadAssetHandleAsync(string path, Func<string, UniTask<AssetHandle>> loadFunc)
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
        private async UniTask<AssetHandle> LoadAssetHandleCoreAsync(string path, Func<string, UniTask<AssetHandle>> loadFunc)
        {
            AssetHandle assetHandle = null;
            try
            {
                assetHandle = await loadFunc(path);
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
        }
    }
}
