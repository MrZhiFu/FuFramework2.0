using System;
using FairyGUI;
using System.IO;
using UnityEngine;
using Hotfix.Web;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using UtilityAOT = FuFramework.Core.Runtime.UtilityAOT;
using YooAsset;
using Object = UnityEngine.Object;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
// ReSharper disable once InconsistentNaming 禁用命名风格检查
namespace Hotfix.UI
{
    /// <summary>
    /// FUI自定义Loader的资源LRU缓存器
    /// 功能：
    ///     1. 提供一个LRU缓存机制，用于缓存加载的纹理资源。
    /// </summary>
    public class LRUCache
    {
        /// <summary>
        /// 缓存项
        /// </summary>
        private class CacheItem
        {
            /// <summary>
            /// 缓存的Key
            /// </summary>
            public readonly string Key;

            /// <summary>
            /// 缓存的纹理
            /// </summary>
            public NTexture Texture;

            /// <summary>
            /// YooAsset资源句柄(如果是YooAsset资源，则不为空，其他情况为null)
            /// </summary>
            public AssetHandle AssetHandle;

            public CacheItem(string key, NTexture texture)
            {
                Key         = key;
                Texture     = texture;
                AssetHandle = null;
            }
        }

        /// <summary>
        /// 最大容量
        /// </summary>
        private readonly int m_MaxCapacity;

        /// <summary>
        /// 缓存字典，Key为资源路径，Value为缓存项
        /// </summary>
        private readonly Dictionary<string, CacheItem> m_CacheDict;

        /// <summary>
        /// 最近使用列表
        /// </summary>
        private readonly LinkedList<CacheItem> m_LruList;

        public LRUCache(int maxCapacity)
        {
            m_MaxCapacity = maxCapacity;
            m_CacheDict   = new Dictionary<string, CacheItem>();
            m_LruList     = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// 获取缓存的纹理，
        /// 更新已有项到最近使用位置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public NTexture Get(string key)
        {
            // 缓存的纹理未找到
            if (!m_CacheDict.TryGetValue(key, out var item)) return null;

            // 移动到最近使用的位置
            m_LruList.Remove(item);
            m_LruList.AddFirst(item);
            return item.Texture;
        }

        /// <summary>
        /// 缓存纹理
        /// </summary>
        /// <param name="key">资源路径</param>
        /// <param name="texture">资源纹理</param>
        /// <param name="assetHandle">YooAsset资源句柄</param>
        public void Put(string key, NTexture texture, AssetHandle assetHandle)
        {
            if (key.IsNull()) return;

            if (m_CacheDict.TryGetValue(key, out var cacheItem))
            {
                // 释放旧的纹理和资源句柄
                cacheItem.Texture?.Dispose();
                if (cacheItem.Texture?.nativeTexture.IsNotNull() == true)
                {
                    Object.Destroy(cacheItem.Texture.nativeTexture);
                }

                if (cacheItem.Texture?.alphaTexture.IsNotNull() == true)
                {
                    Object.Destroy(cacheItem.Texture.alphaTexture);
                }

                cacheItem.AssetHandle?.Release();

                // 更新已有项并移动到最近使用位置
                m_LruList.Remove(cacheItem);
                cacheItem.Texture     = texture;
                cacheItem.AssetHandle = assetHandle;
                m_LruList.AddFirst(cacheItem);
            }
            else
            {
                // 如果超过最大数量，则移除最少使用的项
                if (m_CacheDict.Count >= m_MaxCapacity)
                    RemoveLeastRecentlyUsed();

                // 添加新项
                var newItem = new CacheItem(key, texture);
                newItem.AssetHandle = assetHandle;
                m_CacheDict[key]    = newItem;
                m_LruList.AddFirst(newItem);
            }
        }

        /// <summary>
        /// 移除最少使用的项
        /// </summary>
        private void RemoveLeastRecentlyUsed()
        {
            if (m_LruList.Count <= 0) return;

            var leastUsedItem = m_LruList.Last.Value;

            m_LruList.RemoveLast();
            m_CacheDict.Remove(leastUsedItem.Key);

            // 释放纹理和资源句柄
            leastUsedItem.Texture?.Dispose();
            if (leastUsedItem.Texture?.nativeTexture.IsNotNull() == true)
            {
                Object.Destroy(leastUsedItem.Texture.nativeTexture);
            }

            if (leastUsedItem.Texture?.alphaTexture.IsNotNull() == true)
            {
                Object.Destroy(leastUsedItem.Texture.alphaTexture);
            }

            leastUsedItem.AssetHandle?.Release();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            foreach (var item in m_LruList)
            {
                item.Texture?.Dispose();
                if (item.Texture?.nativeTexture.IsNotNull() == true)
                {
                    Object.Destroy(item.Texture.nativeTexture);
                }

                if (item.Texture?.alphaTexture.IsNotNull() == true)
                {
                    Object.Destroy(item.Texture.alphaTexture);
                }

                item.AssetHandle?.Release();
            }

            m_CacheDict.Clear();
            m_LruList.Clear();
        }
    }

    /// <summary>
    /// 自定义FUI的Loader加载器。
    /// 目标：提供一个自定义的Loader加载器，用于加载Loader的纹理资源。
    /// 功能:
    ///     1. 实现了网络纹理资源和YooAsset包内纹理资源的加载。
    ///     2. 实现了LRU缓存机制，避免重复加载资源。
    /// </summary>
    public sealed class CustomLoader : GLoader
    {
        /// <summary>
        /// Loader纹理LRU缓存
        /// </summary>
        private static readonly LRUCache Cache = new(100);

        /// <summary>
        /// 缓存路径--"Application.persistentDataPath}/FUICache/images/"
        /// </summary>
        private static readonly string CachePath = UtilityAOT.Path.AppHotfixResPath + "/FUICache/images/";

        /// <summary>
        /// 资源管理模块
        /// </summary>
        private readonly AssetModule m_AssetModule;

        public CustomLoader()
        {
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
            if (m_AssetModule == null)
            {
                throw new InvalidOperationException("[CustomLoader] 资源管理模块不存在!");
            }
        }

        /// <summary>
        /// Loader使用外部加载的纹理资源
        /// </summary>
        protected override async void LoadExternal()
        {
            try
            {
                if (url.IsNullOrWhiteSpace())
                {
                    onExternalLoadFailed();
                    return;
                }

                // 1.优先从FairyGUI资源包中加载
                if (url.StartsWithFast("ui://"))
                {
                    LoadContent();
                    return;
                }

                // 2.看缓存中是否有，如果有则直接使用缓存的纹理
                var targetTexture = Cache.Get(url);
                if (!targetTexture.IsNull())
                {
                    onExternalLoadSuccess(targetTexture);
                    return;
                }

                // 根据URL类型加载纹理
                Texture2D   texture2D   = null;
                AssetHandle assetHandle = null;
                if (url.StartsWithFast("http://") || url.StartsWithFast("https://"))
                {
                    // 3.从网络加载
                    texture2D = await LoadTextureFromNetwork(url);
                }
                else
                {
                    // 4.从资源管理模块加载
                    assetHandle = await LoadTextureFromAsset(url);
                    if (assetHandle.IsNotNull() && assetHandle.IsDone)
                    {
                        texture2D = assetHandle.GetAssetObject<Texture2D>();
                        if (texture2D.IsNull())
                        {
                            // 资源存在但不是Texture2D类型，释放句柄
                            assetHandle.Release();
                            assetHandle = null;
                        }
                    }
                }

                // 创建纹理并缓存
                if (texture2D.IsNotNull())
                {
                    targetTexture = new NTexture(texture2D);
                    Cache.Put(url, targetTexture, assetHandle);
                    onExternalLoadSuccess(targetTexture);
                }
                else
                {
                    onExternalLoadFailed();
                }
            }
            catch (Exception e)
            {
                onExternalLoadFailed();
                FuLogger.LogError(e);
            }
        }

        /// <summary>
        /// 从网络加载纹理
        /// </summary>
        /// <param name="url">网络URL地址。</param>
        /// <returns>加载完成的Texture2D。</returns>
        private async UniTask<Texture2D> LoadTextureFromNetwork(string url)
        {
            var textureHashName = Utility.Hash.MD5.Hash(url);
            var texturePath     = $"{CachePath}{textureHashName}.png";

            // 本地文件存在，直接读取(从StreamingAssets或persistentDataPath下)
            if (UtilityAOT.File.IsExists(texturePath))
            {
                return LoadTextureFromFile(texturePath);
            }

            // 从网络下载并保存到本地缓存(persistentDataPath)
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);

            var webBufferResult = await WebModule.Instance.GetToBytes(url, null);
            if (webBufferResult.IsNull() || webBufferResult.Result.IsNull() || webBufferResult.Result.Length == 0)
            {
                FuLogger.LogError($"[CustomLoader] 网络图片下载失败: {url}");
                return null;
            }

            UtilityAOT.File.WriteAllBytes(texturePath, webBufferResult.Result);

            // 创建临时2x2纹理(占位)，LoadImage 内部重新分配为实际图片尺寸
            var tempTexture = new Texture2D(2, 2);
            tempTexture.LoadImage(webBufferResult.Result);
            return tempTexture;
        }

        /// <summary>
        /// 从资源管理模块加载纹理
        /// </summary>
        /// <param name="url">资源路径。</param>
        /// <returns>加载完成的Texture2D。</returns>
        private async UniTask<AssetHandle> LoadTextureFromAsset(string url)
        {
            var assetInfo = m_AssetModule.GetAssetInfo(url);
            if (assetInfo.IsInvalid) return null;
            return await m_AssetModule.LoadAssetAsync<Texture2D>(url);
        }

        /// <summary>
        /// 从本地文件加载纹理
        /// </summary>
        /// <param name="path">文件路径。</param>
        /// <returns>加载完成的Texture2D，失败返回null。</returns>
        private Texture2D LoadTextureFromFile(string path)
        {
            try
            {
                var buffer = UtilityAOT.File.ReadAllBytes(path);
                if (buffer.IsNull() || buffer.Length == 0)
                {
                    FuLogger.LogError($"[CustomLoader] 读取文件失败或文件为空: {path}");
                    return null;
                }

                // 创建临时2x2纹理(占位)，LoadImage方法内部会重新分配为实际图片尺寸
                var tempTexture = new Texture2D(2, 2);
                if (!tempTexture.LoadImage(buffer))
                {
                    FuLogger.LogError($"[CustomLoader] 加载图片数据失败: {path}");
                    Object.Destroy(tempTexture);
                    return null;
                }

                return tempTexture;
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[CustomLoader] 从文件加载纹理异常: {path}, {e.Message}");
                return null;
            }
        }
    }
}
