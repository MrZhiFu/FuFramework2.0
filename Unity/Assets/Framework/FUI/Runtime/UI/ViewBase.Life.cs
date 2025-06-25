using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面基类: 生命周期相关
    /// </summary>
    public abstract partial class ViewBase
    {
        /// <summary>
        /// 界面初始化前执行
        /// </summary>
        public virtual void OnAwake()
        {
            Log.Info($"UI界面[{gameObject.name}]被唤醒-OnAwake().");
            IsAwake = true;
        }

        /// <summary>
        /// 界面初始化。
        /// </summary>
        protected virtual void InitView()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]界面初始化-InitView().");
        }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        public virtual void OnInit()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]初始化-OnInit().");
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public virtual void OnOpen(object userData)
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]打开-OnOpen().");
            Visible = true;
            UserData = userData;
        }

        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 界面暂停。
        /// </summary>
        public virtual void OnPause()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]暂停-OnPause().");
            Visible = false;
        }

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        public virtual void OnResume()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]恢复-OnResume().");
            Visible = true;
        }

        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        public virtual void OnBeCover()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被遮挡-OnBeCover().");
            if (IsFullScreen) Visible = false;
        }

        /// <summary>
        /// 界面被遮挡恢复。
        /// </summary>
        public virtual void OnReveal()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被遮挡恢复-OnReveal().");
            Visible = true;
        }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        public virtual void OnClose(bool isShutdown)
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]关闭-OnClose().");
            Visible = false;
        }

        /// <summary>
        /// 界面回收。
        /// </summary>
        public virtual void OnRecycle()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]回收-OnRecycle().");
            
            SerialId         = 0;
            m_DepthInUIGroup = 0;
            PauseCoveredUI   = true;
            EventRegister.UnSubscribeAll();
        }

        /// <summary>
        /// 界面销毁.
        /// </summary>
        public virtual void OnDispose()
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]被销毁-Dispose().");
            if (IsDisposed) return;
            IsDisposed = true;
        }

        /// <summary>
        /// 界面深度改变。
        /// </summary>
        /// <param name="depthInUIGroup">界面在界面组中的深度。</param>
        public void OnDepthChanged(int depthInUIGroup)
        {
            Log.Info($"UI界面[{SerialId}]{UIName}]深度被改变为'{depthInUIGroup}'-OnDepthChanged().");
            m_DepthInUIGroup = depthInUIGroup;
        }

        /// <summary>
        /// 本地化语言改变事件处理函数。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLocalizationLanguageChanged(object sender, GameEventArgs e)
        {
            UpdateLocalization();
        }

        /// <summary>
        /// 界面更新本地化。
        /// </summary>
        public virtual void UpdateLocalization() { }
    }
}