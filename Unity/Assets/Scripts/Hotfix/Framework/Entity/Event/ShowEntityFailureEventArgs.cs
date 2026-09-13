using System;
using Hotfix.Framework.Event;
using Hotfix.Framework.Core;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 显示实体失败事件。
    /// </summary>
    public sealed class ShowEntityFailureEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(ShowEntityFailureEventArgs).FullName;

        /// <summary>
        /// 获取实体编号。
        /// </summary>
        public int EntityId { get; private set; }

        /// <summary>
        /// 获取实体逻辑类型。
        /// </summary>
        public Type EntityLogicType { get; private set; }

        /// <summary>
        /// 获取实体资源名称。
        /// </summary>
        public string EntityAssetName { get; private set; }

        /// <summary>
        /// 获取实体组名称。
        /// </summary>
        public string EntityGroupName { get; private set; }

        /// <summary>
        /// 获取错误信息。
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }


        /// <summary>
        /// 清理显示实体失败事件。
        /// </summary>
        public override void Clear()
        {
            EntityId        = 0;
            EntityLogicType = null;
            EntityAssetName = null;
            EntityGroupName = null;
            ErrorMessage    = null;
            UserData        = null;
        }

        /// <summary>
        /// 创建显示实体失败事件。
        /// </summary>
        /// <param name="entityId">实体编号。</param>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="entityGroupName">实体组名称。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的显示实体失败事件。</returns>
        public static ShowEntityFailureEventArgs Create(int entityId, string entityAssetName, string entityGroupName, string errorMessage, object userData)
        {
            // 判空/类型校验必须前置到 Acquire 之前：否则强制转换抛异常时，已获取的事件对象永不归还引用池（泄漏）。
            if (userData is not ShowEntityInfoEx showEntityInfoEx)
                throw new InvalidOperationException("[EntityModule] 创建显示实体失败事件失败, 用户自定义数据不是 ShowEntityInfoEx 类型.");

            var showEntityFailureEventArgs = ReferencePool.Acquire<ShowEntityFailureEventArgs>();
            showEntityFailureEventArgs.EntityId        = entityId;
            showEntityFailureEventArgs.EntityAssetName = entityAssetName;
            showEntityFailureEventArgs.EntityGroupName = entityGroupName;
            showEntityFailureEventArgs.ErrorMessage    = errorMessage;
            showEntityFailureEventArgs.UserData        = showEntityInfoEx.UserData;
            showEntityFailureEventArgs.EntityLogicType = showEntityInfoEx.EntityLogicType;
            return showEntityFailureEventArgs;
        }
    }
}