using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面基类分部类之一。
    /// 目标：提供界面的生命周期相关方法。
    /// 功能：
    ///     1. 初始化。
    ///     2. 打开。
    ///     3. 轮询。
    ///     4. 暂停。
    ///     5. 恢复。
    ///     6. 销毁。
    /// </summary>
    public abstract partial class ViewBase
    {
        #region Internal Methods

        /// <summary>
        /// 初始化界面。
        /// </summary>
        private void _OnInit()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]初始化-OnInit().");
            m_UIModule.PkgManager?.AddRef(PackageName);
            OnInit();
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        internal void _OnOpen()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]打开-OnOpen().");
            Visible      = true;
            UIView.alpha = 0;

            // 先刷新界面
            OnOpen();

            // 再执行打开动画
            switch (TweenType)
            {
                case EUITweenType.None:
                    UIView.alpha = 1;
                    return;
                case EUITweenType.Fade:
                    UIView.TweenFade(1, TweenDuration);
                    return;
                case EUITweenType.Custom:
                    UIView.alpha = 1;
                    DoCustomOpenTween();
                    return;
                default:
                    UIView.alpha = 1;
                    return;
            }
        }

        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        internal void _OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            OnUpdate(deltaTime, unscaledDeltaTime);
        }

        /// <summary>
        /// 界面暂停。
        /// </summary>
        internal void _OnPause()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]暂停-OnPause().");
            Visible = false;
            OnPause();
        }

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        internal void _OnResume()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]恢复-OnResume().");
            Visible = true;
            OnResume();
        }

        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        internal void _OnBeCover()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]被遮挡-OnBeCover().");
            if (!AdjustNotch) Visible = false;
            OnBeCover();
        }

        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        internal void _OnReveal()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]被遮挡恢复-OnReveal().");
            Visible = true;
            OnReveal();
        }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        internal void _OnClose()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]关闭-OnClose().");
            Visible = false;

            // 界面关闭动画
            switch (TweenType)
            {
                case EUITweenType.None:
                    OnClose();
                    return;
                case EUITweenType.Fade:
                    UIView.TweenFade(0, TweenDuration).OnComplete(OnClose);
                    return;
                case EUITweenType.Custom:
                    CustomCloseTween();
                    return;
                default:
                    OnClose();
                    return;
            }
        }

        /// <summary>
        /// 界面回收。
        /// </summary>
        internal void _OnRecycle()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]回收-OnRecycle().");

            SerialId = 0;
            OnRecycle();
        }

        /// <summary>
        /// 界面销毁.
        /// </summary>
        internal void _OnDispose()
        {
            FuLogger.LogInfo($"[ViewBase] UI界面[{SerialId}]{UIName}]被销毁-Dispose().");
            m_UIModule.PkgManager.SubRef(PackageName);

            ReleaseEventRegister();   // 释放事件注册器
            ReleaseUIEventRegister(); // 释放UI事件注册器
            ReleaseTimerRegister();   // 释放计时器注册器

            // 注销安全区变化监听
            SafeAreaHelper.OnSafeAreaChanged -= _OnSafeAreaChanged;

            OnDispose();
        }

        /// <summary>
        /// 本地化语言改变事件处理函数。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnLanguageChanged(object sender, GameEventArgs e)
        {
            if (Visible)
                OnOpen();
        }

        /// <summary>
        /// 自定义界面关闭动画
        /// </summary>
        private void CustomCloseTween()
        {
            var gTween = DoCustomCloseTween();
            if (gTween == null)
            {
                OnClose();
                return;
            }

            gTween.OnComplete(OnClose);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 初始化界面。
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 界面打开。
        /// </summary>
        protected virtual void OnOpen() { }

        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }

        /// <summary>
        /// 界面暂停。
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        protected virtual void OnBeCover() { }

        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        protected virtual void OnReveal() { }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// 界面回收。
        /// </summary>
        protected virtual void OnRecycle() { }

        /// <summary>
        /// 界面销毁.
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// 自定义界面打开动画(可重写实现属于自身自定义动画)
        /// </summary>
        protected virtual void DoCustomOpenTween() { }

        /// <summary>
        /// 自定义界面关闭动画(可重写实现属于自身自定义动画)
        /// </summary>
        protected virtual GTweener DoCustomCloseTween() => null;

        #endregion
    }
}