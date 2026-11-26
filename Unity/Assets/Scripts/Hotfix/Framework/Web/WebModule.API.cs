using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Network;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
	/// <summary>
	/// Web 管理模块的公共 API。
	/// 功能：
	///     1. 提供 GET/POST 请求：字符串（GetToString/PostToString）、字节数组（GetToBytes/PostToBytes）、Pb 强类型（Post&lt;T&gt;）。
	///     2. 提供超时（Timeout/ReqTimeout）与每服务器并发上限（MaxConnectionPerServer）配置。
	///     3. 实现 ICancelAsync：Token 观察生命周期取消，CancelAsync 供框架重启排水等待。
	/// </summary>
	public partial class WebModule
	{
		/// <summary>
		/// 模块单例。
		/// </summary>
		public static WebModule Instance { get; private set; }

		/// <summary>
		/// 取消令牌：模块销毁（OnDispose）后触发，在途操作观察它并中止。
		/// 模块未初始化（m_barrier 未建）时返回不可取消的空令牌，避免提前访问抛 NRE。
		/// </summary>
		public CancellationToken Token => m_barrier?.Token ?? CancellationToken.None;

		/// <summary>
		/// 触发取消并等待在途操作完成清理后才返回。供框架重启取消清理。
		/// </summary>
		public UniTask CancelAsync() => m_barrier.CancelAsync();

		/// <summary>
		/// 获取或设置超时时间（秒）。
		/// </summary>
		public float Timeout { get; set; } = 5f;

		/// <summary>
		/// 每个服务器的最大连接数（下限 1）。
		/// 取 0/负数会让更新循环的并发槽位判定恒为 false，请求只入队不发送且无任何报错，故设置时下限兜底为 1。
		/// </summary>
		public int MaxConnectionPerServer
		{
			get => m_maxConnectionPerServer;
			set => m_maxConnectionPerServer = value < 1 ? 1 : value;
		}

		/// <summary>
		/// 每个服务器的最大连接数（后备字段）。
		/// </summary>
		private int m_maxConnectionPerServer = 8;

		/// <summary>
		/// 请求超时时间的 TimeSpan 表示。
		/// </summary>
		public TimeSpan ReqTimeout => TimeSpan.FromSeconds(Timeout);

		#region GET 返回字符串的请求

		/// <summary>
		/// 发送 GET 请求，返回字符串。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字符串结果。</returns>
		public UniTask<WebStringResult> GetToString(string url, CancellationToken token, object userData = null)
		{
			return GetToStringReq(url, null, null, token, userData);
		}

		/// <summary>
		/// 发送 GET 请求，返回字符串。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="queryString">请求参数。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字符串结果。</returns>
		public UniTask<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
		{
			return GetToStringReq(url, queryString, null, token, userData);
		}

		#endregion

		#region GET 返回字节数组的请求

		/// <summary>
		/// 发送 GET 请求，返回字节数组。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字节数组结果。</returns>
		public UniTask<WebBufferResult> GetToBytes(string url, CancellationToken token, object userData = null)
		{
			return GetToBytesReq(url, null, null, token, userData);
		}

		/// <summary>
		/// 发送 GET 请求，返回字节数组。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="queryString">请求参数。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字节数组结果。</returns>
		public UniTask<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
		{
			return GetToBytesReq(url, queryString, null, token, userData);
		}

		#endregion

		#region POST 返回字符串的请求

		/// <summary>
		/// 发送 POST 请求，返回字符串。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="from">请求参数。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字符串结果。</returns>
		public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, CancellationToken token, object userData = null)
			=> PostToStringReq(url, from, null, null, token, userData);

		/// <summary>
		/// 发送 POST 请求，返回字符串。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="from">表单请求参数。</param>
		/// <param name="queryString">URL 请求参数。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字符串结果。</returns>
		public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
			=> PostToStringReq(url, from, queryString, null, token, userData);

		#endregion

		#region POST 返回字节数组的请求

		/// <summary>
		/// 发送 POST 请求，返回字节数组。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="from">请求参数。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字节数组结果。</returns>
		public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, CancellationToken token, object userData = null)
			=> PostToBytesReq(url, from, null, null, token, userData);

		/// <summary>
		/// 发送 POST 请求，返回字节数组。
		/// </summary>
		/// <param name="url">请求地址。</param>
		/// <param name="from">表单请求参数。</param>
		/// <param name="queryString">URL 请求参数。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>字节数组结果。</returns>
		public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
			=> PostToBytesReq(url, from, queryString, null, token, userData);

		#endregion

		#region POST 返回 Pb 对象的请求

		/// <summary>
		/// 发送 Pb POST 请求，负责序列化、发送、接收、反序列化全过程。
		/// </summary>
		/// <param name="url">目标服务器的 URL 地址。</param>
		/// <param name="msgObj">要发送的消息对象，必须继承自 MessageObject。</param>
		/// <param name="token">调用方取消令牌。</param>
		/// <typeparam name="T">返回的数据类型，必须继承自 MessageObject 并且实现 IResponseMessage 接口。</typeparam>
		/// <returns>反序列化后的响应数据，成功时恒非 null。</returns>
		/// <remarks>
		/// 协议失败（响应为 JSON 错误信封、响应消息头反序列化失败、消息为空或 Id 无效、响应类型与 T 不匹配、消息体反序列化失败）抛
		/// <see cref="InvalidOperationException"/>；网络错误/超时抛 <see cref="TimeoutException"/> 或通用异常；
		/// 调用方取消抛 <see cref="OperationCanceledException"/>。调用方应 try-catch 或交由上层统一处理。
		/// </remarks>
		public async UniTask<T> Post<T>(string url, MessageObject msgObj, CancellationToken token) where T : MessageObject, IResponseMessage
		{
			var webBufferResult = await PostPbReq(url, msgObj, token);
			if (webBufferResult.IsNull())
				throw new InvalidOperationException($"Web 请求未返回结果: {url}");

			// 服务端协议层错误以 HTTP 200 + JSON 错误信封返回，不识别则 JSON 字节会被误当 protobuf 反序列化。
			// 信封判别仅凭正文首字节 '{'（Content-Type 头实测在成功响应上也可能误标 json，不作为依据）；
			// 再以正文能否解析出 code/message 信封结构为准，解析失败即按 protobuf 继续正常流程
			if (LooksLikeJson(webBufferResult.Result) && TryDescribeJsonEnvelope(Encoding.UTF8.GetString(webBufferResult.Result), out var envelopeDescription))
				throw new InvalidOperationException($"响应为 JSON 错误信封而非 protobuf: {url} ({envelopeDescription})");

			MessageHttpObject messageObjectHttp;
			try
			{
				messageObjectHttp = SerializerHelper.Deserialize<MessageHttpObject>(webBufferResult.Result);
			}
			catch (Exception e)
			{
				// 附正文片段：垃圾响应（如误发的 JSON/HTML）一眼可辨，不再只报 wire-type
				throw new InvalidOperationException($"响应消息头反序列化失败: {url} ({e.Message}) 正文片段: {PreviewBody(DecodeUtf8Prefix(webBufferResult.Result))}", e);
			}

			if (messageObjectHttp.IsNull() || messageObjectHttp.Id == default)
				throw new InvalidOperationException($"响应消息头解析失败（消息为空或 Id 无效）: {url}");

			var messageType = MessageIdRegistry.GetTypeById(messageObjectHttp.Id);
			if (messageType != typeof(T))
			{
				throw new InvalidOperationException($"响应消息类型不匹配: 期望 '{typeof(T).FullName}', 实际 '{messageType?.FullName ?? "未知"}'");
			}

			T result;
			try
			{
				result = SerializerHelper.Deserialize<T>(messageObjectHttp.Body);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException($"响应消息体反序列化失败: {messageType.FullName} ({e.Message})", e);
			}

			if (result.IsNull())
				throw new InvalidOperationException($"响应消息体反序列化为空: {messageType.FullName}");

			return result;
		}

		#endregion

		/// <summary>
		/// 判断字节序列是否形如 JSON 对象正文：首个非空白字节为 '{'。
		/// </summary>
		/// <param name="data">响应字节。</param>
		/// <returns>是否形如 JSON 对象。</returns>
		private static bool LooksLikeJson(byte[] data)
		{
			if (data == null)
				return false;

			// 跳过前导空白字节（空格/制表/回车/换行）后比对首字节
			foreach (var b in data)
			{
				if (b == 0x20 || b == 0x09 || b == 0x0D || b == 0x0A)
					continue;

				return b == 0x7B;
			}

			return false;
		}

		/// <summary>
		/// 尝试将响应正文解析为 JSON 错误信封：须含 code 或 message 字段（防任意 JSON 对象被误判为信封），
		/// 解析成功输出 code 与 message 原文。经 JObject 仅取两字段，不依赖 Data 字段类型（结构化 data 不致解析失败）。
		/// </summary>
		/// <param name="responseText">响应正文。</param>
		/// <param name="envelopeDescription">输出单行信封描述文本。</param>
		/// <returns>正文是否为可解析的 JSON 错误信封。</returns>
		private static bool TryDescribeJsonEnvelope(string responseText, out string envelopeDescription)
		{
			envelopeDescription = null;
			if (string.IsNullOrWhiteSpace(responseText))
				return false;

			try
			{
				var envelope = JObject.Parse(responseText);
				var codeToken     = envelope["code"];
				var messageToken  = envelope["message"];
				if (codeToken == null && messageToken == null)
					return false;

				var code    = codeToken?.Type == JTokenType.Integer ? codeToken.Value<int>() : 0;
				var message = messageToken?.Type == JTokenType.String ? messageToken.Value<string>() : null;
				envelopeDescription = $"code={code}, message={message}";
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 解码字节序列的 UTF-8 前缀（至多前 256 字节），用于异常信息中的正文预览，避免全量解码开销。
		/// </summary>
		/// <param name="data">响应字节。</param>
		/// <returns>解码文本。</returns>
		private static string DecodeUtf8Prefix(byte[] data)
		{
			if (data == null || data.Length == 0)
				return string.Empty;

			return Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 256));
		}

		/// <summary>
		/// 取正文前 100 字符的单行预览，用于内联到异常信息。
		/// </summary>
		/// <param name="body">响应正文。</param>
		/// <returns>单行预览文本，超长以省略号结尾。</returns>
		private static string PreviewBody(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return string.Empty;

			// 单次遍历生成单行预览：截取前 100 字符，并把全部换行类字符压平为空格
			var sb            = new System.Text.StringBuilder();
			var appendedCount = 0;
			foreach (var c in body)
			{
				if (appendedCount >= 100)
					break;

				if (c == '\r' || c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
				{
					sb.Append(' ');
					appendedCount++;
					continue;
				}

				sb.Append(c);
				appendedCount++;
			}

			return appendedCount >= 100 && body.Length > 100 ? sb.ToString() + "…" : sb.ToString();
		}
	}
}
