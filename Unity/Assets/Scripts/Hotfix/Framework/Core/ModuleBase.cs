// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Core
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
        /// 模块是否处于已初始化（存活）状态。由 ModuleManager 在注册成功时置 true、释放后置 false。
        /// 重启的排水窗口内模块实例仍保留在 ModuleList 中（供重新初始化复用单例）但已释放，
        /// 此时为 false —— 帧驱动据此跳过，避免驱动已销毁的模块。
        /// </summary>
        public bool IsAlive { get; internal set; }

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
        /// 每秒更新
        /// </summary>
        protected internal virtual void OnPerSecondUpdate() { }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal virtual void OnDispose() { }
    }
}
