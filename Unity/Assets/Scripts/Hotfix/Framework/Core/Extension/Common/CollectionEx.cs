using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 集合相关的扩展方法。
    /// 功能：
    ///     1. 合并Dictionary中的value值。
    ///     2. 根据key获取value值，当不存在时通过valueGetter生成value，放入Dictionary并返回value。
    ///     3. 根据条件移除Dictionary中的元素。
    ///     4. 判断集合是否为空。
    ///     5. 根据条件去重集合内的元素。
    ///     6. 打乱列表顺序（洗牌算法）。
    ///     7. 根据条件移除列表内的元素。
    ///     8. 将列表转换为以指定字符串分割的字符串。
    ///     9. 向HashSet批量添加元素。
    /// </summary>
    public static class CollectionEx
    {
        #region DictionaryEx

        /// <summary>
        /// 将k和v进行合并，如果key已存在则使用func合并新旧value，否则直接添加
        /// </summary>
        /// <param name="self">目标字典</param>
        /// <param name="k">键</param>
        /// <param name="v">值</param>
        /// <param name="func">合并函数，参数为(旧值, 新值)</param>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <example>
        /// var dict = new Dictionary&lt;string, int&gt; { { "a", 10 } };
        /// dict.Merge("a", 5, (oldVal, newVal) => oldVal + newVal); // dict["a"] = 15
        /// dict.Merge("b", 3, (oldVal, newVal) => oldVal + newVal); // dict["b"] = 3
        /// </example>
        public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey k, TValue v, Func<TValue, TValue, TValue> func)
        {
            self[k] = self.TryGetValue(k, out var value) ? func(value, v) : v;
        }

        /// <summary>
        /// 根据key获取value值，当不存在时通过valueGetter生成value，放入Dictionary并返回value
        /// </summary>
        /// <param name="self">目标字典</param>
        /// <param name="key">键</param>
        /// <param name="valueGetter">值生成函数</param>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <returns>获取或生成的值</returns>
        /// <example>
        /// var dict = new Dictionary&lt;string, List&lt;int&gt;&gt;();
        /// var list = dict.GetOrAdd("key", k => new List&lt;int&gt;()); // 不存在，创建新列表
        /// list.Add(1);
        /// var sameList = dict.GetOrAdd("key", k => new List&lt;int&gt;()); // 已存在，返回原列表
        /// </example>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> valueGetter)
        {
            if (self.TryGetValue(key, out var value)) return value;
            value     = valueGetter(key);
            self[key] = value;
            return value;
        }

        /// <summary>
        /// 根据key获取value值，当不存在时通过默认构造函数生成value，放入Dictionary并返回value
        /// </summary>
        /// <param name="self">目标字典</param>
        /// <param name="key">键</param>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型，必须有无参构造函数</typeparam>
        /// <returns>获取或生成的值</returns>
        /// <example>
        /// var dict = new Dictionary&lt;string, StringBuilder&gt;();
        /// var sb = dict.GetOrAdd("logs"); // 不存在，创建新StringBuilder
        /// sb.Append("log1");
        /// var sameSb = dict.GetOrAdd("logs"); // 已存在，返回原StringBuilder
        /// </example>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key) where TValue : new()
        {
            return GetOrAdd(self, key, _ => new TValue());
        }

        /// <summary>
        /// 根据条件移除字典中满足条件的元素
        /// </summary>
        /// <param name="self">目标字典</param>
        /// <param name="predict">条件判断函数，参数为(键, 值)，返回true表示需要移除</param>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <returns>被移除的元素数量</returns>
        /// <example>
        /// var dict = new Dictionary&lt;string, int&gt; { { "a", 1 }, { "b", 5 }, { "c", 10 } };
        /// var removed = dict.RemoveIf((k, v) => v > 3); // 移除b和c，返回2
        /// // dict现在为 { { "a", 1 } }
        /// </example>
        public static int RemoveIf<TKey, TValue>(this Dictionary<TKey, TValue> self, Func<TKey, TValue, bool> predict)
        {
            var count  = 0;
            var remove = new HashSet<TKey>();
            foreach (var kv in self)
            {
                if (predict(kv.Key, kv.Value))
                {
                    remove.Add(kv.Key);
                    count++;
                }
            }

            foreach (var key in remove)
            {
                self.Remove(key);
            }

            return count;
        }

        #endregion

        #region ICollectionExtensions

        /// <summary>
        /// 判断集合是否元素数量为0或为null
        /// </summary>
        /// <param name="self">目标集合</param>
        /// <typeparam name="T">元素类型</typeparam>
        /// <returns>如果集合为null或元素数量为0则返回true，否则返回false</returns>
        public static bool IsNullOrEmpty<T>(this ICollection<T> self)
        {
            return self is not { Count: > 0 };
        }

        /// <summary>
        /// 根据条件去重集合内的元素
        /// </summary>
        /// <param name="source">源集合</param>
        /// <param name="keySelector">元素条件选择器</param>
        /// <typeparam name="TSource">元素类型</typeparam>
        /// <typeparam name="TKey">元素条件类型</typeparam>
        /// <returns>去重后的集合</returns>
        /// <example>
        /// var users = new[] { 
        ///     new { Id = 1, Name = "Alice" }, 
        ///     new { Id = 2, Name = "Bob" }, 
        ///     new { Id = 1, Name = "Charlie" } 
        /// };
        /// var distinct = users.DistinctBy(x => x.Id); // 保留Alice和Bob，Charlie被去重
        /// </example>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var identifiedKeys = new HashSet<TKey>();
            foreach (var item in source)
            {
                if (identifiedKeys.Add(keySelector(item)))
                {
                    yield return item;
                }
            }
        }

        #endregion

        #region List<T>

        /// <summary>
        /// 打乱一个列表(洗牌算法/Fisher-Yates算法)
        /// </summary>
        /// <param name="list">目标列表</param>
        /// <typeparam name="T">元素类型</typeparam>
        /// <example>
        /// var cards = new List&lt;int&gt; { 1, 2, 3, 4, 5 };
        /// cards.Shuffle(); // 列表顺序被打乱，如: 3, 1, 5, 2, 4
        /// </example>
        public static void Shuffle<T>(this List<T> list)
        {
            // 0个或1个元素不需要洗牌
            if (list.Count <= 1) return;

            for (var i = 0; i < list.Count; i++)
            {
                var randomIdx = Utility.Random.GetRandom(i, list.Count);
                (list[i], list[randomIdx]) = (list[randomIdx], list[i]);
            }
        }

        /// <summary>
        /// 根据条件移除列表内所有满足条件的元素
        /// </summary>
        /// <param name="list">目标列表</param>
        /// <param name="condition">条件判断函数，返回true表示需要移除</param>
        /// <typeparam name="T">元素类型</typeparam>
        /// <remarks>会移除所有满足条件的元素，不只是第一个</remarks>
        /// <example>
        /// var numbers = new List&lt;int&gt; { 1, 2, 3, 4, 5, 6 };
        /// numbers.RemoveIf(x => x % 2 == 0); // 移除所有偶数
        /// // numbers现在为 { 1, 3, 5 }
        /// </example>
        public static void RemoveIf<T>(this List<T> list, Predicate<T> condition)
        {
            var idx = list.FindIndex(condition);
            while (idx >= 0)
            {
                list.RemoveAt(idx);
                idx = list.FindIndex(condition);
            }
        }

        private static readonly StringBuilder ListToStringBuilder = new();

        /// <summary>
        /// 将列表转换为以指定字符串分割的字符串
        /// </summary>
        /// <param name="list">目标列表</param>
        /// <param name="separator">分隔符，默认为逗号","</param>
        /// <typeparam name="T">元素类型</typeparam>
        /// <returns>拼接后的字符串</returns>
        /// <remarks>使用静态StringBuilder避免频繁创建对象，注意：非线程安全</remarks>
        /// <example>
        /// var numbers = new List&lt;int&gt; { 1, 2, 3 };
        /// var str1 = numbers.ListToString(); // "1,2,3,"
        /// var str2 = numbers.ListToString("|"); // "1|2|3|"
        /// </example>
        public static string ListToString<T>(this List<T> list, string separator = ",")
        {
            ListToStringBuilder.Clear();
            foreach (var t in list)
            {
                ListToStringBuilder.Append(t);
                ListToStringBuilder.Append(separator);
            }

            return ListToStringBuilder.ToString();
        }

        #endregion

        #region HashSet<T>

        /// <summary>
        /// 向HashSet批量添加元素
        /// </summary>
        /// <param name="c">目标HashSet</param>
        /// <param name="e">要添加的元素集合</param>
        /// <typeparam name="T">元素类型</typeparam>
        /// <example>
        /// var set = new HashSet&lt;int&gt; { 1, 2 };
        /// set.AddRange(new[] { 2, 3, 4 }); // 2已存在不会被重复添加
        /// // set现在为 { 1, 2, 3, 4 }
        /// </example>
        public static void AddRange<T>(this HashSet<T> c, IEnumerable<T> e)
        {
            foreach (var item in e)
            {
                c.Add(item);
            }
        }

        #endregion
    }
}
