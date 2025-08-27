using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using Hotfix.Config;
using Hotfix.Config.Tables;
using Hotfix.Manager;
using Hotfix.Proto;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
{
    public partial class CompBagItemInfo
    {
	    private BagItem _selectBagItem;

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
        /// <param name="selectBagItem"></param>
        public void SetData(BagItem selectBagItem)
        {
	        if (selectBagItem.IsNull()) return;
	        _selectBagItem = selectBagItem;
	        var itemConfig = GameApp.Config.GetConfig<TbItemConfig>().Get(selectBagItem.ItemId);
	        txtName.text = itemConfig.Name;
	        txtDesc.text = itemConfig.Description;
	        var eIsCanUse = itemConfig.CanUse == ItemCanUse.CanNot? EIsCanUse.No : EIsCanUse.Yes;
	        SetController(eIsCanUse);
        }

        #region 交互事件以及ListItem渲染回调处理
        
		private void OnBtnUseClick(EventContext ctx)
		{
			if (_selectBagItem.IsNull()) return;
			BagManager.Instance.RequestUseItem(_selectBagItem.ItemId, _selectBagItem.Count).Forget();
		}

		private void OnBtnGetClick(EventContext ctx)
		{
			// todo
			Log.Info("获取道具 TODO");
		}

        #endregion
    }
}