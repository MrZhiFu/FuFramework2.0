// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架模块抽象基类。
    /// 功能：
    ///     1. 定义了模块的相关生命周期。
    ///     2. 纯 C# 类，不依赖 MonoBehaviour，由 ModuleManager 统一驱动。
    /// </summary>
    public abstract class ModuleBase
    {
        /// <summary>
        /// 初始化
        /// </summary>
        protected internal virtual void OnInit() { }

        /// <summary>
        /// 帧更新
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }

        /// <summary>
        /// 延迟帧更新
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal virtual void OnLateUpdate(float deltaTime, float unscaledDeltaTime) { }

        /// <summary>
        /// 固定帧更新
        /// </summary>
        protected internal virtual void OnFixedUpdate() { }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal virtual void OnDispose() { }
    }
}