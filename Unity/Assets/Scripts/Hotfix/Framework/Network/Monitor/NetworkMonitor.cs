#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络监控器静态采集器（仅编辑器调试使用，#if UNITY_EDITOR 条件编译，不进包）。
	/// 功能：
	///     1. 以环形缓冲保留最近 2000 条收发消息记录（请求/响应/推送/心跳），线程安全（收发链路跨线程）。
	///     2. Enabled 开关：面板打开才录制、关闭即停（开启瞬间清空既有历史），正式运行零开销；
	///        IsPaused 暂停期间同样短路，但保留已有记录可查。
	///     3. RPC 三终点（配对回复/超时/断线取消）经 OnRpcSettled 回写请求记录的状态与耗时。
	///     4. 提供消息重发编排（Resend）——Editor 开发工具默认可用，无开关把守。
	/// 说明：
	///     1. 埋点为纯增量调用，不改动收发/超时/取消的既有语义（照 WebModule.Debug 的承诺）。
	///     2. 记录只存生成好的显示数据（JSON 快照/字节拷贝），绝不持有消息对象引用。
	///     3. 仅供编辑器调试面板反射读取。
	/// </summary>
	public static class NetworkMonitor
	{
		#region 常量

		/// <summary>
		/// 环形记录容量（同参考工具；TCP 消息频率高于 HTTP 故大于 WebModule.Debug 的 200）。
		/// </summary>
		internal const int CAPACITY = 2000;

		/// <summary>
		/// 单条 JSON 快照的保留上限，超出截断并标注。
		/// </summary>
		private const int JSON_MAX_LENGTH = 32 * 1024;

		/// <summary>
		/// 单条包体字节的保留上限，超出只保留前段（Hex 查看以头部为主，完整超大包极罕见）。
		/// </summary>
		private const int BODY_MAX_LENGTH = 64 * 1024;

		#endregion

		#region 字段

		/// <summary>
		/// 录制开关（调试专用）。默认关闭；面板打开时置 true、关闭时置回 false，以收敛运行时开销。
		/// </summary>
		public static bool Enabled { get; private set; }

		/// <summary>
		/// 暂停录制（调试专用）。暂停期间埋点短路不新增记录，已有记录保留可查。
		/// </summary>
		public static bool IsPaused { get; set; }

		/// <summary>
		/// 环形记录容器（数组为容器，满时覆盖最旧）。
		/// </summary>
		private static readonly NetworkMessageRecord[] m_records = new NetworkMessageRecord[CAPACITY];

		/// <summary>
		/// 环形缓冲中最旧记录的索引。
		/// </summary>
		private static int m_head;

		/// <summary>
		/// 环形缓冲中现有记录条数。
		/// </summary>
		private static int m_count;

		/// <summary>
		/// 记录自增序号（横跨所有类型）。
		/// </summary>
		private static long s_seq;

		/// <summary>
		/// 等待回写的请求索引（UniqueId → 请求记录 Seq）。
		/// 响应配对/超时/取消时按 UniqueId 找到请求记录回写状态；记录滚出环形后查不到即静默忽略。
		/// </summary>
		private static readonly Dictionary<int, long> m_pendingRequestSeqs = new();

		/// <summary>
		/// 等待回写耗时的响应索引（UniqueId → 响应记录 Seq）：响应在解码点创建时登记，
		/// RPC 终点配对时补写耗时（响应与请求共享同一次往返的耗时）。
		/// </summary>
		private static readonly Dictionary<int, long> m_pendingResponseSeqs = new();

		/// <summary>
		/// 一次性重发标记：Resend 编排置位，紧随其后的 RecordRequest 消费（IsResend 标注）后复位。
		/// </summary>
		private static bool s_nextRecordIsResend;

		/// <summary>
		/// 专用锁对象（收发埋点可能来自不同线程，环形写入与 pending 表更新互斥）。
		/// </summary>
		private static readonly object m_lock = new();

		#endregion

		#region 开关

		/// <summary>
		/// 设置录制开关（调试专用）。开启瞬间清空既有历史（与 WebModule.SetDebugRecording 一致，可重复调用）。
		/// </summary>
		public static void SetEnabled(bool enabled)
		{
			var wasEnabled = Enabled;
			Enabled = enabled;
			if (enabled && !wasEnabled) Clear();
		}

		/// <summary>
		/// 清空全部记录与等待回写索引（调试专用）。
		/// </summary>
		public static void Clear()
		{
			lock (m_lock)
			{
				// 逐槽位置 default 释放记录引用（JSON 快照/字节拷贝等），
				// 仅重置 head/count 会让旧记录一直被数组钉住、无法回收
				for (var i = 0; i < CAPACITY; i++)
				{
					m_records[i] = null;
				}

				m_head  = 0;
				m_count = 0;
				m_pendingRequestSeqs.Clear();
				m_pendingResponseSeqs.Clear();
			}
		}

		#endregion

		#region 录制埋点

		/// <summary>
		/// 记录一条客户端请求（由发送埋点调用：消息序列化成功后、压缩前）。
		/// 纯保活心跳不记录；暂停/未开启时短路。
		/// </summary>
		/// <param name="msgObj">请求消息对象</param>
		/// <param name="messageId">消息 ID</param>
		/// <param name="bodyBytes">未压缩包体字节</param>
		public static void RecordRequest(MessageObject msgObj, int messageId, byte[] bodyBytes)
		{
			if (!Enabled || IsPaused || msgObj == null || IsPureHeartbeat(msgObj)) return;

			// JSON/字节拷贝在锁外完成（锁内只做环形写入与索引更新，收敛临界区）
			var json = BuildJsonSnapshot(msgObj);
			var body = BuildBodySnapshot(bodyBytes, out var bodyTruncated);
			var isResend = s_nextRecordIsResend;
			s_nextRecordIsResend = false;

			var record = new NetworkMessageRecord
			{
				Seq           = ++s_seq,
				Kind          = ENetworkMessageKind.Request,
				TimeUtc       = DateTime.UtcNow,
				UniqueId      = msgObj.UniqueId,
				MessageId     = messageId,
				MessageName   = msgObj.GetType().Name,
				Status        = ENetworkMessageStatus.Sent,
				BodyBytes     = body,
				BodyTruncated = bodyTruncated,
				JsonSnapshot  = json,
				IsResend      = isResend,
			};

			lock (m_lock)
			{
				Push(record);
				m_pendingRequestSeqs[msgObj.UniqueId] = record.Seq;
			}
		}

		/// <summary>
		/// 记录一条接收消息（由解码埋点调用：反序列化成功后）。响应与推送各建一条，创建即终结。
		/// 纯保活心跳不记录；暂停/未开启时短路。
		/// </summary>
		/// <param name="msgObj">接收消息对象</param>
		/// <param name="messageId">消息 ID</param>
		/// <param name="bodyBytes">解压后的包体字节</param>
		public static void RecordReceived(MessageObject msgObj, int messageId, byte[] bodyBytes)
		{
			if (!Enabled || IsPaused || msgObj == null || IsPureHeartbeat(msgObj)) return;

			// 分流与派发同源：回包（IResponseMessage）→ 响应；双实现心跳（IHeartBeatMessage + INotifyMessage，
			// 如校时通知）→ 心跳（单独分类，便于观察心跳节奏）；其余 → 推送
			var kind = msgObj is IResponseMessage ? ENetworkMessageKind.Response
				: msgObj is IHeartBeatMessage ? ENetworkMessageKind.HeartBeat
				: ENetworkMessageKind.Notify;
			var json = BuildJsonSnapshot(msgObj);
			var body = BuildBodySnapshot(bodyBytes, out var bodyTruncated);

			var record = new NetworkMessageRecord
			{
				Seq           = ++s_seq,
				Kind          = kind,
				TimeUtc       = DateTime.UtcNow,
				UniqueId      = msgObj.UniqueId,
				MessageId     = messageId,
				MessageName   = msgObj.GetType().Name,
				Status        = ENetworkMessageStatus.Replied,
				BodyBytes     = body,
				BodyTruncated = bodyTruncated,
				JsonSnapshot  = json,
			};

			lock (m_lock)
			{
				Push(record);
				if (kind == ENetworkMessageKind.Response) m_pendingResponseSeqs[msgObj.UniqueId] = record.Seq;
			}
		}

		/// <summary>
		/// RPC 终点回写：按 UniqueId 找到等待中的请求与响应记录——
		/// 请求回写状态与耗时（状态流转），响应只补耗时（同一 UniqueId 同一次往返，两条记录都能看耗时）。
		/// 记录已滚出环形（Seq 查不到）时静默忽略，绝不抛异常。
		/// </summary>
		/// <param name="uniqueId">请求消息唯一号</param>
		/// <param name="status">终结状态（Replied/Timeout/Canceled/Error）</param>
		/// <param name="elapsedMs">耗时（RpcMessageData.ElapseTime）</param>
		public static void OnRpcSettled(int uniqueId, ENetworkMessageStatus status, long elapsedMs)
		{
			if (!Enabled) return;

			long reqSeq;
			long respSeq;
			lock (m_lock)
			{
				var hasRequest  = m_pendingRequestSeqs.Remove(uniqueId, out reqSeq);
				var hasResponse = m_pendingResponseSeqs.Remove(uniqueId, out respSeq);
				if (!hasRequest && !hasResponse) return; // 记录已滚出环形：等待索引在 Push 淘汰时同步清理，此处兜底

				if (hasRequest)
				{
					var request = FindRecordLocked(reqSeq);
					if (request != null)
					{
						request.Status    = status;
						request.ElapsedMs = elapsedMs;
					}
				}

				if (hasResponse)
				{
					var response = FindRecordLocked(respSeq);
					if (response != null) response.ElapsedMs = elapsedMs;
				}
			}
		}

		#endregion

		#region 查询接口

		/// <summary>
		/// 获取全部记录快照（调试专用）。返回数组按时间从新到旧排列（列表顶部最新）。
		/// </summary>
		public static NetworkMessageRecord[] GetSnapshot()
		{
			lock (m_lock)
			{
				var snapshot = new NetworkMessageRecord[m_count];
				for (var i = 0; i < m_count; i++)
				{
					// 环形索引递增方向为从旧到新，倒序填充使返回数组从新到旧
					var ringIndex = (m_head + i) % CAPACITY;
					snapshot[m_count - 1 - i] = m_records[ringIndex];
				}

				return snapshot;
			}
		}

		#endregion

		#region 重发编排

		/// <summary>
		/// 置一次性重发标记（调试专用）：紧随其后的发送埋点记录将标注 IsResend。
		/// </summary>
		public static void MarkNextResend() => s_nextRecordIsResend = true;

		/// <summary>
		/// 重发一条历史请求（调试专用，由重发窗口调用）：按序号定位原请求 → 消息类型反查 →
		/// JSON 反序列化为新消息对象 → 交网络模块重发。同步校验段失败返回 false 并给出原因日志。
		/// </summary>
		/// <param name="seq">原请求记录序号</param>
		/// <param name="jsonBody">（可能被编辑过的）请求 JSON</param>
		/// <returns>是否成功发起；结果（响应/超时/取消）经既有 RPC 终点自然落入记录列表</returns>
		public static bool Resend(long seq, string jsonBody)
		{
			var source = FindRecord(seq);
			if (source == null || source.Kind != ENetworkMessageKind.Request)
			{
				FuLogger.LogWarning($"NetworkMonitor: 重发失败，未找到序号 {seq} 对应的请求记录。");
				return false;
			}

			var messageType = MessageIdRegistry.GetTypeById(source.MessageId);
			if (messageType == null)
			{
				FuLogger.LogWarning($"NetworkMonitor: 重发失败，消息 ID {source.MessageId} 未注册类型。");
				return false;
			}

			MessageObject msgObj;
			try
			{
				msgObj = UtilityAOT.Json.ToObject(messageType, jsonBody) as MessageObject;
			}
			catch (Exception e)
			{
				FuLogger.LogWarning($"NetworkMonitor: 重发失败，JSON 反序列化异常：{e.Message}");
				return false;
			}

			if (msgObj == null)
			{
				FuLogger.LogWarning($"NetworkMonitor: 重发失败，JSON 反序列化结果不是消息对象（{messageType.Name}）。");
				return false;
			}

			// 置一次性重发标记后交网络模块发起：紧随其后的发送埋点将标注 IsResend；
			// 响应/超时/取消经既有 RPC 终点自然落入记录列表
			MarkNextResend();
			NetworkModule.Instance.ResendForMonitor(msgObj);
			return true;
		}

		#endregion

		#region 内部方法

		/// <summary>
		/// 纯保活心跳判定：心跳是保活侧信道，量大且无业务价值，不记录；
		/// 双实现心跳（如 NotifyHeartBeat 校时通知）正常记录——校时链路必须可见。
		/// </summary>
		private static bool IsPureHeartbeat(MessageObject msgObj)
		{
			return msgObj is IHeartBeatMessage && msgObj is not INotifyMessage;
		}

		/// <summary>
		/// 生成 JSON 快照（调用方线程执行，锁外）：超上限截断并标注。
		/// </summary>
		private static string BuildJsonSnapshot(MessageObject msgObj)
		{
			string json;
			try
			{
				json = UtilityAOT.Json.ToJson(msgObj);
			}
			catch (Exception e)
			{
				json = $"<序列化失败：{e.Message}>";
			}

			if (json != null && json.Length > JSON_MAX_LENGTH)
			{
				json = json[..JSON_MAX_LENGTH] + $"\n...（已截断，共 {json.Length} 字符）";
			}

			return json;
		}

		/// <summary>
		/// 拷贝包体字节快照（调用方线程执行，锁外）：超上限截断。
		/// 必须拷贝——发送侧缓冲/接收侧缓冲均会被复用，直接持有引用会被后续消息覆盖。
		/// </summary>
		private static byte[] BuildBodySnapshot(byte[] bodyBytes, out bool truncated)
		{
			truncated = bodyBytes != null && bodyBytes.Length > BODY_MAX_LENGTH;
			if (bodyBytes == null) return null;
			if (!truncated) return (byte[])bodyBytes.Clone();

			var body = new byte[BODY_MAX_LENGTH];
			Array.Copy(bodyBytes, body, BODY_MAX_LENGTH);
			return body;
		}

		/// <summary>
		/// 环形写入（锁内调用）：满时覆盖最旧并同步清理其等待索引。
		/// </summary>
		private static void Push(NetworkMessageRecord record)
		{
			if (m_count == CAPACITY)
			{
				var evicted = m_records[m_head];
				if (evicted != null)
				{
					m_pendingRequestSeqs.Remove(evicted.UniqueId);
					m_pendingResponseSeqs.Remove(evicted.UniqueId);
				}

				m_records[m_head] = record;
				m_head            = (m_head + 1) % CAPACITY;
			}
			else
			{
				var index = (m_head + m_count) % CAPACITY;
				m_records[index] = record;
				m_count++;
			}
		}

		/// <summary>
		/// 按序号查找记录（锁内调用，线性扫描环形缓冲；外部入口请用 <see cref="FindRecord"/>）。
		/// </summary>
		private static NetworkMessageRecord FindRecordLocked(long seq)
		{
			for (var i = 0; i < m_count; i++)
			{
				var record = m_records[(m_head + i) % CAPACITY];
				if (record != null && record.Seq == seq) return record;
			}

			return null;
		}

		/// <summary>
		/// 按序号查找记录（低频调用，线性扫描环形缓冲）。
		/// </summary>
		private static NetworkMessageRecord FindRecord(long seq)
		{
			lock (m_lock)
			{
				return FindRecordLocked(seq);
			}

			return null;
		}

		#endregion
	}
}
#endif
