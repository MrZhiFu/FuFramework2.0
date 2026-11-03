using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道运行时参数常量（单一事实源）。
	/// 网络底层参数调好后一般不再改——全部集中于此，按分区注释取值依据；
	/// 心跳参数的**实际生效值**由 <see cref="HeartBeatProviderBase"/> 派生类覆写（Game 层），
	/// 此处的心跳默认值仅为 Provider 未注册时的兜底。
	/// </summary>
	public static class ChannelConst
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

		#endregion

		#region 心跳（兜底默认；实际生效值见 HeartBeatProviderBase 派生类）

		/// <summary>
		/// 心跳间隔兜底默认（秒）。实际生效值由 Provider 覆写（当前 5s）
		/// </summary>
		public const float DEFAULT_HEART_BEAT_INTERVAL = 30f;

		/// <summary>
		/// 心跳丢失判死次数兜底默认。实际生效值由 Provider 覆写（当前 5 次，约 25s 判死）
		/// </summary>
		public const int DEFAULT_MISS_HEART_BEAT_COUNT_BY_CLOSE = 10;

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
