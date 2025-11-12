using FuFramework.Core.Runtime;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
{
    public partial class CompRedDot
    {
        /// <summary>
        /// 红点显示模式
        /// </summary>
        public enum RedDotDisplayMode
        {
            DotOnly,   // 只显示红点
            DotNumber, // 红点+数字
            Auto       // 根据数量自动显示，=1显示红点，>1显示数字
        }

        /// <summary>
        /// 红点Key
        /// </summary>
        private string m_RedDotKey;

        /// <summary>
        /// 红点显示模式
        /// </summary>
        private RedDotDisplayMode m_RedDotDisplayMode = RedDotDisplayMode.DotOnly;

        /// <summary>
        /// 初始化
        /// </summary>
        private void OnInit() { }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent() { }

        /// <summary>
        /// 销毁。
        /// 注意：UI事件，业务逻辑事件，计时器会自动从所属的View中移除，无需在这里手动移除。
        /// </summary>
        private void OnDispose() { }

        /// <summary>
        /// 设置所属界面
        /// </summary>
        /// <param name="view"></param>
        public void SetView(ViewBase view) => uiView = view;

        /// <summary>
        /// 设置红点Key
        /// </summary>
        /// <param name="redDotKey"></param>
        public void SetRedDotKey(string redDotKey)
        {
            if (string.IsNullOrEmpty(redDotKey)) return;
            m_RedDotKey = redDotKey;
        }

        /// <summary>
        /// 设置红点显示模式
        /// </summary>
        /// <param name="redDotDisplayMode"></param>
        public void SetRedDotDisplayMode(RedDotDisplayMode redDotDisplayMode)
        {
            m_RedDotDisplayMode = redDotDisplayMode;
        }
    }
}