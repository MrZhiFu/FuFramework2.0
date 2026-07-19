using Hotfix.Game.UI;
using Hotfix.Game.Tables;
using Hotfix.Game.Proto;
using FairyGUI;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class CompPlayerListItem
    {
        /// <summary>
        /// 初始化
        /// </summary>
        private void OnInit()
        {
            InitEvent();
            InitRedDot();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            // Example:Subscribe(XxxEventArgs.EventId, XxxEventArgs.Create(xxx));
        }

        /// <summary>
        /// 注册界面相关红点
        /// </summary>
        private void InitRedDot()
        {
            // Example: RedDotRegister.RegisterRedDot(this.uiView, ERedDotKey.Bag_Item, btnLogin);
        }
        
        /// <summary>
        /// 销毁。
        /// 注意：UI事件，业务逻辑事件，计时器会自动从所属的View中移除，无需在这里手动移除。
        /// </summary>
        private void OnDispose() { }

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
            txtName.text  = playerInfo.Name;
            imgIcon.icon  = UIPackage.GetItemURL("Common", "wrap_1");
        }
    }
}
