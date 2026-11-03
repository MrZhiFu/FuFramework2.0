// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络管理模块——常量分部：网络频道运行时参数（单一事实源）。
	/// 网络底层参数调好后一般不再改——全部集中于此，按分区注释取值依据。
	/// </summary>
	public sealed partial class NetworkModule
	{
		#region 包头与缓冲

		/// <summary>
		/// 线协议包头长度（字节）：包总长(4) + 操作类型(1) + 压缩标记(1) + UniqueId(4) + 消息ID(4)
		/// </summary>
		public const int PACKET_HEADER_LENGTH = 14;

		/// <summary>
		/// 收发解析累积缓冲的初始长度
		/// </summary>
		public const int STREAM_BUFFER_LENGTH = 1024 * 64;

		/// <summary>
		/// Socket 层收发缓冲区大小
		/// </summary>
		public const int SOCKET_BUFFER_SIZE = 1024 * 64 - 1;

		#endregion

		#region 压缩

		/// <summary>
		/// 包体超过此长度（字节）时走压缩
		/// </summary>
		public const int LIMIT_COMPRESS_LENGTH = 512;

		/// <summary>
		/// 单个数据包的包体长度上限（默认 1 MiB，可按协议需要调整）。
		/// 包头中的 PacketLength 直接来自网络报文，若不设上限，畸形包会导致超大数组分配/内存耗尽。
		/// </summary>
		public const int MAX_PACKET_BODY_LENGTH = 1024 * 1024;

		#endregion

		#region 心跳

		/// <summary>
		/// 心跳间隔（秒）
		/// </summary>
		public const float HEART_BEAT_INTERVAL = 5f;

		/// <summary>
		/// 心跳连续丢失判死次数（5s 间隔下约 25~30s 判死触发重连）
		/// </summary>
		public const int MISS_HEART_BEAT_COUNT_BY_CLOSE = 5;

		#endregion

		#region 派发

		/// <summary>
		/// 推送派发每帧上限：防弱网爆发/重连回放把单帧卡死（回包与心跳豁免）
		/// </summary>
		public const int MAX_NOTIFY_DISPATCH_PER_FRAME = 64;

		/// <summary>
		/// 推送缓冲队列上限（派发挂起期间），满时丢弃最旧
		/// </summary>
		public const int MAX_BUFFERED_NOTIFY_COUNT = 1024;

		#endregion

		#region 超时

		/// <summary>
		/// RPC 调用默认超时（毫秒）
		/// </summary>
		public const int RPC_TIMEOUT_DEFAULT = 5000;

		/// <summary>
		/// RPC 调用超时下限（毫秒）：低于此值一律视为配置失误
		/// </summary>
		public const int RPC_TIMEOUT_MIN = 3000;

		/// <summary>
		/// 拨号超时（毫秒）：TCP 与 WebSocket 连接共用
		/// </summary>
		public const int CONNECT_TIMEOUT_MILLISECONDS = 15000;

		#endregion
	}
}
