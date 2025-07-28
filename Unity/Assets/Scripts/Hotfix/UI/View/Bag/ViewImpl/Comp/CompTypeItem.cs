// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Bag
{
    public partial class CompTypeItem
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
        /// <param name="typeName"></param>
        public void SetData(string typeName)
        {
            title = typeName;
        }
    }
}