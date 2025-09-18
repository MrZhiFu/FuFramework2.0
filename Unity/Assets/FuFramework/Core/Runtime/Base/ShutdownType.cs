// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 关闭游戏框架模块时的类型。
    /// </summary>
    public enum ShutdownType : byte
    {
        /// <summary>
        /// 模块手动反注册。
        /// </summary>
        Unregister,
        
        /// <summary>
        /// 关闭游戏框架并重启游戏。
        /// </summary>
        Restart,

        /// <summary>
        /// 关闭游戏框架并退出游戏。
        /// </summary>
        Quit
    }
}
