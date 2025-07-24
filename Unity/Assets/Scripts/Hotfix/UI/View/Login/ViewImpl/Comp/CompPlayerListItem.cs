using FairyGUI;
using GameFrameX.Runtime;
using FuFramework.UI.Runtime;
using Hotfix.Proto;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Login
{
    public partial class CompPlayerListItem
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
        
		private void OnBtnLoginClick(EventContext ctx)
		{
			// todo
		}

        #endregion
        
        /// <summary>
        /// 设置组件显示数据
        /// </summary>
        /// <param name="playerInfo">玩家信息</param>
        public void SetData(PlayerInfo playerInfo)
        {
            txtLevel.text = "当前等级:" + playerInfo.Level;
            txtName.text = playerInfo.Name;
            imgIcon.icon = UIPackage.GetItemURL("Common", "wrap_1");
        }
    }
}