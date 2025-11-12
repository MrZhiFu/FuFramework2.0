// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
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
        /// 销毁。
        /// 注意：UI事件，业务逻辑事件，计时器会自动从所属的View中移除，无需在这里手动移除。
        /// </summary>
        private void OnDispose() { }
        
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