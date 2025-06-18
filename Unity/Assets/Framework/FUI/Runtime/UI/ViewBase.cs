using System;
using FairyGUI;
using GameFrameX.Event.Runtime;
using GameFrameX.Localization.Runtime;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面基类。
    /// </summary>
    public abstract class ViewBase : MonoBehaviour
    {
        private bool m_IsInit = false; //界面是否已初始化
        private int m_OriginalLayer = 0; //界面原始层级
        private int m_DepthInUIGroup; //界面在界面组中的深度

        /// 界面所属的界面组。
        private UIGroup m_UIGroup;
        
        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 获取界面事件订阅器。
        /// </summary>
        public EventRegister EventRegister { get; private set; }

        /// <summary>
        /// UI 对象
        /// </summary>
        public GObject GObject { get; set; }
        
        /// <summary>
        /// 获取界面是否来自对象池。
        /// </summary>
        protected bool IsFromPool { get; set; }

        /// <summary>
        /// 获取界面是否已被销毁。
        /// </summary>
        protected bool IsDisposed { get; set; }

        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取界面完整名称。
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        public string UIAssetName { get; private set; }

        /// <summary>
        /// 获取或设置界面是否可见。
        /// </summary>
        public bool Visible
        {
            get => GObject.visible;
            private set
            {
                if (GObject == null) return;
                if (GObject.visible == value) return;
                GObject.visible = value;
                
                // 触发UI显示状态变化事件
                EventRegister.Fire(UIVisibleChangedEventArgs.EventId, UIVisibleChangedEventArgs.Create(this, value, null));
            }
        }

        /// <summary>
        /// 获取界面实例。
        /// </summary>
        public object Handle => gameObject;

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        public virtual UIGroup UIGroup
        {
            get => m_UIGroup;
            protected set => m_UIGroup = value;
        }

        /// <summary>
        /// 获取界面深度。
        /// </summary>
        public int DepthInUIGroup => m_DepthInUIGroup;

        /// <summary>
        /// 获取是否暂停被覆盖的界面。
        /// </summary>
        public bool PauseCoveredUI { get; private set; }

        /// <summary>
        /// 获取界面是否已唤醒。
        /// </summary>
        public bool IsAwake { get; private set; }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="uiGroup">界面所处的界面组。</param>
        /// <param name="onInitAction">初始化界面前的委托。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        public void Init(int serialId, string uiAssetName, UIGroup uiGroup, Action<ViewBase> onInitAction, bool pauseCoveredUI,
            bool isNewInstance, object userData, bool isFullScreen = false)
        {
            SerialId = serialId;
            UserData = userData;
            m_UIGroup = uiGroup;
            PauseCoveredUI = pauseCoveredUI;

            // 如果已经初始化过，则不再初始化
            if (m_IsInit) return;

            FullName = GetType().FullName;
            UIAssetName = uiAssetName;
            m_DepthInUIGroup = 0;
            m_OriginalLayer = gameObject.layer;

            if (!isNewInstance) return;

            EventRegister = EventRegister.Create(this);

            try
            {
                onInitAction?.Invoke(this);
                InitView();

                if (isFullScreen) MakeFullScreen();

                OnInit();

                // 注册本地化语言改变事件
                EventRegister.CheckSubscribe(LocalizationLanguageChangeEventArgs.EventId, OnLocalizationLanguageChanged);
            }
            catch (Exception exception)
            {
                Log.Error("UI界面'[{0}]{1}' 初始化发生异常：'{2}'.", SerialId, UIAssetName, exception);
            }

            m_IsInit = true;
        }

        /// <summary>
        /// 界面初始化前执行
        /// </summary>
        public virtual void OnAwake() => IsAwake = true;

        /// <summary>
        /// 界面初始化。
        /// </summary>
        protected virtual void InitView() { }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 界面回收。
        /// </summary>
        public virtual void OnRecycle()
        {
            SerialId = 0;
            m_DepthInUIGroup = 0;
            PauseCoveredUI = true;
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public virtual void OnOpen(object userData)
        {
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
        /// 界面关闭。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        public virtual void OnClose(bool isShutdown, object userData)
        {
            gameObject.SetLayerRecursively(m_OriginalLayer);
            Visible = false;
        }

        /// <summary>
        /// 界面暂停。
        /// </summary>
        public virtual void OnPause() => Visible = false;

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        public virtual void OnResume() => Visible = true;

        /// <summary>
        /// 界面遮挡。
        /// </summary>
        public virtual void OnBeCover() { }

        /// <summary>
        /// 界面遮挡恢复。
        /// </summary>
        public virtual void OnReveal() { }

        /// <summary>
        /// 界面深度改变。
        /// </summary>
        /// <param name="depthInUIGroup">界面在界面组中的深度。</param>
        public void OnDepthChanged(int depthInUIGroup) => m_DepthInUIGroup = depthInUIGroup;

        /// <summary>
        /// 销毁界面.
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed) return;
            EventRegister.UnSubscribeAll();
            IsDisposed = true;
        }

        /// <summary>
        /// 本地化语言改变事件处理函数。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLocalizationLanguageChanged(object sender, GameEventArgs e) => UpdateLocalization();

        /// <summary>
        /// 界面更新本地化。
        /// </summary>
        public virtual void UpdateLocalization() { }

        /// <summary>
        /// 设置UI对象
        /// </summary>
        /// <param name="gObject"></param>
        public void SetGObject(GObject gObject) => GObject = gObject;
        
        /// <summary>
        /// 设置界面为全屏
        /// </summary>
        protected void MakeFullScreen()=> GObject?.asCom?.MakeFullScreen();
    }
}