using System;
using FairyGUI;
using System.IO;
using UnityEngine;
using Hotfix.Framework.Web;
using Hotfix.Framework.Asset;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using YooAsset;
using Object = UnityEngine.Object;
using Utility = Hotfix.Framework.Core.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
// ReSharper disable once InconsistentNaming 禁用命名风格检查
namespace Hotfix.Framework.UI
{
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
        private static readonly LRUCache<string, TextureCacheEntry> Cache = new(100, OnCacheEvict);

        /// <summary>
        /// 纹理缓存条目，同时持有 NTexture 和 YooAsset 资源句柄
        /// </summary>
        private sealed class TextureCacheEntry
        {
            /// <summary>
            /// FairyGUI 纹理
            /// </summary>
            public NTexture Texture;

            /// <summary>
            /// YooAsset 资源句柄（非 YooAsset 资源时为 null）
            /// </summary>
            public AssetHandle AssetHandle;
        }

        /// <summary>
        /// LRU 缓存驱逐回调：释放 NTexture 原生纹理和 YooAsset 资源句柄
        /// </summary>
        /// <param name="key">被淘汰的缓存键</param>
        /// <param name="entry">被淘汰的缓存条目</param>
        private static void OnCacheEvict(string key, TextureCacheEntry entry)
        {
            if (entry == null) return;

            entry.Texture?.Dispose();
            if (entry.Texture?.nativeTexture != null)
            {
                Object.Destroy(entry.Texture.nativeTexture);
            }

            if (entry.Texture?.alphaTexture != null)
            {
                Object.Destroy(entry.Texture.alphaTexture);
            }

            entry.AssetHandle?.Release();
        }

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
                if (Cache.TryGet(url, out var cachedEntry))
                {
                    onExternalLoadSuccess(cachedEntry.Texture);
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
                    var targetTexture = new NTexture(texture2D);
                    Cache.Put(url, new TextureCacheEntry { Texture = targetTexture, AssetHandle = assetHandle });
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
            if (assetInfo == null) return null;
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