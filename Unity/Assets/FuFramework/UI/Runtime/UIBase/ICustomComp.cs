// ReSharper disable once CheckNamespace 禁用命名空间检查

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 自定义组件接口。
    /// 职责：用于实现自定义组件在初始化时注入其所属界面，然后跟随界面的生命周期。
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