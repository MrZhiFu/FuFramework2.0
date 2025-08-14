using System;
using FairyGUI;
using FuFramework.Core.Runtime;
using GameFrameX.Localization.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
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
        public virtual bool PauseCoveredUI => false;

        /// <summary>
        /// 界面名称。
        /// </summary>
        public virtual string UIName => "";

        /// <summary>
        /// 界面资源包名称。
        /// </summary>
        public virtual string PackageName => "";

        /// <summary>
        /// 是否是全屏界面。
        /// </summary>
        protected virtual bool IsFullScreen => true;

        /// <summary>
        /// 界面所属的层级。
        /// </summary>
        protected virtual UILayer Layer => UILayer.Normal;

        /// <summary>
        /// 界面打开/关闭时的动画类型。
        /// </summary>
        protected virtual UITweenType TweenType => UITweenType.Fade;

        /// <summary>
        /// 界面打开/关闭时的动画时长。
        /// </summary>
        protected virtual float TweenDuration => 0.3f;

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
        /// <param name="uiView">界面实例。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void Init(int serialId, GComponent uiView, bool isNewInstance, object userData = null)
        {
            SerialId = serialId;
            UserData = userData;

            // 如果已经初始化过，则不再初始化
            if (m_IsInit) return;

            m_IsInit       = true;
            DepthInUIGroup = 0;

            if (!isNewInstance) return;

            // 创建UI事件注册器，事件注册器，定时器注册器
            UIEventRegister = FuiEventRegister.Create();
            EventRegister   = Event.Runtime.EventRegister.Create();
            TimerRegister   = GameFrameX.Timer.Runtime.TimerRegister.Create();

            try
            {
                UIView               = uiView;
                UIView.fairyBatching = true;

                // 设置全屏
                if (IsFullScreen) UIView?.MakeFullScreen();

                // 注册本地化语言改变事件
                Subscribe(LocalizationLanguageChangeEventArgs.EventId, _OnLocalizationLanguageChanged);

                // 初始化
                _OnInit();

                // 递归初始化子组件，传递自身实例给子组件。
                InitChildrenView(UIView);
            }
            catch (Exception exception)
            {
                Log.Error($"UI界面[{SerialId}]{UIName}] 初始化发生异常：'{exception}'.");
            }
        }

        /// <summary>
        /// 递归初始化子组件，传递自身实例给子组件。
        /// 如果子组件实现了ICustomComp接口，则递归初始化子组件。
        /// </summary>
        private void InitChildrenView(GComponent curComp)
        {
            var children = curComp.GetChildren();
            foreach (var child in children)
            {
                var comp = child switch
                {
                    CustomLoader loader  => loader.component,
                    GComponent component => component,
                    _                    => null
                };

                if (comp is not ICustomComp customComp) continue;
                customComp.Init(this);
                InitChildrenView(comp);
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