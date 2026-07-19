using Hotfix.Framework.Core;
using AOT.Framework.Core.Utility;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Game.UI;
using Hotfix.Game.Tables;
using Hotfix.Game.Proto;
using Hotfix.Framework.Config;
using Utility = Hotfix.Framework.Core.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class CompGoodItem
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

        /// <summary>
        /// 设置物品的图标
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public void SetIcon(int itemId)
        {
            var itemConfig = ConfigModule.Instance.GetConfig<TbItem>().Get(itemId);
            if (!itemConfig.IsNotNull()) return;
            loaderGift.icon = UtilityAOT.AssetPath.GetImagePath(itemConfig.Icon);
            loaderBg.icon   = UtilityAOT.AssetPath.GetImagePath(itemConfig.Bg);
        }

        /// <summary>
        /// 设置物品的数量
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public void SetCount(long count)
        {
            txtNum.text = count.ToString();
        }
    }
}
