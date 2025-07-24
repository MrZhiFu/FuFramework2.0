using FairyGUI;
using GameFrameX.Runtime;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Bag
{
    public partial class CompTypeItem
    {
        /// <summary>
        /// UI组件初始化
        /// </summary>
        public void Init(ViewBase view)
        {
            Log.Info($"初始化{view.UIName}界面组件-{GetType().Name}");
            uiView = view;
            InitUIComp();
            InitUIEvent();
            InitEvent();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        public void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, XxxEventArgs.Create(xxx));
        }
        
        #region 交互事件以及ListItem渲染回调处理
        
        #endregion
    }
}