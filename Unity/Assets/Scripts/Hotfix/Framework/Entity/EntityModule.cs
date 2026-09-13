using System;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Config;
using Hotfix.Game.Config;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Event;
using Hotfix.Framework.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 实体管理模块。
    /// 功能：
    ///     1. 管理实体组的创建、销毁等流程。
    ///     2. 管理实体的创建、销毁、显示、隐藏等流程。
    ///     3. 管理实体的生命周期。
    ///     4. 管理实体的资源加载。
    ///     5. 管理实体的对象池。
    ///     6. 管理实体的依赖资源加载。
    /// 对外公共接口见 EntityModule.API.cs。
    /// </summary>
    public sealed partial class EntityModule : ModuleBase, ICancelAsync
    {
        /// <summary>
        /// 记录所有实体的字典，Key为实体编号，Value为实体信息，便于快速查找
        /// </summary>
        private readonly Dictionary<int, EntityInfo> m_EntityDict = new();

        /// <summary>
        /// 记录所有实体组的字典，Key为实体组名称，Value为实体组
        /// </summary>
        private readonly Dictionary<string, EntityGroup> m_EntityGroupDict = new();

        /// <summary>
        /// 正在加载的实体编号字典，Key为实体编号，Value为实体自增编号
        /// </summary>
        private readonly Dictionary<int, int> m_LoadingEntityDict = new();

        /// <summary>
        /// 记录在加载中但是需要释放的实体id集合，防止在加载实体过程中被回收的情况
        /// </summary>
        private readonly HashSet<int> m_LoadingToReleaseSet = new();

        /// <summary>
        /// 待回收的实体信息队列
        /// </summary>
        private readonly Queue<EntityInfo> m_WaitRecycleQueue = new();

        /// <summary>
        /// 实体辅助器
        /// </summary>
        private EntityHelper m_EntityHelper;

        /// <summary>
        /// 实体自增编号
        /// </summary>
        private int m_Serial;

        /// <summary>
        /// 是否关闭
        /// </summary>
        private bool m_IsShutdown;

        /// <summary>
        /// 取消范围：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
        /// OnDispose 时 Cancel，在途实体加载随之取消；框架重启前经 CancelAllAsync 等待取消清理完成。
        /// </summary>
        private CancellationScope m_Scope = new();

        /// <summary>
        /// 事件管理模块
        /// </summary>
        private EventModule m_EventModule;

        /// <summary>
        /// 资源管理模块
        /// </summary>
        private AssetModule m_AssetModule;

        /// <summary>
        /// 对象池管理模块
        /// </summary>
        private ObjectPoolModule m_ObjectPoolModule;

        /// <summary>
        /// 实体对象根节点
        /// </summary>
        private Transform m_EntityRoot;

        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance     = this;
            m_Scope      = new CancellationScope(); // 新生命周期 = 新 Token
            m_IsShutdown = false;                   // 重启时重置关闭标记（OnDispose 曾置位）

            m_AssetModule      = ModuleManager.GetModule<AssetModule>();
            m_EventModule      = ModuleManager.GetModule<EventModule>();
            m_ObjectPoolModule = ModuleManager.GetModule<ObjectPoolModule>();

            if (m_AssetModule == null)
            {
                FuLogger.LogFatal("[EntityModule] 资源管理模块不存在!");
                return;
            }

            if (m_EventModule == null)
            {
                FuLogger.LogFatal("[EntityModule] 事件模块不存在!");
                return;
            }

            if (m_ObjectPoolModule == null)
            {
                FuLogger.LogFatal("[EntityModule] 对象池模块不存在!");
                return;
            }

            // 创建实体对象根节点
            m_EntityRoot            = new GameObject("EntityObject").transform;
            m_EntityRoot.localScale = Vector3.one;

            // 创建实体辅助器
            var entityHelperGo = new GameObject("Entity Helper");
            entityHelperGo.transform.localScale = Vector3.one;
            var entityHelper = entityHelperGo.AddComponent<EntityHelper>();
            m_EntityHelper = entityHelper;

            // 获取实体组配置表，并创建添加实体组
            var tbEntityGroup = ConfigModule.Instance.GetConfig<TbEntityGroup>();
            if (tbEntityGroup == null || tbEntityGroup.Count == 0)
            {
                FuLogger.LogFatal("[EntityModule] 实体组配置表未加载，EntityModule 初始化失败!");
                return;
            }

            foreach (var row in tbEntityGroup.All)
            {
                if (AddEntityGroup(row)) continue;
                FuLogger.LogWarning($"[EntityModule] 添加实体组 '{row.Id}' 失败.");
            }
        }

        /// <summary>
        /// 帧更新。
        /// 1.回收待回收的实体
        /// 2.驱动每个实体组轮询
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="unscaledDeltaTime"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 回收待回收的实体
            while (m_WaitRecycleQueue.Count > 0)
            {
                EntityInfo entityInfo = m_WaitRecycleQueue.Dequeue();

                // ObjectPoolModule.Recycle 在池中找不到目标时会抛异常；若让其逃逸会中断模块帧循环，
                // 且 entityInfo 会因跳过回收而泄漏，故逐项 try/catch/finally 兜底。
                try
                {
                    Entity      entity      = entityInfo.Entity;
                    EntityGroup entityGroup = entity.EntityGroup;

                    if (entityGroup is null) throw new InvalidOperationException($"[EntityModule] 回收实体失败, 实体{entity.EntityAssetName}所属的实体组为空.");

                    entityInfo.Status = EEntityStatus.WillRecycle;
                    entity.OnRecycle();
                    entityInfo.Status = EEntityStatus.Recycled;
                    entityGroup.RecycleEntity(entity);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[EntityModule] 回收实体 '{entityInfo.Entity?.EntityAssetName}' 出现异常: {e.Message}");
                }
                finally
                {
                    ReferencePool.Recycle(entityInfo);
                }
            }

            // 遍历每个实体组，驱动每个实体组轮询
            foreach (var (_, entityGroup) in m_EntityGroupDict)
            {
                entityGroup.Update(deltaTime, unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            Instance = null;

            m_IsShutdown = true;
            m_Scope.Cancel(); // 随模块销毁取消在途实体加载
            HideAllLoadedEntities();

            // 先排空待回收队列，再销毁各实体组对象池：顺序不可颠倒。
            // 排空时 RecycleEntity → 对象池 Recycle 要求目标仍登记在池中；若先销毁池，
            // 这里必定抛“找不到目标对象”并被 catch 降级为告警，回收实际失效（实体未被登记回收）。
            while (m_WaitRecycleQueue.Count > 0)
            {
                var entityInfo = m_WaitRecycleQueue.Dequeue();
                try
                {
                    var entity = entityInfo.Entity;
                    if (entity?.EntityGroup != null)
                    {
                        entity.OnRecycle();
                        entity.EntityGroup.RecycleEntity(entity);
                    }
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[EntityModule] 释放时回收实体 '{entityInfo.Entity?.EntityAssetName}' 出现异常: {e.Message}");
                }
                finally
                {
                    ReferencePool.Recycle(entityInfo);
                }
            }

            // 显式销毁各实体组对象池（含其中所有实体对象持有的句柄），句柄释放收敛到本模块，
            // 不依赖 ObjectPoolModule 逆序销毁的隐式顺序（否则单独 Dispose 或注册顺序变化时句柄永久泄漏）
            // 逐组 try/catch：任一组的 DisposeEntityPool 抛异常不得中断 teardown（否则本组之后的实体组
            // 对象池永久残留、异常还会逃逸到 ModuleManager 的销毁循环影响后续模块）。
            foreach (var (_, entityGroup) in m_EntityGroupDict)
            {
                if (entityGroup == null) continue;

                try
                {
                    entityGroup.DisposeEntityPool(m_ObjectPoolModule);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[EntityModule] 销毁实体组 '{entityGroup.Name}' 的对象池时出现异常: {e.Message}");
                }
            }

            m_EntityGroupDict.Clear();
            m_LoadingEntityDict.Clear();
            m_LoadingToReleaseSet.Clear();

            // 销毁实体根节点与辅助器（OnInit 会重建），避免重启后重复对象泄漏
            if (m_EntityRoot != null)
            {
                UnityEngine.Object.Destroy(m_EntityRoot.gameObject);
                m_EntityRoot = null;
            }

            if (m_EntityHelper != null)
            {
                UnityEngine.Object.Destroy(m_EntityHelper.gameObject);
                m_EntityHelper = null;
            }
        }

        #region 事件处理

        /// <summary>
        /// 加载实体资源成功回调。
        /// </summary>
        /// <param name="tcs">显示实体的Task。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityAssetHandle">实体资源句柄。</param>
        /// <param name="progress">加载进度。</param>
        /// <param name="showEntityInfo">显示时的实体信息。</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void LoadAssetSuccessCallback(UniTaskCompletionSource<Entity> tcs, string entityAssetName, object entityAssetHandle, float progress, ShowEntityInfo showEntityInfo)
        {
            if (showEntityInfo is null)
            {
                // tcs 已 faulted：统一由 await tcs.Task 抛出，不在此 throw（避免从 Completed 同步回调逃逸造成双通道）
                tcs.TrySetException(new InvalidOperationException("[EntityModule]加载实体资源成功, 但是显示时的实体信息为空."));
                return;
            }

            // 如果实体已经在加载中，则释放资源并忽略
            if (m_LoadingToReleaseSet.Contains(showEntityInfo.SerialId))
            {
                m_LoadingToReleaseSet.Remove(showEntityInfo.SerialId);
                ReferencePool.Recycle(showEntityInfo);
                m_EntityHelper.ReleaseEntity(entityAssetHandle, null);
                // 完成 tcs，避免 ShowEntityAsync 的 await 永久挂起
                tcs.TrySetException(new InvalidOperationException($"[EntityModule]实体 '{entityAssetName}' 加载中已被隐藏，取消显示。"));
                return;
            }

            // 从正在加载中的实体字典中移除
            m_LoadingEntityDict.Remove(showEntityInfo.EntityId);

            // 实例化实体
            var entityGo = m_EntityHelper.InstantiateEntity(entityAssetHandle);
            if (entityGo == null)
            {
                // 资源不是 GameObject 或句柄无效：释放句柄、回收信息、完成 tcs，避免句柄/池对象泄漏与 await 挂起
                m_EntityHelper.ReleaseEntity(entityAssetHandle, null);
                ReferencePool.Recycle(showEntityInfo);
                tcs.TrySetException(new InvalidOperationException($"[EntityModule]实体 '{entityAssetName}' 资源不是 GameObject，无法实例化。"));
                return;
            }

            var entityObject = EntityObject.Create(entityAssetName, entityAssetHandle, entityGo, m_EntityHelper);

            // 注册失败时 ObjectPool.Register 会抛异常（目标真实对象已被 Unity 销毁的假 null、目标重复注册等），
            // 而本回调由 YooAsset 的 Completed 同步调用：异常若逃逸会中断回调，tcs 永不完成（await 永久挂起）、
            // showEntityInfo 泄漏，故此处兜底置异常并回收信息（与上方各失败分支风格一致）。
            // 注意：死目标分支在 Register 内部已由 RemoveDeadObject 完成 OnDispose（释放实体句柄）与引用池回收，
            // 此处不可再对 entityObject 调 RecycleEntityObject（该目标已解除登记，重复回收会抛“找不到目标对象”）。
            try
            {
                showEntityInfo.EntityGroup.RegisterEntityObject(entityObject, true);
            }
            catch (Exception e)
            {
                ReferencePool.Recycle(showEntityInfo);
                tcs.TrySetException(e);
                return;
            }

            // 实体资源已经加载完成，开始显示实体
            var showEntityInfoEx = showEntityInfo.UserData as ShowEntityInfoEx;
            try
            {
                InternalShowEntity(tcs, showEntityInfo.EntityId, entityAssetName, showEntityInfo.EntityGroup, entityObject.Target, true, progress, showEntityInfoEx);
            }
            catch (Exception exception)
            {
                // 显示失败：若实体未登记（创建实体失败等），回收已注册的实例对象，避免占用对象池槽位；并确保 tcs 完成、释放 showEntityInfo
                if (!HasEntity(showEntityInfo.EntityId))
                    showEntityInfo.EntityGroup.RecycleEntityObject(entityObject);

                ReferencePool.Recycle(showEntityInfo);
                tcs.TrySetException(exception);
                return;
            }

            ReferencePool.Recycle(showEntityInfo);
        }

        /// <summary>
        /// 加载实体资源失败回调。
        /// </summary>
        /// <param name="tcs">显示实体的Task。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="status">加载资源状态。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        private void LoadAssetFailureCallback(UniTaskCompletionSource<Entity> tcs, string entityAssetName, EOperationStatus status, string errorMessage, object userData)
        {
            var showEntityInfo = (ShowEntityInfo)userData;

            Exception exception;
            if (showEntityInfo is null)
            {
                // tcs 已 faulted：统一由 await tcs.Task 抛出，不在此 throw（避免从 Completed 同步回调逃逸造成双通道）
                tcs.TrySetException(new InvalidOperationException("[EntityModule]加载实体资源失败, 显示时的实体信息为空."));
                return;
            }

            if (m_LoadingToReleaseSet.Contains(showEntityInfo.SerialId))
            {
                m_LoadingToReleaseSet.Remove(showEntityInfo.SerialId);
                // 释放 showEntityInfo（其 Clear 会连带释放 UserData 承载的 ShowEntityInfoEx）
                ReferencePool.Recycle(showEntityInfo);
                // 完成 tcs，避免 ShowEntityAsync 的 await 永久挂起
                tcs.TrySetException(new InvalidOperationException($"[EntityModule]实体 '{entityAssetName}' 加载失败且加载中已被隐藏。"));
                return;
            }

            m_LoadingEntityDict.Remove(showEntityInfo.EntityId);
            exception = new InvalidOperationException($"[EntityModule]加载实体资源失败, 实体资源名称 '{entityAssetName}', 加载状态 '{status}', 错误信息 '{errorMessage}'.");

            // 发送显示实体失败事件（事件参数期望 ShowEntityInfoEx，取 UserData 中的）
            var showEntityInfoEx           = showEntityInfo.UserData as ShowEntityInfoEx;
            var showEntityFailureEventArgs = ShowEntityFailureEventArgs.Create(showEntityInfo.EntityId, entityAssetName, showEntityInfo.EntityGroup.Name, exception.ToString(), showEntityInfoEx);
            m_EventModule.Broadcast(this, showEntityFailureEventArgs);

            // 释放 showEntityInfo（其 Clear 会连带释放 UserData 承载的 ShowEntityInfoEx）
            ReferencePool.Recycle(showEntityInfo);

            tcs.TrySetException(exception); // 统一由 await tcs.Task 抛出，不再 throw（避免从 Completed 同步回调逃逸）
        }

        #endregion

        #region private方法

        /// <summary>
        /// 获取实体信息。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>实体信息。</returns>
        private EntityInfo GetEntityInfo(int entityId) => m_EntityDict.GetValueOrDefault(entityId);

        /// <summary>
        /// 显示实体(内部使用)
        /// </summary>
        /// <param name="tcs">显示实体的Task。</param>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroup">实体组。</param>
        /// <param name="entityGo">实体实例。</param>
        /// <param name="isNewEntity">是否是新实例。</param>
        /// <param name="progress">加载进度。</param>
        /// <param name="showEntityInfoEx">显示的实体额外信息。</param>
        private void InternalShowEntity(UniTaskCompletionSource<Entity> tcs, int entityId, string entityAssetName, EntityGroup entityGroup, object entityGo, bool isNewEntity, float progress,
                                        ShowEntityInfoEx showEntityInfoEx)
        {
            try
            {
                // 创建实体
                var entity = m_EntityHelper.CreateEntity(entityGo, entityGroup);
                if (entity is null)
                {
                    var exception = new InvalidOperationException("[EntityModule] 创建实体失败，实体帮助器返回的实体为空!");
                    tcs.TrySetException(exception);
                    throw exception;
                }

                // 创建实体信息
                var entityInfo = EntityInfo.Create(entity);
                m_EntityDict.Add(entityId, entityInfo);

                // 实体初始化
                entityInfo.Status = EEntityStatus.WillInit;

                // 初始化失败（前置校验失败/逻辑组件创建失败 → Logic 为空）时抛异常走本方法既有的失败分支：
                // 移除实体登记与实体组、回收实体信息、广播失败事件并让 tcs 异常。
                // 不能无视返回值照常继续：那会把无逻辑的实体当作成功上报，随后每帧 Logic.OnUpdate/CachedTransform NRE。
                if (!entity.OnInit(entityId, entityAssetName, entityGroup, isNewEntity, showEntityInfoEx))
                    throw new InvalidOperationException($"[EntityModule]实体 '{entityAssetName}' 初始化失败（实体逻辑为空）。");

                // 实体初始化完成，加入到实体组
                entityInfo.Status = EEntityStatus.Inited;
                entityGroup.AddEntity(entity);

                // 实体显示
                entityInfo.Status = EEntityStatus.WillShow;
                entity.OnShow(showEntityInfoEx);

                // 实体显示完成
                entityInfo.Status = EEntityStatus.Showed;

                // 发送显示实体成功事件
                var showEntitySuccessEventArgs = ShowEntitySuccessEventArgs.Create(entity, progress, showEntityInfoEx);
                m_EventModule.Broadcast(this, showEntitySuccessEventArgs);

                tcs.TrySetResult(entity);
            }
            catch (Exception exception)
            {
                // 注册后初始化/显示失败：清理已登记的实体（移除字典/实体组并回收实体信息），避免僵尸实体占用对象池槽位
                if (m_EntityDict.TryGetValue(entityId, out var registeredEntityInfo))
                {
                    var registeredEntity = registeredEntityInfo.Entity;
                    try
                    {
                        registeredEntity.EntityGroup.RemoveEntity(registeredEntity);
                    }
                    catch
                    {
                        // 实体可能未成功加入实体组，忽略移除异常
                    }

                    m_EntityDict.Remove(entityId);
                    ReferencePool.Recycle(registeredEntityInfo);
                }

                // 发送显示实体失败事件
                var showEntityFailureEventArgs = ShowEntityFailureEventArgs.Create(entityId, entityAssetName, entityGroup.Name, exception.ToString(), showEntityInfoEx);
                m_EventModule.Broadcast(this, showEntityFailureEventArgs);

                tcs.TrySetException(exception);
                throw;
            }
        }

        /// <summary>
        /// 隐藏实体(内部使用)
        /// </summary>
        /// <param name="entityInfo">实体信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        private void InternalHideEntity(EntityInfo entityInfo, object userData)
        {
            while (entityInfo.ChildEntityCount > 0)
            {
                var childEntity = entityInfo.GetChildEntity();
                HideEntity(childEntity.Id, userData);
            }

            if (entityInfo.Status == EEntityStatus.Hidden) return;

            var entity = entityInfo.Entity;
            DetachEntity(entity.Id, userData);
            entityInfo.Status = EEntityStatus.WillHide;

            entity.OnHide(m_IsShutdown, userData);
            entityInfo.Status = EEntityStatus.Hidden;

            entity.EntityGroup.RemoveEntity(entity);
            if (!m_EntityDict.Remove(entity.Id)) throw new InvalidOperationException("[EntityModule] 隐藏实体失败，实体字典中不存在该实体!");

            // 发送隐藏实体成功事件
            var hideEntityCompleteEventArgs = HideEntityCompleteEventArgs.Create(entity.Id, entity.EntityAssetName, entity.EntityGroup, userData);
            m_EventModule.Broadcast(this, hideEntityCompleteEventArgs);

            // 加入待回收队列
            m_WaitRecycleQueue.Enqueue(entityInfo);
        }

        #endregion
    }
}