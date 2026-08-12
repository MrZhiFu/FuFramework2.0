using YooAsset;
using UnityEngine;
using Hotfix.Framework.Core;
using Hotfix.Framework.Asset;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 默认实体辅助器。
    /// 功能：
    ///     1. 用于实现实体的实例化、创建、释放等操作。
    /// </summary>
    public class EntityHelper : MonoBehaviour
    {
        /// <summary>
        /// 同步实例化实体。
        /// </summary>
        /// <param name="entityAssetHandle">要实例化的实体资源句柄。</param>
        /// <returns>实例化后的实体。</returns>
        public GameObject InstantiateEntity(object entityAssetHandle)
        {
            // 句柄仅在本方法内使用（局部变量），不长期持有引用，避免保留已释放句柄的误导性死状态
            var assetOperationHandle = entityAssetHandle as AssetHandle;
            if (assetOperationHandle is null)
            {
                FuLogger.LogError("[EntityHelper]实例化实体失败，要实例化的实体资源句柄为空!");
                return null;
            }

            return assetOperationHandle.InstantiateSync();
        }

        /// <summary>
        /// 创建实体。
        /// </summary>
        /// <param name="entityGo">实体实例。</param>
        /// <param name="entityGroup">实体所属的实体组。</param>
        /// <returns>实体。</returns>
        public Entity CreateEntity(object entityGo, EntityGroup entityGroup)
        {
            var go = entityGo as GameObject;
            if (!go)
            {
                FuLogger.LogError("[EntityHelper]创建实体失败，实体实例不是GameObject.");
                return null;
            }

            go.transform.SetParent(entityGroup.GroupGo.transform);
            return go.GetOrAddComponent<Entity>();
        }

        /// <summary>
        /// 释放实体。
        /// </summary>
        /// <param name="entityAssetHandle">要释放的实体资源句柄。</param>
        /// <param name="entityGo">要释放的实体实例。</param>
        public void ReleaseEntity(object entityAssetHandle, object entityGo)
        {
            if (entityAssetHandle is not AssetHandle assetOperationHandle)
            {
                FuLogger.LogError("[EntityHelper]释放实体失败, 实体资源句柄为空!");
                return;
            }

            var assetPath = assetOperationHandle.GetAssetInfo().AssetPath; // 释放前取路径（_assetInfo 为构造时缓存）
            assetOperationHandle.Release();
            ModuleManager.GetModule<AssetModule>()?.UnloadAsset(assetPath); // 池淘汰后显式卸载，避免 bundle 残留（AutoUnload 默认关闭）
            Destroy(entityGo as Object);
        }
    }
}
