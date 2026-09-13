using FairyGUI;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Event;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
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
    public abstract partial class WinBase
    {
        #region Internal Methods

        /// <summary>
        /// 初始化界面。
        /// </summary>
        private void _OnInit()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]初始化-OnInit().");
            m_UIModule.PkgManager?.AddPkgRef(PackageName);
            m_PkgRefAdded = true; // 置位后 _OnDispose 才允许 SubPkgRef，保证加/减引用严格对称
            OnInit();
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        internal void _OnOpen()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]打开-OnOpen().");
            m_Cancellation.Recreate(); // 新生命周期 = 新 Token（首次与对象池复用都会走）
            Visible     = true;
            WinUI.alpha = 0;

            // 先刷新界面
            OnOpen();

            // 再执行打开动画
            switch (TweenType)
            {
                case EUITweenType.None:
                    WinUI.alpha = 1;
                    return;
                case EUITweenType.Fade:
                    WinUI.TweenFade(1, TweenDuration);
                    return;
                case EUITweenType.Custom:
                    WinUI.alpha = 1;
                    DoCustomOpenTween();
                    return;
                default:
                    WinUI.alpha = 1;
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
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]暂停-OnPause().");
            Visible = false;
            OnPause();
        }

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        internal void _OnResume()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]恢复-OnResume().");
            Visible = true;
            OnResume();
        }

        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        internal void _OnBeCover()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]被遮挡-OnBeCover().");
            if (!AdjustNotch) Visible = false;
            OnBeCover();
        }

        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        internal void _OnReveal()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]被遮挡恢复-OnReveal().");
            Visible = true;
            OnReveal();
        }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        internal void _OnClose()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]关闭-OnClose().");
            m_Cancellation.Cancel(); // 关闭即取消本生命周期在途异步任务
            Visible = false;

            // 关闭动画需要 WinUI；若为空/已销毁（teardown 阶段关闭仍打开的窗口、或半成品实例）
            // 则跳过动画直接走 OnClose()。TweenType 的默认值为 Fade，原实现无守卫会直接 NRE
            // （与 _OnRecycle 的 GTween.Kill 守卫同口径）。
            if (WinUI == null)
            {
                OnClose();
                return;
            }

            // 界面关闭动画
            switch (TweenType)
            {
                case EUITweenType.None:
                    OnClose();
                    return;
                case EUITweenType.Fade:
                    WinUI.TweenFade(0, TweenDuration).OnComplete(OnClose);
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
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]回收-OnRecycle().");

            // 终止挂在 WinUI 上的补间动画（打开/关闭渐变）：
            // _OnClose 若走 Fade/Custom 分支，会给 WinUI 挂一个 OnComplete(OnClose) 的 tween；窗口回收
            //（对象池复用）或销毁后该 tween 仍可能到期，届时会回调用户 OnClose()、甚至写入已销毁的 GObject。
            // 用 complete:true kill：tween 立即走到终点并回调一次 OnComplete——既清掉过期 tween，
            // 又保证用户的 OnClose() 钩子恰好被触发一次（自然完成时 _killed 已置位，不会再重复回调）。
            if (WinUI != null) GTween.Kill(WinUI, true);

            SerialId = 0;
            OnRecycle();
        }

        /// <summary>
        /// 界面销毁.
        /// </summary>
        internal void _OnDispose()
        {
            FuLogger.LogInfo($"[WinBase] UI界面[{SerialId}]{WinName}]被销毁-Dispose().");

            // 终止 WinUI 上残留的补间：不清 complete——WinObject.OnDispose 已先 WinUI.Dispose()，
            // 此时驱动 tween 走到终点会向已销毁的 GObject 写属性（NRE）。这里只解除注册、丢弃补间即可。
            // 用户的 OnClose() 钩子由 _OnRecycle 的 complete kill 保证触发（销毁前必先经过回收）。
            if (WinUI != null) GTween.Kill(WinUI);

            m_Cancellation.Dispose(); // 真销毁，永久释放取消源

            // 半成品实例（Init 未走完，UI模块未绑定/未加过包引用）销毁时不得递减引用计数：
            // 递减会错误扣减同包其它界面的引用计数，导致纹理/音频被提前卸载。判空 + 标记双重守卫。
            if (m_PkgRefAdded)
            {
                m_UIModule?.PkgManager?.SubPkgRef(PackageName);
                m_PkgRefAdded = false;
            }

            // 半成品实例的事件/UI事件/计时器注册器均为 null，Release 前必须判空（否则抛 NRE）
            ReleaseAllRegisters();

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