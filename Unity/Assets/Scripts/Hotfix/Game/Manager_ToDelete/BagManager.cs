using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Proto;
using Hotfix.Framework.Config;
using Hotfix.Game.Events;

namespace Hotfix.Game.Manager_ToDelete
{
	/// <summary>
	/// 背包 管理器
	/// </summary>
	public sealed class BagManager : Singleton<BagManager>
	{
		private readonly Dictionary<int, BagItem> m_itemDic = new Dictionary<int, BagItem>();

		public List<BagItem> GetItems()
		{
			return new List<BagItem>(m_itemDic.Values);
		}

		/// <summary>
		/// 监听道具变化通知
		/// </summary>
		/// <param name="msg"></param>
		/// <remarks>
		/// 可见性契约：生成物 Register/AutoGen/MessageRegistry.gen.cs 会为 [NotifyMessageHandler] 方法生成
		/// 直接委托（<c>((BagManager)handler).OnNotifyBagInfoChanged((NotifyBagInfoChanged)message)</c>），
		/// 因此本方法必须是 internal 或 public（不能是 private / protected），否则生成脚本会报错。
		/// 详见 Framework/Network/README.md「消息注册」章节。
		/// </remarks>
		[NotifyMessageHandler(typeof(NotifyBagInfoChanged), nameof(OnNotifyBagInfoChanged))]
		internal void OnNotifyBagInfoChanged(NotifyBagInfoChanged msg)
		{
			foreach (var keyValuePair in msg.ItemDic)
			{
				if (m_itemDic.TryGetValue(keyValuePair.Key, out var item))
				{
					item.Count = keyValuePair.Value.Count;
					if (m_itemDic[keyValuePair.Key].Count <= 0)
					{
						m_itemDic.Remove(keyValuePair.Key);
					}
				}
				else
				{
					m_itemDic[keyValuePair.Key] = new BagItem() { ItemId = keyValuePair.Key, Count = keyValuePair.Value.Count };
				}
			}

			GlobalModule.EventModule.Broadcast(this, BagChangedEventArgs.Create());
		}

		/// <summary>
		/// 请求背包信息
		/// </summary>
		public async UniTask RequestGetBagInfoAsync()
		{
			var rspBagInfo = await NetworkModule.Instance.Call(new ReqBagInfo());
			if (rspBagInfo.ErrorCode != default)
			{
				return;
			}

			foreach (var item in rspBagInfo.ItemDic)
			{
				m_itemDic[item.Key] = new BagItem() { ItemId = item.Key, Count = item.Value };
			}
		}

		/// <summary>
		/// 请求使用道具
		/// </summary>
		/// <param name="itemId">道具ID</param>
		/// <param name="count">道具数量</param>
		public async UniTask RequestUseItemAsync(int itemId, long count = 1)
		{
			var rspUseItem = await NetworkModule.Instance.Call(new ReqUseItem() { ItemId = itemId, Count = count });
			if (rspUseItem.ErrorCode != default)
			{
				return;
			}

			if (m_itemDic.TryGetValue(rspUseItem.ItemId, out var value))
			{
				value.Count -= rspUseItem.Count;
				if (value.Count <= 0)
				{
					m_itemDic.Remove(rspUseItem.ItemId);
				}
			}

			GlobalModule.EventModule.Broadcast(this, BagChangedEventArgs.Create());
		}

		/// <summary>
		/// 获取指定类型的道具
		/// </summary>
		/// <param name="bagType">背包类型</param>
		/// <returns></returns>
		public List<BagItem> GetBagItemsByType(EItemType bagType)
		{
			var result = new List<BagItem>(m_itemDic.Count);
			var tbItemConfig = ConfigModule.Instance.GetConfig<TbItem>();
			var itemType = bagType;
			foreach (var bagItem in m_itemDic)
			{
				var itemConfig = tbItemConfig.Get(bagItem.Key);
				if (itemConfig.IsNotNull() && itemConfig.Type == itemType)
				{
					result.Add(bagItem.Value);
				}
			}

			return result;
		}
	}
}
