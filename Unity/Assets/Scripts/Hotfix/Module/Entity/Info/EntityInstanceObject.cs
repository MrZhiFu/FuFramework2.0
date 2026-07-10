using FuFramework.Core.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.ReferencePool.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Entity
{
    /// <summary>
    /// 实体实例对象。
    /// 功能：
    ///     1. 包装实体资源和实体帮助器，用于实体实例对象池管理，方便释放实体资源。
    /// </summary>
    public sealed class EntityInstanceObject : ObjectBase
    {
        /// <summary>
        /// 实体资源句柄
        /// </summary>
        private object m_EntityAssetHandle;

        /// <summary>
        /// 实体帮助器
        /// </summary>
        private EntityHelper m_EntityHelper;

        /// <summary>
        /// 创建实体实例对象
        /// </summary>
        /// <param name="name">实体名称</param>
        /// <param name="entityAssetHandle">实体资源句柄</param>
        /// <param name="entityInstanceGo">实体实例GameObject</param>
        /// <param name="entityHelper"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        public static EntityInstanceObject Create(string name, object entityAssetHandle, GameObject entityInstanceGo, EntityHelper entityHelper)
        {
            if (entityAssetHandle is null) throw new FuException("[EntityInstanceObject] 创建实体实例对象失败，实体资源句柄为空.");
            if (entityHelper is null) throw new FuException("[EntityInstanceObject] 创建实体实例对象失败，实体辅助器为空.");

            var entityInstanceObject = ReferencePool.Acquire<EntityInstanceObject>();
            entityInstanceObject.Initialize(name, entityInstanceGo);
            entityInstanceObject.m_EntityAssetHandle = entityAssetHandle;
            entityInstanceObject.m_EntityHelper      = entityHelper;
            return entityInstanceObject;
        }

        /// <summary>
        /// 清理实体实例对象
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            m_EntityAssetHandle = null;
            m_EntityHelper      = null;
        }

        /// <summary>
        /// 释放实体
        /// </summary>
        protected override void OnRelease()
        {
            m_EntityHelper.ReleaseEntity(m_EntityAssetHandle, Target);
        }
    }
}