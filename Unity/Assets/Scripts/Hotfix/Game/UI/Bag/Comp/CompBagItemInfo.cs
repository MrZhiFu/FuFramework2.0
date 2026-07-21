using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;
using Hotfix.Framework.Config;
using Hotfix.Game.Manager_ToDelete;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class CompBagItemInfo
    {
        private BagItem m_SelectBagItem;

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

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="selectBagItem"></param>
        public void SetData(BagItem selectBagItem)
        {
            if (selectBagItem.IsNull()) return;
            m_SelectBagItem = selectBagItem;
            var itemConfig = ConfigModule.Instance.GetConfig<TbItem>().Get(selectBagItem.ItemId);
            txtName.text = itemConfig.Name;
            txtDesc.text = itemConfig.Desc;
            var eIsCanUse = itemConfig.CanUse == ItemUseType.CanNot ? EIsCanUse.No : EIsCanUse.Yes;
            SetController(eIsCanUse);
        }

        #region 交互事件以及ListItem渲染回调处理

        private void OnBtnUseClick(EventContext ctx)
        {
            if (m_SelectBagItem.IsNull()) return;
            BagManager.Instance.RequestUseItemAsync(m_SelectBagItem.ItemId, m_SelectBagItem.Count).Forget();
        }

        private void OnBtnGetClick(EventContext ctx)
        {
            // todo
            FuLogger.LogInfo("获取道具 TODO");
        }

        #endregion
    }
}
