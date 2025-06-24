using System;
using FairyGUI;
using GameFrameX.Localization.Runtime;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面基类。
    /// </summary>
    public abstract partial class ViewBase : MonoBehaviour
    {
        private bool m_IsInit = false; //界面是否已初始化
        private int m_DepthInUIGroup; //界面在界面组中的深度

        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        public string UIName { get; private set; }

        /// <summary>
        /// 获取界面完整名称。
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// UI 对象
        /// </summary>
        public GObject GObject { get; set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 获取界面是否已被销毁。
        /// </summary>
        protected bool IsDisposed { get; set; }

        /// <summary>
        /// 获取界面事件订阅器。
        /// </summary>
        public EventRegister EventRegister { get; private set; }

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
        public virtual UIGroup UIGroup { get; protected set; } 

        /// <summary>
        /// 获取界面深度。
        /// </summary>
        public int DepthInUIGroup => m_DepthInUIGroup;

        /// <summary>
        /// 显示时是否暂停被覆盖的界面。
        /// </summary>
        public virtual bool PauseCoveredUI { get; protected set; } = false;

        /// <summary>
        /// 是否是全屏界面。
        /// </summary>
        public virtual bool IsFullScreen { get; protected set; } = true;
        
        /// <summary>
        /// 获取界面是否已唤醒。
        /// </summary>
        public bool IsAwake { get; private set; }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiName">界面资源名称。</param>
        /// <param name="uiInstance">界面实例。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void Init(int serialId, string uiName, GObject uiInstance, bool isNewInstance, object userData)
        {
            SerialId = serialId;
            UserData = userData;

            // 如果已经初始化过，则不再初始化
            if (m_IsInit) return;
            m_IsInit = true;

            UIName = uiName;
            FullName = GetType().FullName;
            m_DepthInUIGroup = 0;

            if (!isNewInstance) return;

            EventRegister = EventRegister.Create(this);

            try
            {
                GObject = uiInstance;
                InitView();

                if (IsFullScreen) MakeFullScreen();

                OnInit();

                // 注册本地化语言改变事件
                EventRegister.CheckSubscribe(LocalizationLanguageChangeEventArgs.EventId, OnLocalizationLanguageChanged);
            }
            catch (Exception exception)
            {
                Log.Error($"UI界面[{SerialId}]{UIName}] 初始化发生异常：'{exception}'.");
            }
        }

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