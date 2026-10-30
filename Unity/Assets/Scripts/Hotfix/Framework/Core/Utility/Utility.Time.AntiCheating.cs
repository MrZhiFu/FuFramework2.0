using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using AOT.Framework.Core.Log;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif


// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
	/// <summary>
	/// 时间防作弊工具类。
	/// 功能：
	///     1. 防止用户修改系统时间导致的时间作弊。
	///
	/// 【防作弊核心原理】
	/// 1. 双时间源验证：对比"系统运行时间(TickCount)"和"实际系统时间(DateTime.UtcNow)"
	///    - 系统运行时间：从开机到现在经过的时间，用户难以修改
	///    - 实际系统时间：系统显示的UTC时间，用户可以随意修改
	///
	/// 2. 作弊检测逻辑：
	///    - 正常情况：系统运行时间增量 ≈ 实际系统时间增量
	///    - 作弊情况：用户修改系统时间后，两个时间增量出现巨大差异
	///
	/// 3. 时间纠正机制：
	///    - 检测到作弊时，基于系统运行时间自动纠正返回的时间
	///    - 保证游戏逻辑使用的时间不受用户修改系统时间的影响
	///
	/// 【工作模式】
	/// 1. 在线模式：优先使用NTP网络时间，完全可靠
	/// 2. 离线模式：使用双时间源验证，检测并纠正时间作弊
	///
	/// 【已知漏洞和限制】
	/// 1. 断网用户重启电脑后修改时间，无法判断是否作弊（核心漏洞）
	///    - 原因：系统重启后TickCount重置，失去时间验证基准
	///    - 攻击方法：退出游戏 → 重启电脑 → 修改时间 → 启动游戏
	///
	/// 2. 断网用户如果超过49.8天没关机，会每49.8天有一次作弊机会
	///    - 原因：Environment.TickCount 32位溢出周期为49.8天
	///    - 攻击方法：等待TickCount溢出重置时修改时间
	///
	/// 3. 极端情况下可能出现误判
	///    - 系统时钟精度误差、线程调度延迟等可能触发误报
	///    - 通过60秒容差机制缓解此问题
	/// </summary>
	public static class TimeAntiCheating
	{
		#region 常量

		private const string LAST_TICK_TIME_KEY   = "LastTickTime"; // 最后一次存储的系统启动时间记录key
		private const string LAST_UTC_TIME_KEY    = "LastUtcTime";  // 最后一次存储在本地的Utc时间戳记录key
		private const int    MAX_RECHECK_ATTEMPTS = 3;              // 重查网络时间的最大尝试次数

		/// <summary>
		/// 时间记录校验和混淆盐：防止随手篡改记录数值（配合 GetSavedTime/SaveTime 的 "秒|校验和" 格式）。
		/// 注意：盐随代码可被逆向，这是"提高门槛"而非"加密保险"——单机防作弊的合理上限。
		/// </summary>
		private const string TIME_RECORD_SALT = "FuFramework.TimeAntiCheating.v1";

		/// <summary>
		/// 每隔多少秒才真正把时间记录写进本地存档(秒)。
		/// 这个记录只在下次打开游戏时用来检测时间作弊，平时没必要每秒都写 PlayerPrefs
		/// </summary>
		private const float SAVE_FLUSH_INTERVAL_SEC = 30f;

		/// <summary>
		/// "钟走了多久"和"秒表走了多久"对不上多少秒以内算正常(秒)。
		/// 不能太小：NTP网络校时、电脑睡眠唤醒、虚拟机挂起恢复都会让钟一次跳几秒~几十秒，太小会误伤正常玩家（时间被强行纠正、倒计时跳变）；
		/// 不能太大：超过它的部分就是"改了也不被抓"的免费额度，60秒下改1小时必被抓、改几十秒没有油水——不建议随意调小
		/// </summary>
		private const long CLOCK_CHECK_TOLERANCE_SEC = 60;

		#endregion

		#region 静态字段

		private static bool     s_forbidCheck;                              // 禁止检测（方便测试）
		private static DateTime s_cachedTime;                               // 缓存的时间结果
		private static long     s_lastTickCount;                            // 上次计算时的系统启动时间（秒）
		private static bool     s_gotNetTime;                               // 是否得到了在线时间
		private static DateTime s_nowOnlineDateTime;                        // 当前在线时间
		private static float    s_lastRecheckTime;                          // 上次重查网络时间时的时间
		private static int      s_recheckAttemptCount;                      // 重查网络时间的尝试次数
		private static bool     s_benchmarksLoaded;                         // 是否已从本地存档读出上次的时间记录（只读一次，之后都用内存值）
		private static long     s_savedTickTime;                            // 上次保存的"系统已运行秒数"，用来和当前值对比检测时间作弊（0=还没记录）
		private static long     s_savedUtcTime;                             // 上次保存的"UTC时间戳(秒)"，作用同上（0=还没记录）
		private static float    s_lastFlushTime = -SAVE_FLUSH_INTERVAL_SEC; // 上次把记录写进本地存档的时刻（初始值保证首次必定写入）
		private static IntPtr   s_androidSystemClockClass;                  // 缓存的 SystemClock 类引用（JNI 一次性查找）
		private static IntPtr   s_androidElapsedRealtimeMethodId;           // 缓存的 elapsedRealtime 方法ID

		#endregion

		#region 属性

		/// <summary>
		/// 获取系统启动后经过的毫秒数，如果返回零则获取失败（概率约等于0）
		/// (不要每帧调用，会有性能问题)
		/// </summary>
		/// <returns>系统启动后经过的毫秒数</returns>
		public static long TickCountMs
		{
			get
			{
#if UNITY_EDITOR
				return GetTickCount();
#elif UNITY_IOS
				return GetTickCount();
#elif UNITY_ANDROID
				return GetAndroidTickCount();
#else
				// 其余平台（Windows/Mac/Linux 独立包等）走系统 TickCount，
				// 不可落入 Android JNI 分支——那会让 TickCount 恒为 0，防作弊链失效返回 0001-01-01
				return GetTickCount();
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
		public static bool s_NeedRecheckNetTime = true;

		/// <summary>
		/// 网络时间重查间隔(秒), 默认5分钟
		/// </summary>
		public static float s_NetTimeRecheckInterval = 300f;

		#endregion

		#region 外部调用

		/// <summary>
		/// 获取当前时间(本地时间)
		/// </summary>
		/// <returns></returns>
		public static DateTime GetLocalNow()
		{
			var utcTime  = GetUtcNow();
			var tempTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
			return tempTime;
		}

		/// <summary>
		/// 获取当前时间(UTC)
		/// </summary>
		/// <returns></returns>
		public static DateTime GetUtcNow()
		{
			if (s_forbidCheck) return DateTime.UtcNow;

			var currentTick = TickCount;

			// 拿不到系统启动时间（返回0，如JNI失败/平台不支持）就没有对比依据，
			// 直接用系统时间兜底，避免返回错误的纪元时间
			if (currentTick <= 0) return DateTime.UtcNow;

			// 同一秒内直接复用缓存
			if (currentTick == s_lastTickCount) return s_cachedTime;

			DateTime result;

			// 上次的时间记录只在首次调用时从本地存档读一次，之后都用内存里的值（见 FlushSavedTimesThrottled）
			LoadBenchmarksIfNeeded(currentTick);

			if (s_gotNetTime)
			{
				// 已经获得过网络时间：结果 = 网络时间 + 系统启动时间差
				s_nowOnlineDateTime = s_nowOnlineDateTime.AddSeconds(currentTick - s_savedTickTime);
				s_savedUtcTime      = Time2Timestamp(s_nowOnlineDateTime);
				s_savedTickTime     = currentTick;
				result              = s_nowOnlineDateTime;
			}
			else
			{
				// 未获得过网络时间：
				var now          = DateTime.UtcNow;
				var nowTimestamp = Time2Timestamp(now);

				// 上次保存的UTC时间戳（0 表示没有记录：首次运行/清存档/重装——
				// 此时拿本次真实时间当起点，避免时间差算出天文数字而误判作弊返回 1970 年）
				var lastTimestamp = s_savedUtcTime > 0 ? s_savedUtcTime : nowTimestamp;

				// 获取当前时间戳和上次记录的时间戳差值
				var timestampDelta = nowTimestamp - lastTimestamp;

				// 获取本次和上次的系统启动经过的时间差值
				var tickTimeDelta = currentTick - s_savedTickTime;

				var delta = Math.Abs(timestampDelta - tickTimeDelta);

				// 差值在容差内=钟正常；秒表倒退(<=0)=电脑重启过、秒表归零，没有对比意义，直接信任钟
				if (delta < CLOCK_CHECK_TOLERANCE_SEC || tickTimeDelta <= 0)
				{
					s_savedUtcTime  = nowTimestamp;
					s_savedTickTime = currentTick;
					result          = now;
				}
				else
				{
					// 说明时间不可靠,返回纠正后的时间：结果 = 当前Utc时间 + (时间戳差值 - 系统启动时间差值)
					result = now.AddSeconds(-(timestampDelta - tickTimeDelta));
					Log($"时间不可靠，纠正后的当前时间：{result}");
				}

				// 尝试重新获取下网络时间
				TryRecheckNetTime();
			}

			// 时间记录攒够间隔才写进本地存档（见 FlushSavedTimesThrottled）
			FlushSavedTimesThrottled();

			// 更新缓存
			s_lastTickCount = currentTick;
			s_cachedTime    = result;

			return result;
		}

		/// <summary>
		/// 应用退出时调用(需要自行设置位置)
		/// </summary>
		public static void OnApplicationQuit()
		{
			// 取消并释放 NTP 异步链的生命周期所有者，避免退出后仍有裸异步任务在跑
			sr_ntpCancellation.Dispose();

			var now          = GetUtcNow();
			var nowTimestamp = Time2Timestamp(now);
			SaveTime(LAST_UTC_TIME_KEY,  nowTimestamp);
			SaveTime(LAST_TICK_TIME_KEY, TickCount);
			FuLogger.LogInfo($"TimeAntiCheating: 离开游戏最后保存在本地的Utc时间：{now}, 时间戳：{nowTimestamp}, 系统启动时间：{TickCount}");
		}

		/// <summary>
		/// 禁止时间检测（方便测试用，正式版请不要调用）
		/// </summary>
		/// <param name="forbid"></param>
		public static void ForbidCheck(bool forbid) => s_forbidCheck = forbid;

		#endregion

		#region 内部方法

		/// <summary>
		/// 尝试重新获取网络时间（带频率限制和重试机制）
		/// </summary>
		private static void TryRecheckNetTime()
		{
			if (!s_NeedRecheckNetTime || s_gotNetTime) return;

			// 检查重查频率限制
			var currentTime = Time.realtimeSinceStartup;
			if (currentTime - s_lastRecheckTime < s_NetTimeRecheckInterval) return;

			// 检查重试次数限制
			if (s_recheckAttemptCount >= MAX_RECHECK_ATTEMPTS)
			{
				Log("已达到最大网络时间重试次数，停止重试");
				return;
			}

			s_lastRecheckTime = currentTime;
			s_recheckAttemptCount++;

			Log($"第{s_recheckAttemptCount}次尝试重新获取网络时间");

			// 异步链的生命周期由 m_ntpCancellation 持有：Token 透传给所有在途请求，OnApplicationQuit 取消释放
			MultipleNptGetTimeAsync(sr_ntpCancellation.Token).Forget();
		}

		/// <summary>
		/// 重置网络时间重查状态（在成功获取网络时间后调用）
		/// </summary>
		private static void ResetRecheckState()
		{
			s_recheckAttemptCount = 0;
			s_lastRecheckTime     = 0f;
		}

		/// <summary>
		/// 只在首次调用时从本地存档读出上次保存的时间记录，之后都用内存里的值——
		/// 每次都读 PlayerPrefs（还要算校验和）太浪费
		/// </summary>
		private static void LoadBenchmarksIfNeeded(long currentTick)
		{
			if (s_benchmarksLoaded) return;

			s_savedTickTime = GetSavedTime(LAST_TICK_TIME_KEY);
			if (s_savedTickTime <= 0)
			{
				Log($"首次运行或记录的数据被删除了，重新保存系统启动后经过的秒数{currentTick}s");
				s_savedTickTime = currentTick;
			}

			// 0 表示没有可用记录（首次运行/清存档/记录被篡改），调用方会拿当前时间当起点重新记
			s_savedUtcTime = GetSavedTime(LAST_UTC_TIME_KEY);

			s_benchmarksLoaded = true;
		}

		/// <summary>
		/// 马上把内存里的时间记录写进本地存档（只在关键节点调用：拿到网络时间/游戏退出）
		/// </summary>
		private static void FlushSavedTimes()
		{
			SaveTime(LAST_UTC_TIME_KEY,  s_savedUtcTime);
			SaveTime(LAST_TICK_TIME_KEY, s_savedTickTime);
			s_lastFlushTime = Time.realtimeSinceStartup;
		}

		/// <summary>
		/// 距上次写入超过 <see cref="SAVE_FLUSH_INTERVAL_SEC"/> 才真正写一次本地存档。
		/// 这个记录只在下次打开游戏时用来检测时间作弊，没必要每秒都写；
		/// 游戏崩溃时最多丢 30 秒记录，对防作弊没有影响
		/// </summary>
		private static void FlushSavedTimesThrottled()
		{
			if (Time.realtimeSinceStartup - s_lastFlushTime < SAVE_FLUSH_INTERVAL_SEC) return;
			FlushSavedTimes();
		}

		#region 多npt服务器地址获取全球时间方法

		/// <summary>
		/// NTP 异步请求的生命周期取消源（静态类自持）。
		/// 发起请求时透传其 Token；OnApplicationQuit 取消并释放，避免脱离生命周期的裸异步链。
		/// </summary>
		private static readonly LifecycleCancellationSource sr_ntpCancellation = new();

		/// <summary>
		/// 多地址获取网络时间方法（fire-and-forget）。
		/// 生命周期由 m_ntpCancellation 持有：其 Token 被取消时，在途 DNS/UDP 请求立即中止。
		/// </summary>
		/// <param name="cancellationToken">生命周期取消令牌（必传，由持有方 m_ntpCancellation 提供）。</param>
		private static async UniTaskVoid MultipleNptGetTimeAsync(CancellationToken cancellationToken)
		{
			string[] ntpServers =
			{
				"time.google.com",  //国际通用地址 Google 提供的 NTP 服务
				"time.windows.com", //国际用地址 微软提供的 NTP 服务
				"time.apple.com",   //国际用地址 苹果提供的 NTP 服务
				"time1.aliyun.com", //中国区 阿里云提供的 NTP 服务
				"ntp.ntsc.ac.cn",   //中国区 中国科学院国家授时中心
				"ntp1.inrim.it",    //意大利国家计量研究所
				"ntp.nict.jp",      // 日本国立信息通信研究所
				"time.nist.gov",    // 美国国家标准与技术研究院
				"us.pool.ntp.org",  // 美国地区的 NTP 服务器池
			};
			try
			{
				var time = await GetFirstAvailableTimestampAsync(ntpServers, cancellationToken);
				SetOnlineTime(time);
			}
			catch (OperationCanceledException)
			{
				Log("未能获取网络时间: 请求已取消（生命周期结束）");
			}
			catch (Exception ex)
			{
				Log($"Error:未能获取网络时间: {ex.Message}");
			}
		}

		/// <summary>
		/// 获取第一个可用的 NTP 服务器的时间戳
		/// </summary>
		/// <param name="servers"></param>
		/// <param name="cancellationToken">生命周期取消令牌（必传）。</param>
		/// <param name="sendTimeout"></param>
		/// <param name="receiveTimeout"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static async UniTask<DateTimeOffset> GetFirstAvailableTimestampAsync(string[] servers, CancellationToken cancellationToken, int sendTimeout = 3000, int receiveTimeout = 3000)
		{
			foreach (var server in servers)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					Log($"正在尝试从服务器 {server} 获取时间...");
					var result = await ReqTimestampFromNtpAsync(server, sendTimeout, receiveTimeout, cancellationToken);

					if (result.error == null)
					{
						Log($"成功从服务器 {server} 获取时间");
						return result.timestamp;
					}

					Log($"服务器 {server} 获取时间失败: {result.error.Message}");
				}
				catch (Exception ex)
				{
					Log($"服务器 {server} 请求异常: {ex.Message}");
				}

				// 可选：在服务器之间添加短暂延迟（UniTask，且随生命周期令牌取消）
				await UniTask.Delay(100, cancellationToken: cancellationToken);
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
		private static async UniTask<(DateTimeOffset timestamp, Exception error)> ReqTimestampFromNtpAsync(string server, int sendTimeout, int receiveTimeout, CancellationToken cancellationToken)
		{
			try
			{
				var ntpData = new byte[48];
				ntpData[0] = 0x1B; // LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

				// 使用 Dns.GetHostEntryAsync 获取服务器 IP 地址
				var addresses = (await Dns.GetHostEntryAsync(server)).AddressList;

				// 过滤出 IPv4 地址
				var ipv4AddressList = new List<IPAddress>();
				foreach (var a in addresses)
				{
					if (a.AddressFamily == AddressFamily.InterNetwork)
					{
						ipv4AddressList.Add(a);
					}
				}

				if (ipv4AddressList.Count == 0) throw new Exception("无法解析 NTP 服务器地址");

				var ipEndPoint = new IPEndPoint(ipv4AddressList[0], 123); // NTP 默认端口

				using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				socket.SendTimeout    = sendTimeout;
				socket.ReceiveTimeout = receiveTimeout;

				await socket.ConnectAsync(ipEndPoint);

				if (!socket.Connected) throw new Exception("未连接到 NTP 服务器");

				await socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None, cancellationToken);
				await socket.ReceiveAsync(new ArraySegment<byte>(ntpData), SocketFlags.None, cancellationToken);

				// 解析 NTP 时间戳
				var iPart        = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | ntpData[43];
				var fPart        = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | ntpData[47];
				var milliseconds = iPart * 1000L + fPart * 1000L / 0x100000000L;

				// NTP 时间戳是从 1900-01-01 开始计算的
				var epochStart = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				var now        = epochStart.AddMilliseconds(milliseconds); //DateTime
				var timeOffset = new DateTimeOffset(now);                  //转化为DateTImeOffset
				Log($"获取网络Utc时间成功:server:{server}, DateTime:{now}");
				return (timeOffset, null);
			}
			catch (Exception e)
			{
				Log($"Error:server:{server} msg:{e.Message}");
				return (default, e);
			}
		}

		/// <summary>
		/// 设置为在线网络时间
		/// </summary>
		/// <param name="offTime"></param>
		private static void SetOnlineTime(DateTimeOffset offTime)
		{
			if (s_gotNetTime) return;

			s_nowOnlineDateTime = offTime.UtcDateTime;
			s_gotNetTime        = true;

			// 拿到权威网络时间是关键节点：更新内存记录并马上写进本地存档
			s_savedUtcTime  = Time2Timestamp(s_nowOnlineDateTime);
			s_savedTickTime = TickCount;
			FlushSavedTimes();

			// 重置重查状态
			ResetRecheckState();
			Log("成功获取网络时间，重置重查状态");
		}

		#endregion

		/// <summary>
		/// 获取系统启动时间(毫秒)。PC 端 49.8 天轮转一次（32位 TickCount），iOS/安卓端无重置
		/// </summary>
		/// <returns></returns>
		private static long GetTickCount()
		{
			var tickCount = -1;
#if UNITY_IOS && !UNITY_EDITOR
			// iOS：mach_continuous_time() 是含睡眠时间的单调时钟（与安卓 elapsedRealtime 语义对等），
			// 设备息屏挂机期间也在走，与系统时钟的增量对比不会被睡眠打断。
			tickCount = GetIosUptimeMs();
#else
			//由于 TickCount 属性值的值是32位有符号整数，因此，如果系统连续运行，TickCount 将从零递增到 Int32.MaxValue 大约24.9 天，
			//然后跳转到 Int32.MinValue，这是一个负数，然后在下一个24.9 天内递增为零
			//这里将负数转为整数，一轮周期就变为49.8天
			tickCount = Environment.TickCount;
			if (tickCount < 0)
			{
				tickCount = tickCount - int.MinValue + int.MaxValue;
			}
#endif
			return tickCount;
		}


#if UNITY_IOS && !UNITY_EDITOR
		/// <summary>
		/// 含睡眠时间的单调时钟刻度（Mach 时间单位，非纳秒）。
		/// libsystem 公开符号，__Internal 可直接解析，无需往 Xcode 工程注入任何原生代码。
		/// </summary>
		[DllImport("__Internal")]
		private static extern ulong mach_continuous_time();

		/// <summary>
		/// Mach 时间刻度到纳秒的换算系数（numer/denom，全机恒定）。
		/// </summary>
		private struct MachTimebaseInfo
		{
			public uint Numer;
			public uint Denom;
		}

		[DllImport("__Internal")]
		private static extern int mach_timebase_info(out MachTimebaseInfo info);

		private static uint s_machTimebaseNumer;  // 换算分子
		private static uint s_machTimebaseDenom;  // 换算分母（0=尚未查询）

		/// <summary>
		/// 获取 iOS 设备开机后经过的毫秒数：刻度 × numer ÷ denom = 纳秒，再转毫秒。
		/// timebase 全机恒定只查一次；换算系数异常时返回 0，由 GetUtcNow 的失败兜底分支接住。
		/// </summary>
		private static long GetIosUptimeMs()
		{
			if (s_machTimebaseDenom == 0)
			{
				mach_timebase_info(out var info);
				s_machTimebaseNumer = info.Numer;
				s_machTimebaseDenom = info.Denom > 0 ? info.Denom : 1;
			}

			var ticks       = mach_continuous_time();
			var nanoseconds = ticks * s_machTimebaseNumer / s_machTimebaseDenom;
			return (long)(nanoseconds / 1_000_000);
		}
#endif


		/// <summary>
		/// 获取安卓端系统启动时间(毫秒)(必须在安卓真机上才有用) 无重置
		/// </summary>
		/// <returns></returns>
		private static long GetAndroidTickCount()
		{
			try
			{
				// jclass/jmethodID 仅首次查找（Unity 的 FindClass 返回全局引用，可跨调用缓存），
				// 之后每次调用只剩一次 CallStaticLongMethod JNI 往返——TickCount 是 GetUtcNow 每次调用的必经之路
				if (s_androidSystemClockClass == IntPtr.Zero)
				{
					var clazz = AndroidJNI.FindClass("android/os/SystemClock");
					if (clazz == IntPtr.Zero) return 0;

					var methodId = AndroidJNI.GetStaticMethodID(clazz, "elapsedRealtime", "()J");
					if (methodId == IntPtr.Zero) return 0;

					s_androidSystemClockClass        = clazz;
					s_androidElapsedRealtimeMethodId = methodId;
				}

				return AndroidJNI.CallStaticLongMethod(s_androidSystemClockClass, s_androidElapsedRealtimeMethodId, Array.Empty<jvalue>());
			}
			catch (Exception e)
			{
				Log("获取安卓端系统启动时间失败: " + e.Message);
				return 0;
			}
		}

		/// <summary>
		/// 获取本地记录的时间(秒)。格式为 "秒|校验和"（旧版纯数字格式兼容读取，下次保存自动升级）。
		/// 校验和不匹配 = 记录被篡改 → 回退为无基准(0)，由 <see cref="GetUtcNow"/> 的无基准分支重建。
		/// 记录为非法数据时同样回退为无基准(0)，不打崩时间链。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private static long GetSavedTime(string key)
		{
			try
			{
				var resStr = PlayerPrefs.GetString(key, "0");

				// 旧格式兼容：纯数字为校验和机制启用前的记录，按秒读取
				if (long.TryParse(resStr, out var legacyValue)) return legacyValue;

				// 新格式：秒|校验和
				var separatorIndex = resStr.IndexOf('|');
				if (separatorIndex <= 0) return 0;

				var secondsPart  = resStr[..separatorIndex];
				var checksumPart = resStr[(separatorIndex + 1)..];
				if (!long.TryParse(secondsPart, out var seconds)) return 0;

				var expectedChecksum = Utility.Hash.XxHash.Hash64(key + seconds + TIME_RECORD_SALT).ToString();
				return checksumPart == expectedChecksum ? seconds : 0;
			}
			catch (Exception e)
			{
				Log($"读取本地时间记录 '{key}' 失败（数据被篡改？），已回退为无基准：{e.Message}");
				return 0;
			}
		}

		/// <summary>
		/// 保存时间记录(秒)。格式为 "秒|校验和"（校验和由 key+秒值+盐 计算出，防随手篡改记录数值）
		/// </summary>
		/// <param name="key"></param>
		/// <param name="seconds"></param>
		private static void SaveTime(string key, long seconds)
		{
			var checksum = Utility.Hash.XxHash.Hash64(key + seconds + TIME_RECORD_SALT).ToString();
			PlayerPrefs.SetString(key, $"{seconds}|{checksum}");
		}

		/// <summary>
		/// 获取时间戳(秒)
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		private static long Time2Timestamp(DateTime time)
		{
			var ts = time - Utility.Time.sr_UtcEpoch;
			return Convert.ToInt64(ts.TotalSeconds);
		}

		/// <summary>
		/// 显示日志
		/// </summary>
		/// <param name="str"></param>
		private static void Log(string str) => FuLogger.LogInfo($"TimeAntiCheating: {str}");

		#endregion
	}
}