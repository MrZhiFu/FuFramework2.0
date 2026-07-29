using Hotfix.Framework.UI;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    /// <summary>
    /// 全局遮罩界面
    /// </summary>
    public partial class WinGlobalMask : WinBase
    {
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
