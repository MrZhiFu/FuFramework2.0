using System;
using System.Collections.Generic;
using FairyGUI;
using Hotfix.Framework.Event;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
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

		private List<ItemTypeData> m_tabs = new(); // 道具类型页签列表
		private List<BagItem> m_bagItems = new(); // 背包道具列表

		private BagItem m_selectBagItem = null; // 选中的背包道具

		/// <summary>
		/// 初始化
		/// </summary>
		private void OnInit()
		{
			InitEvent();

			InitData();
		}

		/// <summary>
		/// UI组件事件初始化
		/// </summary>
		private void InitUIEvent()
		{
			AddUIListener(listItem.onClickItem, OnClickListItemItem);
			AddUIListener(listType.onClickItem, OnClickListTypeItem);
			listItem.itemRenderer = OnRenderListItemItem;
			listType.itemRenderer = OnRenderListTypeItem;
		}

		/// <summary>
		/// 注册相关逻辑事件
		/// </summary>
		private void InitEvent()
		{
			Subscribe(BagChangedEventArgs.EventId, OnBagChangedEventArgs);
		}


		private void InitData()
		{
			m_bagItems = new List<BagItem>();
			m_tabs = new List<ItemTypeData>
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
			listItem.numItems = m_bagItems.Count;
			listType.numItems = m_tabs.Count;
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
			m_selectBagItem = bagItem;
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
			var bagItem = m_bagItems[idx];
			UpdateSelectItem(bagItem);
		}

		/// <summary>
		/// 背包道具列表渲染回调
		/// </summary>
		/// <param name="idx"></param>
		/// <param name="item"></param>
		private void OnRenderListItemItem(int idx, GObject item)
		{
			var bagItem = m_bagItems[idx];
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
			var itemTypeData = m_tabs[idx];

			m_bagItems.Clear();
			m_bagItems.AddRange(BagManager.Instance.GetBagItemsByType(itemTypeData.Type));
			if (m_bagItems.Count > 0)
			{
				listItem.selectedIndex = 0;
				SetController(EIsSelectedItem.Yes);
				var bagItem = m_bagItems[0];
				UpdateSelectItem(bagItem);
			}
			else
			{
				SetController(EIsSelectedItem.No);
				m_selectBagItem = null;
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
			compItem.SetData(m_tabs[idx].Name);
		}

		#endregion
	}
}
