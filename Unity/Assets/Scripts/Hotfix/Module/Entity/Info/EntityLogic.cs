using FuFramework.Core.Runtime;
using UnityEngine;

// ReSharper disable NotAccessedField.Local

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Hotfix.Entity
{
    /// <summary>
    /// 实体逻辑基类。
    /// 功能：
    ///     1. 实现实体逻辑的基本功能，如初始化、回收、显示、隐藏、附加子实体、解除子实体等。
    ///     2. 可继承此类实现自身的实体逻辑。
    /// </summary>
    public abstract class EntityLogic : MonoBehaviour
    {
        /// <summary>
        /// 实体是否可见。
        /// </summary>
        private bool m_Visible;

        /// <summary>
        /// 实体的原始层级。
        /// </summary>
        private int m_OriginalLayer;

        /// <summary>
        /// 实体的原始 Transform。
        /// </summary>
        private Transform m_OriginalTransform;

        /// <summary>
        /// 实体的用户自定义数据。
        /// </summary>
        private object m_UserData;

        /// <summary>
        /// 获取或设置实体。
        /// </summary>
        public Entity Entity { get; private set; }

        /// <summary>
        /// 获取或设置实体是否可用。
        /// </summary>
        public bool Available { get; private set; }

        /// <summary>
        /// 获取或设置已缓存的 Transform。
        /// </summary>
        public Transform CachedTransform { get; private set; }


        /// <summary>
        /// 获取或设置实体名称。
        /// </summary>
        public string Name
        {
            get => gameObject.name;
            set => gameObject.name = value;
        }

        /// <summary>
        /// 获取或设置实体是否可见。
        /// </summary>
        public bool Visible
        {
            get => Available && m_Visible;
            set
            {
                if (!Available)
                {
                    FuLogger.LogWarning($"[EntityLogic] 设置实体是否可见失败, 实体 '{Name}' 不可用");
                    return;
                }

                if (m_Visible == value) return;

                m_Visible = value;
                InternalSetVisible(value);
            }
        }

        /// <summary>
        /// 实体初始化。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnInit(object userData)
        {
            m_UserData = userData;

            if (!CachedTransform)
                CachedTransform = transform;

            Entity              = GetComponent<Entity>();
            m_OriginalLayer     = gameObject.layer;
            m_OriginalTransform = CachedTransform.parent;
        }

        /// <summary>
        /// 实体轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }

        /// <summary>
        /// 实体显示。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnShow(object userData)
        {
            Available = true;
            Visible   = true;
        }

        /// <summary>
        /// 实体隐藏。
        /// </summary>
        /// <param name="isShutdown">是否是关闭实体管理模块时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnHide(bool isShutdown, object userData)
        {
            gameObject.SetLayerRecursively(m_OriginalLayer);
            Visible   = false;
            Available = false;
        }

        /// <summary>
        /// 实体回收。
        /// </summary>
        protected internal virtual void OnRecycle() { }

        /// <summary>
        /// 实体附加子实体。
        /// </summary>
        /// <param name="childEntity">附加的子实体。</param>
        /// <param name="parentTransform">被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData) { }

        /// <summary>
        /// 实体解除子实体。
        /// </summary>
        /// <param name="childEntity">解除的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnDetached(EntityLogic childEntity, object userData) { }

        /// <summary>
        /// 实体附加子实体。
        /// </summary>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransform">被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            CachedTransform.SetParent(parentTransform);
        }

        /// <summary>
        /// 实体解除子实体。
        /// </summary>
        /// <param name="parentEntity">被解除的父实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected internal virtual void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            CachedTransform.SetParent(m_OriginalTransform);
        }

        /// <summary>
        /// 设置实体的可见性。
        /// </summary>
        /// <param name="visible">实体的可见性。</param>
        protected virtual void InternalSetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}