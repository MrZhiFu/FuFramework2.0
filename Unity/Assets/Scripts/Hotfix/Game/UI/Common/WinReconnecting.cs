using FairyGUI;
using Hotfix.Framework.UI;
using Hotfix.Game.Network;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class WinReconnecting : WinBase
    {
        // 界面基本属性(Layer、TweenType、AdjustNotch、PauseCoveredUI等)已迁移至 UIConfig 配置表，无需代码重写。

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            InitUIComp();
            InitUIEvent();
            InitEvent();
        }

        /// <summary>
        /// 界面可交互组件事件初始化
        /// </summary>
        private void InitUIEvent()
        {
            AddUIListener(btnCancel.onClick, _ => ReconnectManager.Instance.CancelReconnect());
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);
        }

        /// <summary>
        /// 界面打开：开始转圈动画
        /// </summary>
        protected override void OnOpen()
        {
            rotationAnim?.Play();
            Refresh();
        }

        /// <summary>
        /// 界面关闭：停止转圈动画
        /// </summary>
        protected override void OnClose()
        {
            rotationAnim?.Stop();
        }

        /// <summary>
        /// 界面销毁
        /// </summary>
        protected override void OnDispose() { }

        /// <summary>
        /// 刷新界面
        /// </summary>
        private void Refresh()
        {
        	// TODO：刷新逻辑
        }

    }
}