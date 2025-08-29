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
        /// 设置物品的图标
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public void SetIcon(int itemId)
        {
            var itemConfig = GlobalModule.ConfigModule.GetConfig<TbItemConfig>().Get(itemId);
            if (!itemConfig.IsNotNull()) return;
            loaderGift.icon = Utility.Asset.Path.GetImagePath(itemConfig.Icon);
            loaderBg.icon = Utility.Asset.Path.GetImagePath(itemConfig.BgIcon);
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