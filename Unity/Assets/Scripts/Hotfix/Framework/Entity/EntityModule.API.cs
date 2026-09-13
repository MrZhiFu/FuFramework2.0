using System;
using System.Threading;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using EntityGroupCfg = Hotfix.Game.Config.EntityGroup;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 实体管理模块的公共 API。
    /// 功能：
    ///     1. 提供实体组的查询与添加。
    ///     2. 提供实体的查询（按编号/资源名/父子关系）。
    ///     3. 提供实体的异步显示与隐藏。
    ///     4. 提供实体的依附与解除。
    ///     5. 实现 ICancelAsync：Token 观察生命周期取消，CancelAsync 供框架重启排水等待。
    /// 内部实现（字段、生命周期、私有处理）见 EntityModule.cs。
    /// </summary>
    public sealed partial class EntityModule
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static EntityModule Instance { get; private set; }

        /// <summary>
        /// 取消令牌：模块销毁（OnDispose）后触发，在途操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途操作完成清理后才返回。供框架重启取消清理。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();

        /// <summary>
        /// 获取实体数量。
        /// </summary>
        public int EntityCount => m_EntityDict.Count;

        /// <summary>
        /// 获取实体组数量。
        /// </summary>
        public int EntityGroupCount => m_EntityGroupDict.Count;

        #region 实体组相关方法

        /// <summary>
        /// 是否存在实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <returns>是否存在实体组。</returns>
        public bool HasEntityGroup(string entityGroupName)
        {
            if (string.IsNullOrEmpty(entityGroupName)) throw new InvalidOperationException("[EntityModule] 实体组名称不能为空.");
            return m_EntityGroupDict.ContainsKey(entityGroupName);
        }

        /// <summary>
        /// 获取实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <returns>要获取的实体组。</returns>
        public EntityGroup GetEntityGroup(string entityGroupName)
        {
            if (string.IsNullOrEmpty(entityGroupName)) throw new InvalidOperationException("[EntityModule] 实体组名称不能为空.");
            return m_EntityGroupDict.GetValueOrDefault(entityGroupName);
        }

        /// <summary>
        /// 获取所有实体组。
        /// </summary>
        /// <returns>所有实体组。</returns>
        public EntityGroup[] GetAllEntityGroups()
        {
            var index   = 0;
            var results = new EntityGroup[m_EntityGroupDict.Count];
            foreach (var (_, entityGroup) in m_EntityGroupDict)
            {
                results[index++] = entityGroup;
            }

            return results;
        }

        /// <summary>
        /// 获取所有实体组。
        /// </summary>
        /// <param name="results">所有实体组。</param>
        public void GetAllEntityGroups(List<EntityGroup> results)
        {
            if (results is null) throw new InvalidOperationException("[EntityModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, entityGroup) in m_EntityGroupDict)
            {
                results.Add(entityGroup);
            }
        }

        /// <summary>
        /// 添加实体组。
        /// </summary>
        /// <param name="row">实体组信息配置行</param>
        /// <returns>是否增加实体组成功。</returns>
        public bool AddEntityGroup(EntityGroupCfg row)
        {
            if (m_ObjectPoolModule is null) throw new InvalidOperationException("[EntityModule] 增加实体组失败, 请先设置对象池管理模块.");

            var groupName = row.Id.ToString();
            if (HasEntityGroup(groupName))
            {
                FuLogger.LogWarning($"[EntityModule] 添加实体组'{groupName}'失败, 实体组已存在.");
                return false;
            }

            var entityGroupGo = new GameObject($"Entity Group - {groupName}");
            entityGroupGo.transform.SetParent(m_EntityRoot);
            entityGroupGo.transform.localScale = Vector3.one;
            var entityGroup = new EntityGroup(row, entityGroupGo, m_ObjectPoolModule);
            m_EntityGroupDict.Add(groupName, entityGroup);

            return true;
        }

        #endregion

        #region 实体Get

        /// <summary>
        /// 是否存在实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>是否存在实体。</returns>
        public bool HasEntity(int entityId)
        {
            return m_EntityDict.ContainsKey(entityId);
        }

        /// <summary>
        /// 是否存在实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>是否存在实体。</returns>
        public bool HasEntity(string entityAssetName)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityModule] 实体资源名称不能为空.");
            foreach (var (_, entityInfo) in m_EntityDict)
            {
                if (entityInfo.Entity.EntityAssetName == entityAssetName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>要获取的实体。</returns>
        public Entity GetEntity(int entityId) => GetEntityInfo(entityId)?.Entity;

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>要获取的实体。</returns>
        public Entity GetEntity(string entityAssetName)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityModule] 实体资源名称不能为空.");

            foreach (var (_, entityInfo) in m_EntityDict)
            {
                if (entityInfo.Entity.EntityAssetName != entityAssetName) continue;
                return entityInfo.Entity;
            }

            return null;
        }

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>要获取的实体。</returns>
        public Entity[] GetEntities(string entityAssetName)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityModule] 实体资源名称不能为空.");

            var results = new List<Entity>();
            foreach (var entityInfo in m_EntityDict)
            {
                if (entityInfo.Value.Entity.EntityAssetName != entityAssetName) continue;
                results.Add(entityInfo.Value.Entity);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="results">要获取的实体。</param>
        public void GetEntities(string entityAssetName, List<Entity> results)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityModule] 实体资源名称不能为空.");
            if (results is null) throw new InvalidOperationException("[EntityModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, entityInfo) in m_EntityDict)
            {
                if (entityInfo.Entity.EntityAssetName != entityAssetName) continue;
                results.Add(entityInfo.Entity);
            }
        }

        /// <summary>
        /// 获取所有已加载的实体。
        /// </summary>
        /// <returns>所有已加载的实体。</returns>
        public Entity[] GetAllLoadedEntities()
        {
            var index   = 0;
            var results = new Entity[m_EntityDict.Count];
            foreach (var (_, entityInfo) in m_EntityDict)
            {
                results[index++] = entityInfo.Entity;
            }

            return results;
        }

        /// <summary>
        /// 获取所有已加载的实体。
        /// </summary>
        /// <param name="results">所有已加载的实体。</param>
        public void GetAllLoadedEntities(List<Entity> results)
        {
            if (results is null) throw new InvalidOperationException("[EntityModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, entityInfo) in m_EntityDict)
            {
                results.Add(entityInfo.Entity);
            }
        }

        /// <summary>
        /// 获取所有正在加载实体的编号。
        /// </summary>
        /// <returns>所有正在加载实体的编号。</returns>
        public int[] GetAllLoadingEntityIds()
        {
            var index   = 0;
            var results = new int[m_LoadingEntityDict.Count];
            foreach (var (entityId, _) in m_LoadingEntityDict)
            {
                results[index++] = entityId;
            }

            return results;
        }

        /// <summary>
        /// 获取所有正在加载实体的编号。
        /// </summary>
        /// <param name="results">所有正在加载实体的编号。</param>
        public void GetAllLoadingEntityIds(List<int> results)
        {
            if (results is null) throw new InvalidOperationException("[EntityModule] 结果列表不能为空.");
            results.Clear();
            foreach (var (entityId, _) in m_LoadingEntityDict)
            {
                results.Add(entityId);
            }
        }

        /// <summary>
        /// 是否正在加载实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>是否正在加载实体。</returns>
        public bool IsLoadingEntity(int entityId) => m_LoadingEntityDict.ContainsKey(entityId);

        /// <summary>
        /// 是否是合法的实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <returns>实体是否合法。</returns>
        public bool IsValidEntity(Entity entity) => entity != null && HasEntity(entity.Id);

        #endregion

        #region 显示实体

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="token">取消令牌。</param>
        /// <typeparam name="T">实体逻辑类型。</typeparam>
        public UniTask<Entity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName, CancellationToken token) where T : EntityLogic
        {
            return ShowEntityAsync(entityId, typeof(T), entityAssetName, entityGroupName, token);
        }

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityLogicType">实体逻辑类型。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="token">取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public async UniTask<Entity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, CancellationToken token, object userData = null)
        {
            if (m_EntityHelper is null) throw new InvalidOperationException("[EntityModule] 显示实体失败, 请先设置实体辅助器.");
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityModule] 显示实体失败, 实体资源名称不能为空.");
            if (string.IsNullOrEmpty(entityGroupName)) throw new InvalidOperationException($"[EntityModule] 显示实体{entityAssetName}失败, 实体组名称不能为空.");
            if (HasEntity(entityId)) throw new InvalidOperationException($"[EntityModule] 显示实体{entityAssetName}失败, 实体已存在.");
            if (IsLoadingEntity(entityId)) throw new InvalidOperationException($"[EntityModule] 显示实体{entityAssetName}失败, 实体已在加载中.");

            var entityGroup = GetEntityGroup(entityGroupName);
            if (entityGroup is null) throw new InvalidOperationException($"[EntityModule] 显示实体{entityAssetName}失败, 实体组 '{entityGroupName}' 不存在.");

            // 创建一个加载实体资源的任务，先从对象池获取实体，没有才从资源加载
            var tcs       = new UniTaskCompletionSource<Entity>();
            var entityObj = entityGroup.SpawnEntityObject(entityAssetName);

            // 实体额外信息
            var showEntityInfoEx = ShowEntityInfoEx.Create(entityLogicType, userData);

            if (entityObj is null)
            {
                var serialId = ++m_Serial;
                m_LoadingEntityDict.Add(entityId, serialId);

                var capturedToken = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧任务据此识别并拒绝写回新生命周期
                // 仅包裹 LoadAssetAsync 的同步抛异常（包未就绪等）：此时 showEntityInfoEx 尚未交给回调，需回收并清理 loading 状态（否则 IsLoadingEntity 恒 true）
                AssetHandle assetOperationHandle;
                try
                {
                    assetOperationHandle = await m_AssetModule.LoadAssetAsync<Object>(entityAssetName, token);
                }
                catch
                {
                    m_LoadingEntityDict.Remove(entityId);
                    m_LoadingToReleaseSet.Remove(serialId);
                    ReferencePool.Recycle(showEntityInfoEx);
                    throw;
                }

                // 订阅完成回调：回调内部已接管并连带回收 showEntityInfoEx（ShowEntityInfo.Clear → UserData）；
                // 若回调抛异常或 tcs 完成异常，直接传播，此处不再二次回收（否则引用池抛"该对象已经被释放"掩盖真实异常）
                assetOperationHandle.Completed += handle =>
                {
                    // 生命周期变更（重启）：旧生命周期在途加载的句柄不得写回新生命周期，释放并拒绝
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token)
                    {
                        handle.Release();
                        // 加载成功即已占用 bundle：跨生命周期中止仅 Release 在 AutoUnloadBundleWhenUnused=false 下不卸载，
                        // 配对显式卸载防旧生命周期实体 prefab 的 bundle 常驻（失败句柄未获取 bundle 无需卸载）
                        if (handle.Status == EOperationStatus.Succeeded)
                            m_AssetModule.UnloadAsset(entityAssetName);
                        ReferencePool.Recycle(showEntityInfoEx);
                        tcs.TrySetException(new OperationCanceledException(capturedToken));
                        return;
                    }

                    // 实体信息
                    var showEntityInfo = ShowEntityInfo.Create(serialId, entityId, entityGroup, showEntityInfoEx);

                    // 用 Status 而非 IsDone 判断成功（失败句柄 IsDone 同样为 true，会误走成功回调）
                    if (handle.Status == EOperationStatus.Succeeded)
                        LoadAssetSuccessCallback(tcs, entityAssetName, handle, handle.Progress, showEntityInfo);
                    else
                    {
                        var status       = handle.Status;
                        var errorMessage = handle.Error;
                        handle.Release(); // 失败句柄未被实体系统接管，释放避免残留
                        LoadAssetFailureCallback(tcs, entityAssetName, status, errorMessage, showEntityInfo);
                    }
                };

                return await tcs.Task;
            }

            // 实体资源已经加载完成，开始显示实体
            try
            {
                InternalShowEntity(tcs, entityId, entityAssetName, entityGroup, entityObj.Target, false, 1f, showEntityInfoEx);
            }
            catch
            {
                // 显示失败：若实体未登记（创建实体失败等），回收已获取的实例对象，避免占用对象池槽位
                if (!HasEntity(entityId))
                    entityGroup.RecycleEntityObject(entityObj);
                ReferencePool.Recycle(showEntityInfoEx);
                throw;
            }

            // 显示完成，释放临时传递数据的引用池对象
            ReferencePool.Recycle(showEntityInfoEx);
            return await tcs.Task;
        }

        #endregion

        #region 隐藏实体

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        public void HideEntity(int entityId) => HideEntity(entityId, null);

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void HideEntity(int entityId, object userData)
        {
            if (IsLoadingEntity(entityId))
            {
                m_LoadingToReleaseSet.Add(m_LoadingEntityDict[entityId]);
                m_LoadingEntityDict.Remove(entityId);
                return;
            }

            var entityInfo = GetEntityInfo(entityId);
            if (entityInfo is null) throw new InvalidOperationException($"[EntityModule] 隐藏实体失败, 实体{entityId}不存在.");

            InternalHideEntity(entityInfo, userData);
        }

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        public void HideEntity(Entity entity) => HideEntity(entity, null);

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void HideEntity(Entity entity, object userData)
        {
            if (entity is null) throw new InvalidOperationException($"[EntityModule] 隐藏实体失败, 实体不存在.");
            HideEntity(entity.Id, userData);
        }

        /// <summary>
        /// 隐藏所有已加载的实体。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void HideAllLoadedEntities(object userData = null)
        {
            while (m_EntityDict.Count > 0)
            {
                foreach (var (_, entityInfo) in m_EntityDict)
                {
                    InternalHideEntity(entityInfo, userData);
                    break;
                }
            }
        }

        /// <summary>
        /// 隐藏所有正在加载的实体。
        /// </summary>
        public void HideAllLoadingEntities()
        {
            foreach (var (_, entityId) in m_LoadingEntityDict)
            {
                m_LoadingToReleaseSet.Add(entityId);
            }

            m_LoadingEntityDict.Clear();
        }

        #endregion

        #region 父实体/子实体Get

        /// <summary>
        /// 获取父实体。
        /// </summary>
        /// <param name="childEntityId">要获取父实体的子实体的实体编号。</param>
        /// <returns>子实体的父实体。</returns>
        public Entity GetParentEntity(int childEntityId)
        {
            var childEntityInfo = GetEntityInfo(childEntityId);
            if (childEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 获取父实体失败, 实体{childEntityId}信息不存在.");
            return childEntityInfo.ParentEntity;
        }

        /// <summary>
        /// 获取父实体。
        /// </summary>
        /// <param name="childEntity">要获取父实体的子实体。</param>
        /// <returns>子实体的父实体。</returns>
        public Entity GetParentEntity(Entity childEntity)
        {
            if (childEntity is null) throw new InvalidOperationException("[EntityModule] 获取父实体失败, 实体不存在.");
            return GetParentEntity(childEntity.Id);
        }

        /// <summary>
        /// 获取其下的子实体数量。
        /// </summary>
        /// <param name="parentEntityId">要获取子实体数量的父实体的实体编号。</param>
        /// <returns>子实体数量。</returns>
        public int GetChildEntityCount(int parentEntityId)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 获取子实体数量失败, 父实体{parentEntityId}信息不存在.");
            return parentEntityInfo.ChildEntityCount;
        }

        /// <summary>
        /// 获取其下的子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取子实体的父实体的实体编号。</param>
        /// <returns>子实体。</returns>
        public Entity GetChildEntity(int parentEntityId)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 获取子实体失败, 父实体{parentEntityId}信息不存在.");
            return parentEntityInfo.GetChildEntity();
        }

        /// <summary>
        /// 获取其下的子实体。
        /// </summary>
        /// <param name="parentEntity">要获取子实体的父实体。</param>
        /// <returns>子实体。</returns>
        public Entity GetChildEntity(Entity parentEntity)
        {
            if (parentEntity is null) throw new InvalidOperationException("[EntityModule] 获取子实体数量失败, 父实体不存在.");
            return GetChildEntity(parentEntity.Id);
        }

        /// <summary>
        /// 获取其下的所有子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取所有子实体的父实体的实体编号。</param>
        /// <returns>所有子实体。</returns>
        public Entity[] GetChildEntities(int parentEntityId)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 获取所有子实体失败, 父实体{parentEntityId}信息不存在.");
            return parentEntityInfo.GetChildEntities();
        }

        /// <summary>
        /// 获取其下的所有子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取所有子实体的父实体的实体编号。</param>
        /// <param name="results">所有子实体。</param>
        public void GetChildEntities(int parentEntityId, List<Entity> results)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 获取所有子实体失败, 父实体{parentEntityId}信息不存在.");
            parentEntityInfo.GetChildEntities(results);
        }

        /// <summary>
        /// 获取其下的所有子实体。
        /// </summary>
        /// <param name="parentEntity">要获取所有子实体的父实体。</param>
        /// <returns>所有子实体。</returns>
        public Entity[] GetChildEntities(Entity parentEntity)
        {
            if (parentEntity is null) throw new InvalidOperationException("[EntityModule] 获取所有子实体失败, 父实体不存在.");
            return GetChildEntities(parentEntity.Id);
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntity">要获取所有子实体的父实体。</param>
        /// <param name="results">所有子实体。</param>
        public void GetChildEntities(Entity parentEntity, List<Entity> results)
        {
            if (parentEntity is null) throw new InvalidOperationException("[EntityModule] 获取所有子实体失败, 父实体不存在.");
            GetChildEntities(parentEntity.Id, results);
        }

        #endregion

        #region 附加子实体

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="parentTransform">被附加的父实体的Transform</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, object userData, Transform parentTransform = null)
        {
            if (childEntity is null) throw new InvalidOperationException("[EntityModule] 附加子实体失败, 子实体不存在.");
            if (parentEntity is null) throw new InvalidOperationException("[EntityModule] 附加子实体失败, 父实体不存在.");
            AttachEntity(childEntity.Id, parentEntity.Id, userData, parentTransform);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="parentTransformPath">被附加的父实体的Transform路径</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, object userData, string parentTransformPath = "")
        {
            if (childEntity is null) throw new InvalidOperationException("[EntityModule] 附加子实体失败, 子实体不存在.");
            if (parentEntity is null) throw new InvalidOperationException("[EntityModule] 附加子实体失败, 父实体不存在.");
            AttachEntity(childEntity.Id, parentEntity.Id, userData, parentTransformPath);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransformPath">被附加的父实体的Transform路径</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, object userData, string parentTransformPath = "")
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 父实体{parentEntityId}不存在.");

            if (parentEntityInfo.Status >= EEntityStatus.WillHide)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 父实体{parentEntityId}处于将要隐藏状态.");

            var parentEntity = parentEntityInfo.Entity;

            // 如果相对于父实体的Transform路径为空，则默认直接附加到父实体的Transform上
            Transform parentTransform;
            if (string.IsNullOrEmpty(parentTransformPath))
            {
                parentTransform = parentEntity.Logic.CachedTransform;
            }
            else
            {
                parentTransform = parentEntity.Logic.CachedTransform.Find(parentTransformPath);
                if (parentTransform is null)
                {
                    FuLogger.LogWarning($"[EntityModule] 找不到父实体 '{parentEntity.Logic.Name}' 下的Transform路径 '{parentTransformPath}', 将直接附加到父实体的Transform上.");
                    parentTransform = parentEntity.Logic.CachedTransform;
                }
            }

            AttachEntity(childEntityId, parentEntityId, userData, parentTransform);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransform">相对于被附加的父实体的Transform。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, object userData, Transform parentTransform = null)
        {
            if (childEntityId == parentEntityId)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 子实体{childEntityId}和父实体{parentEntityId}不能相同.");

            var childEntityInfo = GetEntityInfo(childEntityId);
            if (childEntityInfo is null)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 子实体{childEntityId}不存在.");

            if (childEntityInfo.Status >= EEntityStatus.WillHide)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 子实体{childEntityId}处于将要隐藏状态.");

            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 父实体{parentEntityId}不存在.");

            if (parentEntityInfo.Status >= EEntityStatus.WillHide)
                throw new InvalidOperationException($"[EntityModule] 附加子实体失败, 父实体{parentEntityId}处于将要隐藏状态.");

            var childEntity  = childEntityInfo.Entity;
            var parentEntity = parentEntityInfo.Entity;

            // 如果指定的相对于于父实体的Transform路径为空，则默认直接附加到父实体的Transform上
            parentTransform ??= parentEntity.Logic.CachedTransform;

            // 创建附加实体信息
            var attachEntityInfo = AttachEntityInfo.Create(parentTransform, userData);

            // attachEntityInfo 的所有权：中间任一步骤抛异常（DetachEntity 失败、AddChildEntity 子实体重复等）都会
            // 跳过 OnAttachTo，导致该池对象永不归还；用 handedOver 标志区分"是否已交接"，
            // 未交接则在 finally 中兜底回收，已交接则由 childEntity.OnAttachTo 内部唯一负责回收（其成功/异常路径均回收）。
            var handedOver = false;
            try
            {
                // 解除之前的附加关系
                DetachEntity(childEntity.Id, attachEntityInfo);

                // 附加到新的父实体
                childEntityInfo.ParentEntity = parentEntity;
                parentEntityInfo.AddChildEntity(childEntity);

                // 通知父实体有新子实体附加进来
                parentEntity.OnAttached(childEntity, attachEntityInfo);

                // 通知子实体被附加到新的父实体上：OnAttachTo 内部负责回收 attachEntityInfo
                handedOver = true;
                childEntity.OnAttachTo(parentEntity, attachEntityInfo);
            }
            finally
            {
                if (!handedOver)
                {
                    ReferencePool.Recycle(attachEntityInfo);
                }
            }
        }

        #endregion

        #region 解除子实体

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntityId">要解除的子实体的实体编号。</param>
        public void DetachEntity(int childEntityId) => DetachEntity(childEntityId, null);

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntityId">要解除的子实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachEntity(int childEntityId, object userData)
        {
            var childEntityInfo = GetEntityInfo(childEntityId);
            if (childEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 解除子实体{childEntityId}失败, 子实体信息不存在.");

            var parentEntity = childEntityInfo.ParentEntity;
            if (parentEntity is null) return;

            var parentEntityInfo = GetEntityInfo(parentEntity.Id);
            if (parentEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 解除子实体{childEntityId}失败, 父实体{parentEntity.Id}信息不存在.");

            var childEntity = childEntityInfo.Entity;
            childEntityInfo.ParentEntity = null;
            parentEntityInfo.RemoveChildEntity(childEntity);
            parentEntity.OnDetached(childEntity, userData);
            childEntity.OnDetachFrom(parentEntity, userData);
        }

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntity">要解除的子实体。</param>
        public void DetachEntity(Entity childEntity) => DetachEntity(childEntity, null);

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntity">要解除的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachEntity(Entity childEntity, object userData)
        {
            if (childEntity is null) throw new InvalidOperationException("[EntityModule] 解除子实体失败, 子实体不存在.");
            DetachEntity(childEntity.Id, userData);
        }

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntityId">被解除的父实体的实体编号。</param>
        public void DetachChildEntities(int parentEntityId) => DetachChildEntities(parentEntityId, null);

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntityId">被解除的父实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachChildEntities(int parentEntityId, object userData)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo is null) throw new InvalidOperationException($"[EntityModule] 解除所有子实体失败, 父实体{parentEntityId}信息不存在.");

            while (parentEntityInfo.ChildEntityCount > 0)
            {
                var childEntity = parentEntityInfo.GetChildEntity();
                DetachEntity(childEntity.Id, userData);
            }
        }

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        public void DetachChildEntities(Entity parentEntity) => DetachChildEntities(parentEntity, null);

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachChildEntities(Entity parentEntity, object userData)
        {
            if (parentEntity is null) throw new InvalidOperationException("[EntityModule] 解除所有子实体失败, 父实体不存在.");
            DetachChildEntities(parentEntity.Id, userData);
        }

        #endregion
    }
}