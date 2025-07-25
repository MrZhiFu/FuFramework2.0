// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 自定义组件接口
    /// </summary>
    public interface ICustomComp
    {
        /// <summary>
        /// 初始化View
        /// </summary>
        /// <param name="view"></param>
        void InitView(ViewBase view);
    }
}