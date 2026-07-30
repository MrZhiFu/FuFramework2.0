using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace 禁用命名空间检查
// ReSharper disable once InconsistentNaming 禁用命名风格检查
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 泛型 LRU（最近最少使用）缓存器。
    /// 功能：
    ///     1. 提供泛型 LRU 缓存机制，支持任意 Key/Value 类型。
    ///     2. 支持设置最大容量，超出容量时自动淘汰最少使用的项。
    ///     3. 通过构造函数注入驱逐回调，由调用方自行处理资源释放。
    ///     4. 支持清空全部缓存并触发驱逐回调。
    /// </summary>
    /// <typeparam name="TKey">缓存键类型</typeparam>
    /// <typeparam name="TValue">缓存值类型</typeparam>
    public sealed class LRUCache<TKey, TValue>
    {
        /// <summary>
        /// 缓存项
        /// </summary>
        private sealed class CacheItem
        {
            /// <summary>
            /// 缓存的 Key
            /// </summary>
            public readonly TKey Key;

            /// <summary>
            /// 缓存的 Value
            /// </summary>
            public TValue Value;

            public CacheItem(TKey key, TValue value)
            {
                Key   = key;
                Value = value;
            }
        }

        /// <summary>
        /// 最大容量
        /// </summary>
        private readonly int m_Capacity;

        /// <summary>
        /// 缓存字典，Key 为缓存键，Value 为链表节点（实现 O(1) 查找 + O(1) 链表移动）
        /// </summary>
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> m_CacheDict;

        /// <summary>
        /// 最近使用列表（链表头为最近使用，链表尾为最少使用）
        /// </summary>
        private readonly LinkedList<CacheItem> m_LruList;

        /// <summary>
        /// 驱逐回调，参数为（被驱逐的 Key, 被驱逐的 Value）
        /// </summary>
        private readonly Action<TKey, TValue> m_OnEvict;

        /// <summary>
        /// 当前缓存数量
        /// </summary>
        public int Count => m_CacheDict.Count;

        /// <summary>
        /// 最大容量
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// 初始化 LRU 缓存器的新实例。
        /// </summary>
        /// <param name="capacity">最大容量，默认为 64，必须大于 0</param>
        /// <param name="onEvict">驱逐回调，当缓存项被淘汰或替换时触发。参数为（Key, Value），由调用方在此回调中释放资源。</param>
        /// <exception cref="ArgumentOutOfRangeException">maxCapacity 小于等于 0 时抛出</exception>
        public LRUCache(int capacity = 64, Action<TKey, TValue> onEvict = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "最大容量必须大于 0");

            m_Capacity  = capacity;
            m_CacheDict = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            m_LruList   = new LinkedList<CacheItem>();
            m_OnEvict   = onEvict;
        }

        /// <summary>
        /// 尝试获取缓存的值，并将该项移动到最近使用位置。
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">获取到的值，未找到时为 default</param>
        /// <returns>找到返回 true，否则返回 false</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            if (!m_CacheDict.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }

            // 移动到最近使用的位置
            m_LruList.Remove(node);
            m_LruList.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        /// <summary>
        /// 获取缓存的值，并将该项移动到最近使用位置。
        /// 未找到时返回 default(TValue)。
        /// 注意：对于值类型，无法区分"未找到"和"值为 default"，推荐使用 TryGet。
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的值，未找到时返回 default(TValue)</returns>
        public TValue Get(TKey key)
        {
            TryGet(key, out var value);
            return value;
        }

        /// <summary>
        /// 添加或更新缓存项。
        /// 如果 Key 已存在，则先驱逐旧项（触发 onEvict），再更新为新值并移到最近使用位置。
        /// 如果 Key 不存在且缓存已满，则淘汰最少使用的项（触发 onEvict）再添加新项。
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        public void Put(TKey key, TValue value)
        {
            if (m_CacheDict.TryGetValue(key, out var existingNode))
            {
                // 替换已有项：先驱逐旧值，再更新
                m_OnEvict?.Invoke(key, existingNode.Value.Value);
                existingNode.Value.Value = value;
                m_LruList.Remove(existingNode);
                m_LruList.AddFirst(existingNode);
            }
            else
            {
                // 如果超过最大数量，则移除最少使用的项
                if (m_CacheDict.Count >= m_Capacity)
                {
                    var lastNode = m_LruList.Last;
                    m_LruList.Remove(lastNode);
                    m_CacheDict.Remove(lastNode.Value.Key);
                    m_OnEvict?.Invoke(lastNode.Value.Key, lastNode.Value.Value);
                }

                // 添加新项
                var newItem = new CacheItem(key, value);
                var newNode = m_LruList.AddFirst(newItem);
                m_CacheDict[key] = newNode;
            }
        }

        /// <summary>
        /// 移除指定 Key 的缓存项，并触发驱逐回调。
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>成功移除返回 true，Key 不存在返回 false</returns>
        public bool Remove(TKey key)
        {
            if (!m_CacheDict.TryGetValue(key, out var node))
                return false;

            m_LruList.Remove(node);
            m_CacheDict.Remove(key);
            m_OnEvict?.Invoke(node.Value.Key, node.Value.Value);
            return true;
        }

        /// <summary>
        /// 清空全部缓存，对每个缓存项触发驱逐回调。
        /// </summary>
        public void Clear()
        {
            if (m_OnEvict != null)
            {
                foreach (var item in m_LruList)
                {
                    m_OnEvict.Invoke(item.Key, item.Value);
                }
            }

            m_CacheDict.Clear();
            m_LruList.Clear();
        }
    }
}