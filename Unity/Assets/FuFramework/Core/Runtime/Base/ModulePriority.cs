// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 框架模块优先级定义
    /// </summary>
    /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
    public static class ModulePriority
    {
        /// <summary>
        /// 系统级：输入、网络
        /// </summary>
        public const int System = 100;

        /// <summary>
        /// 核心：资源、事件
        /// </summary>
        public const int Core = 80;

        /// <summary>
        /// 游戏逻辑：角色、战斗、场景
        /// </summary>
        public const int Game = 60;

        /// <summary>
        /// UI相关
        /// </summary>
        public const int UI = 50;

        /// <summary>
        /// 默认优先级
        /// </summary>
        public const int Default = 0;
    }
}