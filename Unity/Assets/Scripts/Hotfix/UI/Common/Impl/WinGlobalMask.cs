using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
{
    /// <summary>
    /// 全局遮罩界面
    /// </summary>
    public partial class WinGlobalMask : ViewBase
    {
        #region 界面基本属性(无特殊需求，可不做修改)
 
         //@formatter:off
         protected override EUILayer Layer         => EUILayer.Guide;   // 界面所属的层级。
         protected override EUITweenType TweenType => EUITweenType.None; // 界面打开/关闭时的动画效果。
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
        }

        /// <summary>
        /// 界面打开
        /// </summary>
        protected override void OnOpen() { }

        /// <summary>
        /// 界面关闭
        /// </summary>
        protected override void OnClose() { }

        /// <summary>
        /// 界面销毁
        /// </summary>
        protected override void OnDispose() { }
    }
}