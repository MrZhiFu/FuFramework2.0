using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 日期时间扩展
    /// </summary>
    public static class DateTimeEx
    {
        /// <summary>
        /// 获取两个日期间的天数
        /// </summary>
        /// <param name="now"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int GetDaysFrom(this DateTime now, DateTime dt)
        {
            return (int)(now.Date - dt).TotalDays;
        }

        /// <summary>
        /// 获取当前日期距离1970-01-01的天数
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public static int GetDaysFromDefault(this DateTime now)
        {
            return now.GetDaysFrom(new DateTime(1970, 1, 1).Date);
        }
    }
}