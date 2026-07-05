using System;
using FairyGUI;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
{
    public partial class WinDialogGuide : ViewBase
    {
        /// <summary>
        /// 提交按钮点击回调
        /// </summary>
        private Action _OnConfirm;

        #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override UILayer Layer         => UILayer.Guide;   // 界面所属的层级。
         protected override UITweenType TweenType => UITweenType.Fade; // 界面打开/关闭时的动画效果。
         protected override bool AdjustNotch      => false;            // 是否适配刘海/打孔区域（全屏覆盖）。
         public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
        //@formatter:on

        #endregion

        /// <summary>
        /// 初始化
        /// </summary>  
        protected override void OnInit()
        {
            InitUIComp();
            InitUIEvent();
            InitEvent();
            InitRedDot();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, OnXxxEventHandler);
        }

        /// <summary>
        /// 注册界面相关红点
        /// </summary>
        private void InitRedDot()
        {
            // Example: RedDotRegister.RegisterRedDot(this, RedDotKeys.BagItem, btnLogin, displayMode: CompRedDot.DisplayMode.Auto);
        }

        /// <summary>
        /// 界面打开
        /// </summary>
        protected override void OnOpen()
        {
            Refresh();
        }

        /// <summary>
        /// 界面关闭
        /// </summary>
        protected override void OnClose() { }

        /// <summary>
        /// 界面销毁
        /// </summary>
        protected override void OnDispose()
        {
            _OnConfirm = null;
        }

        /// <summary>
        /// 显示对话
        /// </summary>
        /// <param name="content"></param>
        /// <param name="onConfirm"></param>
        public void ShowDialog(string content, Action onConfirm)
        {
            txtContent.text = content;
            _OnConfirm = onConfirm;
        }

        /// <summary>
        /// 刷新界面
        /// </summary>
        private void Refresh()
        {
            // TODO：刷新逻辑
        }

        #region 交互事件与ListItem渲染回调处理

        private void OnBtnNextClick(EventContext ctx)
        {
            _OnConfirm?.Invoke();
        }

        #endregion
    }
}