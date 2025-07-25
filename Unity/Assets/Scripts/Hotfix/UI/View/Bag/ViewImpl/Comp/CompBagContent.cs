using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Runtime;
using FuFramework.UI.Runtime;
using GameFrameX.Event.Runtime;
using Hotfix.Config;
using Hotfix.Events;
using Hotfix.Manager;
using Hotfix.Proto;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.UI.View.Bag
{
    public partial class CompBagContent
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

        private List<ItemTypeData> _tabs = new(); // 道具类型页签列表
        private List<BagItem> _bagItems = new(); // 背包道具列表

        private BagItem _selectBagItem = null; // 选中的背包道具

        /// <summary>
        /// UI组件初始化
        /// </summary>
        public void Init(ViewBase view)
        {
            Log.Info($"初始化{view.UIName}界面组件-{GetType().Name}");
            uiView = view;
            InitUIComp();
            InitUIEvent();
            InitEvent();
            InitData();
        }

        private void InitData()
        {
            _bagItems = new List<BagItem>();
            _tabs = new List<ItemTypeData>
            {
                new(ItemType.Item, "道具"),
                new(ItemType.Equip, "装备"),
                new(ItemType.Fragment, "碎片"),
                new(ItemType.Material, "材料"),
                new(ItemType.Expendable, "消耗品"),
            };
            
            listItem.numItems = _tabs.Count;
        }

        /// <summary>
        /// 注册相关逻辑事件
        /// </summary>
        public void InitEvent()
        {
            Subscribe(BagChangedEventArgs.EventId, OnBagChangedEventArgs);
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
            _selectBagItem = bagItem;
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
            var bagItem = _bagItems[idx];
            UpdateSelectItem(bagItem);
        }

        /// <summary>
        /// 背包道具列表渲染回调
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        private void OnRenderListItemItem(int idx, GObject item)
        {
            var bagItem = _bagItems[idx];
            ((CompBagItem)item)?.SetData(bagItem.ItemId, bagItem.Count);
        }

        /// <summary>
        /// 道具页签点击回调
        /// </summary>
        /// <param name="ctx"></param>
        private void OnClickListTypeItem(EventContext ctx)
        {
            var idx = listType.GetChildIndex((GObject)ctx.data);
            var itemTypeData = _tabs[idx];
            
            _bagItems.Clear();
            _bagItems.AddRange(BagManager.Instance.GetBagItemsByType(itemTypeData.Type));
            if (_bagItems.Count > 0)
            {
                listItem.selectedIndex = 0;
                SetController(EIsSelectedItem.Yes);
                var bagItem = _bagItems[0];
                UpdateSelectItem(bagItem);
            }
            else
            {
                SetController(EIsSelectedItem.No);
                _selectBagItem = null;
            }
        }

        /// <summary>
        /// 道具页签列表渲染回调
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        private void OnRenderListTypeItem(int idx, GObject item)
        {
            ((CompTypeItem)item)?.SetData(_tabs[idx].Name);
        }

        #endregion
    }
}