using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 去重帮助类
    /// </summary>
    public static class DistinctHelper
    {
        /// <summary>
        /// 根据条件去重集合内的元素
        /// </summary>
        /// <param name="source">源集合</param>
        /// <param name="keySelector">元素条件选择器</param>
        /// <typeparam name="TSource">元素类型</typeparam>
        /// <typeparam name="TKey">元素条件类型</typeparam>
        /// <returns>去重后的集合</returns>
        /// <example> DistinctBy(new[] { new { Id = 1, Name = "Alice" }, new { Id = 2, Name = "Bob" }, new { Id = 1, Name = "Charlie" } }, x => x.Id) => new[] { new { Id = 1, Name = "Alice" }, new { Id = 2, Name = "Bob" } } </example>
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
    }
}