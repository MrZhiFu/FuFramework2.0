using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;

namespace GameFrameX.UI.Runtime
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
            FuiPackageMgr.Instance.AddRef(PackageName);
            OnInit();
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        internal void _OnOpen(object userData)
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]打开-OnOpen().");
            Visible  = true;
            UserData = userData;
            OnOpen(userData);
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
        internal  void _OnPause()
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
        internal  void _OnBeCover()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被遮挡-OnBeCover().");
            if (IsFullScreen) Visible = false;
            OnBeCover();
        }

        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        internal  void _OnReveal()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被遮挡恢复-OnReveal().");
            Visible = true;
            OnReveal();
        }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        internal  void _OnClose()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]关闭-OnClose().");
            Visible = false;
            OnClose();
        }

        /// <summary>
        /// 界面回收。
        /// </summary>
        internal void _OnRecycle()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]回收-OnRecycle().");

            SerialId         = 0;
            DepthInUIGroup = 0;
            PauseCoveredUI   = true;
            EventRegister.UnSubscribeAll();
            OnRecycle();
        }

        /// <summary>
        /// 界面销毁.
        /// </summary>
        internal void _OnDispose()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被销毁-Dispose().");
            FuiPackageMgr.Instance.SubRef(PackageName);
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

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 初始化界面。
        /// </summary>
        protected virtual void OnInit() { }
        
        /// <summary>
        /// 界面打开。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public virtual void OnOpen(object userData) { }
        
        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds) { }
        
        /// <summary>
        /// 界面暂停。
        /// </summary>
        public virtual void OnPause() { }
        
        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        public virtual void OnResume() { }
        
        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        public virtual void OnBeCover() { }
        
        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        public virtual void OnReveal() { }
        
        /// <summary>
        /// 界面关闭。
        /// </summary>
        public virtual void OnClose() { }
        
        /// <summary>
        /// 界面回收。
        /// </summary>
        public virtual void OnRecycle() { }
        
        /// <summary>
        /// 界面销毁.
        /// </summary>
        public virtual void OnDispose() { }
        
        /// <summary>
        /// 界面更新本地化。
        /// </summary>
        public virtual void UpdateLocalization() { }

        #endregion
    }
}