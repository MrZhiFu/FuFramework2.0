using System;
using Hotfix.Framework.Event;
using Hotfix.Framework.Core;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 显示实体成功事件。
    /// </summary>
    public sealed class ShowEntitySuccessEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(ShowEntitySuccessEventArgs).FullName;

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
            // 判空/类型校验必须前置到 Acquire 之前：否则强制转换抛异常时，已获取的事件对象永不归还引用池（泄漏）。
            if (userData is not ShowEntityInfoEx showEntityInfoEx)
                throw new InvalidOperationException("[EntityModule] 创建显示实体成功事件失败, 用户自定义数据不是 ShowEntityInfoEx 类型.");

            var showEntitySuccessEventArgs = ReferencePool.Acquire<ShowEntitySuccessEventArgs>();
            showEntitySuccessEventArgs.Entity          = entity;
            showEntitySuccessEventArgs.Duration        = duration;
            showEntitySuccessEventArgs.UserData        = showEntityInfoEx.UserData;
            showEntitySuccessEventArgs.EntityLogicType = showEntityInfoEx.EntityLogicType;
            return showEntitySuccessEventArgs;
        }
    }
}