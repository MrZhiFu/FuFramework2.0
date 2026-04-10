using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
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
            // Example: RedDotRegister.RegisterRedDot(this.uiView, RedDotKeys.BagItem, btnLogin, displayMode: CompRedDot.DisplayMode.Auto);
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
            _selectBagItem = selectBagItem;
            var itemConfig = GlobalModule.ConfigModule.GetConfig<TbItem>().Get(selectBagItem.ItemId);
            txtName.text = itemConfig.Name;
            txtDesc.text = itemConfig.Desc;
            var eIsCanUse = itemConfig.CanUse == ItemUseType.CanNot ? EIsCanUse.No : EIsCanUse.Yes;
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
            FuLogger.LogInfo("获取道具 TODO");
        }

        #endregion
    }
}