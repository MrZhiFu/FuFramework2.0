using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.ObjectPool;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 实体对象。
    /// 功能：
    ///     1. 包装实体资源和实体帮助器，用于实体实例对象池管理，方便销毁实体资源。
    /// </summary>
    public sealed class EntityObject : ObjectBase
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
        /// <param name="entityGo">实体实例GameObject</param>
        /// <param name="entityHelper"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static EntityObject Create(string name, object entityAssetHandle, GameObject entityGo, EntityHelper entityHelper)
        {
            if (entityAssetHandle is null) throw new InvalidOperationException("[EntityObject] 创建实体实例对象失败，实体资源句柄为空.");
            if (entityHelper is null) throw new InvalidOperationException("[EntityObject] 创建实体实例对象失败，实体辅助器为空.");

            var entityObject = GlobalModule.ReferencePoolModule.Acquire<EntityObject>();
            entityObject.Initialize(name, entityGo);
            entityObject.m_EntityAssetHandle = entityAssetHandle;
            entityObject.m_EntityHelper      = entityHelper;
            return entityObject;
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
        /// 销毁实体。
        /// ObjectBase.OnDispose 为 protected internal abstract，ObjectBase 现与子类同属 Hotfix 程序集，
        /// 同程序集重写须保留 internal（写成 protected 会触发 CS0507），请勿改为 protected override。
        /// </summary>
        protected internal override void OnDispose()
        {
            m_EntityHelper.ReleaseEntity(m_EntityAssetHandle, Target);
        }
    }
}
