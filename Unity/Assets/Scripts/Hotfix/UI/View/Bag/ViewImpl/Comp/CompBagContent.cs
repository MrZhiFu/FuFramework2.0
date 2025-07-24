using FairyGUI;
using GameFrameX.Runtime;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Bag
{
    public partial class CompBagContent
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
        
		private void OnClickListItemItem(EventContext ctx)
		{
			//var idx = ((GObject)ctx.data)?.ItemIndex;
			//var data = xxxModel:GetListDataByIdx(idx);
			// todo
		}

		private void OnRenderListItemItem(int idx, GObject item)
		{
			//var data = xxxModel:GetListItemDataByIdx(idx);
			//Make Sure the CompBagItem is Exported
			//((CompBagItem)item)?.SetData(data);
			// todo
		}

		private void OnClickListTypeItem(EventContext ctx)
		{
			//var idx = ((GObject)ctx.data)?.ItemIndex;
			//var data = xxxModel:GetListDataByIdx(idx);
			// todo
		}

		private void OnRenderListTypeItem(int idx, GObject item)
		{
			//var data = xxxModel:GetListTypeDataByIdx(idx);
			//Make Sure the CompTypeItem is Exported
			//((CompTypeItem)item)?.SetData(data);
			// todo
		}

        #endregion
    }
}