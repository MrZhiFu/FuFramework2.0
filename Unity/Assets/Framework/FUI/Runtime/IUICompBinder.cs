namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 定义绑定 or 移除绑定 某个Package下的自定义组件接口
    /// </summary>
    public interface IUICompBinder
    {
        void BindComp();
        
        void RemoveBindComp();
    }
}