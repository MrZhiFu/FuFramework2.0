using UnityEngine;

// ReSharper disable Unity.RedundantEventFunction
// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架模块抽象基类。
    /// 定义了模块的相关生命周期。
    /// 继承`MonoBehaviour`为了更好的在模块属性面板中显示模块信息, 隐藏了MonoBehaviour的相关生命周期，
    /// 防止子类使用，造成模块生命周期混乱
    /// </summary>
    public abstract class FuModule : MonoBehaviour
    {
        /// <summary>
        /// 优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected internal virtual int Priority => ModulePriority.Default;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; internal set; }

        #region 框架生命周期方法(由子类实现)

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal abstract void OnInit();

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
        protected internal abstract void OnDispose();

        #endregion

        #region 隐藏Unity生命周期方法(禁止子类使用)

        private void Awake() { }

        private void Start() { }

        private void Update() { }

        private void OnDestroy() { }

        private void OnEnable() { }

        private void OnDisable() { }

        private void FixedUpdate() { }

        private void LateUpdate() { }

        #endregion
    }
}