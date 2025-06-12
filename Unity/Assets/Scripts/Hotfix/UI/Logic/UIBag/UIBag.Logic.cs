using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Event.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Config;
using Hotfix.Config.Tables;
using Hotfix.Events;
using Hotfix.Manager;
using Hotfix.Proto;

namespace Hotfix.UI
{
    public partial class UIBag
    {
        /// <summary>
        /// 道具类型
        /// </summary>
        private class ItemTypeData
        {
            /// 道具类型
            public ItemType Type { get; }

            /// 分类名称
            public string Name { get; }

            public ItemTypeData(ItemType type, string name)
            {
                Type = type;
                Name = name;
            }
        }

        private List<ItemTypeData> _tabs     = new(); // 道具类型页签列表
        private List<object>       _bagItems = new(); // 背包道具列表

        private BagItem _selectBagItem = null; // 选中的背包道具

        public override void OnAwake()
        {
            UIGroup = UIManager.Instance.GetUIGroup(UIGroupConstants.Window.Name);
            base.OnAwake();
            GameApp.Event.CheckSubscribe(BagChangedEventArgs.EventId, OnBagChangedEventArgs);
        }

        public override void OnOpen(object userData)
        {
            _bagItems = new List<object>();
            _tabs = new List<ItemTypeData>
            {
                new(ItemType.Item, "道具"),
                new(ItemType.Equip, "装备"),
                new(ItemType.Fragment, "碎片"),
                new(ItemType.Material, "材料"),
                new(ItemType.Expendable, "消耗品"),
            };


            base.OnOpen(userData);

            m_content.m_list.onClickItem.Set(OnBagItemClick);
            m_content.m_list.itemRenderer      = BagItemRenderer;
            m_content.m_type_list.itemRenderer = TypeItemRenderer;
            m_content.m_type_list.onClickItem.Set(OnTabTypeClick);
            m_content.m_type_list.DataList = new List<object>(_tabs);
            m_content.m_type_list.GetChildAt(0).onClick.Call();

            m_content.m_info.m_use_button.onClick.Add(OnUseButtonClick);
            m_bg.onClick.Set(OnCloseClick);
        }


        /// <summary>
        /// 背包变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBagChangedEventArgs(object sender, GameEventArgs e)
        {
            m_content.m_type_list.GetChildAt(m_content.m_type_list.selectedIndex).onClick.Call();
        }

        /// <summary>
        /// 使用道具
        /// </summary>
        private async void OnUseButtonClick()
        {
            if (_selectBagItem.IsNull()) return;
            await BagManager.Instance.RequestUseItem(_selectBagItem.ItemId, _selectBagItem.Count);
        }

        /// <summary>
        /// 背包道具点击事件
        /// </summary>
        /// <param name="context"></param>
        private void OnBagItemClick(EventContext context)
        {
            var data         = UIBagItem.GetFormPool((GObject)context.data);
            var itemTypeData = (BagItem)data.self.dataSource;
            UpdateSelectItem(itemTypeData);
        }


        /// <summary>
        /// 更新选中的道具
        /// </summary>
        /// <param name="bagItem"></param>
        private void UpdateSelectItem(BagItem bagItem)
        {
            _selectBagItem = bagItem;
            UIGoodItem.GetFormPool(m_content.m_info.m_good_item).SetCount(bagItem.Count).SetIcon(bagItem.ItemId);
            var itemConfig = GameApp.Config.GetConfig<TbItemConfig>().Get(bagItem.ItemId);
            m_content.m_info.m_IsCanUse.SetSelectedIndex(itemConfig.CanUse == ItemCanUse.CanNot ? 0 : 1);

            m_content.m_info.m_name_text.text = itemConfig.Name;
            m_content.m_info.m_desc_text.text = itemConfig.Description;
        }

        /// <summary>
        /// 背包道具渲染器
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void BagItemRenderer(int index, GObject item)
        {
            var bagItemData = (BagItem)item.dataSource;
            var uiBagItem   = UIBagItem.GetFormPool(item);
            var uiGoodItem  = UIGoodItem.GetFormPool(uiBagItem.m_good_item);
            uiGoodItem.SetCount(bagItemData.Count);
            uiGoodItem.SetIcon(bagItemData.ItemId);
        }

        /// <summary>
        /// 页签点击事件
        /// </summary>
        /// <param name="context"></param>
        private void OnTabTypeClick(EventContext context)
        {
            var data         = UIBagTypeItem.GetFormPool((GButton)context.data);
            var itemTypeData = (ItemTypeData)data.self.dataSource;
            _bagItems.Clear();
            _bagItems.AddRange(BagManager.Instance.GetBagItemsByType(itemTypeData.Type));
            m_content.m_list.DataList = _bagItems;
            if (_bagItems.Count > 0)
            {
                m_content.m_list.selectedIndex = 0;
                m_content.m_IsSelectedItem.SetSelectedIndex(1);
                var bagItem = (BagItem)_bagItems[0];
                UpdateSelectItem(bagItem);
            }
            else
            {
                m_content.m_IsSelectedItem.SetSelectedIndex(0);
                _selectBagItem = null;
            }
        }

        /// <summary>
        /// 页签渲染器
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void TypeItemRenderer(int index, GObject item)
        {
            var itemTypeData = (ItemTypeData)item.dataSource;
            item.asButton.title = itemTypeData.Name;
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void OnCloseClick()
        {
            UIManager.Instance.CloseUI<UIBag>();
        }
    }
}