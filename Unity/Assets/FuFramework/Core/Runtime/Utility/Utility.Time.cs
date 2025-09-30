using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 时间相关的实用函数。
        /// 1. 服务器与客户端时间差设置
        /// 2. 时间戳与时间的相互转换
        /// 3. 时间格式转换
        /// </summary>
        /// <remarks>
        /// 方法名约定：不带Ms的为秒级时间戳，带Ms的为毫秒级时间戳。
        /// </remarks>
        public static class Time
        {
            /// <summary>
            /// Unix纪元起点 1970-01-01 00:00:00 UTC 格林威治时间。主动声明，避免new DateTime(1970, 1, 1) 每次都会在堆上创建新对象
            /// </summary>
            public static readonly DateTime UtcEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            /// <summary>
            /// Unix纪元起点时间戳(刻度)
            /// 621355968000000000。0001-01-01 00:00:00 ~ 1970.1.1  00:00:00  之间的时间差刻度数
            /// </summary>
            public static readonly long EpochTicks = UtcEpoch.Ticks;

            /// <summary>
            /// 是否是秒级
            /// </summary>
            private static bool _isSecLevel = true;

            /// <summary>
            /// 服务器与客户端的时间差。
            /// 单位：_isSecLevel为true时为秒，为false时为毫秒。
            /// </summary>
            private static long _diffTime;

            #region 服务器/客户端时间相关

            /// <summary>
            /// 设置服务器与客户端的时间差
            /// </summary>
            /// <param name="serverTimestamp">服务器时间戳</param>
            /// <remarks>
            /// 自动检测时间戳精度：
            /// 通过比较与当前时间的差值，选择差值更小的精度
            /// </remarks>
            public static void SetDifferenceTime(long serverTimestamp)
            {
                if (serverTimestamp < 0)
                    throw new ArgumentException("时间戳不能为负数", nameof(serverTimestamp));

                // 获取当前客户端时间
                var currentSeconds = ClientNow();
                var currentMilliseconds = ClientNowMs();

                // 计算与两种精度的差值
                var diffSeconds = System.Math.Abs(serverTimestamp - currentSeconds);
                var diffMilliseconds = System.Math.Abs(serverTimestamp - currentMilliseconds);

                // 选择差值更小的精度
                _isSecLevel = diffSeconds < diffMilliseconds;

                if (_isSecLevel)
                {
                    _diffTime = serverTimestamp - currentSeconds;
                    FuLog.Info($"检测为秒级时间戳，服务器与客户端时间差: {_diffTime}秒");
                }
                else
                {
                    _diffTime = serverTimestamp - currentMilliseconds;
                    FuLog.Info($"检测为毫秒级时间戳，服务器与客户端时间差: {_diffTime}毫秒");
                }
            }

            /// <summary>
            /// 服务器今天时间戳(秒/毫秒)
            /// 如果是秒级，返回秒级时间戳，如果是毫秒级，返回毫秒级时间戳
            /// </summary>
            /// <returns></returns>
            public static long ServerToday()
            {
                if (_isSecLevel) return _diffTime + ClientToday();
                return (_diffTime + ClientTodayMs()) / 1000;
            }

            /// <summary>
            /// 服务器当前时间戳(秒/毫秒)
            /// 如果是秒级，返回秒级时间戳，如果是毫秒级，返回毫秒级时间戳
            /// </summary>
            /// <returns></returns>
            public static long ServerNow()
            {
                if (_isSecLevel) return _diffTime + ClientNow();
                return (_diffTime + ClientNowMs()) / 1000;
            }

            /// <summary>
            /// 客户端今天时间戳(秒)
            /// </summary>
            /// <returns></returns>
            public static long ClientToday() => (DateTime.UtcNow.Date.Ticks - EpochTicks) / TimeSpan.TicksPerSecond;

            /// <summary>
            /// 客户端今天时间戳(毫秒)
            /// </summary>
            /// <returns></returns>
            public static long ClientTodayMs() => (DateTime.UtcNow.Date.Ticks - EpochTicks) / TimeSpan.TicksPerMillisecond;

            /// <summary>
            /// 客户端当前时间戳(秒)
            /// </summary>
            /// <returns></returns>
            public static long ClientNow() => (DateTime.UtcNow.Ticks - EpochTicks) / TimeSpan.TicksPerSecond;
            
            /// <summary>
            /// 客户端当前时间戳(毫秒)
            /// </summary>
            /// <returns></returns>
            public static long ClientNowMs() => (DateTime.UtcNow.Ticks - EpochTicks) / TimeSpan.TicksPerMillisecond;
            
            #endregion

            #region 时间转换相关
            
            /// <summary>
            /// 获取指定时间的时间戳(秒)
            /// </summary>
            /// <param name="timeDate">指定时间。</param>
            /// <param name="utc">是否使用UTC时间。</param>
            /// <returns>距离纪元时间的秒数。</returns>
            public static long TimeToTimestamp(DateTime timeDate, bool utc = false)
            {
                if (utc) return (long)(timeDate - UtcEpoch).TotalSeconds;
                return (long)(timeDate - UtcEpoch.ToLocalTime()).TotalSeconds;
            }

            /// <summary>
            /// 获取指定时间的时间戳(毫秒)
            /// </summary>
            /// <param name="timeDate">指定时间。</param>
            /// <param name="utc">是否使用UTC时间。</param>
            /// <returns>距离纪元时间的毫秒数。</returns>
            public static long TimeToTimestampMs(DateTime timeDate, bool utc = false)
            {
                if (utc) return (long)(timeDate - UtcEpoch).TotalMilliseconds;
                return (long)(timeDate - UtcEpoch.ToLocalTime()).TotalMilliseconds;
            }
            
            /// <summary>
            /// Unix时间戳(毫秒)转时间
            /// </summary>
            /// <param name="timestampMs">毫秒时间戳。</param>
            /// <param name="utc">是否使用UTC时间。</param>
            /// <returns>转换后的时间。</returns>
            public static DateTime TimestampMsToTime(long timestampMs, bool utc = false)
            {
                return utc ? UtcEpoch.AddMilliseconds(timestampMs) : UtcEpoch.ToLocalTime().AddMilliseconds(timestampMs);
            }

            /// <summary>
            /// 时间戳(秒)转时间
            /// </summary>
            /// <param name="timestamp">秒时间戳。</param>
            /// <param name="utc">是否使用UTC时间。</param>
            /// <returns>转换后的时间。</returns>
            public static DateTime TimestampToTime(long timestamp, bool utc = false)
            {
                return utc ? UtcEpoch.AddSeconds(timestamp) : UtcEpoch.ToLocalTime().AddSeconds(timestamp);
            }

            /// <summary>
            /// 将秒数转换成TimeSpan时间跨度
            /// </summary>
            /// <param name="seconds">秒</param>
            /// <returns></returns>
            public static TimeSpan SecondsToTimeSpan(int seconds) => TimeSpan.FromSeconds(seconds);
            
            /// <summary>
            /// 将Unix时间戳转换为相对于当前时间的时间间隔。
            /// 即计算当前时间与给定时间戳表示的时间之间的差值。
            /// </summary>
            /// <param name="timestampMs">Unix时间戳（毫秒）</param>
            /// <returns>从指定时间戳到当前时间的时间间隔</returns>
            public static TimeSpan GetElapsedTimeSince(long timestampMs)
            {
                var nowTimestamp = ClientNowMs();
                var timeSpan = TimestampMsToTime(nowTimestamp, true) - TimestampMsToTime(timestampMs, true);
                return timeSpan;
            }

            /// <summary>
            /// 将Unix时间戳转换为相对于本地当前时间的时间间隔。
            /// 即计算本地当前时间与给定时间戳表示的时间之间的差值。
            /// </summary>
            /// <param name="timestampMs">Unix时间戳（毫秒）</param>
            /// <returns>从指定时间戳到当本地前时间的时间间隔</returns>
            public static TimeSpan GetElapsedTimeSinceLocal(long timestampMs)
            {
                var timeSpan = DateTime.Now - TimestampMsToTime(timestampMs, true);
                return timeSpan;
            }
            
            /// <summary>
            /// 将Utc时间戳转换为自公元1年1月1日以来的刻度数。
            /// </summary>
            /// <remarks>
            /// 将Unix毫秒时间戳转换为刻度数，每毫秒等于10000刻度
            /// 621355968000000000是公元1年1月1日至1970年1月1日的刻度数差值
            /// </remarks>
            /// <param name="timestamp">Utc时间戳，从1970年1月1日以来的秒数。</param>
            /// <returns>自公元1年1月1日以来的刻度数。</returns>
            public static long TimestampToTicks(long timestamp)
            {
                return timestamp * 10000000L + 621355968000000000L;
            }

            /// <summary>
            /// 将Unix毫秒时间戳转换为自公元1年1月1日以来的刻度数。
            /// </summary>
            /// <remarks>
            /// 将Unix毫秒时间戳转换为刻度数，每毫秒等于10000刻度
            /// 621355968000000000是公元1年1月1日至1970年1月1日的刻度数差值
            /// </remarks>
            /// <param name="timestampMs">Unix毫秒时间戳，从1970年1月1日以来的毫秒数。</param>
            /// <returns>自公元1年1月1日以来的刻度数。</returns>
            public static long TimestampMillisToTicks(long timestampMs)
            {
                return timestampMs * 10000L + 621355968000000000L;
            }
            
            #endregion

            #region 当前时间相关

            /// <summary>
            /// 获取当前本地时区的日期，格式为yyyyMMdd的整数
            /// </summary>
            /// <returns>返回一个8位整数，表示当前本地时区的日期。例如：20231225表示2023年12月25日</returns>
            public static int NowLocalDate() => Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));

            /// <summary>
            /// 获取当前UTC时区的日期，格式为yyyyMMdd的整数
            /// </summary>
            /// <returns>返回一个8位整数，表示当前UTC时区的日期。例如：20231225表示2023年12月25日</returns>
            public static int NowUtcDate() => Convert.ToInt32(DateTime.UtcNow.ToString("yyyyMMdd"));

            /// <summary>
            /// 获取当前UTC时间，格式为HHmmss的整数
            /// </summary>
            /// <returns>返回一个6位整数，表示当前UTC时间。例如：143045表示14:30:45</returns>
            public static int NowUtcTime() => Convert.ToInt32(NowUtcTimeStr());

            /// <summary>
            /// 获取当前本地时间，格式为HHmmss的整数
            /// </summary>
            /// <returns>返回一个6位整数，表示当前本地时间。例如：143045表示14:30:45</returns>
            public static int NowLocalTime() => Convert.ToInt32(NowLocalTimeStr());

            /// <summary>
            /// 获取当前UTC时间，格式为HHmmss的字符串
            /// </summary>
            /// <returns>返回一个6位字符串，表示当前UTC时间。例如：143045表示14:30:45</returns>
            public static string NowUtcTimeStr() => DateTime.UtcNow.ToString("HHmmss");

            /// <summary>
            /// 获取当前本地时间，格式为HHmmss的字符串
            /// </summary>
            /// <returns>返回一个6位字符串，表示当前本地时间。例如：143045表示14:30:45</returns>
            public static string NowLocalTimeStr() => DateTime.Now.ToString("HHmmss");

            /// <summary>
            /// 获取当前本地时区时间的完整格式字符串
            /// </summary>
            /// <returns>返回格式为"yyyy-MM-dd-HH-mm-ss.fff K"的时间字符串，包含年-月-日-时-分-秒.毫秒 时区偏移。例如："2023-12-25-14-30-45.123 +08:00"</returns>
            public static string NowLocalDateTimeStr() => DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss.fff K");

            /// <summary>
            /// 获取当前UTC时区时间的完整格式字符串
            /// </summary>
            /// <returns>返回格式为"yyyy-MM-dd-HH-mm-ss.fff K"的UTC时间字符串，包含年-月-日-时-分-秒.毫秒 时区偏移。例如："2023-12-25-06-30-45.123 +00:00"</returns>
            public static string NowUtcDateTimeStr() => DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss.fff K");

            #endregion

            #region 跨越天数相关

            /// <summary>
            /// 获取指定时间戳到当前UTC时间之间跨越的天数。
            /// </summary>
            /// <param name="beginTimestamp">起始时间戳,从1970年1月1日以来经过的秒数。</param>
            /// <param name="hour">小时。</param>
            /// <returns>跨越的天数。</returns>
            public static int GetCrossDaysToNowUtc(long beginTimestamp, int hour = 0)
            {
                var begin = TimestampToTime(beginTimestamp, true);
                return GetCrossDaysToNowUtc(begin, hour);
            }

            /// <summary>
            /// 获取从指定日期到当前UTC日期之间跨越的天数。
            /// </summary>
            /// <param name="startTime">起始日期。</param>
            /// <param name="hour">小时。</param>
            /// <returns>跨越的天数。</returns>
            public static int GetCrossDaysToNowUtc(DateTime startTime, int hour = 0) => GetCrossDays(startTime, DateTime.UtcNow, hour);

            /// <summary>
            /// 获取两个UTC时间戳之间跨越的天数。
            /// </summary>
            /// <param name="beginTimestamp">开始时间戳(秒)，从1970年1月1日以来经过的秒数。</param>
            /// <param name="afterTimestamp">结束时间戳(秒)，从1970年1月1日以来经过的秒数。</param>
            /// <param name="hour">小时。</param>
            /// <returns>跨越的天数。</returns>
            public static int GetCrossDaysUtc(long beginTimestamp, long afterTimestamp, int hour = 0)
            {
                var begin = TimestampToTime(beginTimestamp, true);
                var after = TimestampToTime(afterTimestamp, true);
                return GetCrossDays(begin, after, hour);
            }

            
            /// <summary>
            /// 获取从指定日期到当前本地日期之间跨越的天数。
            /// </summary>
            /// <param name="beginTimestamp">起始日期。</param>
            /// <param name="hour">小时。</param>
            /// <returns>跨越的天数。</returns>
            public static int GetCrossDaysToNowLocal(long beginTimestamp, int hour = 0)
            {
                var begin = TimestampToTime(beginTimestamp);
                return GetCrossDaysToNowUtc(begin, hour);
            }
            
            /// <summary>
            /// 获取从指定日期到当前本地日期之间跨越的天数。
            /// </summary>
            /// <param name="startTime">起始日期。</param>
            /// <param name="hour">小时。</param>
            /// <returns>跨越的天数。</returns>
            public static int GetCrossDaysToNowLocal(DateTime startTime, int hour = 0) => GetCrossDays(startTime, DateTime.Now, hour);

            /// <summary>
            /// 获取两个本地时间戳之间的跨越的天数
            /// </summary>
            /// <param name="startTimestamp">开始时间戳(秒)</param>
            /// <param name="endTimestamp">结束时间戳(秒)</param>
            /// <returns>跨越的天数</returns>
            public static int GetCrossDaysLocal(long startTimestamp, long endTimestamp)
            {
                var startTime = TimestampToTime(startTimestamp);
                var endTime = TimestampToTime(endTimestamp);
                return GetCrossDays(startTime, endTime);
            }

            
            /// <summary>
            /// 获取两个日期之间跨越的天数。
            /// </summary>
            /// <param name="startTime">起始日期。</param>
            /// <param name="endTime">结束日期。</param>
            /// <param name="hour">小时。</param>
            /// <returns>跨越的天数。</returns>
            public static int GetCrossDays(DateTime startTime, DateTime endTime, int hour = 0)
            {
                var days = (int)(endTime.Date - startTime.Date).TotalDays;
                if (startTime.Hour < hour) days++;
                if (endTime.Hour < hour) days--;
                return days;
            }

            #endregion

            #region 天相关

            /// <summary>
            /// 按照UTC时间判断两个时间戳是否是同一天
            /// </summary>
            /// <param name="timestamp1">时间戳1</param>
            /// <param name="timestamp2">时间戳2</param>
            /// <returns>是否是同一天</returns>
            public static bool IsSameDayUtc(long timestamp1, long timestamp2)
            {
                var time1 = TimestampToTime(timestamp1, true);
                var time2 = TimestampToTime(timestamp2, true);
                return IsSameDay(time1, time2);
            }

            /// <summary>
            /// 按照本地时间判断两个时间戳是否是同一天
            /// </summary>
            /// <param name="timestamp1">时间戳1</param>
            /// <param name="timestamp2">时间戳2</param>
            /// <returns>是否是同一天</returns>
            public static bool IsSameDayLocal(long timestamp1, long timestamp2)
            {
                var time1 = TimestampToTime(timestamp1);
                var time2 = TimestampToTime(timestamp2);
                return IsSameDay(time1, time2);
            }

            /// <summary>
            /// 判断两个时间是否是同一天
            /// </summary>
            /// <param name="time1">时间1</param>
            /// <param name="time2">时间2</param>
            /// <returns>是否是同一天</returns>
            public static bool IsSameDay(DateTime time1, DateTime time2)
            {
                return time1.Date.Year == time2.Date.Year && time1.Date.Month == time2.Date.Month && time1.Date.Day == time2.Date.Day;
            }

            /// <summary>
            /// 获取今天开始时间
            /// </summary>
            /// <returns>今天零点时间</returns>
            public static DateTime GetStartTimeToday() => DateTime.Today;

            /// <summary>
            /// 获取今天开始时间戳
            /// </summary>
            /// <returns>今天零点时间戳(秒)</returns>
            public static long GetStartTimestampToday() => new DateTimeOffset(GetStartTimeToday()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取今天结束时间
            /// </summary>
            /// <returns>今天23:59:59的时间</returns>
            public static DateTime GetEndTimeToday() => DateTime.Today.AddDays(1).AddSeconds(-1);

            /// <summary>
            /// 获取今天结束时间戳
            /// </summary>
            /// <returns>今天23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampToday() => new DateTimeOffset(GetEndTimeToday()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取明天开始时间
            /// </summary>
            /// <returns>明天零点时间</returns>
            public static DateTime GetStartTimeTomorrow() => DateTime.Today.AddDays(1);

            /// <summary>
            /// 获取明天开始时间戳
            /// </summary>
            /// <returns>明天零点时间戳(秒)</returns>
            public static long GetStartTimestampTomorrow() => new DateTimeOffset(GetStartTimeTomorrow()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取明天结束时间
            /// </summary>
            /// <returns>明天23:59:59的时间</returns>
            public static DateTime GetEndTimeTomorrow() => DateTime.Today.AddDays(2).AddSeconds(-1);

            /// <summary>
            /// 获取明天结束时间戳
            /// </summary>
            /// <returns>明天23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampTomorrow() => new DateTimeOffset(GetEndTimeTomorrow()).ToUnixTimeSeconds();

            #endregion

            #region 周相关

            /// <summary>
            /// 判断当前时间是否与指定时间处于同一周。
            /// </summary>
            /// <param name="targetTime">指定时间。</param>
            /// <returns>如果当前时间与指定时间处于同一周，则为 true；否则为 false。</returns>
            public static bool IsSameWeekNow(long targetTime) => IsSameWeekNow(new DateTime(targetTime));

            /// <summary>
            /// 判断当前时间是否与指定时间处于同一周。
            /// </summary>
            /// <param name="targetDate">指定时间。</param>
            /// <returns>如果当前时间与指定时间处于同一周，则为 true；否则为 false。</returns>
            public static bool IsSameWeekNow(DateTime targetDate) => IsSameWeek(targetDate, DateTime.Now);

            /// <summary>
            /// 判断两个时间是否处于同一周。
            /// </summary>
            /// <param name="startDate">起始时间。</param>
            /// <param name="endDate">结束时间。</param>
            /// <returns>如果两个时间处于同一周，则为 true；否则为 false。</returns>
            public static bool IsSameWeek(DateTime startDate, DateTime endDate)
            {
                // 让start是较早的时间
                if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

                var dayOfWeek = (int)startDate.DayOfWeek;
                if (dayOfWeek == (int)DayOfWeek.Sunday) dayOfWeek = 7;

                // 获取较早时间所在周的周天的0点
                var startsWeekLastDate = startDate.AddDays(7 - dayOfWeek).Date;

                // 判断end是否在start所在周
                return startsWeekLastDate >= endDate.Date;
            }

            /// <summary>
            /// 获取在指定日期所在周中指定周几的日期。
            /// 如：传入2022年1月1日，周五，则返回2022年1月5日。
            /// </summary>
            /// <param name="dateTime">指定日期。</param>
            /// <param name="day">周几。</param>
            /// <returns>在指定日期所在周中指定周几的日期。</returns>
            public static DateTime GetDateOfWeek(DateTime dateTime, DayOfWeek day)
            {
                var tempDay = (int)day;
                if (tempDay == 0) tempDay = 7;

                var dayOfWeek = (int)dateTime.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;

                return dateTime.AddDays(tempDay - dayOfWeek).Date;
            }

            /// <summary>
            /// 获取当前日期所在周中指定周几的日期。
            /// 如：传入周一，则返回当前日期所在周的周一的日期。
            /// </summary>
            /// <param name="day">周几。</param>
            /// <returns>当前日期所在周中指定周几的日期。</returns>
            public static DateTime GetNowDateOfWeek(DayOfWeek day) => GetDateOfWeek(DateTime.Now, day);

            /// <summary>
            /// 将DayOfWeek转换为中国习惯的星期数字
            /// </summary>
            /// <param name="dayOfWeek">系统星期枚举</param>
            /// <returns>中国星期数字(1=周一，7=周日)</returns>
            private static int GetChinaDayOfWeek(DayOfWeek dayOfWeek)
            {
                return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
            }
            
            /// <summary>
            /// 获取指定日期在中国对应的星期数字(1-7，1=周一，7=周日)
            /// </summary>
            /// <param name="date">要检查的日期，默认为当前时间</param>
            /// <returns>星期数字(1-7)</returns>
            public static int GetChinaWeekdayNumber(DateTime? date = null)
            {
                var targetDate = date ?? DateTime.Now;
                return GetChinaDayOfWeek(targetDate.DayOfWeek);
            }

            /// <summary>
            /// 获取本周开始时间
            /// </summary>
            /// <returns>本周一零点时间</returns>
            public static DateTime GetStartTimeThisWeek()
            {
                var now = DateTime.Now;
                var dayOfWeek = (int)now.DayOfWeek;
                dayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek;
                return now.AddDays(1 - dayOfWeek).Date;
            }

            /// <summary>
            /// 获取本周开始时间戳
            /// </summary>
            /// <returns>本周一零点时间戳(秒)</returns>
            public static long GetStartTimestampThisWeek() => new DateTimeOffset(GetStartTimeThisWeek()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取本周结束时间
            /// </summary>
            /// <returns>本周日23:59:59的时间</returns>
            public static DateTime GetEndTimeThisWeek() => GetStartTimeThisWeek().AddDays(7).AddSeconds(-1);

            /// <summary>
            /// 获取本周结束时间戳
            /// </summary>
            /// <returns>本周日23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampThisWeek() => new DateTimeOffset(GetEndTimeThisWeek()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取下周开始时间
            /// </summary>
            /// <returns>下周一零点时间</returns>
            public static DateTime GetStartTimeNextWeek() => GetStartTimeThisWeek().AddDays(7);

            /// <summary>
            /// 获取下周开始时间戳
            /// </summary>
            /// <returns>下周一零点时间戳(秒)</returns>
            public static long GetStartTimestampNextWeek() => new DateTimeOffset(GetStartTimeNextWeek()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取下周结束时间
            /// </summary>
            /// <returns>下周日23:59:59的时间</returns>
            public static DateTime GetEndTimeNextWeek() => GetStartTimeNextWeek().AddDays(7).AddSeconds(-1);

            /// <summary>
            /// 获取下周结束时间戳
            /// </summary>
            /// <returns>下周日23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampNextWeek() => new DateTimeOffset(GetEndTimeNextWeek()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期所在周的开始时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在周周一零点时间</returns>
            public static DateTime GetStartTimeOfWeek(DateTime date)
            {
                var dayOfWeek = (int)date.DayOfWeek;
                dayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek;
                return date.AddDays(1 - dayOfWeek).Date;
            }

            /// <summary>
            /// 获取指定日期所在周的开始时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在周周一零点时间戳(秒)</returns>
            public static long GetStartTimestampOfWeek(DateTime date) => new DateTimeOffset(GetStartTimeOfWeek(date)).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期所在周的结束时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在周周日23:59:59的时间</returns>
            public static DateTime GetEndTimeOfWeek(DateTime date) => GetStartTimeOfWeek(date).AddDays(7).AddSeconds(-1);

            /// <summary>
            /// 获取指定日期所在周的结束时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在周周日23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampOfWeek(DateTime date) => new DateTimeOffset(GetEndTimeOfWeek(date)).ToUnixTimeSeconds();

            #endregion

            #region 月相关

            /// <summary>
            /// 获取本月开始时间
            /// </summary>
            /// <returns>本月1号零点时间</returns>
            public static DateTime GetStartTimeThisMonth() => new(DateTime.Now.Year, DateTime.Now.Month, 1);

            /// <summary>
            /// 获取本月开始时间戳
            /// </summary>
            /// <returns>本月1号零点时间戳(秒)</returns>
            public static long GetStartTimestampThisMonth() => new DateTimeOffset(GetStartTimeThisMonth()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取本月结束时间
            /// </summary>
            /// <returns>本月最后一天23:59:59的时间</returns>
            public static DateTime GetEndTimeThisMonth() => GetStartTimeThisMonth().AddMonths(1).AddSeconds(-1);

            /// <summary>
            /// 获取本月结束时间戳
            /// </summary>
            /// <returns>本月最后一天23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampThisMonth() => new DateTimeOffset(GetEndTimeThisMonth()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取下月开始时间
            /// </summary>
            /// <returns>下月1号零点时间</returns>
            public static DateTime GetStartTimeNextMonth() => GetStartTimeThisMonth().AddMonths(1);

            /// <summary>
            /// 获取下月开始时间戳
            /// </summary>
            /// <returns>下月1号零点时间戳(秒)</returns>
            public static long GetStartTimestampNextMonth() => new DateTimeOffset(GetStartTimeNextMonth()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取下月结束时间
            /// </summary>
            /// <returns>下月最后一天23:59:59的时间</returns>
            public static DateTime GetEndTimeNextMonth() => GetStartTimeNextMonth().AddMonths(1).AddSeconds(-1);

            /// <summary>
            /// 获取下月结束时间戳
            /// </summary>
            /// <returns>下月最后一天23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampNextMonth() => new DateTimeOffset(GetEndTimeNextMonth()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期所在月的开始时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在月1号零点时间</returns>
            public static DateTime GetStartTimeOfMonth(DateTime date) => new(date.Year, date.Month, 1);

            /// <summary>
            /// 获取指定日期所在月的开始时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在月1号零点时间戳(秒)</returns>
            public static long GetStartTimestampOfMonth(DateTime date) => new DateTimeOffset(GetStartTimeOfMonth(date)).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期所在月的结束时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在月最后一天23:59:59的时间</returns>
            public static DateTime GetEndTimeOfMonth(DateTime date) => GetStartTimeOfMonth(date).AddMonths(1).AddSeconds(-1);

            /// <summary>
            /// 获取指定日期所在月的结束时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在月最后一天23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampOfMonth(DateTime date) => new DateTimeOffset(GetEndTimeOfMonth(date)).ToUnixTimeSeconds();

            #endregion

            #region 年相关

            /// <summary>
            /// 获取本年开始时间
            /// </summary>
            /// <returns>本年1月1日零点时间</returns>
            public static DateTime GetStartTimeThisYear() => new(DateTime.Now.Year, 1, 1);

            /// <summary>
            /// 获取本年开始时间戳
            /// </summary>
            /// <returns>本年1月1日零点时间戳(秒)</returns>
            public static long GetStartTimestampThisYear() => new DateTimeOffset(GetStartTimeThisYear()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取本年结束时间
            /// </summary>
            /// <returns>本年12月31日23:59:59的时间</returns>
            public static DateTime GetEndTimeThisYear() => GetStartTimeThisYear().AddYears(1).AddSeconds(-1);

            /// <summary>
            /// 获取本年结束时间戳
            /// </summary>
            /// <returns>本年12月31日23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampThisYear() => new DateTimeOffset(GetEndTimeThisYear()).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期所在年的开始时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在年1月1日零点时间</returns>
            public static DateTime GetStartTimeOfYear(DateTime date) => new(date.Year, 1, 1);

            /// <summary>
            /// 获取指定日期所在年的开始时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在年1月1日零点时间戳(秒)</returns>
            public static long GetStartTimestampOfYear(DateTime date) => new DateTimeOffset(GetStartTimeOfYear(date)).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期所在年的结束时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在年12月31日23:59:59的时间</returns>
            public static DateTime GetEndTimeOfYear(DateTime date) => GetStartTimeOfYear(date).AddYears(1).AddSeconds(-1);

            /// <summary>
            /// 获取指定日期所在年的结束时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>所在年12月31日23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampOfYear(DateTime date) => new DateTimeOffset(GetEndTimeOfYear(date)).ToUnixTimeSeconds();

            #endregion

            #region 指定日期相关

            /// <summary>
            /// 获取指定日期的开始时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>指定日期零点时间</returns>
            public static DateTime GetStartTimeOfDay(DateTime date) => date.Date;

            /// <summary>
            /// 获取指定日期的开始时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>指定日期零点时间戳(秒)</returns>
            public static long GetStartTimestampOfDay(DateTime date) => new DateTimeOffset(GetStartTimeOfDay(date)).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定日期的结束时间
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>指定日期23:59:59的时间</returns>
            public static DateTime GetEndTimeOfDay(DateTime date) => date.Date.AddDays(1).AddSeconds(-1);

            /// <summary>
            /// 获取指定日期的结束时间戳
            /// </summary>
            /// <param name="date">指定日期</param>
            /// <returns>指定日期23:59:59的时间戳(秒)</returns>
            public static long GetEndTimestampOfDay(DateTime date) => new DateTimeOffset(GetEndTimeOfDay(date)).ToUnixTimeSeconds();

            /// <summary>
            /// 获取指定时间是否在指定的时间范围内
            /// </summary>
            /// <param name="time">指定时间</param>
            /// <param name="startTime">开始时间</param>
            /// <param name="endTime">结束时间</param>
            /// <returns>是否在范围内</returns>
            public static bool IsTimeInRange(DateTime time, DateTime startTime, DateTime endTime)
            {
                return time >= startTime && time <= endTime;
            }

            /// <summary>
            /// 获取指定时间戳是否在指定的时间戳范围内
            /// </summary>
            /// <param name="timestamp">指定时间戳</param>
            /// <param name="startTimestamp">开始时间戳</param>
            /// <param name="endTimestamp">结束时间戳</param>
            /// <returns>是否在范围内</returns>
            public static bool IsTimestampInRange(long timestamp, long startTimestamp, long endTimestamp)
            {
                return timestamp >= startTimestamp && timestamp <= endTimestamp;
            }

            #endregion
        }
    }
}