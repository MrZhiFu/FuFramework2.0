using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.Event.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面基类: 生命周期相关
    /// </summary>
    public abstract partial class ViewBase
    {
        #region Internal Methods

        /// <summary>
        /// 初始化界面。
        /// </summary>
        private void _OnInit()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]初始化-OnInit().");
            FuiPackageManager.Instance.AddRef(PackageName);
            OnInit();
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        internal void _OnOpen()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]打开-OnOpen().");
            Visible = true;

            // 界面打开动画
            switch (TweenType)
            {
                case UITweenType.None: OnOpen(); return;
                case UITweenType.Fade: UIView.TweenFade(1, TweenDuration).OnComplete(OnOpen); return;
                case UITweenType.Custom: OnCustomTweenOpen(); return;
                default: OnOpen(); break;
            }
        }

        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal void _OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            OnUpdate(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 界面暂停。
        /// </summary>
        internal void _OnPause()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]暂停-OnPause().");
            Visible = false;
            OnPause();
        }

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        internal void _OnResume()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]恢复-OnResume().");
            Visible = true;
            OnResume();
        }

        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        internal void _OnBeCover()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被遮挡-OnBeCover().");
            if (IsFullScreen) Visible = false;
            OnBeCover();
        }

        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        internal void _OnReveal()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被遮挡恢复-OnReveal().");
            Visible = true;
            OnReveal();
        }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        internal void _OnClose()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]关闭-OnClose().");
            Visible = false;

            // 界面关闭动画
            switch (TweenType)
            {
                case UITweenType.None: OnClose(); return;
                case UITweenType.Fade: UIView.TweenFade(0, TweenDuration).OnComplete(OnClose); return;
                case UITweenType.Custom: OnCustomTweenClose(); return;
                default: OnClose(); return;
            }
        }
        
        /// <summary>
        /// 界面回收。
        /// </summary>
        internal void _OnRecycle()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]回收-OnRecycle().");

            SerialId = 0;
            DepthInUIGroup = 0;
            OnRecycle();
        }

        /// <summary>
        /// 界面销毁.
        /// </summary>
        internal void _OnDispose()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被销毁-Dispose().");
            FuiPackageManager.Instance.SubRef(PackageName);

            ReleaseEventRegister(); // 释放事件注册器
            ReleaseUIEventRegister(); // 释放UI事件注册器
            ReleaseTimerRegister(); // 释放计时器注册器

            OnDispose();
        }

        /// <summary>
        /// 界面深度改变。
        /// </summary>
        /// <param name="depthInUIGroup">界面在界面组中的深度。</param>
        internal void _OnDepthChanged(int depthInUIGroup)
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]深度被改变为'{depthInUIGroup}'-OnDepthChanged().");
            DepthInUIGroup = depthInUIGroup;
        }

        /// <summary>
        /// 本地化语言改变事件处理函数。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnLocalizationLanguageChanged(object sender, GameEventArgs e)
        {
            UpdateLocalization();
        }
        
        /// <summary>
        /// 自定义界面打开动画
        /// </summary>
        private void OnCustomTweenOpen()
        {
            var gTween = DoCustomTweenOpen();
            if (gTween == null)
            {
                OnOpen();
                return;
            }
            gTween.OnComplete(OnOpen);
        }

        /// <summary>
        /// 自定义界面关闭动画
        /// </summary>
        private void OnCustomTweenClose()
        {
            var gTween = DoCustomTweenClose();
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
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected virtual void OnUpdate(float elapseSeconds, float realElapseSeconds) { }

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
        /// 界面更新本地化。
        /// </summary>
        public virtual void UpdateLocalization() { }

        /// <summary>
        /// 自定义界面打开动画(可重写实现属于自身自定义动画)
        /// </summary>
        protected virtual GTweener DoCustomTweenOpen() => null;

        /// <summary>
        /// 自定义界面关闭动画(可重写实现属于自身自定义动画)
        /// </summary>
        protected virtual GTweener DoCustomTweenClose() => null;
        
        #endregion
    }
}