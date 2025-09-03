using System;
using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 显示实体成功事件。
    /// </summary>
    public sealed class ShowEntitySuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取显示实体成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 显示实体成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(ShowEntitySuccessEventArgs).FullName;

        /// <summary>
        /// 获取实体逻辑类型。
        /// </summary>
        public Type EntityLogicType { get; private set; }

        /// <summary>
        /// 获取显示成功的实体。
        /// </summary>
        public Entity Entity { get; private set; }

        /// <summary>
        /// 获取加载持续时间。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }


        /// <summary>
        /// 清理显示实体成功事件。
        /// </summary>
        public override void Clear()
        {
            EntityLogicType = null;
            Entity          = null;
            Duration        = 0f;
            UserData        = null;
        }

        /// <summary>
        /// 创建显示实体成功事件。
        /// </summary>
        /// <param name="entity">加载成功的实体。</param>
        /// <param name="duration">加载持续时间。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的显示实体成功事件。</returns>
        public static ShowEntitySuccessEventArgs Create(Entity entity, float duration, object userData)
        {
            var showEntitySuccessEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<ShowEntitySuccessEventArgs>();
            showEntitySuccessEventArgs.Entity   = entity;
            showEntitySuccessEventArgs.Duration = duration;
            showEntitySuccessEventArgs.UserData = userData;
            return showEntitySuccessEventArgs;
        }
    }
}