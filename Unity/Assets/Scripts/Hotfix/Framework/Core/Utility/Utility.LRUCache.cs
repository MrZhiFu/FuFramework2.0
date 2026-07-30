using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace 禁用命名空间检查
// ReSharper disable once InconsistentNaming 禁用命名风格检查
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// LRU（最近最少使用）缓存器。
    /// 功能：
    ///     1. 提供一个LRU缓存机制，用于缓存纹理资源。
    ///     2. 支持设置最大容量，超出容量时自动淘汰最少使用的项。
    ///     3. 支持清空全部缓存并释放资源。
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
            /// YooAsset资源句柄（如果是YooAsset资源，则不为空，其他情况为null）
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
                var newItem = new CacheItem(key, texture)
                {
                    AssetHandle = assetHandle
                };
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
}
