using YooAsset;
using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 默认实体辅助器。
    /// 功能：用于实现实体的实例化、创建、释放等操作。
    /// </summary>
    public class DefaultEntityHelper: MonoBehaviour
    {
        // /// <summary>
        // /// 资源管理器。
        // /// </summary>
        // private IAssetManager _assetManager;

        /// <summary>
        /// 资源操作句柄。
        /// </summary>
        private AssetHandle _assetOperationHandle;

        // private void Start()
        // {
        //     _assetManager = FuEntry.GetModule<IAssetManager>();
        //     if (_assetManager == null) Log.Fatal("Resource component is invalid.");
        // }

        /// <summary>
        /// 同步实例化实体。
        /// </summary>
        /// <param name="entityAsset">要实例化的实体资源。</param>
        /// <returns>实例化后的实体。</returns>
        public object InstantiateEntity(object entityAsset)
        {
            _assetOperationHandle = entityAsset as AssetHandle;
            if (_assetOperationHandle != null)
                return _assetOperationHandle.InstantiateSync();

            Log.Error("entityAsset is AssetOperationHandle invalid.");
            return null;
        }

        /// <summary>
        /// 创建实体。
        /// </summary>
        /// <param name="entityInstance">实体实例。</param>
        /// <param name="entityGroup">实体所属的实体组。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>实体。</returns>
        public Entity CreateEntity(object entityInstance, EntityManager.EntityGroup entityGroup, object userData)
        {
            var go = entityInstance as GameObject;
            if (!go)
            {
                Log.Error("Entity instance is invalid.");
                return null;
            }

            go.transform.SetParent(entityGroup.Helper.transform);
            return go.GetOrAddComponent<Entity>();
        }

        /// <summary>
        /// 释放实体。
        /// </summary>
        /// <param name="entityAsset">要释放的实体资源。</param>
        /// <param name="entityInstance">要释放的实体实例。</param>
        public  void ReleaseEntity(object entityAsset, object entityInstance)
        {
            if (entityAsset is not AssetHandle assetOperationHandle)
            {
                Log.Error("entityAsset is AssetOperationHandle invalid.");
                return;
            }

            assetOperationHandle.Release();
            Destroy(entityInstance as Object);
        }
    }
}