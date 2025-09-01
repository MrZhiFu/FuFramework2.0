using System;
using FuFramework.Core.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 实体显示类。
    /// 功能：定义实体的基本属性和生命周期。并将生命周期的逻辑委托给实体逻辑类去处理。
    /// </summary>
    public sealed class Entity : MonoBehaviour, IEntity
    {
        /// <summary>
        /// 获取实体编号。
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 获取实体资源名称。
        /// </summary>
        public string EntityAssetName { get; private set; }

        /// <summary>
        /// 获取实体所属的实体组。
        /// </summary>
        public IEntityGroup EntityGroup { get; private set; }

        /// <summary>
        /// 获取实体逻辑。
        /// </summary>
        public EntityLogic Logic { get; private set; }

        /// <summary>
        /// 获取实体实例。
        /// </summary>
        public object Handle => gameObject;

        /// <summary>
        /// 实体初始化。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroup">实体所属的实体组。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnInit(int entityId, string entityAssetName, IEntityGroup entityGroup, bool isNewInstance, object userData)
        {
            Id              = entityId;
            EntityAssetName = entityAssetName;

            if (isNewInstance)
            {
                EntityGroup = entityGroup;
            }
            else if (EntityGroup != entityGroup)
            {
                Log.Error("Entity group is inconsistent for non-new-instance entity.");
                return;
            }

            var showEntityInfo  = (ShowEntityInfo)userData;
            var entityLogicType = showEntityInfo.EntityLogicType;
            if (entityLogicType == null)
            {
                Log.Error("Entity logic type is invalid.");
                return;
            }

            if (Logic)
            {
                if (Logic.GetType() == entityLogicType)
                {
                    Logic.enabled = true;
                }
                else
                {
                    Destroy(Logic);
                    Logic = null;
                }
            }

            if (!Logic)
            {
                Logic = gameObject.AddComponent(entityLogicType) as EntityLogic;
                if (!Logic)
                {
                    Log.Error("Entity '{0}' can not add entity logic.", entityAssetName);
                    return;
                }
            }

            try
            {
                Logic.OnInit(showEntityInfo.UserData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnInit with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }

        /// <summary>
        /// 实体轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            try
            {
                Logic.OnUpdate(elapseSeconds, realElapseSeconds);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnUpdate with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }

        /// <summary>
        /// 实体显示。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void OnShow(object userData)
        {
            var showEntityInfo = (ShowEntityInfo)userData;
            try
            {
                Logic.OnShow(showEntityInfo.UserData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnShow with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }

        /// <summary>
        /// 实体隐藏。
        /// </summary>
        /// <param name="isShutdown">是否是关闭实体管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnHide(bool isShutdown, object userData)
        {
            try
            {
                Logic.OnHide(isShutdown, userData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnHide with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }

        /// <summary>
        /// 实体回收。
        /// </summary>
        public void OnRecycle()
        {
            try
            {
                Logic.OnRecycle();
                Logic.enabled = false;
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnRecycle with exception '{2}'.", Id, EntityAssetName, exception);
            }

            Id = 0;
        }

        /// <summary>
        /// 实体附加子实体。
        /// </summary>
        /// <param name="childEntity">附加的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnAttached(IEntity childEntity, object userData)
        {
            var attachEntityInfo = (AttachEntityInfo)userData;
            try
            {
                Logic.OnAttached(((Entity)childEntity).Logic, attachEntityInfo.ParentTransform, attachEntityInfo.UserData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnAttached with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }

        /// <summary>
        /// 实体解除子实体。
        /// </summary>
        /// <param name="childEntity">解除的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnDetached(IEntity childEntity, object userData)
        {
            try
            {
                Logic.OnDetached(((Entity)childEntity).Logic, userData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnDetached with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }

        /// <summary>
        /// 实体附加子实体。
        /// </summary>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnAttachTo(IEntity parentEntity, object userData)
        {
            var attachEntityInfo = (AttachEntityInfo)userData;
            try
            {
                Logic.OnAttachTo(((Entity)parentEntity).Logic, attachEntityInfo.ParentTransform, attachEntityInfo.UserData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnAttachTo with exception '{2}'.", Id, EntityAssetName, exception);
            }

            ReferencePool.Runtime.ReferencePool.Release(attachEntityInfo);
        }

        /// <summary>
        /// 实体解除子实体。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnDetachFrom(IEntity parentEntity, object userData)
        {
            try
            {
                Logic.OnDetachFrom(((Entity)parentEntity).Logic, userData);
            }
            catch (Exception exception)
            {
                Log.Error("Entity '[{0}]{1}' OnDetachFrom with exception '{2}'.", Id, EntityAssetName, exception);
            }
        }
    }
}