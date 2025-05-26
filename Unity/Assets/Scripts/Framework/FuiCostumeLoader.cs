#if ENABLE_UI_FAIRYGUI
using System.IO;
using FairyGUI;
using GameFrameX.Runtime;

namespace Unity.Startup
{
    using System.Collections.Generic;
    using UnityEngine;

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
            /// <summary>
            /// 缓存的Key
            /// </summary>
            public readonly string Key;

            /// <summary>
            /// 缓存的纹理
            /// </summary>
            public NTexture Texture;

            public CacheItem(string key, NTexture texture)
            {
                Key     = key;
                Texture = texture;
            }
        }

        private readonly int                           _maxCapacity; // 最大容量
        private readonly Dictionary<string, CacheItem> _cacheDict;   // 缓存字典，Key为资源路径，Value为缓存项
        private readonly LinkedList<CacheItem>         _lruList;     // 最近使用列表

        public LRUCache(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _cacheDict   = new Dictionary<string, CacheItem>();
            _lruList     = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// 获取缓存的纹理，
        /// 更新已有项到最近使用位置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public NTexture Get(string key)
        {
            if (!_cacheDict.TryGetValue(key, out var item)) return null; // 纹理未找到

            // 移动到最近使用的位置
            _lruList.Remove(item);
            _lruList.AddFirst(item);
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

            if (_cacheDict.TryGetValue(key, out var cacheItem))
            {
                // 更新已有项并移动到最近使用位置
                _lruList.Remove(cacheItem);
                cacheItem.Texture = texture;
                _lruList.AddFirst(cacheItem);
            }
            else
            {
                // 如果超过最大数量，则移除最少使用的项
                if (_cacheDict.Count >= _maxCapacity)
                    RemoveLeastRecentlyUsed();

                // 添加新项
                var newItem = new CacheItem(key, texture);
                _cacheDict[key] = newItem;
                _lruList.AddFirst(newItem);
            }
        }


        /// <summary>
        /// 移除最少使用的项
        /// </summary>
        private void RemoveLeastRecentlyUsed()
        {
            if (_lruList.Count <= 0) return;

            var leastUsedItem = _lruList.Last.Value;

            _lruList.RemoveLast();
            _cacheDict.Remove(leastUsedItem.Key);
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
            foreach (var item in _lruList)
            {
                item.Texture.Dispose();
                if (item.Texture.nativeTexture.IsNotNull())
                    Object.Destroy(item.Texture.nativeTexture); // 释放纹理资源

                if (item.Texture.alphaTexture.IsNotNull())
                    Object.Destroy(item.Texture.alphaTexture); // 释放纹理资源
            }

            _cacheDict.Clear();
            _lruList.Clear();
        }
    }


    /// <summary>
    /// FairyGUI 自定义Loader加载器，
    /// 功能：
    /// 1.实现了网络资源和AB包内资源的加载
    /// 2.实现了LRU缓存机制，避免重复加载资源
    /// </summary>
    public sealed class FuiCostumeLoader : GLoader
    {
        /// <summary>
        /// Loader 纹理LRU缓存
        /// </summary>
        private static readonly LRUCache s_Cache = new(100);

        /// <summary>
        /// 缓存路径--"Application.persistentDataPath}/{game}/cache/images/"
        /// </summary>
        private static string _cachePath;

        public FuiCostumeLoader()
        {
            _cachePath = PathHelper.AppHotfixResPath + "/cache/images/";
            if (!Directory.Exists(_cachePath))
            {
                Directory.CreateDirectory(_cachePath);
            }
        }

        /// <summary>
        /// Loader使用外部资源
        /// </summary>
        protected override async void LoadExternal()
        {
            if (url.IsNullOrWhiteSpace())
            {
                onExternalLoadFailed();
                return;
            }

            NTexture tempTexture = null;
            if (url.StartsWithFast("http://") || url.StartsWithFast("https://"))
            {
                // 1.先看缓存中是否有，如果没有则从网络资源获取
                var nTexture = s_Cache.Get(url);
                if (!nTexture.IsNull())
                {
                    tempTexture = nTexture;
                }
                else
                {
                    var hash = Utility.Hash.MD5.Hash(url);

                    var path      = $"{_cachePath}{hash}.png";
                    var isExists  = FileHelper.IsExists(path);
                    var texture2D = Texture2D.whiteTexture;
                    if (isExists)
                    {
                        var buffer = FileHelper.ReadAllBytes(path);
                        texture2D.LoadImage(buffer);
                    }
                    else
                    {
                        var webBufferResult = await GameApp.Web.GetToBytes(url, null);
                        FileHelper.WriteAllBytes(path, webBufferResult.Result);
                        texture2D.LoadImage(webBufferResult.Result);
                    }

                    tempTexture = new NTexture(texture2D);
                    s_Cache.Put(url, tempTexture);
                }
            }
            else if (url.StartsWithFast("ui://"))
            {
                // 2.从FairyGUI的Package包内获取
                LoadContent();
            }
            else
            {
                // 1.先看缓存中是否有，没有则从资源管理器中获取
                var nTexture = s_Cache.Get(url);
                if (nTexture.IsNotNull())
                {
                    tempTexture = nTexture;
                }
                else
                {
                    var assetInfo = GameApp.Asset.GetAssetInfo(url);
                    if (assetInfo.IsInvalid == false)
                    {
                        var assetHandle = await GameApp.Asset.LoadAssetAsync<Texture2D>(url);
                        if (assetHandle.IsSucceed)
                        {
                            tempTexture = new NTexture(assetHandle.GetAssetObject<Texture2D>());
                            s_Cache.Put(url, tempTexture);
                        }
                    }
                    else
                    {
                        if (FileHelper.IsExists(url))
                        {
                            var buffer = FileHelper.ReadAllBytes(url);

                            var texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false, false);
                            texture2D.LoadImage(buffer);
                            tempTexture = new NTexture(texture2D);
                            s_Cache.Put(url, tempTexture);
                        }
                    }
                }
            }

            if (tempTexture.IsNotNull())
                onExternalLoadSuccess(tempTexture);
            else
                onExternalLoadFailed();
        }
    }
}
#endif