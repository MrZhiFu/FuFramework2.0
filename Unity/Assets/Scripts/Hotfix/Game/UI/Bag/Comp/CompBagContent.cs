using System;
using System.Collections.Generic;
using FairyGUI;
using Hotfix.Framework.Event;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;
using Hotfix.Game.Events;
using Hotfix.Game.Manager_ToDelete;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class CompBagContent
    {
        /// <summary>
        /// 道具类型
        /// </summary>
        private class ItemTypeData
        {
            /// 道具类型
            public EItemType Type { get; }

            /// 分类名称
            public string Name { get; }

            public ItemTypeData(EItemType type, string name)
            {
                Type = type;
                Name = name;
            }
        }

        private List<ItemTypeData> m_Tabs = new(); // 道具类型页签列表
        private List<BagItem> m_BagItems = new(); // 背包道具列表

        private BagItem m_SelectBagItem = null; // 选中的背包道具

        /// <summary>
        /// 初始化
        /// </summary>
        private void OnInit()
        {
            InitEvent();
            InitRedDot();
            
            InitData();
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        private void InitEvent()
        {
            Subscribe(BagChangedEventArgs.EventId, OnBagChangedEventArgs);
        }

        /// <summary>
        /// 注册界面相关红点
        /// </summary>
        private void InitRedDot()
        {
            // Example: RedDotRegister.RegisterRedDot(this.winUI, ERedDotKey.Bag_Item, btnLogin);
        }

        private void InitData()
        {
            m_BagItems = new List<BagItem>();
            m_Tabs = new List<ItemTypeData>
            {
                new(EItemType.Item, "道具"),
                new(EItemType.Equip, "装备"),
                new(EItemType.Fragment, "碎片"),
                new(EItemType.Material, "材料"),
                new(EItemType.Expendable, "消耗品"),
            };
        }

        /// <summary>
        /// 销毁。
        /// 注意：UI事件，业务逻辑事件，计时器在 Dispose 中统一释放，无需在这里手动移除。
        /// </summary>
        private void OnDispose() { }
        
        public void Refresh()
        {  
            listItem.numItems = m_BagItems.Count;
            listType.numItems = m_Tabs.Count;
        }

        /// <summary>
        /// 背包变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBagChangedEventArgs(object sender, GameEventArgs e)
        {
            listType.GetChildAt(listType.selectedIndex).onClick.Call();
        }

        /// <summary>
        /// 更新选中的道具
        /// </summary>
        /// <param name="bagItem"></param>
        private void UpdateSelectItem(BagItem bagItem)
        {
            m_SelectBagItem = bagItem;
            compBagItem.SetData(bagItem);
        }

        #region 交互事件以及ListItem渲染回调处理

        /// <summary>
        /// 背包道具item点击回调
        /// </summary>
        /// <param name="ctx"></param>
        private void OnClickListItemItem(EventContext ctx)
        {
            var idx = listItem.GetChildIndex((GObject)ctx.data);
            var bagItem = m_BagItems[idx];
            UpdateSelectItem(bagItem);
        }

        /// <summary>
        /// 背包道具列表渲染回调
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        private void OnRenderListItemItem(int idx, GObject item)
        {
            var bagItem = m_BagItems[idx];
            if (item is not CompBagItem compItem) return;
            //var data = xxxModel:GetListPlayerDataByIdx(idx);
            compItem.SetData(bagItem.ItemId, bagItem.Count);
        }

        /// <summary>
        /// 道具页签点击回调
        /// </summary>
        /// <param name="ctx"></param>
        private void OnClickListTypeItem(EventContext ctx)
        {
            var idx = listType.GetChildIndex((GObject)ctx.data);
            var itemTypeData = m_Tabs[idx];

            m_BagItems.Clear();
            m_BagItems.AddRange(BagManager.Instance.GetBagItemsByType(itemTypeData.Type));
            if (m_BagItems.Count > 0)
            {
                listItem.selectedIndex = 0;
                SetController(EIsSelectedItem.Yes);
                var bagItem = m_BagItems[0];
                UpdateSelectItem(bagItem);
            }
            else
            {
                SetController(EIsSelectedItem.No);
                m_SelectBagItem = null;
            }
        }

        /// <summary>
        /// 道具页签列表渲染回调
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        private void OnRenderListTypeItem(int idx, GObject item)
        {
            if (item is not CompTypeItem compItem) return;
            //var data = xxxModel:GetListPlayerDataByIdx(idx);
            compItem.SetData(m_Tabs[idx].Name);
        }

        #endregion
    }
}
