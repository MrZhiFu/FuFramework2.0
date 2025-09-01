using System;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 显示时的实体信息(逻辑处理对象和用户数据)。
    /// 用于在显示时暂时保存实体的逻辑处理对象和用户数据信息，以便在显示过程中传递数据。
    /// </summary>
    internal sealed class ShowEntityInfo : IReference
    {
        /// <summary>
        /// 实体逻辑处理类型。
        /// </summary>
        public Type EntityLogicType { get; private set; }

        /// <summary>
        /// 用户数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建实体显示信息。
        /// </summary>
        /// <param name="entityLogicType"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static ShowEntityInfo Create(Type entityLogicType, object userData)
        {
            var showEntityInfo = ReferencePool.Runtime.ReferencePool.Acquire<ShowEntityInfo>();
            showEntityInfo.EntityLogicType = entityLogicType;
            showEntityInfo.UserData        = userData;
            return showEntityInfo;
        }

        /// <summary>
        /// 清理实体显示信息。
        /// </summary>
        public void Clear()
        {
            EntityLogicType = null;
            UserData        = null;
        }
    }
}