using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;
using YooAsset;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源加载注册器。
    /// 1.加载资源(只提供异步加载接口)。
    /// 2.记录加载过的资源句柄，避免重复加载。
    /// 3.卸载资源。
    /// </summary>
    public class AssetLoadRegister : IReference
    {
        /// <summary>
        /// 资源管理模块
        /// </summary>
        private readonly AssetModule m_AssetModule = ModuleManager.GetModule<AssetModule>();

        /// <summary>
        /// 缓存已经加载的资源句柄，key为资源路径，value为资源句柄
        /// </summary>
        private readonly Dictionary<string, AssetHandle> m_HandleDict = new();

        /// <summary>
        /// 创建资源加载器
        /// </summary>
        /// <returns></returns>
        public static AssetLoadRegister Create()
        {
            return ReferencePool.Runtime.ReferencePool.Acquire<AssetLoadRegister>();
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

            AssetHandle assetHandle = null;
            try
            {
                assetHandle = await loadFunc();
                if (assetHandle == null || assetHandle.AssetObject == null)
                {
                    throw new FuException($"[AssetLoadRegister]资源{path}加载失败");
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
            foreach (var path in m_HandleDict.Keys)
            {
                Unload(path);
            }

            m_HandleDict.Clear();
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public void Clear() => UnloadAll();

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Runtime.ReferencePool.Release(this);
    }
}