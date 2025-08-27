// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.Bag
{
    public partial class CompBagItem
    {
        /// <summary>
        /// 初始化
        /// </summary>
        private void OnInit()
        { 
            // DoSomething
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        public void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, XxxEventArgs.Create(xxx));
        }

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