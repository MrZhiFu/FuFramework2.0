using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS
// using UnitySDK;
using System.Runtime.InteropServices;
#endif

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 防止时间作弊系统。
    /// 注意，方案已知漏洞如下：
    /// 1、断网用户退出游戏超过15分钟再修改时间，无法判断是否作弊
    /// 2、断网用户重启手机后修改时间，无法判断是否作弊
    /// 3、断网用户第二次重启手机后，打开游戏时，如果开机时间与上次获取的开机时间刚好在15分钟内，会被判断为作弊。
    /// 4、断网用户如果超过49.8天没关机，会每49.8天有一次作弊机会。
    /// </summary>
    public static class AntiCheatingTime
    {
        #region 外部调用

        /// <summary>
        /// 获取系统启动后经过的毫秒数，如果返回零则获取失败（概率约等于0）
        /// (强烈建议不要每帧调用，会有性能问题)
        /// </summary>
        /// <returns>系统启动后经过的毫秒数</returns>
        public static long TickCountMs
        {
            get
            {
#if NET_CORE
            //Unity支持的.Net版本无法使用
            return Environment.TickCount64;
#elif UNITY_EDITOR
                return GetTickCount();
#elif UNITY_IOS
            return GetTickCount();
#else
                return GetAndroidTickCount();
#endif
            }
        }

        /// <summary>
        /// 获取系统启动后经过的秒数
        /// (强烈建议不要每帧调用，会有性能问题)
        /// </summary>
        /// <returns>系统启动后经过的秒数</returns>
        public static long TickCount => TickCountMs / 1000;

        /// <summary>
        /// 是否需要在未获得网络时间时重新查找网络时间(用于在未获取到在线时间时判断是否需要重新获取)
        /// </summary>
        public static bool NeedRecheckNetTime = true;

        /// <summary>
        /// 网络时间重查间隔(秒), 默认5分钟
        /// </summary>
        public static float NetTimeRecheckInterval = 300f;

        /// <summary>
        /// 是否已经获取到了线上时间
        /// </summary>
        public static bool GotNetTime => _gotNetTime;

        /// <summary>
        /// 是否已经尝试获得过在线时间(用于判断在线参数获取是否已经结束)
        /// </summary>
        public static bool CheckedNetTime => _checkedNetTime;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="useNetTime">是否使用网络时间</param>
        public static void Init(bool useNetTime = true)
        {
            ShowDebug("初始化时间防作弊组件");
            _useNetTime = useNetTime;

            // 如果启用网络时间，开始获取网络时间
            if (_useNetTime)
                MultipleNptGetTime();
        }

        /// <summary>
        /// 注册获取时间时的回调
        /// </summary>
        /// <param name="online"></param>
        /// <param name="offline"></param>
        /// <param name="cheat"></param>
        public static void Register(Action online, Action offline, Action cheat)
        {
            ActionDic[ActionType.Online] = online;
            ActionDic[ActionType.OffLine] = offline;
            ActionDic[ActionType.Cheat] = cheat;
        }

        /// <summary>
        /// 取消注册获取时间时的回调
        /// </summary>
        public static void UnRegisterAll()
        {
            ActionDic[ActionType.Online] = null;
            ActionDic[ActionType.OffLine] = null;
            ActionDic[ActionType.Cheat] = null;
        }

        /// <summary>
        /// 获取本地时区时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetLocalTime()
        {
            var utcTime = GetUtcTime();
            var tempTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local); //完成转换
            ShowDebug($"获取本地时区时间: {tempTime}");
            return tempTime;
        }

        /// <summary>
        /// 获取当前时间(UTC)
        /// </summary>
        /// <returns></returns>
        public static DateTime GetUtcTime()
        {
            if (_forbidCheck) return DateTime.UtcNow;

            // 使用缓存优化，1秒内避免重复计算
            long currentTick = TickCount;
            if (Math.Abs(currentTick - _lastTickCount) < 1) // 1秒内使用缓存
            {
                ShowDebug($"使用缓存时间，当前Utc时间：{_cachedTime}");
                return _cachedTime;
            }

            DateTime result;

            // 系统启动后经过的秒数
            var tickTime = TickCount;

            // 是否已经获得过网络时间
            if (_gotNetTime)
            {
                // 获取上次记录的系统启动时间
                var lastTickTime = GetSavedTime(LastTickTimeKey);
                if (lastTickTime <= 0)
                {
                    // 特殊情况，初始化后，记录的数据被删除了（这种情况下需要项目开发人员查看是否在初始化后调用了 PlayerPrefs.DeleteAll
                    ShowDebug($"首次运行或记录的数据被删除了，重新保存系统启动后经过的秒数{tickTime}s");
                    SaveTime(LastTickTimeKey, tickTime);
                    lastTickTime = tickTime;
                }

                _nowOnlineDateTime = _nowOnlineDateTime.AddSeconds(tickTime - lastTickTime);
                SaveTime(LastUtcTimeKey, Time2Timestamp(_nowOnlineDateTime));
                SaveTime(LastTickTimeKey, tickTime);
                ActionDic[ActionType.Online]?.Invoke();
                result = _nowOnlineDateTime;
                ShowDebug($"使用网络时间刷新时间记录点，时间可靠，当前Utc时间：{result}");
            }
            else
            {
                var lastTickTime = GetSavedTime(LastTickTimeKey);
                if (lastTickTime <= 0)
                {
                    // 特殊情况，初始化后，记录的数据被删除了(这种情况下需要项目开发人员查看是否在本脚本初始化后调用了 PlayerPrefs.DeleteAll)
                    ShowDebug($"首次运行或记录的数据被删除了，重新保存系统启动后经过的秒数{tickTime}s");
                    SaveTime(LastTickTimeKey, tickTime);
                    lastTickTime = tickTime;
                }

                var now = DateTime.UtcNow;
                var nowTimestamp = Time2Timestamp(now);

                var lastTimestamp = GetSavedTime(LastUtcTimeKey);
                if (lastTimestamp < 0) lastTimestamp = 0;

                var tickTimeDelta = tickTime - lastTickTime; // 获取本次启动和上次启动的系统启动时间的差值
                ShowDebug($"本次系统启动时间: {tickTime}s，上次系统启动时间: {lastTickTime}s, 两者差值: {tickTimeDelta}s");

                var localTimeDelta = nowTimestamp - lastTimestamp; // 获取本次启动和上次启动的本地时间的差值
                ShowDebug($"本次启动本地时间戳: {nowTimestamp}s，上次启动本地时间戳: {lastTimestamp}s, 两者差值: {localTimeDelta}s");

                var delta = Math.Abs(localTimeDelta - tickTimeDelta);
                if (delta < 60 || tickTimeDelta <= 0)
                {
                    if (delta < 60)
                        ShowDebug($"使用离线本地时间，系统启动时间与时间戳差值为{delta}s，低于1分钟，时间可靠，当前Utc时间：{now}");
                    if (tickTimeDelta <= 0)
                        ShowDebug($"本次启动和上次启动的系统启动时间的差值<=0，首次运行或记录的数据被删除了或系统重启了，时间可靠，当前Utc时间：{now}");

                    SaveTime(LastUtcTimeKey, nowTimestamp);
                    SaveTime(LastTickTimeKey, tickTime);
                    ActionDic[ActionType.OffLine]?.Invoke();
                    TryRecheckNetTime();
                    result = now;
                }
                else
                {
                    // 说明时间不可靠,返回纠正后的时间
                    ActionDic[ActionType.Cheat]?.Invoke();
                    result = now.AddSeconds(-(localTimeDelta - tickTimeDelta));
                    TryRecheckNetTime();
                    ShowDebug($"时间不可靠，纠正后的当前时间：{result}");
                }
            }

            // 更新缓存
            _lastTickCount = currentTick;
            _cachedTime = result;

            return result;
        }

        /// <summary>
        /// 应用退出时调用(需要自行设置位置)
        /// </summary>
        public static void OnApplicationQuit()
        {
            var now = GetUtcTime();
            var nowTimestamp = Time2Timestamp(now);
            SaveTime(LastUtcTimeKey, nowTimestamp);
            SaveTime(LastTickTimeKey, TickCount);
            Debug.Log($"AntiCheatingTime: 离开游戏最后保存在本地的Utc时间：{now}, 时间戳：{nowTimestamp}, 系统启动时间：{TickCount}");
        }

        /// <summary>
        /// 禁止时间检测（方便测试用，正式版请不要调用）
        /// </summary>
        /// <param name="forbid"></param>
        public static void ForbidCheck(bool forbid)
        {
            _forbidCheck = forbid;
        }

        public static bool IsForbidCheck => _forbidCheck;

        #endregion

        #region 内部方法

        private const string LastTickTimeKey = "LastTickTime"; //最后一次存储的系统启动时间记录key
        private const string LastUtcTimeKey = "LastUtcTime"; //最后一次存储在本地的Utc时间戳记录key

        private static bool _forbidCheck; //禁止检测（方便测试）
        private static DateTime _cachedTime; // 缓存的时间结果
        private static long _lastTickCount; // 上次计算时的系统启动时间（秒）
        private static bool _useNetTime; //是否使用在线时间
        private static bool _gotNetTime; //是否得到了在线时间
        private static bool _checkedNetTime; //获得在线时间是否结束
        private static DateTime _nowOnlineDateTime; //当前在线时间

        private static float _lastRecheckTime; // 上次重查网络时间时的时间
        private static int _recheckAttemptCount; // 重查网络时间的尝试次数
        private const int MaxRecheckAttempts = 3; // 重查网络时间的最大尝试次数

        private enum ActionType
        {
            Online, // 使用在线时间
            OffLine, // 使用离线本地时间
            Cheat, // 作弊了
        }

        private static readonly Dictionary<ActionType, Action> ActionDic = new()
        {
            { ActionType.Online, null },
            { ActionType.OffLine, null },
            { ActionType.Cheat, null }
        };

        private static int _checkCount;

        /// <summary>
        /// 尝试重新获取网络时间（带频率限制和重试机制）
        /// </summary>
        private static void TryRecheckNetTime()
        {
            if (!NeedRecheckNetTime || _gotNetTime) return;

            // 检查重查频率限制
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastRecheckTime < NetTimeRecheckInterval) return;

            // 检查重试次数限制
            if (_recheckAttemptCount >= MaxRecheckAttempts)
            {
                ShowDebug("已达到最大网络时间重试次数，停止重试");
                return;
            }

            _lastRecheckTime = currentTime;
            _recheckAttemptCount++;

            ShowDebug($"第{_recheckAttemptCount}次尝试重新获取网络时间");
            MultipleNptGetTime();
        }

        /// <summary>
        /// 重置网络时间重查状态（在成功获取网络时间后调用）
        /// </summary>
        private static void ResetRecheckState()
        {
            _recheckAttemptCount = 0;
            _lastRecheckTime = 0f;
        }

        #region 多npt服务器地址获取全球时间方法

        /// <summary>
        /// 多地址获取网络时间方法
        /// </summary>
        private static async void MultipleNptGetTime()
        {
            string[] ntpServers =
            {
                "time.google.com", //国际通用地址 Google 提供的 NTP 服务
                "time.windows.com", //国际用地址 微软提供的 NTP 服务
                "time.apple.com", //国际用地址 苹果提供的 NTP 服务
                "time1.aliyun.com", //中国区 阿里云提供的 NTP 服务
                "ntp.ntsc.ac.cn", //中国区 中国科学院国家授时中心
                "ntp1.inrim.it", //意大利国家计量研究所
                "ntp.nict.jp", // 日本国立信息通信研究所
                "time.nist.gov", // 美国国家标准与技术研究院
                "us.pool.ntp.org", // 美国地区的 NTP 服务器池
            };
            try
            {
                var time = await GetFirstAvailableTimestampAsync(ntpServers);
                SetOnlineTime(time);
            }
            catch (Exception ex)
            {
                ShowDebug($"Error:未能获取网络时间: {ex.Message}");
            }
            finally
            {
                _checkedNetTime = true;
            }
        }

        /// <summary>
        /// 获取第一个可用的 NTP 服务器的时间戳
        /// </summary>
        /// <param name="servers"></param>
        /// <param name="sendTimeout"></param>
        /// <param name="receiveTimeout"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task<DateTimeOffset> GetFirstAvailableTimestampAsync(string[] servers, int sendTimeout = 3000, int receiveTimeout = 3000)
        {
            foreach (var server in servers)
            {
                try
                {
                    ShowDebug($"正在尝试从服务器 {server} 获取时间...");
                    var result = await RequestTimestampFromNtpAsync(server, sendTimeout, receiveTimeout, CancellationToken.None);

                    if (result.error == null)
                    {
                        ShowDebug($"成功从服务器 {server} 获取时间");
                        return result.timestamp;
                    }

                    ShowDebug($"服务器 {server} 获取时间失败: {result.error.Message}");
                }
                catch (Exception ex)
                {
                    ShowDebug($"服务器 {server} 请求异常: {ex.Message}");
                }

                // 可选：在服务器之间添加短暂延迟
                await Task.Delay(100);
            }

            throw new Exception("无法从任何提供的 NTP 服务器获取时间");
        }

        /// <summary>
        /// 向 NTP 服务器请求时间戳
        /// </summary>
        /// <param name="server"></param>
        /// <param name="sendTimeout"></param>
        /// <param name="receiveTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task<(DateTimeOffset timestamp, Exception error)> RequestTimestampFromNtpAsync(string server, int sendTimeout,
            int receiveTimeout,
            CancellationToken cancellationToken)
        {
            try
            {
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

                // 使用 Dns.GetHostEntryAsync 获取服务器 IP 地址
                var addresses = (await Dns.GetHostEntryAsync(server)).AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
                if (!addresses.Any()) throw new Exception("无法解析 NTP 服务器地址");

                var ipEndPoint = new IPEndPoint(addresses[0], 123); // NTP 默认端口
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SendTimeout = sendTimeout;
                socket.ReceiveTimeout = receiveTimeout;
                await socket.ConnectAsync(ipEndPoint);
                if (!socket.Connected) throw new Exception("未连接到 NTP 服务器");
                await socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None, cancellationToken);
                await socket.ReceiveAsync(new ArraySegment<byte>(ntpData), SocketFlags.None, cancellationToken);

                // 解析 NTP 时间戳
                var iPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | ntpData[43];
                var fPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | ntpData[47];
                var milliseconds = iPart * 1000L + fPart * 1000L / 0x100000000L;

                // NTP 时间戳是从 1900-01-01 开始计算的
                var epochStart = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var now = epochStart.AddMilliseconds(milliseconds); //DateTime
                var res = new DateTimeOffset(now); //转化为DateTImeOffset
                ShowDebug($"获取网络Utc时间成功:server:{server}, DateTime:{now}");

                // var localTime =  TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Local);
                // ShowDebug($"Suc:server:{server} 获取到了时间 DateTime:{now} local:{localTime} DateTimeOff:{res.UtcDateTime}");
                return (res, null);
            }
            catch (Exception e)
            {
                ShowDebug($"Error:server:{server} msg:{e.Message}");
                return (default, e);
            }
        }

        /// <summary>
        /// 设置为在线网络时间
        /// </summary>
        /// <param name="offTime"></param>
        private static void SetOnlineTime(DateTimeOffset offTime)
        {
            if (_gotNetTime) return;
            _nowOnlineDateTime = offTime.UtcDateTime;
            _gotNetTime = true;
            _checkedNetTime = true;
            SaveTime(LastUtcTimeKey, Time2Timestamp(_nowOnlineDateTime));
            SaveTime(LastTickTimeKey, TickCount);

            // 重置重查状态
            ResetRecheckState();
            ShowDebug("成功获取网络时间，重置重查状态");

            //添加一个更新时间的处理方法
            // TimeUtility.UpdateTime();
        }

        #endregion

        /// <summary>
        /// 获取系统启动时间(毫秒) 49.8天后重置
        /// </summary>
        /// <returns></returns>
        private static long GetTickCount()
        {
            long tickCount = -1;
#if UNITY_IOS && !UNITY_EDITOR
            //此方法获取的时间有时候不准确，需要确定。不可用时就需要通过原生端来获取。
            tickCount = _GetUpTime() * 1000;
            //这里是单独实现ios的方法，一般需要通过xcode来获取。具体实现方式
            // var data = UnitySDK.UnityAgent.CallNativeReturn<DoubleNativeData>(SystemDefine.IOSGetSystemOpenTime);
            // tickCount = (long)data.Data * 1000;
            ShowDebug("获取IOS端系统启动时间(毫秒):" + tickCount);
#else
            //由于 TickCount 属性值的值是32位有符号整数，因此，如果系统连续运行，TickCount 将从零递增到 Int32.MaxValue 大约24.9 天，
            //然后跳转到 Int32.MinValue，这是一个负数，然后在下一个24.9 天内递增为零
            //这里将负数转为整数，一轮周期就变为49.8天
            tickCount = Environment.TickCount;
            if (tickCount < 0)
            {
                tickCount = tickCount - int.MinValue + int.MaxValue;
            }

            ShowDebug("获取系统启动时间(毫秒):" + tickCount);
#endif
            return tickCount;
        }
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern long _GetUpTime();
#endif
        /// <summary>
        /// 获取安卓端系统启动时间(毫秒)(必须在安卓上机上才有用) 无重置
        /// </summary>
        /// <returns></returns>
        private static long GetAndroidTickCount()
        {
            try
            {
                var clazz = AndroidJNI.FindClass("android/os/SystemClock");
                if (clazz == IntPtr.Zero)
                {
                    ShowDebug("找不到 android.os.SystemClock 类");
                    return 0;
                }

                var methodId = AndroidJNI.GetStaticMethodID(clazz, "elapsedRealtime", "()J");
                if (methodId == IntPtr.Zero)
                {
                    ShowDebug("找不到 elapsedRealtime 方法");
                    return 0;
                }

                var args = Array.Empty<jvalue>();
                var result = AndroidJNI.CallStaticLongMethod(clazz, methodId, args);

                ShowDebug("获取安卓端系统启动时间(毫秒):" + result);
                return result;
            }
            catch (Exception e)
            {
                ShowDebug("获取安卓端系统启动时间失败: " + e.Message);
                return 0;
            }
        }

        /// <summary>
        /// 获取本地记录的时间(毫秒)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static long GetSavedTime(string key)
        {
            var resStr = PlayerPrefs.GetString(key, "0");
            var res = Convert.ToInt64(resStr, CultureInfo.InvariantCulture);
            return res;
        }

        /// <summary>
        /// 保存时间记录(毫秒)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ms"></param>
        private static void SaveTime(string key, long ms)
        {
            PlayerPrefs.SetString(key, ms.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 获取时间戳(秒)
        /// </summary>
        /// <param name="tarTime"></param>
        /// <returns></returns>
        private static long Time2Timestamp(DateTime tarTime)
        {
            var ts = tarTime - Utility.Time.UtcEpoch;
            return Convert.ToInt64(ts.TotalSeconds);
        }

        /// <summary>
        /// 显示日志
        /// </summary>
        /// <param name="str"></param>
        private static void ShowDebug(string str)
        {
            // if (!LoadDll.Instance.openTimeAnitCheatLog) return;
            // KPLog.Log($"AntiCheatingTime: {str}");
        }

        #endregion
    }
}