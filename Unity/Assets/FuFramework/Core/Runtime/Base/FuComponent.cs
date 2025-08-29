using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架组件抽象基类。
    /// 实现了组件的自动注册功能。
    /// </summary>
    public abstract class FuComponent : MonoBehaviour
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected internal virtual int Priority => 0;

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal abstract void OnInit();
        
        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected internal abstract void OnUpdate(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected internal abstract void OnShutdown(ShutdownType shutdownType);
    }
}