using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架模块抽象基类。
    /// 定义了模块的相关生命周期。
    /// </summary>
    public abstract class FuModule : MonoBehaviour
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected internal virtual int Priority => ModulePriority.Default;
        
        /// <summary>
        /// 获取模块是否已初始化。
        /// </summary>
        public bool IsInitialized { get; internal set; }

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal abstract void OnInit();

        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型</param>
        protected internal abstract void OnShutdown(ShutdownType shutdownType);
    }
}