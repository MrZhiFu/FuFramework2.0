// ReSharper disable once CheckNamespace

namespace FuFramework.Entity.Runtime
{
    public sealed partial class EntityManager
    {
        /// <summary>
        /// 实体状态。
        /// </summary>
        private enum EntityStatus : byte
        {
            /// <summary>
            /// 未知。
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// 将要初始化。
            /// </summary>
            WillInit,

            /// <summary>
            /// 初始化完毕
            /// </summary>
            Inited,

            /// <summary>
            /// 将要显示。
            /// </summary>
            WillShow,

            /// <summary>
            /// 显示完毕。
            /// </summary>
            Showed,

            /// <summary>
            /// 将要隐藏。
            /// </summary>
            WillHide,

            /// <summary>
            /// 隐藏完毕。
            /// </summary>
            Hidden,

            /// <summary>
            /// 将要回收。
            /// </summary>
            WillRecycle,

            /// <summary>
            /// 回收完毕。
            /// </summary>
            Recycled
        }
    }
}