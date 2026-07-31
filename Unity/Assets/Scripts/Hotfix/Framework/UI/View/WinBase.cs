using System;
using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;
using Hotfix.Framework.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Config;
using UIConfigRow = Hotfix.Game.Config.Tables.UIConfig;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 界面基类。
    /// 目标: 所有界面的基类，记录界面的FUI显示对象和基本信息。
    /// </summary>
    public abstract partial class WinBase
    {
        /// <summary>
        /// 界面是否已初始化。
        /// </summary>
        private bool m_IsInit;

        /// <summary>
        /// UI管理模块
        /// </summary>
        private UIModule m_UIModule;

        /// <summary>
        /// 界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// UI显示对象
        /// </summary>
        public GComponent WinUI { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// UI 配置数据（来自 UIConfig 配置表）。为 null 时使用默认值。
        /// </summary>
        public UIConfigRow UIConfig { get; private set; }

        /// <summary>
        /// 获取界面所属的层级（仅框架内部使用，外部请读 UIConfig.Layer）。
        /// </summary>
        private EUILayer Layer => UIConfig?.Layer ?? EUILayer.Normal;

        /// <summary>
        /// 获取界面打开/关闭时的动画类型（仅框架内部使用）。
        /// </summary>
        private EUITweenType TweenType => UIConfig?.TweenType ?? EUITweenType.Fade;

        /// <summary>
        /// 获取界面打开/关闭时的动画时长（仅框架内部使用）。
        /// </summary>
        private float TweenDuration => UIConfig?.TweenDuration ?? 0.3f;

        /// <summary>
        /// 是否适配刘海/打孔区域（仅框架内部使用）。
        /// </summary>
        private bool AdjustNotch => UIConfig?.AdjustNotch ?? true;

        /// <summary>
        /// 显示时是否暂停被覆盖的界面。UIGroup 通过 win.PauseCoveredUI 外部访问，保持 public。
        /// </summary>
        public bool PauseCoveredUI => UIConfig?.PauseCoveredUI ?? false;

        /// <summary>
        /// 界面名称。
        /// </summary>
        public virtual string WinName => "";

        /// <summary>
        /// 界面资源包名称。
        /// </summary>
        public virtual string PackageName => "";

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        public UIGroup UIGroup => m_UIModule?.GetGroup(Layer);

        /// <summary>
        /// 获取或设置界面是否可见。
        /// </summary>
        public bool Visible
        {
            get => WinUI.visible;
            private set
            {
                if (WinUI         == null) return;
                if (WinUI.visible == value) return;
                WinUI.visible = value;

                // 触发UI显示状态变化事件
                Broadcast(ChangeUIVisibleEventArgs.EventId, ChangeUIVisibleEventArgs.Create(this, value, null));
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

            m_UIModule = ModuleManager.GetModule<UIModule>();
            m_IsInit   = true;

            // 加载 UI 配置表（通过 Get(string) 方法查表；StrKeyDataDict 为 protected，外部不可直接访问）
            UIConfig = ConfigModule.Instance?.GetConfig<TbUIConfig>()?.Get(WinName);

            if (!isNewInstance) return;

            // 创建UI事件注册器，事件注册器，计时器注册器
            UIEventRegister = FuiEventRegister.Create();
            EventRegister   = Event.EventRegister.Create();
            TimerRegister   = Timer.TimerRegister.Create();

            try
            {
                WinUI               = uiView;
                WinUI.fairyBatching = true;

                // 初始化时，设置一次UI对象全屏和安全区适配
                _OnSafeAreaChanged();

                // 注册本地化语言改变事件
                Subscribe("Event.Localization.LanguageChange", _OnLanguageChanged);

                // 初始化
                _OnInit();

                // 注册安全区变化监听
                SafeAreaHelper.OnSafeAreaChanged += _OnSafeAreaChanged;
            }
            catch (Exception exception)
            {
                FuLogger.LogError($"[WinBase] UI界面[{SerialId}]{WinName}] 初始化发生异常：'{exception}'.");
            }
        }

        /// <summary>
        /// 获取界面子对象。
        /// </summary>
        /// <param name="childName"></param>
        /// <returns></returns>
        protected GObject GetChild(string childName) => WinUI.GetChild(childName);

        /// <summary>
        /// 添加界面子对象。
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected void AddChild(GObject child) => WinUI.AddChild(child);

        /// <summary>
        /// 关闭自身。
        /// </summary>
        protected void CloseSelf()
        {
            if (m_UIModule is null) throw new InvalidOperationException("[WinBase] 关闭自身失败，UI管理模块为空。");
            m_UIModule.Close(this);
        }

        /// <summary>
        /// 安全区变化回调（方向切换等）。
        /// 全屏 UI（AdjustNotch = false）需要重新计算负偏移覆盖刘海；普通 UI 跟随 GRoot 适配。
        /// </summary>
        private void _OnSafeAreaChanged()
        {
            if (WinUI == null) return;
            if (AdjustNotch)
            {
                // 普通 UI 跟随 GRoot
                WinUI.SetSize(GRoot.inst.width, GRoot.inst.height);
                return;
            }

            // 全屏 UI：整屏尺寸 + 负偏移，覆盖 GRoot 外的刘海区域
            WinUI.SetSize(Screen.width / UIContentScaler.scaleFactor, Screen.height / UIContentScaler.scaleFactor);
            WinUI.SetXY(-SafeAreaHelper.OffsetX, -SafeAreaHelper.OffsetY);
        }
    }
}