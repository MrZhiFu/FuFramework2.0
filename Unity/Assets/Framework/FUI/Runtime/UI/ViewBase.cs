using System;
using FairyGUI;
using GameFrameX.Localization.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面基类。
    /// </summary>
    public abstract partial class ViewBase
    {
        /// <summary>
        /// 界面是否已初始化。
        /// </summary>
        private bool m_IsInit = false;

        /// <summary>
        /// 界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 界面名称。
        /// </summary>
        public string UIName { get; private set; }

        /// <summary>
        /// 界面资源包名称。
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// UI显示对象
        /// </summary>
        public GComponent UIView { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 获取界面深度。
        /// </summary>
        public int DepthInUIGroup { get; private set; }

        /// <summary>
        /// 显示时是否暂停被覆盖的界面。
        /// </summary>
        public virtual bool PauseCoveredUI { get; protected set; } = false;

        /// <summary>
        /// 是否是全屏界面。
        /// </summary>
        protected virtual bool IsFullScreen { get; set; } = true;

        /// <summary>
        /// 获取界面所属的层级。
        /// </summary>
        protected UILayer Layer = UILayer.Normal;

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        public UIGroup UIGroup => UIManager.Instance.GetUIGroup(Layer);

        /// <summary>
        /// 获取或设置界面是否可见。
        /// </summary>
        public bool Visible
        {
            get => UIView.visible;
            private set
            {
                if (UIView         == null) return;
                if (UIView.visible == value) return;
                UIView.visible = value;

                // 触发UI显示状态变化事件
                Fire(UIVisibleChangedEventArgs.EventId, UIVisibleChangedEventArgs.Create(this, value, null));
            }
        }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="packageName">界面资源包名称。</param>
        /// <param name="uiName">界面资源名称。</param>
        /// <param name="uiView">界面实例。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void Init(int serialId, string packageName, string uiName, GComponent uiView, bool isNewInstance, object userData)
        {
            SerialId = serialId;
            UserData = userData;

            // 如果已经初始化过，则不再初始化
            if (m_IsInit) return;

            m_IsInit       = true;
            PackageName    = packageName;
            UIName         = uiName;
            DepthInUIGroup = 0;

            if (!isNewInstance) return;

            // 创建UI事件注册器，事件注册器，定时器注册器
            UIEventRegister = FuiEventRegister.Create();
            EventRegister   = Event.Runtime.EventRegister.Create(this);
            TimerRegister   = Timer.Runtime.TimerRegister.Create();

            // 初始化自定义组件
            var customGComps = uiView.GetChildren();
            foreach (var customGComp in customGComps)
            {
                if (customGComp is not IViewCompBase viewComp) continue;
                viewComp.Init(this);
            }
            
            try
            {
                UIView = uiView;
                if (IsFullScreen) UIView?.MakeFullScreen();

                // 注册本地化语言改变事件
                Subscribe(LocalizationLanguageChangeEventArgs.EventId, _OnLocalizationLanguageChanged);

                // 初始化
                _OnInit();
            }
            catch (Exception exception)
            {
                Log.Error($"UI界面[{SerialId}]{UIName}] 初始化发生异常：'{exception}'.");
            }
        }

        /// <summary>
        /// 设置UI对象
        /// </summary>
        /// <param name="view"></param>
        protected void SetUIView(GComponent view) => UIView = view;

        /// <summary>
        /// 获取界面子对象。
        /// </summary>
        /// <param name="childName"></param>
        /// <returns></returns>
        protected GObject GetChild(string childName) => UIView.GetChild(childName);

        /// <summary>
        /// 获取界面子对象。
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected void AddChild(GObject child) => UIView.AddChild(child);

        /// <summary>
        /// 关闭自身。
        /// </summary>
        protected void CloseSelf() => UIManager.Instance.CloseUI(this);
    }
}