using Hotfix.Config;
// ReSharper disable once CheckNamespace 禁用命名空间检查

namespace Hotfix.UI
{
    public partial class CompBagItem
    {
        /// <summary>
        /// 初始化
        /// </summary>
        private void OnInit()
        {
            InitEvent();
            InitRedDot();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, XxxEventArgs.Create(xxx));
        }

        /// <summary>
        /// 注册界面相关红点
        /// </summary>
        private void InitRedDot()
        {
            // Example: RedDotRegister.RegisterRedDot(this.uiView, ERedDotKey.Bag_Item, btnLogin);
        }
        
        /// <summary>
        /// 销毁。
        /// 注意：UI事件，业务逻辑事件，计时器会自动从所属的View中移除，无需在这里手动移除。
        /// </summary>
        private void OnDispose() { }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        public void SetData(int itemId, long count)
        {
            compGoodItem.SetIcon(itemId);
            compGoodItem.SetCount(count);
        }
    }
}