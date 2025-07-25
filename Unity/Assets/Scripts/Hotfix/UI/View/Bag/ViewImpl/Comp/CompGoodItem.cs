using GameFrameX.Runtime;
using FuFramework.UI.Runtime;
using Hotfix.Config.Tables;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Bag
{
    public partial class CompGoodItem
    {
        /// <summary>
        /// 初始化所属界面.
        /// 注意，如果该组件作为列表的Item使用，请在列表渲染回调方法OnRenderListXxxItem()中确保被成功调用，否则无法注册组件所属界面
        /// </summary>
        public void InitView(ViewBase view)
        {
            Log.Info($"初始化{view.UIName}界面组件-{GetType().Name}");
            uiView = view;
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

        /// <summary>
        /// 设置物品的图标
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public void SetIcon(int itemId)
        {
            var itemConfig = GameApp.Config.GetConfig<TbItemConfig>().Get(itemId);
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