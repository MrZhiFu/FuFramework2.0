using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using Object = UnityEngine.Object;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源加载注册器。
    /// 1.加载资源。
    /// 2.记录加载过的资源路径，避免重复加载。
    /// 3.卸载已经加载的资源。
    /// </summary>
    public class AssetLoadRegister : IReference
    {

        /// 缓存已经加载的资源路径列表
        private readonly Dictionary<string, Object> m_resDict = new();

        /// <summary>
        /// 创建资源加载器
        /// </summary>
        /// <returns></returns>
        public static AssetLoadRegister Create()
        {
            return ReferencePool.Acquire<AssetLoadRegister>();
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<T> Load<T>(string path) where T : Object
        {
            if (m_resDict.TryGetValue(path, out var obj)) return obj as T;

            var assetHandle = await AssetManager.Instance.LoadAssetAsync<T>(path);
            var isSuccess   = assetHandle != null && assetHandle.AssetObject != null;
            if (!isSuccess) throw new FuException($"[AssetLoader]资源{path}加载失败.");

            var assetObject = assetHandle.GetAssetObject<T>();
            assetHandle.Release();

            Log.Info($"[AssetLoader]加载{path}资源完成.");

            m_resDict.Add(path, assetObject);
            return assetObject;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public async UniTask<Object> Load(string path, Type type)
        {
            if (m_resDict.TryGetValue(path, out var obj)) return obj;

            // 等待资源文件加载完成
            var assetHandle = await AssetManager.Instance.LoadAssetAsync(path, type);
            var isSuccess   = assetHandle != null && assetHandle.AssetObject != null;

            if (!isSuccess)
                throw new FuException($"[AssetLoader]资源{path}加载失败");

            var assetObject = assetHandle.AssetObject;
            assetHandle.Release();

            Log.Info($"[AssetLoader]加载{path}资源完成.");

            m_resDict.Add(path, assetObject);
            return assetObject;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public async UniTask<Object> Load(string path)
        {
            if (m_resDict.TryGetValue(path, out var obj)) return obj;

            var assetHandle = await AssetManager.Instance.LoadAssetAsync(path);
            var isSuccess   = assetHandle != null && assetHandle.AssetObject != null;
            if (!isSuccess) throw new FuException($"[AssetLoader]资源{path}加载失败.");

            var assetObject = assetHandle.AssetObject;
            assetHandle.Release();

            Log.Info($"[AssetLoader]加载{path}资源完成.");

            m_resDict.Add(path, assetObject);
            return assetObject;
        }

        /// <summary>
        /// 卸载已经加载的资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public void Unload(string path)
        {
            if (!m_resDict.ContainsKey(path)) return;
            AssetManager.Instance.UnloadAsset(path);
            m_resDict.Remove(path);
            Log.Info($"[AssetLoader]释放{path}资源完成.");
        }

        /// <summary>
        /// 卸载所有已经加载的资源。
        /// </summary>
        private void UnloadAll()
        {
            foreach (var path in m_resDict.Keys)
            {
                AssetManager.Instance.UnloadAsset(path);
                Log.Info($"[AssetLoader]释放{path}资源完成.");
            }

            m_resDict.Clear();
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public void Clear() => UnloadAll();

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Release(this);
    }
}