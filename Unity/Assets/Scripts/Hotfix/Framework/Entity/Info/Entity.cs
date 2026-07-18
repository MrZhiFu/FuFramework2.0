using System;
using FuFramework.Core.Runtime;
using AOT.Framework.Core.Log;
using FuFramework.ReferencePool.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Entity
{
    /// <summary>
    /// 实体显示类。
    /// 功能：
    ///     1. 定义实体的基本属性和生命周期。并将生命周期的逻辑委托给实体逻辑类(EntityLogic)去处理。
    /// </summary>
    public sealed class Entity : MonoBehaviour
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
        public EntityGroup EntityGroup { get; private set; }

        /// <summary>
        /// 获取实体逻辑。
        /// </summary>
        public EntityLogic Logic { get; private set; }

        /// <summary>
        /// 获取实体实例。
        /// </summary>
        public object Go => gameObject;

        /// <summary>
        /// 实体初始化时触发。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroup">实体所属的实体组。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="showEntityInfoEx">显示的实体额外信息。</param>
        public void OnInit(int entityId, string entityAssetName, EntityGroup entityGroup, bool isNewInstance, ShowEntityInfoEx showEntityInfoEx)
        {
            Id              = entityId;
            EntityAssetName = entityAssetName;

            if (isNewInstance)
            {
                EntityGroup = entityGroup;
            }
            else if (EntityGroup != entityGroup)
            {
                FuLogger.LogError("[Entity]初始化实体失败, 非新实例实体的实体组不一致!");
                return;
            }

            if (showEntityInfoEx is null)
            {
                FuLogger.LogError("[Entity]初始化实体失败, 显示的实体额外信息为空!");
                return;
            }

            if (showEntityInfoEx.EntityLogicType is null)
            {
                FuLogger.LogError("[Entity]初始化实体失败, 显示的实体的逻辑类型为空!");
                return;
            }

            if (Logic)
            {
                if (Logic.GetType() == showEntityInfoEx.EntityLogicType)
                {
                    Logic.enabled = true;
                }
                else
                {
                    Destroy(Logic);
                    Logic = null;
                }
            }

            if (Logic is null)
            {
                Logic = gameObject.AddComponent(showEntityInfoEx.EntityLogicType) as EntityLogic;
                if (Logic is null)
                {
                    FuLogger.LogError($"[Entity]初始化实体失败, 添加实体{entityAssetName}逻辑组件失败!");
                    return;
                }
            }

            try
            {
                Logic.OnInit(showEntityInfoEx.UserData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]初始化实体失败, 实体'[{Id}]-{entityAssetName}逻辑组件(OnInit)时发生异常: {exception}");
            }
        }

        /// <summary>
        /// 实体轮询时触发。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        public void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            try
            {
                Logic.OnUpdate(deltaTime, unscaledDeltaTime);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 轮询(OnUpdate)时发生异常: {exception}'.");
            }
        }

        /// <summary>
        /// 实体显示时触发。
        /// </summary>
        /// <param name="entityInfoEx">用户自定义数据。</param>
        public void OnShow(ShowEntityInfoEx entityInfoEx)
        {
            try
            {
                Logic.OnShow(entityInfoEx.UserData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 显示(OnShow)时发生异常: {exception}'.");
            }
        }

        /// <summary>
        /// 实体隐藏时触发。
        /// </summary>
        /// <param name="isShutdown">是否是关闭实体管理模块时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnHide(bool isShutdown, object userData)
        {
            try
            {
                Logic.OnHide(isShutdown, userData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 隐藏(OnHide)时发生异常: {exception}'.");
            }
        }

        /// <summary>
        /// 实体回收时触发。
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
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 回收(OnRecycle)时发生异常: {exception}'.");
            }

            Id = 0;
        }

        /// <summary>
        /// 附加子实体时触发。
        /// </summary>
        /// <param name="childEntity">附加的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnAttached(Entity childEntity, object userData)
        {
            if (userData is not AttachEntityInfo attachEntityInfo)
            {
                FuLogger.LogError("[Entity]实体附加子实体失败, 附加实体信息不是AttachEntityInfo类型!");
                return;
            }

            try
            {
                Logic.OnAttached(childEntity.Logic, attachEntityInfo.ParentTransform, attachEntityInfo.UserData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 附加子实体(OnAttached)时发生异常: {exception}'.");
            }
        }

        /// <summary>
        /// 解除附加的子实体时触发。
        /// </summary>
        /// <param name="childEntity">解除的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnDetached(Entity childEntity, object userData)
        {
            try
            {
                Logic.OnDetached(childEntity.Logic, userData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 解除附加的子实体(OnDetached)时发生异常: {exception}'.");
            }
        }

        /// <summary>
        /// 被附加到父实体上时触发。
        /// </summary>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnAttachTo(Entity parentEntity, object userData)
        {
            if (userData is not AttachEntityInfo attachEntityInfo)
            {
                FuLogger.LogError("[Entity]实体附加子实体失败, 附加实体信息不是AttachEntityInfo类型!");
                return;
            }

            try
            {
                Logic.OnAttachTo(parentEntity.Logic, attachEntityInfo.ParentTransform, attachEntityInfo.UserData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 被附加到父实体上(OnAttachTo)时发生异常: {exception}'.");
            }

            ReferencePool.Release(attachEntityInfo);
        }

        /// <summary>
        /// 被从父实体上解除时触发。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnDetachFrom(Entity parentEntity, object userData)
        {
            try
            {
                Logic.OnDetachFrom(parentEntity.Logic, userData);
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[Entity]实体 '[{Id}]-{EntityAssetName}' 被从父实体上解除时触发(OnDetachFrom)时发生异常: {exception}'.");
            }
        }
    }
}