using System;
using System.Threading;
using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;
using Hotfix.Framework.Config;
using Hotfix.Game.Config;
using UIConfigRow = Hotfix.Game.Config.UIConfig;

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
        /// UI包引用是否已添加（_OnInit 中 AddPkgRef 后置位）。
        /// 半成品实例（未走到 _OnInit 或其中途失败）为 false，销毁时不得 SubPkgRef：
        /// 它从未加过引用，递减会错误扣减同包其它界面的引用计数，导致纹理/音频被提前卸载。
        /// </summary>
        private bool m_PkgRefAdded;

        /// <summary>
        /// UI管理模块
        /// </summary>
        private UIModule m_UIModule;

        /// <summary>
        /// 界面生命周期取消源：每次打开（_OnOpen）重建 Token，关闭（_OnClose）取消，销毁（_OnDispose）释放。
        /// 窗口内发起的异步任务（网络请求/资源加载）应传 Token，随界面关闭自动取消。
        /// </summary>
        private readonly LifecycleCancellationSource m_Cancellation = new();

        /// <summary>
        /// 界面生命周期取消令牌：窗口内 await 统一传参，界面关闭（_OnClose）时触发取消。
        /// </summary>
        protected CancellationToken Token => m_Cancellation.Token;

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

                // 触发UI显示状态变化事件。
                // 首参是 sender（事件源），不是事件 ID：原先误传 ChangeUIVisibleEventArgs.EventId，
                // 订阅者拿到的事件源成了字符串。事件 ID 由 EventArgs 自身的 Id 承载（见 ChangeUIVisibleEventArgs.Id），
                // 查找订阅者不受影响，故此处按框架惯例传 this。
                Broadcast(this, ChangeUIVisibleEventArgs.Create(this, value, null));
            }
        }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="winUI">界面实例。</param>
        /// <param name="isNewWin">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void Init(int serialId, GComponent winUI, bool isNewWin, object userData = null)
        {
            SerialId = serialId;
            UserData = userData;

            // 已经初始化过且界面对象可用：不再初始化，只保留本次 SerialId/UserData（对象池复用路径）。
            // winUI 为空说明上次初始化未走完（半成品实例，WinUI 未赋值），此时不能早退：
            // 早退会让后续 uiGroup.AddChild(win.WinUI) 拿 null 直接抛 NRE，故按新实例重新完整初始化。
            if (m_IsInit && winUI != null) return;

            // 半成品重新初始化：先释放上次可能已创建的注册器，避免重复创建导致引用池泄漏
            if (m_IsInit) ReleaseAllRegisters();

            m_UIModule = ModuleManager.GetModule<UIModule>();
            m_IsInit   = true;

            // 加载 UI 配置表（通过 Get(string) 方法查表；StrKeyDataDict 为 protected，外部不可直接访问）
            UIConfig = ConfigModule.Instance?.GetConfig<TbUIConfig>()?.Get(WinName);

            if (!isNewWin) return;

            // 创建UI事件注册器，事件注册器，计时器注册器
            UIEventRegister = FuiEventRegister.Create();
            EventRegister   = Event.EventRegister.Create();
            TimerRegister   = Timer.TimerRegister.Create();

            try
            {
                WinUI               = winUI;
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
        /// 释放事件/UI事件/计时器三个注册器（半成品实例下它们可能为 null，故统一判空后再释放）。
        /// </summary>
        private void ReleaseAllRegisters()
        {
            if (EventRegister != null) ReleaseEventRegister();
            if (UIEventRegister != null) ReleaseUIEventRegister();
            if (TimerRegister != null) ReleaseTimerRegister();
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

            // scaleFactor 可能尚未初始化（0），除零会得到 NaN 尺寸，防御兜底为 1
            var scaleFactor = UIContentScaler.scaleFactor;
            if (scaleFactor <= 0) scaleFactor = 1;

            if (AdjustNotch)
            {
                // 普通 UI 跟随 GRoot
                WinUI.SetSize(GRoot.inst.width, GRoot.inst.height);
                return;
            }

            // 全屏 UI：整屏尺寸 + 负偏移，覆盖 GRoot 外的刘海区域。
            // WinUI 是 GRoot 子节点，坐标为设计坐标（渲染 × scaleFactor），
            // SafeAreaHelper.OffsetX 是屏幕像素（用于 GRoot 自身定位），此处须除以 scaleFactor 转成设计坐标。
            WinUI.SetSize(Screen.width / scaleFactor, Screen.height / scaleFactor);
            WinUI.SetXY(-SafeAreaHelper.OffsetX / scaleFactor, -SafeAreaHelper.OffsetY / scaleFactor);
        }
    }
}