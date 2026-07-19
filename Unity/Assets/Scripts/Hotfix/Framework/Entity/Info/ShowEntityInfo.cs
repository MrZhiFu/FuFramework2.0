using Hotfix.Framework.ReferencePools;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 显示时的实体信息。
    /// 功能：
    ///     1. 用于在显示时暂时保存实体信息，以便在显示过程中传递实体信息。
    /// </summary>
    public sealed class ShowEntityInfo : IReference
    {
        /// <summary>
        /// 实体自增编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 实体编号。
        /// </summary>
        public int EntityId { get; private set; }

        /// <summary>
        /// 实体所属组。
        /// </summary>
        public EntityGroup EntityGroup { get; private set; }


        /// <summary>
        /// 用户数据
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建实体信息。
        /// </summary>
        /// <param name="serialId"></param>
        /// <param name="entityId"></param>
        /// <param name="entityGroup"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static ShowEntityInfo Create(int serialId, int entityId, EntityGroup entityGroup, object userData)
        {
            var showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
            showEntityInfo.SerialId    = serialId;
            showEntityInfo.EntityId    = entityId;
            showEntityInfo.EntityGroup = entityGroup;
            showEntityInfo.UserData    = userData;
            return showEntityInfo;
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public void Clear()
        {
            SerialId    = 0;
            EntityId    = 0;
            EntityGroup = null;
            UserData    = null;
        }
    }
}
