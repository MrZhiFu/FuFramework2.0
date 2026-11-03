#if UNITY_EDITOR
using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络消息方向/类型。
	/// </summary>
	public enum ENetworkMessageKind
	{
		/// <summary>
		/// 客户端请求（RPC 一问一答中的问）。
		/// </summary>
		Request,

		/// <summary>
		/// 服务器响应（RPC 回包）。
		/// </summary>
		Response,

		/// <summary>
		/// 服务器推送（Notify，无请求配对）。
		/// </summary>
		Notify,

		/// <summary>
		/// 心跳通知（IHeartBeatMessage 与 INotifyMessage 双实现的消息，如 NotifyHeartBeat 校时通知——
		/// 保活侧信道但承载校时等附加语义，单独分类便于观察心跳节奏）。
		/// </summary>
		HeartBeat,
	}

	/// <summary>
	/// 网络消息终结状态。Request 记录创建时为 Sent，由 RPC 三终点（配对/超时/断线取消）回写；
	/// Response 恒为 Replied；Notify 创建即终结（无状态概念）。
	/// </summary>
	public enum ENetworkMessageStatus
	{
		/// <summary>
		/// 已发送等待回复。
		/// </summary>
		Sent,

		/// <summary>
		/// 已收到回复。
		/// </summary>
		Replied,

		/// <summary>
		/// 等待回复超时。
		/// </summary>
		Timeout,

		/// <summary>
		/// 断线/销毁导致等待被取消。
		/// </summary>
		Canceled,

		/// <summary>
		/// 异常（如重发编排失败）。
		/// </summary>
		Error,
	}

	/// <summary>
	/// 网络调试面板的单条消息记录。
	/// 注意：只存生成好的显示数据（JSON 快照/字节拷贝），绝不持有消息对象引用——
	/// 规避消息对象池化回收与跨线程生命周期问题。
	/// </summary>
	public sealed class NetworkMessageRecord
	{
		/// <summary>
		/// 自增序号（横跨所有类型，用于排序与重发定位）。
		/// </summary>
		public long Seq { get; set; }

		/// <summary>
		/// 方向/类型。
		/// </summary>
		public ENetworkMessageKind Kind { get; set; }

		/// <summary>
		/// 记录时刻（UTC）。
		/// </summary>
		public DateTime TimeUtc { get; set; }

		/// <summary>
		/// 消息唯一号（Request 与其 Response 以此精确配对）。
		/// </summary>
		public int UniqueId { get; set; }

		/// <summary>
		/// 消息 ID。
		/// </summary>
		public int MessageId { get; set; }

		/// <summary>
		/// 消息类型名（如 "ReqPlayerLogin"）。
		/// </summary>
		public string MessageName { get; set; }

		/// <summary>
		/// 终结状态（Request 由 RPC 三终点回写）。
		/// </summary>
		public ENetworkMessageStatus Status { get; set; }

		/// <summary>
		/// 耗时（毫秒）。仅 Request 记录在配对/超时/取消时回写；其余为 0。
		/// </summary>
		public long ElapsedMs { get; set; }

		/// <summary>
		/// 未压缩包体字节快照（发送侧压缩前抓取，接收侧解压后抓取，两侧对称）。
		/// 超 64KB 截断，<see cref="BodyTruncated"/> 标注。
		/// </summary>
		public byte[] BodyBytes { get; set; }

		/// <summary>
		/// 包体字节数（截断后为实际保留的字节数）。
		/// </summary>
		public int BodySize => BodyBytes?.Length ?? 0;

		/// <summary>
		/// 包体是否被截断（超 64KB 只保留前 64KB）。
		/// </summary>
		public bool BodyTruncated { get; set; }

		/// <summary>
		/// 消息内容 JSON 快照（记录时立即生成）。超 32KB 截断并标注。
		/// </summary>
		public string JsonSnapshot { get; set; }

		/// <summary>
		/// 是否由重发产生（与原始请求区分）。
		/// </summary>
		public bool IsResend { get; set; }
	}
}
#endif
