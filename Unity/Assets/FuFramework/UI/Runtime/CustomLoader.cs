using System;
using FairyGUI;
using System.IO;
using UnityEngine;
using FuFramework.Web.Runtime;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using Object = UnityEngine.Object;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
// ReSharper disable once InconsistentNaming 禁用命名风格检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// FUI自定义Loader加载器的LRU缓存机制
    /// </summary>
    public class LRUCache
    {
        /// <summary>
        /// 缓存项
        /// </summary>
        private class CacheItem
        {
            /// 缓存的Key
            public readonly string Key;

            /// 缓存的纹理
            public NTexture Texture;

            public CacheItem(string key, NTexture texture)
            {
                Key = key;
                Texture = texture;
            }
        }

        private readonly int m_MaxCapacity; // 最大容量
        private readonly Dictionary<string, CacheItem> m_CacheDict; // 缓存字典，Key为资源路径，Value为缓存项
        private readonly LinkedList<CacheItem> m_LruList; // 最近使用列表

        public LRUCache(int maxCapacity)
        {
            m_MaxCapacity = maxCapacity;
            m_CacheDict = new Dictionary<string, CacheItem>();
            m_LruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// 获取缓存的纹理，
        /// 更新已有项到最近使用位置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public NTexture Get(string key)
        {
            if (!m_CacheDict.TryGetValue(key, out var item)) return null; // 纹理未找到

            // 移动到最近使用的位置
            m_LruList.Remove(item);
            m_LruList.AddFirst(item);
            return item.Texture;
        }

        /// <summary>
        /// 缓存纹理
        /// </summary>
        /// <param name="key"></param>
        /// <param name="texture"></param>
        public void Put(string key, NTexture texture)
        {
            if (key == null) return;

            if (m_CacheDict.TryGetValue(key, out var cacheItem))
            {
                // 更新已有项并移动到最近使用位置
                m_LruList.Remove(cacheItem);
                cacheItem.Texture = texture;
                m_LruList.AddFirst(cacheItem);
            }
            else
            {
                // 如果超过最大数量，则移除最少使用的项
                if (m_CacheDict.Count >= m_MaxCapacity)
                    RemoveLeastRecentlyUsed();

                // 添加新项
                var newItem = new CacheItem(key, texture);
                m_CacheDict[key] = newItem;
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
            leastUsedItem.Texture.Dispose();

            if (leastUsedItem.Texture.nativeTexture.IsNotNull())
                Object.Destroy(leastUsedItem.Texture.nativeTexture); // 释放纹理资源

            if (leastUsedItem.Texture.alphaTexture.IsNotNull())
                Object.Destroy(leastUsedItem.Texture.alphaTexture); // 释放纹理资源
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            foreach (var item in m_LruList)
            {
                item.Texture.Dispose();
                if (item.Texture.nativeTexture.IsNotNull())
                    Object.Destroy(item.Texture.nativeTexture); // 释放纹理资源

                if (item.Texture.alphaTexture.IsNotNull())
                    Object.Destroy(item.Texture.alphaTexture); // 释放纹理资源
            }

            m_CacheDict.Clear();
            m_LruList.Clear();
        }
    }

    /// <summary>
    /// FairyGUI 自定义Loader加载器，
    /// 功能：
    /// 1.实现了网络纹理资源和YooAsset包内纹理资源的加载
    /// 2.实现了LRU缓存机制，避免重复加载资源
    /// </summary>
    public sealed class CustomLoader : GLoader
    {
        /// <summary>
        /// Loader 纹理LRU缓存
        /// </summary>
        private static readonly LRUCache Cache = new(100);

        /// <summary>
        /// 缓存路径--"Application.persistentDataPath}/FUICache/images/"
        /// </summary>
        private static string m_CachePath;

        /// <summary>
        /// 资源管理器
        /// </summary>
        private readonly AssetManager m_AssetManager;
        
        public CustomLoader()
        {
            m_AssetManager = ModuleManager.GetModule<AssetManager>();
            if (!m_AssetManager)
            {
                FuLogger.LogFatal("[CustomLoader] 资源管理器不存在!");
                return;
            }
            
            m_CachePath = Utility.Path.AppHotfixResPath + "/FUICache/images/";
        }

        /// <summary>
        /// Loader使用外部资源
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

                NTexture tempTexture = null;

                // 1.从网络资源获取
                if (url.StartsWithFast("http://") || url.StartsWithFast("https://"))
                {
                    // 先看缓存中是否有，如果有则直接使用缓存的纹理
                    var nTexture = Cache.Get(url);
                    if (!nTexture.IsNull())
                    {
                        tempTexture = nTexture;
                    }
                    else
                    {
                        var hash = Utility.Hash.MD5.Hash(url);
                        var path = $"{m_CachePath}{hash}.png";
                        var isExists = Utility.File.IsExists(path);
                        var texture2D = Texture2D.whiteTexture;

                        if (isExists)
                        {
                            var buffer = Utility.File.ReadAllBytes(path);
                            texture2D.LoadImage(buffer);
                        }
                        else
                        {
                            if (!Directory.Exists(m_CachePath)) 
                                Directory.CreateDirectory(m_CachePath);
                            
                            var webBufferResult = await ModuleManager.GetModule<WebManager>().GetToBytes(url, null);
                            Utility.File.WriteAllBytes(path, webBufferResult.Result);
                            texture2D.LoadImage(webBufferResult.Result);
                        }

                        tempTexture = new NTexture(texture2D);
                        Cache.Put(url, tempTexture);
                    }
                }

                // 2.从FairyGUI的Package包内获取
                else if (url.StartsWithFast("ui://"))
                {
                    LoadContent();
                }

                // 3.从资源管理器中获取
                else
                {
                    // 先看缓存中是否有，有则使用缓存的纹理
                    var nTexture = Cache.Get(url);
                    if (nTexture.IsNotNull())
                    {
                        tempTexture = nTexture;
                    }
                    else
                    {
                        var assetInfo = m_AssetManager.GetAssetInfo(url);
                        if (assetInfo.IsInvalid == false)
                        {
                            var assetHandle = await m_AssetManager.LoadAssetAsync<Texture2D>(url);
                            if (assetHandle.IsDone)
                            {
                                tempTexture = new NTexture(assetHandle.GetAssetObject<Texture2D>());
                                Cache.Put(url, tempTexture);
                            }
                        }
                        else
                        {
                            if (Utility.File.IsExists(url))
                            {
                                var buffer = Utility.File.ReadAllBytes(url);

                                var texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false, false);
                                texture2D.LoadImage(buffer);
                                tempTexture = new NTexture(texture2D);
                                Cache.Put(url, tempTexture);
                            }
                        }
                    }
                }

                if (tempTexture.IsNotNull())
                    onExternalLoadSuccess(tempTexture);
                else
                    onExternalLoadFailed();
            }
            catch (Exception e)
            {
                onExternalLoadFailed();
                FuLogger.LogError(e);
            }
        }
    }
}