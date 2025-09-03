using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.ObjectPool.Runtime;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 实体管理器。
    /// 功能：
    /// 1. 管理实体组的创建、销毁等流程。
    /// 2. 管理实体的创建、销毁、显示、隐藏等流程。
    /// 3. 管理实体的生命周期。
    /// 4. 管理实体的资源加载。
    /// 5. 管理实体的对象池。
    /// 6. 管理实体的依赖资源加载。
    /// </summary>
    public sealed partial class EntityManager : FuComponent
    {
        protected override int Priority => 1;
        
        private readonly Dictionary<int, EntityInfo>     m_EntityDict = new();                 // 记录所有实体的字典，Key为实体编号，Value为实体信息
        private readonly Dictionary<string, EntityGroup> m_EntityGroupDict = new();            // 记录所有实体组的字典，Key为实体组名称，Value为实体组
        private readonly Dictionary<int, int>            m_LoadingEntityDict = new();          // 正在加载的实体编号字典，Key为实体编号，Value为实体自增编号
        private readonly HashSet<int>                    m_WaitReleaseOnLoadEntitySet = new(); // 在加载中但待释放的所有实体编号集合
        private readonly Queue<EntityInfo>               m_RecycleQueue = new();               // 待回收的实体信息队列

        private DefaultEntityHelper m_EntityHelper;      // 实体辅助器

        private int  m_Serial;     // 实体自增编号
        private bool m_IsShutdown; // 是否关闭

        private IObjectPoolManager m_ObjectPoolManager; // 对象池管理器
        private EventManager m_EventManager; // 实体管理器
        private AssetManager m_AssetManager; // 资源管理器
        
        private Transform m_InstanceRoot; // 实体实例对象池根节点
        private EntityGroup[] m_EntityGroups; // 实体组数组
        
        private EventHandler<ShowEntitySuccessEventArgs>  m_ShowEntitySuccessEventHandler;  // 显示实体成功事件
        private EventHandler<ShowEntityFailureEventArgs>  m_ShowEntityFailureEventHandler;  // 显示实体失败事件
        private EventHandler<HideEntityCompleteEventArgs> m_HideEntityCompleteEventHandler; // 隐藏实体完成事件

        protected override void OnInit()
        {
            m_AssetManager = ModuleManager.GetModule<AssetManager>();
            m_EventManager = ModuleManager.GetModule<EventManager>();
            m_ObjectPoolManager = FuEntry.GetModule<IObjectPoolManager>();

            // 创建实体实例对象池根节点
            m_InstanceRoot = new GameObject("Entity Instances").transform;
            m_InstanceRoot.SetParent(gameObject.transform);
            m_InstanceRoot.localScale = Vector3.one;
            
            // 创建实体辅助器
            var entityHelperGo = new GameObject("Entity Helper");
            entityHelperGo.transform.SetParent(transform);
            entityHelperGo.transform.localScale = Vector3.one;
            var entityHelper = entityHelperGo.AddComponent<DefaultEntityHelper>();
            m_EntityHelper = entityHelper;
            
            // 添加实体组
            foreach (var entityGroup in m_EntityGroups)
            {
                if (AddEntityGroup(entityGroup.Name, entityGroup.InstanceAutoReleaseInterval, entityGroup.InstanceCapacity, entityGroup.InstanceExpireTime, entityGroup.InstancePriority)) continue;
                Log.Warning("Add entity group '{0}' failure.", entityGroup.Name);
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            while (m_RecycleQueue.Count > 0)
            {
                EntityInfo  entityInfo  = m_RecycleQueue.Dequeue();
                Entity     entity      = entityInfo.Entity;
                EntityGroup entityGroup = entity.EntityGroup;

                if (entityGroup == null)
                    throw new FuException("Entity group is invalid.");

                entityInfo.Status = EntityStatus.WillRecycle;
                entity.OnRecycle();
                entityInfo.Status = EntityStatus.Recycled;
                entityGroup.RecycleEntity(entity);
                ReferencePool.Runtime.ReferencePool.Release(entityInfo);
            }

            // 遍历每个实体组，驱动每个实体组轮询
            foreach (var (_, entityGroup) in m_EntityGroupDict)
            {
                entityGroup.Update(elapseSeconds, realElapseSeconds);
            }
        }

        protected override void OnShutdown(ShutdownType shutdownType)
        {
            m_IsShutdown = true;
            HideAllLoadedEntities();
            m_EntityGroupDict.Clear();
            m_LoadingEntityDict.Clear();
            m_WaitReleaseOnLoadEntitySet.Clear();
            m_RecycleQueue.Clear();
        }

        /// <summary>
        /// 获取实体数量。
        /// </summary>
        public int EntityCount => m_EntityDict.Count;

        /// <summary>
        /// 获取实体组数量。
        /// </summary>
        public int EntityGroupCount => m_EntityGroupDict.Count;

        /// <summary>
        /// 显示实体成功事件。
        /// </summary>
        public event EventHandler<ShowEntitySuccessEventArgs> ShowEntitySuccess
        {
            add => m_ShowEntitySuccessEventHandler += value;
            remove => m_ShowEntitySuccessEventHandler -= value;
        }

        /// <summary>
        /// 显示实体失败事件。
        /// </summary>
        public event EventHandler<ShowEntityFailureEventArgs> ShowEntityFailure
        {
            add => m_ShowEntityFailureEventHandler += value;
            remove => m_ShowEntityFailureEventHandler -= value;
        }

        /// <summary>
        /// 隐藏实体完成事件。
        /// </summary>
        public event EventHandler<HideEntityCompleteEventArgs> HideEntityComplete
        {
            add => m_HideEntityCompleteEventHandler += value;
            remove => m_HideEntityCompleteEventHandler -= value;
        }
        
        #region 实体组相关方法

        /// <summary>
        /// 是否存在实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <returns>是否存在实体组。</returns>
        public bool HasEntityGroup(string entityGroupName)
        {
            if (string.IsNullOrEmpty(entityGroupName)) throw new FuException("Entity group name is invalid.");
            return m_EntityGroupDict.ContainsKey(entityGroupName);
        }

        /// <summary>
        /// 获取实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <returns>要获取的实体组。</returns>
        public EntityGroup GetEntityGroup(string entityGroupName)
        {
            if (string.IsNullOrEmpty(entityGroupName)) throw new FuException("Entity group name is invalid.");
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
            if (results == null) throw new FuException("Results is invalid.");

            results.Clear();
            foreach (var (_, entityGroup) in m_EntityGroupDict)
            {
                results.Add(entityGroup);
            }
        }

        /// <summary>
        /// 增加实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="instanceAutoReleaseInterval">实体实例对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="instanceCapacity">实体实例对象池容量。</param>
        /// <param name="instanceExpireTime">实体实例对象池对象过期秒数。</param>
        /// <param name="instancePriority">实体实例对象池的优先级。</param>
        /// <returns>是否增加实体组成功。</returns>
        public bool AddEntityGroup(string entityGroupName, float instanceAutoReleaseInterval, int instanceCapacity, float instanceExpireTime, int instancePriority)
        {
            if (string.IsNullOrEmpty(entityGroupName)) throw new FuException("Entity group name is invalid.");
            if (m_ObjectPoolManager == null) throw new FuException("You must set object pool manager first.");

            if (HasEntityGroup(entityGroupName)) return false;
            
            var entityGroupHelperGo = new GameObject($"Entity Group - {entityGroupName}");
            entityGroupHelperGo.transform.SetParent(m_InstanceRoot);
            entityGroupHelperGo.transform.localScale = Vector3.one;
            var entityGroupHelper = entityGroupHelperGo.AddComponent<DefaultEntityGroupHelper>();
            var entityGroup = new EntityGroup(entityGroupName, instanceAutoReleaseInterval, instanceCapacity, instanceExpireTime, instancePriority, entityGroupHelper, m_ObjectPoolManager);
            m_EntityGroupDict.Add(entityGroupName, entityGroup);

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
            if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
            return m_EntityDict.Any(entityInfo => entityInfo.Value.Entity.EntityAssetName == entityAssetName);
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
            if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");

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
            if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");

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
            if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
            if (results == null) throw new FuException("Results is invalid.");

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
            if (results == null) throw new FuException("Results is invalid.");

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
            if (results == null) throw new FuException("Results is invalid.");
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

        #region 实体Show

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        public UniTask<Entity> ShowEntityAsync(int entityId, string entityAssetName, string entityGroupName)
            => ShowEntityAsync(entityId, entityAssetName, entityGroupName, 0, null);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="priority">加载实体资源的优先级。</param>
        public UniTask<Entity> ShowEntityAsync(int entityId, string entityAssetName, string entityGroupName, int priority)
            => ShowEntityAsync(entityId, entityAssetName, entityGroupName, priority, null);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public UniTask<Entity> ShowEntityAsync(int entityId, string entityAssetName, string entityGroupName, object userData)
            => ShowEntityAsync(entityId, entityAssetName, entityGroupName, 0, userData);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="priority">加载实体资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public async UniTask<Entity> ShowEntityAsync(int entityId, string entityAssetName, string entityGroupName, int priority, object userData)
        {
            if (m_EntityHelper == null) throw new FuException("You must set entity helper first.");
            if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
            if (string.IsNullOrEmpty(entityGroupName)) throw new FuException("Entity group name is invalid.");
            if (HasEntity(entityId)) throw new FuException(Utility.Text.Format("Entity id '{0}' is already exist.",           entityId));
            if (IsLoadingEntity(entityId)) throw new FuException(Utility.Text.Format("Entity '{0}' is already being loaded.", entityId));

            var entityGroup = GetEntityGroup(entityGroupName);
            if (entityGroup is null) throw new FuException(Utility.Text.Format("Entity group '{0}' is not exist.", entityGroupName));

            var tcs = new UniTaskCompletionSource<Entity>();

            var entityInstanceObject = entityGroup.SpawnEntityInstanceObject(entityAssetName);
            if (entityInstanceObject == null)
            {
                var serialId = ++m_Serial;
                m_LoadingEntityDict.Add(entityId, serialId);

                var assetOperationHandle = await m_AssetManager.LoadAssetAsync<Object>(entityAssetName);
                assetOperationHandle.Completed += handle =>
                {
                    var newUserData = ShowEntityInfo.Create(serialId, entityId, entityGroup, userData);
                    if (handle.IsDone)
                        LoadAssetSuccessCallback(tcs, entityAssetName, handle, handle.Progress, newUserData);
                    else
                        LoadAssetFailureCallback(tcs, entityAssetName, handle.Status, handle.LastError, newUserData);
                };

                return await tcs.Task;
            }

            InternalShowEntity(tcs, entityId, entityAssetName, entityGroup, entityInstanceObject.Target, false, 0f, userData);
            return await tcs.Task;
        }

        #endregion

        #region 实体Hide

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
                m_WaitReleaseOnLoadEntitySet.Add(m_LoadingEntityDict[entityId]);
                m_LoadingEntityDict.Remove(entityId);
                return;
            }

            var entityInfo = GetEntityInfo(entityId);
            if (entityInfo == null) throw new FuException(Utility.Text.Format("Can not find entity '{0}'.", entityId));

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
            if (entity == null) throw new FuException("Entity is invalid.");
            HideEntity(entity.Id, userData);
        }

        /// <summary>
        /// 隐藏所有已加载的实体。
        /// </summary>
        public void HideAllLoadedEntities()
        {
            HideAllLoadedEntities(null);
        }

        /// <summary>
        /// 隐藏所有已加载的实体。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void HideAllLoadedEntities(object userData)
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
                m_WaitReleaseOnLoadEntitySet.Add(entityId);
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
            if (childEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find child entity '{0}'.", childEntityId));
            return childEntityInfo.ParentEntity;
        }

        /// <summary>
        /// 获取父实体。
        /// </summary>
        /// <param name="childEntity">要获取父实体的子实体。</param>
        /// <returns>子实体的父实体。</returns>
        public Entity GetParentEntity(Entity childEntity)
        {
            if (childEntity == null) throw new FuException("Child entity is invalid.");
            return GetParentEntity(childEntity.Id);
        }

        /// <summary>
        /// 获取子实体数量。
        /// </summary>
        /// <param name="parentEntityId">要获取子实体数量的父实体的实体编号。</param>
        /// <returns>子实体数量。</returns>
        public int GetChildEntityCount(int parentEntityId)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntityId));
            return parentEntityInfo.ChildEntityCount;
        }

        /// <summary>
        /// 获取子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取子实体的父实体的实体编号。</param>
        /// <returns>子实体。</returns>
        public Entity GetChildEntity(int parentEntityId)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntityId));
            return parentEntityInfo.GetChildEntity();
        }

        /// <summary>
        /// 获取子实体。
        /// </summary>
        /// <param name="parentEntity">要获取子实体的父实体。</param>
        /// <returns>子实体。</returns>
        public Entity GetChildEntity(Entity parentEntity)
        {
            if (parentEntity == null) throw new FuException("Parent entity is invalid.");
            return GetChildEntity(parentEntity.Id);
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取所有子实体的父实体的实体编号。</param>
        /// <returns>所有子实体。</returns>
        public Entity[] GetChildEntities(int parentEntityId)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntityId));
            return parentEntityInfo.GetChildEntities();
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取所有子实体的父实体的实体编号。</param>
        /// <param name="results">所有子实体。</param>
        public void GetChildEntities(int parentEntityId, List<Entity> results)
        {
            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntityId));
            parentEntityInfo.GetChildEntities(results);
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntity">要获取所有子实体的父实体。</param>
        /// <returns>所有子实体。</returns>
        public Entity[] GetChildEntities(Entity parentEntity)
        {
            if (parentEntity == null) throw new FuException("Parent entity is invalid.");
            return GetChildEntities(parentEntity.Id);
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntity">要获取所有子实体的父实体。</param>
        /// <param name="results">所有子实体。</param>
        public void GetChildEntities(Entity parentEntity, List<Entity> results)
        {
            if (parentEntity == null) throw new FuException("Parent entity is invalid.");
            GetChildEntities(parentEntity.Id, results);
        }

        #endregion

        #region 附加子实体

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        public void AttachEntity(int childEntityId, int parentEntityId) => AttachEntity(childEntityId, parentEntityId, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity) => AttachEntity(childEntityId, parentEntity, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity, object userData)
        {
            if (parentEntity == null) throw new FuException("Parent entity is invalid.");
            AttachEntity(childEntityId, parentEntity.Id, userData);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId) => AttachEntity(childEntity, parentEntityId, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId, object userData)
        {
            if (childEntity == null) throw new FuException("Child entity is invalid.");
            AttachEntity(childEntity.Id, parentEntityId, userData);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity) => AttachEntity(childEntity, parentEntity, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, object userData)
        {
            if (childEntity  == null) throw new FuException("Child entity is invalid.");
            if (parentEntity == null) throw new FuException("Parent entity is invalid.");
            AttachEntity(childEntity.Id, parentEntity.Id, userData);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, object userData)
        {
            if (childEntityId == parentEntityId)
                throw new FuException(Utility.Text.Format("Can not attach entity when child entity id equals to parent entity id '{0}'.", parentEntityId));

            var childEntityInfo = GetEntityInfo(childEntityId);
            if (childEntityInfo == null)
                throw new FuException(Utility.Text.Format("Can not find child entity '{0}'.", childEntityId));

            if (childEntityInfo.Status >= EntityStatus.WillHide)
                throw new FuException(Utility.Text.Format("Can not attach entity when child entity status is '{0}'.", childEntityInfo.Status));

            var parentEntityInfo = GetEntityInfo(parentEntityId);
            if (parentEntityInfo == null)
                throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntityId));

            if (parentEntityInfo.Status >= EntityStatus.WillHide)
                throw new FuException(Utility.Text.Format("Can not attach entity when parent entity status is '{0}'.", parentEntityInfo.Status));

            var childEntity  = childEntityInfo.Entity;
            var parentEntity = parentEntityInfo.Entity;

            DetachEntity(childEntity.Id, userData);

            childEntityInfo.ParentEntity = parentEntity;
            parentEntityInfo.AddChildEntity(childEntity);
            parentEntity.OnAttached(childEntity, userData);
            childEntity.OnAttachTo(parentEntity, userData);
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
            if (childEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find child entity '{0}'.", childEntityId));

            var parentEntity = childEntityInfo.ParentEntity;
            if (parentEntity == null) return;

            var parentEntityInfo = GetEntityInfo(parentEntity.Id);
            if (parentEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntity.Id));

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
            if (childEntity == null) throw new FuException("Child entity is invalid.");
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
            if (parentEntityInfo == null) throw new FuException(Utility.Text.Format("Can not find parent entity '{0}'.", parentEntityId));

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
            if (parentEntity == null) throw new FuException("Parent entity is invalid.");
            DetachChildEntities(parentEntity.Id, userData);
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
        /// <param name="entityInstance">实体实例。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="duration">持续时间。</param>
        /// <param name="userData">用户自定义数据。</param>
        private void InternalShowEntity(UniTaskCompletionSource<Entity> tcs, int entityId, string entityAssetName, EntityGroup entityGroup, object entityInstance, bool isNewInstance, float duration,
                                        object userData)
        {
            try
            {
                var entity = m_EntityHelper.CreateEntity(entityInstance, entityGroup, userData);
                if (entity == null)
                {
                    var exception = new FuException("Can not create entity in entity helper.");
                    tcs.TrySetException(exception);
                    throw exception;
                }

                var entityInfo = EntityInfo.Create(entity);
                m_EntityDict.Add(entityId, entityInfo);

                entityInfo.Status = EntityStatus.WillInit;
                entity.OnInit(entityId, entityAssetName, entityGroup, isNewInstance, userData);

                entityInfo.Status = EntityStatus.Inited;
                entityGroup.AddEntity(entity);

                entityInfo.Status = EntityStatus.WillShow;
                entity.OnShow(userData);

                entityInfo.Status = EntityStatus.Showed;

                if (m_ShowEntitySuccessEventHandler != null)
                {
                    var showEntitySuccessEventArgs = ShowEntitySuccessEventArgs.Create(entity, duration, userData);
                    m_ShowEntitySuccessEventHandler(this, showEntitySuccessEventArgs);
                    ReferencePool.Runtime.ReferencePool.Release(showEntitySuccessEventArgs);
                }

                tcs.TrySetResult(entity);
            }
            catch (Exception exception)
            {
                if (m_ShowEntityFailureEventHandler != null)
                {
                    var showEntityFailureEventArgs = ShowEntityFailureEventArgs.Create(entityId, entityAssetName, entityGroup.Name, exception.ToString(), userData);
                    m_ShowEntityFailureEventHandler(this, showEntityFailureEventArgs);
                    ReferencePool.Runtime.ReferencePool.Release(showEntityFailureEventArgs);
                    return;
                }

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

            if (entityInfo.Status == EntityStatus.Hidden) return;

            var entity = entityInfo.Entity;
            DetachEntity(entity.Id, userData);
            entityInfo.Status = EntityStatus.WillHide;

            entity.OnHide(m_IsShutdown, userData);
            entityInfo.Status = EntityStatus.Hidden;

            var entityGroup = (EntityGroup)entity.EntityGroup;
            if (entityGroup == null) throw new FuException("Entity group is invalid.");

            entityGroup.RemoveEntity(entity);
            if (!m_EntityDict.Remove(entity.Id)) throw new FuException("Entity info is unmanaged.");

            if (m_HideEntityCompleteEventHandler != null)
            {
                var hideEntityCompleteEventArgs = HideEntityCompleteEventArgs.Create(entity.Id, entity.EntityAssetName, entityGroup, userData);
                m_HideEntityCompleteEventHandler(this, hideEntityCompleteEventArgs);
                ReferencePool.Runtime.ReferencePool.Release(hideEntityCompleteEventArgs);
            }

            m_RecycleQueue.Enqueue(entityInfo);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 加载实体资源成功回调。
        /// </summary>
        /// <param name="tcs">显示实体的Task。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityAsset">实体资源对象。</param>
        /// <param name="duration">持续时间。</param>
        /// <param name="userData"></param>
        /// <exception cref="FuException"></exception>
        private void LoadAssetSuccessCallback(UniTaskCompletionSource<Entity> tcs, string entityAssetName, object entityAsset, float duration, object userData)
        {
            if (userData is not ShowEntityInfo showEntityInfo)
            {
                var exception = new FuException("Show entity info is invalid.");
                tcs.TrySetException(exception);
                throw exception;
            }

            if (m_WaitReleaseOnLoadEntitySet.Contains(showEntityInfo.SerialId))
            {
                m_WaitReleaseOnLoadEntitySet.Remove(showEntityInfo.SerialId);
                ReferencePool.Runtime.ReferencePool.Release(showEntityInfo);
                m_EntityHelper.ReleaseEntity(entityAsset, null);
                return;
            }

            m_LoadingEntityDict.Remove(showEntityInfo.EntityId);
            var entityInstanceObject = EntityInstanceObject.Create(entityAssetName, entityAsset, m_EntityHelper.InstantiateEntity(entityAsset), m_EntityHelper);
            showEntityInfo.EntityGroup.RegisterEntityInstanceObject(entityInstanceObject, true);

            InternalShowEntity(tcs, showEntityInfo.EntityId, entityAssetName, showEntityInfo.EntityGroup, entityInstanceObject.Target, true, duration, showEntityInfo.UserData);
            ReferencePool.Runtime.ReferencePool.Release(showEntityInfo);
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

            FuException exception;
            if (showEntityInfo == null)
            {
                exception = new FuException("Show entity info is invalid.");
                tcs.TrySetException(exception);
                throw exception;
            }

            if (m_WaitReleaseOnLoadEntitySet.Contains(showEntityInfo.SerialId))
            {
                m_WaitReleaseOnLoadEntitySet.Remove(showEntityInfo.SerialId);
                return;
            }

            m_LoadingEntityDict.Remove(showEntityInfo.EntityId);
            var appendErrorMessage = Utility.Text.Format("Load entity failure, asset name '{0}', status '{1}', error message '{2}'.", entityAssetName, status, errorMessage);
            exception = new FuException(appendErrorMessage);
            if (m_ShowEntityFailureEventHandler != null)
            {
                var showEntityFailureEventArgs =
                    ShowEntityFailureEventArgs.Create(showEntityInfo.EntityId, entityAssetName, showEntityInfo.EntityGroup.Name, appendErrorMessage, showEntityInfo.UserData);
                m_ShowEntityFailureEventHandler(this, showEntityFailureEventArgs);
                ReferencePool.Runtime.ReferencePool.Release(showEntityFailureEventArgs);
            }

            tcs.TrySetException(exception);
            throw exception;
        }

        #endregion
    }
}