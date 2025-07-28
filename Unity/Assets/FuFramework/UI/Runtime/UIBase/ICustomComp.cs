// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 自定义组件接口，用于实现自定义组件的初始化，注入自定义组件所属界面
    /// </summary>
    public interface ICustomComp
    {
        /// <summary>
        /// 初始化View
        /// </summary>
        /// <param name="view"></param>
        void Init(ViewBase view);
    }
}