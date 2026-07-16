using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using Hotfix.Network;
using Hotfix;
using Hotfix.Config;
using Hotfix.Config.Tables;
using Hotfix.ModuleConfig;
using Hotfix.Events;
using Hotfix.Proto;

namespace Hotfix.Manager
{
    /// <summary>
    /// 背包 管理器
    /// </summary>
    public sealed class BagManager : Singleton<BagManager>, IMessageHandler
    {
        private readonly Dictionary<int, BagItem> m_ItemDic = new Dictionary<int, BagItem>();

        public List<BagItem> GetItems()
        {
            return new List<BagItem>(m_ItemDic.Values);
        }

        /// <summary>
        /// 监听道具变化通知
        /// </summary>
        /// <param name="msg"></param>
        [MessageHandler(typeof(NotifyBagInfoChanged), nameof(NotifyBagInfoChanged))]
        private void NotifyBagInfoChanged(NotifyBagInfoChanged msg)
        {
            foreach (var keyValuePair in msg.ItemDic)
            {
                if (m_ItemDic.TryGetValue(keyValuePair.Key, out var item))
                {
                    item.Count = keyValuePair.Value.Count;
                    if (m_ItemDic[keyValuePair.Key].Count <= 0)
                    {
                        m_ItemDic.Remove(keyValuePair.Key);
                    }
                }
                else
                {
                    m_ItemDic[keyValuePair.Key] = new BagItem() { ItemId = keyValuePair.Key, Count = keyValuePair.Value.Count };
                }
            }

            GlobalModule.EventModule.Broadcast(this, BagChangedEventArgs.Create());
        }

        /// <summary>
        /// 请求背包信息
        /// </summary>
        public async UniTask RequestGetBagInfoAsync()
        {
            var respBagInfo = await NetworkModule.Instance.GetNetworkChannel("network").Call<RespBagInfo>(new ReqBagInfo());
            if (respBagInfo.ErrorCode != default)
            {
                return;
            }

            foreach (var item in respBagInfo.ItemDic)
            {
                m_ItemDic[item.Key] = new BagItem() { ItemId = item.Key, Count = item.Value };
            }
        }

        /// <summary>
        /// 请求使用道具
        /// </summary>
        /// <param name="itemId">道具ID</param>
        /// <param name="count">道具数量</param>
        public async UniTask RequestUseItemAsync(int itemId, long count = 1)
        {
            var respUseItem = await NetworkModule.Instance.GetNetworkChannel("network").Call<RespUseItem>(new ReqUseItem() { ItemId = itemId, Count = count });
            if (respUseItem.ErrorCode != default)
            {
                return;
            }

            if (m_ItemDic.TryGetValue(respUseItem.ItemId, out var value))
            {
                value.Count -= respUseItem.Count;
                if (value.Count <= 0)
                {
                    m_ItemDic.Remove(respUseItem.ItemId);
                }
            }

            GlobalModule.EventModule.Broadcast(this, BagChangedEventArgs.Create());
        }

        /// <summary>
        /// 获取指定类型的道具
        /// </summary>
        /// <param name="bagType">背包类型</param>
        /// <returns></returns>
        public List<BagItem> GetBagItemsByType(ItemType bagType)
        {
            var result = new List<BagItem>(m_ItemDic.Count);
            var tbItemConfig = ConfigModule.Instance.GetConfig<TbItem>();
            var itemType = bagType;
            foreach (var bagItem in m_ItemDic)
            {
                var itemConfig = tbItemConfig.Get(bagItem.Key);
                if (itemConfig.IsNotNull() && itemConfig.Type == itemType)
                {
                    result.Add(bagItem.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// 由于是单例对象。所以在初始化的时候自动调用一次注册消息
        /// </summary>
        public BagManager()
        {
            Register();
        }

        /// <summary>
        /// 注册消息。请勿多次调用
        /// </summary>
        public void Register()
        {
            ProtoMessageHandler.Add(this);
        }
    }
}