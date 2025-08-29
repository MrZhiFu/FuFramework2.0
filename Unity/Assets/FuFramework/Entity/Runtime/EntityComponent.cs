using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 实体组件。
    /// 功能：封装实体管理相关的功能。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Entity")]
    public sealed partial class EntityComponent : FuComponent
    {
        /// <summary>
        /// 实体加载默认优先级
        /// </summary>
        private const int DefaultPriority = 0;

        /// <summary>
        /// 实体管理器
        /// </summary>
        private IEntityManager m_EntityManager;

        /// <summary>
        /// 事件组件
        /// </summary>
        private EventComponent m_EventComponent;

        private readonly List<IEntity> m_InternalEntityResults = new();

        /// <summary>
        /// 是否显示实体加载更新事件
        /// </summary>
        [SerializeField] private bool m_EnableShowEntityUpdateEvent;

        /// <summary>
        /// 是否显示实体依赖资源加载事件
        /// </summary>
        [SerializeField] private bool m_EnableShowEntityDependencyAssetEvent;

        /// <summary>
        /// 实体实例对象池根节点
        /// </summary>
        [SerializeField] private Transform m_InstanceRoot;

        /// <summary>
        /// 实体帮助器类型名称
        /// </summary>
        [SerializeField] private string m_EntityHelperTypeName = "FuFramework.Entity.Runtime.DefaultEntityHelper";

        /// <summary>
        /// 自定义实体帮助器
        /// </summary>
        [SerializeField] private EntityHelperBase m_CustomEntityHelper;

        /// <summary>
        /// 实体组帮助器类型名称
        /// </summary>
        [SerializeField] private string m_EntityGroupHelperTypeName = "FuFramework.Entity.Runtime.DefaultEntityGroupHelper";

        /// <summary>
        /// 自定义实体组帮助器
        /// </summary>
        [SerializeField] private EntityGroupHelperBase m_CustomEntityGroupHelper;

        /// <summary>
        /// 实体组列表
        /// </summary>
        [SerializeField] private EntityGroup[] m_EntityGroups;

        /// <summary>
        /// 获取实体数量。
        /// </summary>
        public int EntityCount => m_EntityManager.EntityCount;

        /// <summary>
        /// 获取实体组数量。
        /// </summary>
        public int EntityGroupCount => m_EntityManager.EntityGroupCount;

        protected override void OnInit()
        {
            m_EntityManager = FuEntry.GetModule<IEntityManager>();
            if (m_EntityManager == null)
            {
                Log.Fatal("Entity manager is invalid.");
                return;
            }

            m_EntityManager.ShowEntitySuccess += OnShowEntitySuccess;
            m_EntityManager.ShowEntityFailure += OnShowEntityFailure;
            m_EntityManager.HideEntityComplete += OnHideEntityComplete;

            var baseComponent = ModuleManager.GetModule<BaseComponent>();
            if (!baseComponent)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = ModuleManager.GetModule<EventComponent>();
            if (!m_EventComponent)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            m_EntityManager.SetObjectPoolManager(FuEntry.GetModule<IObjectPoolManager>());

            var entityHelper = Helper.CreateHelper(m_EntityHelperTypeName, m_CustomEntityHelper);
            if (!entityHelper)
            {
                Log.Error("Can not create entity helper.");
                return;
            }

            entityHelper.name = "Entity Helper";
            entityHelper.transform.SetParent(transform);
            entityHelper.transform.localScale = Vector3.one;
            m_EntityManager.SetEntityHelper(entityHelper);

            if (!m_InstanceRoot)
            {
                m_InstanceRoot = new GameObject("Entity Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            // 添加实体组
            foreach (var entityGroup in m_EntityGroups)
            {
                if (AddEntityGroup(entityGroup.Name, entityGroup.InstanceAutoReleaseInterval, entityGroup.InstanceCapacity,
                        entityGroup.InstanceExpireTime, entityGroup.InstancePriority)) continue;
                Log.Warning("Add entity group '{0}' failure.", entityGroup.Name);
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds) { }
        protected override void OnShutdown(ShutdownType shutdownType) { }


        #region 实体组相关方法

        /// <summary>
        /// 是否存在实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <returns>是否存在实体组。</returns>
        public bool HasEntityGroup(string entityGroupName) => m_EntityManager.HasEntityGroup(entityGroupName);

        /// <summary>
        /// 获取实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <returns>要获取的实体组。</returns>
        public IEntityGroup GetEntityGroup(string entityGroupName) => m_EntityManager.GetEntityGroup(entityGroupName);

        /// <summary>
        /// 获取所有实体组。
        /// </summary>
        /// <returns>所有实体组。</returns>
        public IEntityGroup[] GetAllEntityGroups() => m_EntityManager.GetAllEntityGroups();

        /// <summary>
        /// 获取所有实体组。
        /// </summary>
        /// <param name="results">所有实体组。</param>
        public void GetAllEntityGroups(List<IEntityGroup> results) => m_EntityManager.GetAllEntityGroups(results);

        /// <summary>
        /// 增加实体组。
        /// </summary>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="instanceAutoReleaseInterval">实体实例对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="instanceCapacity">实体实例对象池容量。</param>
        /// <param name="instanceExpireTime">实体实例对象池对象过期秒数。</param>
        /// <param name="instancePriority">实体实例对象池的优先级。</param>
        /// <returns>是否增加实体组成功。</returns>
        public bool AddEntityGroup(string entityGroupName, float instanceAutoReleaseInterval, int instanceCapacity, float instanceExpireTime,
            int instancePriority)
        {
            if (m_EntityManager.HasEntityGroup(entityGroupName)) return false;

            var entityGroupHelper = Helper.CreateHelper(m_EntityGroupHelperTypeName, m_CustomEntityGroupHelper, EntityGroupCount);
            if (!entityGroupHelper)
            {
                Log.Error("Can not create entity group helper.");
                return false;
            }

            entityGroupHelper.name = Utility.Text.Format("Entity Group - {0}", entityGroupName);
            entityGroupHelper.transform.SetParent(m_InstanceRoot);
            entityGroupHelper.transform.localScale = Vector3.one;

            return m_EntityManager.AddEntityGroup(entityGroupName, instanceAutoReleaseInterval, instanceCapacity, instanceExpireTime,
                instancePriority, entityGroupHelper);
        }

        #endregion

        #region 实体Get

        /// <summary>
        /// 是否存在实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>是否存在实体。</returns>
        public bool HasEntity(int entityId) => m_EntityManager.HasEntity(entityId);

        /// <summary>
        /// 是否存在实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>是否存在实体。</returns>
        public bool HasEntity(string entityAssetName) => m_EntityManager.HasEntity(entityAssetName);

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>实体。</returns>
        public Entity GetEntity(int entityId) => m_EntityManager.GetEntity(entityId) as Entity;

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>要获取的实体。</returns>
        public Entity GetEntity(string entityAssetName) => m_EntityManager.GetEntity(entityAssetName) as Entity;

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>要获取的实体。</returns>
        public Entity[] GetEntities(string entityAssetName)
        {
            var entities = m_EntityManager.GetEntities(entityAssetName);
            var entityImpls = new Entity[entities.Length];
            for (var i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = entities[i] as Entity;
            }

            return entityImpls;
        }

        /// <summary>
        /// 获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="results">要获取的实体。</param>
        public void GetEntities(string entityAssetName, List<Entity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetEntities(entityAssetName, m_InternalEntityResults);
            results.AddRange(m_InternalEntityResults.Cast<Entity>());
        }

        /// <summary>
        /// 获取所有已加载的实体。
        /// </summary>
        /// <returns>所有已加载的实体。</returns>
        public Entity[] GetAllLoadedEntities()
        {
            var entities = m_EntityManager.GetAllLoadedEntities();
            var entityImpls = new Entity[entities.Length];
            for (var i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = entities[i] as Entity;
            }

            return entityImpls;
        }

        /// <summary>
        /// 获取所有已加载的实体。
        /// </summary>
        /// <param name="results">所有已加载的实体。</param>
        public void GetAllLoadedEntities(List<Entity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetAllLoadedEntities(m_InternalEntityResults);
            results.AddRange(m_InternalEntityResults.Cast<Entity>());
        }

        /// <summary>
        /// 获取所有正在加载实体的编号。
        /// </summary>
        /// <returns>所有正在加载实体的编号。</returns>
        public int[] GetAllLoadingEntityIds() => m_EntityManager.GetAllLoadingEntityIds();

        /// <summary>
        /// 获取所有正在加载实体的编号。
        /// </summary>
        /// <param name="results">所有正在加载实体的编号。</param>
        public void GetAllLoadingEntityIds(List<int> results) => m_EntityManager.GetAllLoadingEntityIds(results);

        /// <summary>
        /// 是否正在加载实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <returns>是否正在加载实体。</returns>
        public bool IsLoadingEntity(int entityId) => m_EntityManager.IsLoadingEntity(entityId);

        /// <summary>
        /// 是否是合法的实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <returns>实体是否合法。</returns>
        public bool IsValidEntity(Entity entity) => m_EntityManager.IsValidEntity(entity);

        #endregion

        #region 实体Show

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <typeparam name="T">实体逻辑类型。</typeparam>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        public Task<IEntity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName) where T : EntityLogic
            => ShowEntityAsync(entityId, typeof(T), entityAssetName, entityGroupName, DefaultPriority, null);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityLogicType">实体逻辑类型。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        public Task<IEntity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName)
            => ShowEntityAsync(entityId, entityLogicType, entityAssetName, entityGroupName, DefaultPriority, null);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <typeparam name="T">实体逻辑类型。</typeparam>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="priority">加载实体资源的优先级。</param>
        public Task<IEntity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName, int priority) where T : EntityLogic
            => ShowEntityAsync(entityId, typeof(T), entityAssetName, entityGroupName, priority, null);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityLogicType">实体逻辑类型。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="priority">加载实体资源的优先级。</param>
        public Task<IEntity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, int priority)
            => ShowEntityAsync(entityId, entityLogicType, entityAssetName, entityGroupName, priority, null);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <typeparam name="T">实体逻辑类型。</typeparam>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public Task<IEntity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName, object userData) where T : EntityLogic
            => ShowEntityAsync(entityId, typeof(T), entityAssetName, entityGroupName, DefaultPriority, userData);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityLogicType">实体逻辑类型。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public Task<IEntity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, object userData)
            => ShowEntityAsync(entityId, entityLogicType, entityAssetName, entityGroupName, DefaultPriority, userData);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <typeparam name="T">实体逻辑类型。</typeparam>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="priority">加载实体资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public Task<IEntity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName, int priority, object userData)
            where T : EntityLogic
            => ShowEntityAsync(entityId, typeof(T), entityAssetName, entityGroupName, priority, userData);

        /// <summary>
        /// 显示实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityLogicType">实体逻辑类型。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="priority">加载实体资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public async Task<IEntity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, int priority,
            object userData)
        {
            if (entityLogicType == null)
            {
                Log.Error("Entity type is invalid.");
                return null;
            }

            return await m_EntityManager.ShowEntityAsync(entityId, entityAssetName, entityGroupName, priority,
                ShowEntityInfo.Create(entityLogicType, userData));
        }

        #endregion

        #region 实体Hide

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        public void HideEntity(int entityId) => m_EntityManager.HideEntity(entityId);

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void HideEntity(int entityId, object userData) => m_EntityManager.HideEntity(entityId, userData);

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        public void HideEntity(Entity entity) => m_EntityManager.HideEntity(entity);

        /// <summary>
        /// 隐藏实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void HideEntity(Entity entity, object userData) => m_EntityManager.HideEntity(entity, userData);

        /// <summary>
        /// 隐藏所有已加载的实体。
        /// </summary>
        public void HideAllLoadedEntities() => m_EntityManager.HideAllLoadedEntities();

        /// <summary>
        /// 隐藏所有已加载的实体。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void HideAllLoadedEntities(object userData) => m_EntityManager.HideAllLoadedEntities(userData);

        /// <summary>
        /// 隐藏所有正在加载的实体。
        /// </summary>
        public void HideAllLoadingEntities() => m_EntityManager.HideAllLoadingEntities();

        #endregion

        #region 父实体/子实体Get

        /// <summary>
        /// 获取父实体。
        /// </summary>
        /// <param name="childEntityId">要获取父实体的子实体的实体编号。</param>
        /// <returns>子实体的父实体。</returns>
        public Entity GetParentEntity(int childEntityId) => m_EntityManager.GetParentEntity(childEntityId) as Entity;

        /// <summary>
        /// 获取父实体。
        /// </summary>
        /// <param name="childEntity">要获取父实体的子实体。</param>
        /// <returns>子实体的父实体。</returns>
        public Entity GetParentEntity(Entity childEntity) => m_EntityManager.GetParentEntity(childEntity) as Entity;

        /// <summary>
        /// 获取子实体数量。
        /// </summary>
        /// <param name="parentEntityId">要获取子实体数量的父实体的实体编号。</param>
        /// <returns>子实体数量。</returns>
        public int GetChildEntityCount(int parentEntityId) => m_EntityManager.GetChildEntityCount(parentEntityId);

        /// <summary>
        /// 获取子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取子实体的父实体的实体编号。</param>
        /// <returns>子实体。</returns>
        public Entity GetChildEntity(int parentEntityId) => m_EntityManager.GetChildEntity(parentEntityId) as Entity;

        /// <summary>
        /// 获取子实体。
        /// </summary>
        /// <param name="parentEntity">要获取子实体的父实体。</param>
        /// <returns>子实体。</returns>
        public Entity GetChildEntity(IEntity parentEntity) => m_EntityManager.GetChildEntity(parentEntity) as Entity;

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取所有子实体的父实体的实体编号。</param>
        /// <returns>所有子实体。</returns>
        public Entity[] GetChildEntities(int parentEntityId)
        {
            var entities = m_EntityManager.GetChildEntities(parentEntityId);
            var entityImpls = new Entity[entities.Length];
            for (var i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = entities[i] as Entity;
            }

            return entityImpls;
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntityId">要获取所有子实体的父实体的实体编号。</param>
        /// <param name="results">所有子实体。</param>
        public void GetChildEntities(int parentEntityId, List<IEntity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetChildEntities(parentEntityId, m_InternalEntityResults);
            foreach (var entity in m_InternalEntityResults)
            {
                results.Add(entity as Entity);
            }
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntity">要获取所有子实体的父实体。</param>
        /// <returns>所有子实体。</returns>
        public Entity[] GetChildEntities(Entity parentEntity)
        {
            var entities = m_EntityManager.GetChildEntities(parentEntity);
            var entityImpls = new Entity[entities.Length];
            for (var i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = entities[i] as Entity;
            }

            return entityImpls;
        }

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="parentEntity">要获取所有子实体的父实体。</param>
        /// <param name="results">所有子实体。</param>
        public void GetChildEntities(IEntity parentEntity, List<IEntity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetChildEntities(parentEntity, m_InternalEntityResults);
            foreach (var entity in m_InternalEntityResults)
            {
                results.Add((Entity)entity);
            }
        }

        #endregion

        #region 附加子实体

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        public void AttachEntity(int childEntityId, int parentEntityId)
            => AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), string.Empty, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity)
            => AttachEntity(GetEntity(childEntityId), parentEntity, string.Empty, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId)
            => AttachEntity(childEntity, GetEntity(parentEntityId), string.Empty, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity)
            => AttachEntity(childEntity, parentEntity, string.Empty, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, string parentTransformPath)
            => AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransformPath, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity, string parentTransformPath)
            => AttachEntity(GetEntity(childEntityId), parentEntity, parentTransformPath, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId, string parentTransformPath)
            => AttachEntity(childEntity, GetEntity(parentEntityId), parentTransformPath, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, string parentTransformPath)
            => AttachEntity(childEntity, parentEntity, parentTransformPath, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, Transform parentTransform)
            => AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransform, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity, Transform parentTransform)
            => AttachEntity(GetEntity(childEntityId), parentEntity, parentTransform, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId, Transform parentTransform)
            => AttachEntity(childEntity, GetEntity(parentEntityId), parentTransform, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, Transform parentTransform)
            => AttachEntity(childEntity, parentEntity, parentTransform, null);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, object userData)
            => AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), string.Empty, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity, object userData)
            => AttachEntity(GetEntity(childEntityId), parentEntity, string.Empty, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId, object userData)
            => AttachEntity(childEntity, GetEntity(parentEntityId), string.Empty, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, object userData)
            => AttachEntity(childEntity, parentEntity, string.Empty, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, string parentTransformPath, object userData)
            => AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransformPath, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity, string parentTransformPath, object userData)
            => AttachEntity(GetEntity(childEntityId), parentEntity, parentTransformPath, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId, string parentTransformPath, object userData)
            => AttachEntity(childEntity, GetEntity(parentEntityId), parentTransformPath, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransformPath">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, string parentTransformPath, object userData)
        {
            if (childEntity == null)
            {
                Log.Warning("Child entity is invalid.");
                return;
            }

            if (parentEntity == null)
            {
                Log.Warning("Parent entity is invalid.");
                return;
            }

            Transform parentTransform;
            if (string.IsNullOrEmpty(parentTransformPath))
            {
                parentTransform = parentEntity.Logic.CachedTransform;
            }
            else
            {
                parentTransform = parentEntity.Logic.CachedTransform.Find(parentTransformPath);
                if (parentTransform == null)
                {
                    Log.Warning("Can not find transform path '{0}' from parent entity '{1}'.", parentTransformPath,
                        parentEntity.Logic.Name);
                    parentTransform = parentEntity.Logic.CachedTransform;
                }
            }

            AttachEntity(childEntity, parentEntity, parentTransform, userData);
        }

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, int parentEntityId, Transform parentTransform, object userData)
            => AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransform, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntityId">要附加的子实体的实体编号。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(int childEntityId, Entity parentEntity, Transform parentTransform, object userData)
            => AttachEntity(GetEntity(childEntityId), parentEntity, parentTransform, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntityId">被附加的父实体的实体编号。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, int parentEntityId, Transform parentTransform, object userData)
            => AttachEntity(childEntity, GetEntity(parentEntityId), parentTransform, userData);

        /// <summary>
        /// 附加子实体。
        /// </summary>
        /// <param name="childEntity">要附加的子实体。</param>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransform">相对于被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void AttachEntity(Entity childEntity, Entity parentEntity, Transform parentTransform, object userData)
        {
            if (childEntity == null)
            {
                Log.Warning("Child entity is invalid.");
                return;
            }

            if (parentEntity == null)
            {
                Log.Warning("Parent entity is invalid.");
                return;
            }

            if (!parentTransform)
                parentTransform = parentEntity.Logic.CachedTransform;

            m_EntityManager.AttachEntity(childEntity, parentEntity, AttachEntityInfo.Create(parentTransform, userData));
        }

        #endregion

        #region 解除子实体

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntityId">要解除的子实体的实体编号。</param>
        public void DetachEntity(int childEntityId) => m_EntityManager.DetachEntity(childEntityId);

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntityId">要解除的子实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachEntity(int childEntityId, object userData) => m_EntityManager.DetachEntity(childEntityId, userData);

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntity">要解除的子实体。</param>
        public void DetachEntity(Entity childEntity) => m_EntityManager.DetachEntity(childEntity);

        /// <summary>
        /// 解除子实体。
        /// </summary>
        /// <param name="childEntity">要解除的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachEntity(Entity childEntity, object userData) => m_EntityManager.DetachEntity(childEntity, userData);

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntityId">被解除的父实体的实体编号。</param>
        public void DetachChildEntities(int parentEntityId) => m_EntityManager.DetachChildEntities(parentEntityId);

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntityId">被解除的父实体的实体编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachChildEntities(int parentEntityId, object userData) => m_EntityManager.DetachChildEntities(parentEntityId, userData);

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        public void DetachChildEntities(Entity parentEntity) => m_EntityManager.DetachChildEntities(parentEntity);

        /// <summary>
        /// 解除所有子实体。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void DetachChildEntities(Entity parentEntity, object userData) => m_EntityManager.DetachChildEntities(parentEntity, userData);

        #endregion

        #region 实体加锁，优先级设置

        /// <summary>
        /// 设置实体是否被加锁。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="locked">实体是否被加锁。</param>
        public void SetEntityInstanceLocked(Entity entity, bool locked)
        {
            if (!entity)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            var entityGroup = entity.EntityGroup;
            if (entityGroup == null)
            {
                Log.Warning("Entity group is invalid.");
                return;
            }

            entityGroup.SetEntityInstanceLocked(entity.gameObject, locked);
        }

        /// <summary>
        /// 设置实体的优先级。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="priority">实体优先级。</param>
        public void SetInstancePriority(Entity entity, int priority)
        {
            if (!entity)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            var entityGroup = entity.EntityGroup;
            if (entityGroup == null)
            {
                Log.Warning("Entity group is invalid.");
                return;
            }

            entityGroup.SetEntityInstancePriority(entity.gameObject, priority);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 实体显示成功事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnShowEntitySuccess(object sender, ShowEntitySuccessEventArgs eventArgs) => m_EventComponent.Fire(this, eventArgs);

        /// <summary>
        /// 实体显示失败事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnShowEntityFailure(object sender, ShowEntityFailureEventArgs eventArgs)
        {
            Log.Warning("Show entity failure, entity id '{0}', asset name '{1}', entity group name '{2}', error message '{3}'.", eventArgs.EntityId,
                eventArgs.EntityAssetName,
                eventArgs.EntityGroupName, eventArgs.ErrorMessage);
            m_EventComponent.Fire(this, eventArgs);
        }

        /// <summary>
        /// 实体隐藏成功事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnHideEntityComplete(object sender, HideEntityCompleteEventArgs eventArgs) => m_EventComponent.Fire(this, eventArgs);

        #endregion
    }
}