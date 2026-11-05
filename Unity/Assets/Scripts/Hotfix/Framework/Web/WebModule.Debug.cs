#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using AOT.Framework.Core.Utility;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using ProtoBuf;
using Hotfix.Framework.Core;
using Hotfix.Framework.Network;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
	/// <summary>
	/// Web 管理模块的调试埋点与调试查询接口。
	/// 功能：
	///     1. 在请求入队/发送/完成咽喉点累计计数（发起/成功/失败/超时/取消、收发字节）。
	///     2. 以环形缓冲保留最近 200 条请求终结记录（含请求体与响应体预览），供调试面板展开查看。
	///     3. 提供实时请求快照（等待/发送中）与模块级统计快照查询。
	///     4. 提供急救操作：清空全部等待/发送中请求并取消调用方任务（仅调试使用）。
	/// 说明：
	///     纯增量埋点，不改动请求发送/取消/并发的既有语义；仅供编辑器调试面板反射读取。
	/// </summary>
	public partial class WebModule
	{
		/// <summary>
		/// 调试环形记录容量。
		/// </summary>
		internal const int DEBUG_LOG_CAPACITY = 200;

		/// <summary>
		/// 记录中请求体文本的最大保留长度，超出部分截断。
		/// </summary>
		private const int DEBUG_LOG_BODY_MAX_LENGTH = 2000;

		#region 调试字段

		/// <summary>
		/// 累计发起请求总数（入队即计）。
		/// </summary>
		private int m_submitCount;

		/// <summary>
		/// 累计发起的 JSON 请求数。
		/// </summary>
		private int m_jsonSubmitCount;

		/// <summary>
		/// 累计发起的 Pb 请求数。
		/// </summary>
		private int m_pbSubmitCount;

		/// <summary>
		/// 累计成功请求数。
		/// </summary>
		private int m_successCount;

		/// <summary>
		/// 累计失败请求数（网络错误 / 构建异常等）。
		/// </summary>
		private int m_failedCount;

		/// <summary>
		/// 累计超时请求数。
		/// </summary>
		private int m_timeoutCount;

		/// <summary>
		/// 累计取消请求数（调用方取消 / 模块销毁 / 调试急救取消）。
		/// </summary>
		private int m_canceledCount;

		/// <summary>
		/// 累计发送字节数。
		/// </summary>
		private long m_sentBytes;

		/// <summary>
		/// 累计接收字节数。
		/// </summary>
		private long m_recvBytes;

		/// <summary>
		/// 最近请求记录的环形缓冲（数组为容器）。
		/// </summary>
		private readonly WebLogEntry[] m_debugLogRing = new WebLogEntry[DEBUG_LOG_CAPACITY];

		/// <summary>
		/// 环形缓冲中最旧记录的索引。
		/// </summary>
		private int m_debugLogHead;

		/// <summary>
		/// 环形缓冲中现有记录条数。
		/// </summary>
		private int m_debugLogCount;

		#endregion

		#region 请求入队统计

		/// <summary>
		/// JSON 请求提交打点（调试专用）：登记入队时刻与提交计数（调试开关关闭时跳过）。入队本身由主文件负责。
		/// </summary>
		/// <param name="webJsonData">JSON 请求数据。</param>
		private void RecordJsonSubmit(WebJsonDataBase webJsonData)
		{
			if (DebugRecordingEnabled)
			{
				webJsonData.EnqueueTimeUtc = DateTime.UtcNow;
				m_jsonSubmitCount++;
				m_submitCount++;
			}
		}

		/// <summary>
		/// Pb 请求提交打点（调试专用）：登记入队时刻与提交计数（调试开关关闭时跳过）。入队本身由主文件负责。
		/// </summary>
		/// <param name="webPbData">Pb 请求数据。</param>
		private void RecordPbSubmit(WebPbData webPbData)
		{
			if (DebugRecordingEnabled)
			{
				webPbData.EnqueueTimeUtc = DateTime.UtcNow;
				m_pbSubmitCount++;
				m_submitCount++;
			}
		}

		#endregion

		#region 请求结束记账

		/// <summary>
		/// 记录构建请求阶段的失败（请求在发送前即因非法 URL/序列化异常终止）。
		/// </summary>
		/// <param name="webData">请求数据。</param>
		/// <param name="exception">构建/发送异常。</param>
		private void RecordBuildFailed(WebDataBase webData, Exception exception)
		{
			RecordResult(EWebRequestResult.Failed, webData, 0, 0, exception?.Message, null);
		}

		/// <summary>
		/// 按终结结果累计计数，并将摘要写入最近请求记录。
		/// </summary>
		/// <param name="result">请求终结结果。</param>
		/// <param name="webData">请求数据（须已设置入队时间）。</param>
		/// <param name="sendBytes">发送字节数。</param>
		/// <param name="recvBytes">接收字节数。</param>
		/// <param name="error">失败/取消原因，成功时为空。</param>
		/// <param name="responseBody">响应体文本（成功为服务器返回内容，失败为服务器错误详情），取消时为空。</param>
		private void RecordResult(EWebRequestResult result, WebDataBase webData, int sendBytes, int recvBytes, string error, string responseBody)
		{
			// 调试开关关闭时短路，避免正式运行承担计数与记录开销
			if (!DebugRecordingEnabled) return;

			switch (result)
			{
				case EWebRequestResult.Success:
					m_successCount++;
					break;
				case EWebRequestResult.Timeout:
					m_timeoutCount++;
					break;
				case EWebRequestResult.Failed:
					m_failedCount++;
					break;
				case EWebRequestResult.Canceled:
					m_canceledCount++;
					break;
			}

			m_sentBytes += sendBytes;
			m_recvBytes += recvBytes;

			var nowUtc  = DateTime.UtcNow;
			var totalMs = webData.EnqueueTimeUtc == default ? 0 : Math.Max(0, (int)(nowUtc              - webData.EnqueueTimeUtc).TotalMilliseconds);
			var waitMs  = webData.SendTimeUtc    == default ? 0 : Math.Max(0, (int)(webData.SendTimeUtc - webData.EnqueueTimeUtc).TotalMilliseconds);

			// 请求体/响应体预览；过长则截断以控制环形记录内存
			var requestBody       = TruncateBody(webData.DebugRequestBody);
			var responseBodyText  = TruncateBody(responseBody);

			PushLog(new WebLogEntry(nowUtc, result, webData is WebPbData, webData.IsGet, webData.URL, waitMs, totalMs, sendBytes, recvBytes, error, requestBody, responseBodyText));
		}

		/// <summary>
		/// 截断记录正文文本，超出上限的部分以省略提示结尾。
		/// </summary>
		/// <param name="text">原始文本。</param>
		/// <returns>截断后的文本；空文本原样返回。</returns>
		private static string TruncateBody(string text)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= DEBUG_LOG_BODY_MAX_LENGTH)
			{
				return text;
			}

			return text.Substring(0, DEBUG_LOG_BODY_MAX_LENGTH) + "\n...（已截断）";
		}

		/// <summary>
		/// 将记录写入环形缓冲（满时覆盖最旧记录）。
		/// </summary>
		/// <param name="entry">记录条目。</param>
		private void PushLog(WebLogEntry entry)
		{
			if (m_debugLogCount == DEBUG_LOG_CAPACITY)
			{
				m_debugLogRing[m_debugLogHead] = entry;
				m_debugLogHead                 = (m_debugLogHead + 1) % DEBUG_LOG_CAPACITY;
			}
			else
			{
				var index = (m_debugLogHead + m_debugLogCount) % DEBUG_LOG_CAPACITY;
				m_debugLogRing[index] = entry;
				m_debugLogCount++;
			}
		}

		/// <summary>
		/// 解包 Pb 响应为可读文本（调试专用）：从 UnityWebRequest 提取响应字节后走 <see cref="FormatPbEnvelope"/> 解包。
		/// </summary>
		/// <param name="unityWebRequest">Unity 网络请求（须在释放前调用）。</param>
		/// <returns>可读文本；解不出内容时为描述性提示。</returns>
		private static string GetPbResponseText(UnityWebRequest unityWebRequest)
		{
			var data = unityWebRequest.downloadHandler?.data;
			if (data == null || data.Length == 0) return string.Empty;

			return FormatPbEnvelope(data);
		}

		/// <summary>
		/// 将 Pb 响应字节解包为可读文本：先解 HTTP 信封（MessageHttpObject），按信封 Id 反查响应类型，
		/// 再反序列化内层 Body 成 JSON 快照。任何一步失败都降级为描述性文本，不影响调用方逻辑。
		/// </summary>
		/// <param name="data">Pb 响应字节（MessageHttpObject 序列化产物）。</param>
		/// <returns>可读文本；解不出内容时为描述性提示。</returns>
		private static string FormatPbEnvelope(byte[] data)
		{
			if (data == null || data.Length == 0) return string.Empty;

			MessageHttpObject envelope;
			try
			{
				envelope = SerializerHelper.Deserialize<MessageHttpObject>(data);
			}
			catch (Exception e)
			{
				return $"（Pb 响应信封解包失败，共 {data.Length} 字节：{e.Message}）";
			}

			if (envelope == null) return $"（Pb 响应信封为空，共 {data.Length} 字节）";

			// 信封 Id 反查响应类型；类型未注册属异常降级
			var messageType = MessageIdRegistry.GetTypeById(envelope.Id);
			if (messageType == null)
			{
				return $"（信封 JSON）{envelope}";
			}

			// 内层 Body 为空是正常业务情况：protobuf 空消息（所有字段均为默认值，如空列表）序列化不产生字节
			if (envelope.Body == null || envelope.Body.Length == 0)
			{
				return $"{messageType.Name}: Null";
			}

			try
			{
				var msgObj = SerializerHelper.Deserialize(envelope.Body, messageType) as MessageObject;
				return msgObj == null ? $"（信封 JSON）{envelope}" : UtilityAOT.Json.ToJson(msgObj);
			}
			catch (Exception e)
			{
				return $"（Body 反序列化失败：{e.Message}；信封 JSON）{envelope}";
			}
		}

		#endregion

		#region 调试查询接口

		/// <summary>
		/// 调试记录开关（调试专用）。默认关闭；调试面板打开时置为 true、关闭时置回 false，以收敛运行时开销。
		/// </summary>
		public bool DebugRecordingEnabled { get; private set; }

		/// <summary>
		/// 设置调试记录开关（调试专用）。开启瞬间会清空既有历史，仅在此前为关闭状态时清空（可重复调用）。
		/// </summary>
		/// <param name="enabled">是否开启。</param>
		public void SetDebugRecording(bool enabled)
		{
			var wasEnabled = DebugRecordingEnabled;
			DebugRecordingEnabled = enabled;
			if (enabled && !wasEnabled) ClearDebugHistory();
		}

		/// <summary>
		/// 获取模块级调试统计与当前队列状态快照（调试专用）。
		/// </summary>
		/// <returns>模块调试信息快照。</returns>
		public WebModuleDebugInfo GetDebugSnapshot()
		{
			return new WebModuleDebugInfo(m_submitCount, m_jsonSubmitCount, m_pbSubmitCount, m_successCount, m_failedCount, m_timeoutCount, m_canceledCount,
			                              m_sentBytes, m_recvBytes, m_waitingJsonQueue.Count, m_sendingJsonList.Count, m_waitingPbQueue.Count, m_sendingPbList.Count);
		}

		/// <summary>
		/// 获取当前所有等待/发送中请求的快照（调试专用）。按 JSON 等待、JSON 发送、Pb 等待、Pb 发送分组顺序返回。
		/// </summary>
		/// <returns>实时请求信息数组。</returns>
		public WebLiveRequestInfo[] GetCurrentRequests()
		{
			var totalCount = m_waitingJsonQueue.Count + m_sendingJsonList.Count + m_waitingPbQueue.Count + m_sendingPbList.Count;
			var infos      = new WebLiveRequestInfo[totalCount];
			var index      = 0;

			// 直接 foreach 具体容器（Queue/List 的结构化枚举器），避免经 IEnumerable<T> 迭代产生装箱分配
			foreach (var data in m_waitingJsonQueue)
			{
				infos[index++] = CreateJsonLiveInfo(data, EWebRequestState.Waiting);
			}

			foreach (var data in m_sendingJsonList)
			{
				infos[index++] = CreateJsonLiveInfo(data, EWebRequestState.Sending);
			}

			foreach (var data in m_waitingPbQueue)
			{
				infos[index++] = CreatePbLiveInfo(data, EWebRequestState.Waiting);
			}

			foreach (var data in m_sendingPbList)
			{
				infos[index++] = CreatePbLiveInfo(data, EWebRequestState.Sending);
			}

			return infos;
		}

		/// <summary>
		/// 获取最近请求记录（调试专用）。返回数组按时间从新到旧排列，最多 DEBUG_LOG_CAPACITY 条。
		/// </summary>
		/// <returns>最近请求记录数组。</returns>
		public WebLogEntry[] GetRecentLogs()
		{
			var logs = new WebLogEntry[m_debugLogCount];
			for (var i = 0; i < m_debugLogCount; i++)
			{
				// 环形索引递增方向为从旧到新，倒序填充使返回数组从新到旧
				var ringIndex = (m_debugLogHead + i) % DEBUG_LOG_CAPACITY;
				logs[m_debugLogCount - 1 - i] = m_debugLogRing[ringIndex];
			}

			return logs;
		}

		/// <summary>
		/// 清空调试统计与最近请求记录（调试专用）。
		/// </summary>
		public void ClearDebugHistory()
		{
			m_submitCount     = 0;
			m_jsonSubmitCount = 0;
			m_pbSubmitCount   = 0;
			m_successCount    = 0;
			m_failedCount     = 0;
			m_timeoutCount    = 0;
			m_canceledCount   = 0;
			m_sentBytes       = 0;
			m_recvBytes       = 0;

			// 逐槽位置 default 释放记录引用（URL/Error/RequestBody 等字符串），
			// 仅重置 head/count 会让旧记录一直被数组钉住、无法回收
			for (var i = 0; i < DEBUG_LOG_CAPACITY; i++)
			{
				m_debugLogRing[i] = default;
			}

			m_debugLogHead  = 0;
			m_debugLogCount = 0;
		}

		/// <summary>
		/// 急救操作：清空全部等待/发送中的请求并取消调用方任务，释放全部并发槽位（调试专用）。
		/// 发送中的请求会标记调试取消，其底层传输不再记账，随自然超时回收。
		/// </summary>
		public void CancelAllPendingForDebug()
		{
			while (m_waitingJsonQueue.Count > 0)
			{
				CancelRequestForDebug(m_waitingJsonQueue.Dequeue(), "等待中被调试面板取消");
			}

			while (m_waitingPbQueue.Count > 0)
			{
				CancelRequestForDebug(m_waitingPbQueue.Dequeue(), "等待中被调试面板取消");
			}

			while (m_sendingJsonList.Count > 0)
			{
				var webData = m_sendingJsonList[0];
				m_sendingJsonList.RemoveAt(0);
				CancelRequestForDebug(webData, "发送中被调试面板取消");
			}

			while (m_sendingPbList.Count > 0)
			{
				var webData = m_sendingPbList[0];
				m_sendingPbList.RemoveAt(0);
				CancelRequestForDebug(webData, "发送中被调试面板取消");
			}
		}

		/// <summary>
		/// 急救取消单个请求：标记调试取消并释放调用方任务，登记取消记录。
		/// </summary>
		/// <param name="webData">请求数据。</param>
		/// <param name="reason">取消原因。</param>
		private void CancelRequestForDebug(WebDataBase webData, string reason)
		{
			// 发送中请求标记后在途完成回调不再重复记账；等待中请求无完成回调，标记无害
			webData.IsDebugCanceled = true;
			webData.Dispose();
			RecordResult(EWebRequestResult.Canceled, webData, 0, 0, reason, null);
		}

		#endregion

		#region 手动测试请求

		/// <summary>
		/// 手动测试请求是否正在运行（调试专用）。
		/// </summary>
		public bool DebugManualRunning { get; private set; }

		/// <summary>
		/// 手动测试请求的状态文本：空闲 / 请求中 / 成功 / 失败 / 已取消 / 参数错误（调试专用）。
		/// </summary>
		public string DebugManualStatus { get; private set; } = "空闲";

		/// <summary>
		/// 手动测试请求的结果内容（响应文本或错误信息，调试专用）。
		/// </summary>
		public string DebugManualMessage { get; private set; } = "";

		/// <summary>
		/// 发送手动测试请求（调试专用）。0=GET 直接请求；1=POST(JSON) 请求体按 JSON 表单发送；
		/// 2=POST(Pb) 由 URL 末段反推请求消息类型、请求体 JSON 转消息对象后走 Pb 通道发送。
		/// 请求会走模块既有入队/发送管道，因此自动进入最近请求记录。
		/// </summary>
		/// <param name="url">请求 URL。</param>
		/// <param name="method">请求模式（0 GET / 1 POST(JSON) / 2 POST(Pb)）。</param>
		/// <param name="postBody">POST 请求体（JSON 文本），仅 method 为 1/2 时生效。</param>
		public void SendDebugRequest(string url, int method, string postBody)
		{
			if (DebugManualRunning) return;

			if (string.IsNullOrWhiteSpace(url))
			{
				DebugManualStatus  = "参数错误";
				DebugManualMessage = "URL 不能为空";
				return;
			}

			SendDebugRequestAsync(url, method, postBody).Forget();
		}

		/// <summary>
		/// 手动测试请求异步执行体。
		/// </summary>
		/// <param name="url">请求 URL。</param>
		/// <param name="method">请求模式（0 GET / 1 POST(JSON) / 2 POST(Pb)）。</param>
		/// <param name="postBody">POST 请求体（JSON 文本）。</param>
		private async UniTaskVoid SendDebugRequestAsync(string url, int method, string postBody)
		{
			DebugManualRunning = true;
			DebugManualStatus  = "请求中";
			DebugManualMessage = "";

			try
			{
				string responseText;
				if (method == 2)
				{
					// Pb 模式：URL 末段反推请求消息类型（与 ConvertToSnakeCase 互逆约定），请求体 JSON 转消息对象后走 Pb 通道
					var parts     = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
					var typeName  = SnakeToPascal(parts.Length > 0 ? parts[^1] : string.Empty);
					var messageType = MessageIdRegistry.GetTypeByName(typeName);
					if (messageType == null)
					{
						DebugManualStatus  = "参数错误";
						DebugManualMessage = $"无法从 URL 反推请求消息类型：{typeName} 未注册（约定：URL 末段为消息名的 snake_case）";
						return;
					}

					MessageObject reqObj;
					try
					{
						reqObj = UtilityAOT.Json.ToObject(messageType, string.IsNullOrWhiteSpace(postBody) ? "{}" : postBody) as MessageObject;
					}
					catch (Exception e)
					{
						DebugManualStatus  = "参数错误";
						DebugManualMessage = $"请求体不是合法的 JSON：{e.Message}";
						return;
					}

					if (reqObj == null)
					{
						DebugManualStatus  = "参数错误";
						DebugManualMessage = "请求体反序列化结果为空";
						return;
					}

					var webBufferResult = await PostPbReq(url, reqObj, m_scope.Token);
					responseText = webBufferResult.IsNull() ? "（Pb 请求未返回结果）" : FormatPbEnvelope(webBufferResult.Result);
				}
				else if (method == 1)
				{
					Dictionary<string, object> form = null;
					if (!string.IsNullOrWhiteSpace(postBody))
					{
						try
						{
							form = UtilityAOT.Json.ToObject<Dictionary<string, object>>(postBody);
						}
						catch (Exception)
						{
							DebugManualStatus  = "参数错误";
							DebugManualMessage = "POST 请求体不是合法的 JSON 对象";
							return;
						}
					}

					var result = await PostToString(url, form, m_scope.Token);
					responseText = result?.Result;
				}
				else
				{
					var result = await GetToString(url, m_scope.Token);
					responseText = result?.Result;
				}

				DebugManualStatus  = "成功";
				DebugManualMessage = TruncateManualResult(responseText);
			}
			catch (OperationCanceledException)
			{
				DebugManualStatus  = "已取消";
				DebugManualMessage = "";
			}
			catch (Exception e)
			{
				DebugManualStatus  = "失败";
				DebugManualMessage = e.Message;
			}
			finally
			{
				DebugManualRunning = false;
			}
		}

		/// <summary>
		/// snake_case 转 PascalCase（与 <c>ConvertToSnakeCase</c> 互逆），用于由 URL 末段反推消息类型名。
		/// </summary>
		/// <param name="snake">snake_case 文本（如 req_user_login）。</param>
		/// <returns>PascalCase 文本（如 ReqUserLogin）。</returns>
		private static string SnakeToPascal(string snake)
		{
			if (string.IsNullOrEmpty(snake)) return string.Empty;

			var sb = new StringBuilder(snake.Length);
			foreach (var part in snake.Split('_'))
			{
				if (part.Length == 0) continue;

				sb.Append(char.ToUpperInvariant(part[0]));
				if (part.Length > 1) sb.Append(part, 1, part.Length - 1);
			}

			return sb.ToString();
		}

		/// <summary>
		/// 截断手动测试请求结果文本。
		/// </summary>
		/// <param name="text">原始结果文本。</param>
		/// <returns>截断后的文本。</returns>
		private static string TruncateManualResult(string text)
		{
			if (string.IsNullOrEmpty(text)) return "(空响应)";
			return text.Length <= 2000 ? text : text.Substring(0, 2000) + $"\n...（已截断，共 {text.Length} 字符）";
		}

		/// <summary>
		/// 清空手动测试请求的状态与结果（调试专用）。
		/// </summary>
		public void ClearDebugManualResult()
		{
			DebugManualStatus  = "空闲";
			DebugManualMessage = "";
		}

		#endregion

		#region 实时快照辅助

		/// <summary>
		/// 由 JSON 请求数据构造实时快照项。
		/// </summary>
		/// <param name="data">JSON 请求数据。</param>
		/// <param name="state">请求状态。</param>
		/// <returns>实时快照项。</returns>
		private static WebLiveRequestInfo CreateJsonLiveInfo(WebJsonDataBase data, EWebRequestState state)
		{
			return new WebLiveRequestInfo(state, false, data.IsGet, data.URL, data.EnqueueTimeUtc, data.SendTimeUtc, data.Token.IsCancellationRequested, data);
		}

		/// <summary>
		/// 由 Pb 请求数据构造实时快照项。
		/// </summary>
		/// <param name="data">Pb 请求数据。</param>
		/// <param name="state">请求状态。</param>
		/// <returns>实时快照项。</returns>
		private static WebLiveRequestInfo CreatePbLiveInfo(WebPbData data, EWebRequestState state)
		{
			return new WebLiveRequestInfo(state, true, data.IsGet, data.URL, data.EnqueueTimeUtc, data.SendTimeUtc, data.Token.IsCancellationRequested, data);
		}

		#endregion
	}
}
#endif
