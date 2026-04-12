using UnityEngine;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 附加实体信息。
    /// 功能：
    ///     1. 用于在实体附加到其他实体时保存附加实体的信息。
    /// </summary>
    internal sealed class AttachEntityInfo : IReference
    {
        /// <summary>
        /// 父级对象
        /// </summary>
        public Transform ParentTransform { get; private set; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建附加实体信息实例
        /// </summary>
        /// <param name="parentTransform"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static AttachEntityInfo Create(Transform parentTransform, object userData)
        {
            var attachEntityInfo = ReferencePool.Runtime.ReferencePool.Acquire<AttachEntityInfo>();
            attachEntityInfo.ParentTransform = parentTransform;
            attachEntityInfo.UserData        = userData;
            return attachEntityInfo;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            ParentTransform = null;
            UserData        = null;
        }
    }
}