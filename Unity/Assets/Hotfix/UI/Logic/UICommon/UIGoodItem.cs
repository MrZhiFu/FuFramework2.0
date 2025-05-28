using GameFrameX.Runtime;
using Hotfix.Config.Tables;

namespace Hotfix.UI
{
    public partial class UIGoodItem
    {
        /// <summary>
        /// 设置物品的图标
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public UIGoodItem SetIcon(int itemId)
        {
            var itemConfig = GameApp.Config.GetConfig<TbItemConfig>().Get(itemId);
            if (itemConfig.IsNotNull())
            {
                m_gift.icon = Utility.Asset.Path.GetImagePath(itemConfig.Icon);
                m_bg.icon   = Utility.Asset.Path.GetImagePath(itemConfig.BgIcon);
            }

            return this;
        }

        /// <summary>
        /// 设置物品的数量
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public UIGoodItem SetCount(long count)
        {
            m_number.text = count.ToString();
            return this;
        }
    }
}