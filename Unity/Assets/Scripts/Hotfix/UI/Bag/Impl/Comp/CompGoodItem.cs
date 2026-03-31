using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using Hotfix.Config.Tables;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI
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
            // Example: RedDotRegister.RegisterRedDot(this.uiView, RedDotKeys.BagItem, btnLogin, displayMode: CompRedDot.DisplayMode.Auto);
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
            var itemConfig = GlobalModule.ConfigModule.GetConfig<TbItem>().Get(itemId);
            if (!itemConfig.IsNotNull()) return;
            loaderGift.icon = Utility.AssetPath.GetImagePath(itemConfig.Icon);
            loaderBg.icon   = Utility.AssetPath.GetImagePath(itemConfig.Bg);
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