using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FuFramework.Foundation.Http.Normalization;
using FuFramework.Foundation.Json;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.Messages;
using FuFramework.ProtoBuf.Net;
using FuFramework.Utility.Extensions;
using FuFramework.Utility.Setting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FuFramework.NetWork.HTTP;

/// <summary>
/// HTTP处理器，用于处理HTTP请求
/// </summary>
public static class HttpHandler
{
	private const string JsonContentType = "application/json; charset=utf-8";

	private const string ProtoBufContentType = "application/x-protobuf";

	/// <summary>
	/// 处理HTTP请求
	/// </summary>
	/// <param name="context">HTTP上下文</param>
	/// <param name="baseHandler">基础HTTP处理器工厂方法</param>
	/// <param name="aopHandlerTypes">AOP处理器列表，可选</param>
	public static async Task HandleRequest(HttpContext context, Func<string, BaseHttpHandler> baseHandler, List<IHttpAopHandler> aopHandlerTypes = null)
	{
		string ip = context.Connection.RemoteIpAddress?.ToString();
		string url = context.Request.PathBase + context.Request.Path;
		string command = context.Request.Path.ToString().Substring(GlobalSettings.CurrentSetting.HttpUrl.Length);
		string logHeader = $"[HTTPServer] TraceIdentifier:[{context.TraceIdentifier}], 来源[{ip}], url:[{url}]";
		LogHelper.Debug(logHeader + "，请求方式:[" + context.Request.Method + "]");
		try
		{
			Dictionary<string, object> paramMap = new Dictionary<string, object>();
			foreach (KeyValuePair<string, StringValues> item in context.Request.Query)
			{
				paramMap.Add(item.Key, item.Value.ToString());
			}
			context.Response.Headers.ContentType = "application/json; charset=utf-8";
			MessageObject message = null;
			string contentType = context.Request.ContentType;
			if (contentType.IsNullOrWhiteSpace())
			{
				await context.Response.WriteAsync("http header content type is null");
				return;
			}
			bool isProtoBuf = contentType.Equals("application/x-protobuf", StringComparison.OrdinalIgnoreCase);
			if (isProtoBuf)
			{
				using MemoryStream memoryStream = new MemoryStream();
				await context.Request.Body.CopyToAsync(memoryStream);
				MessageHttpObject messageHttpObject = ProtoBufSerializerHelper.Deserialize<MessageHttpObject>(memoryStream.ToArray());
				Type messageTypeById = MessageProtoHelper.GetMessageTypeById(messageHttpObject.Id);
				message = (MessageObject)ProtoBufSerializerHelper.Deserialize(messageHttpObject.Body, messageTypeById);
				message.SetMessageId(messageHttpObject.Id);
				message.SetUniqueId(messageHttpObject.UniqueId);
			}
			else
			{
				if (!context.Request.HasJsonContentType())
				{
					await context.Response.WriteAsync(HttpJsonResult.ErrorString(13, "不支持的Content Type: " + contentType));
					return;
				}
				Dictionary<string, object> dictionary = JsonHelper.Deserialize<Dictionary<string, object>>(await new StreamReader(context.Request.Body).ReadToEndAsync());
				foreach (KeyValuePair<string, object> item2 in dictionary)
				{
					if (!paramMap.TryAdd(item2.Key, item2.Value))
					{
						await context.Response.WriteAsync(HttpJsonResult.ErrorString(13, "参数重复了:" + item2.Key));
						return;
					}
				}
			}
			if (paramMap.Count > 0)
			{
				LogHelper.Debug("请求参数:" + JsonHelper.Serialize(paramMap));
			}
			if (command.IsNullOrEmptyOrWhiteSpace())
			{
				await context.Response.WriteAsync(HttpJsonResult.ErrorString(11, "undefined command"));
				return;
			}
			if (!GlobalSettings.IsAppRunning)
			{
				await context.Response.WriteAsync(HttpJsonResult.ErrorString(15, "服务器状态错误[正在起/关服]"));
				return;
			}
			if (aopHandlerTypes != null && aopHandlerTypes.Count > 0)
			{
				foreach (IHttpAopHandler aopHandlerType in aopHandlerTypes)
				{
					if (!aopHandlerType.Run(context, ip, url, paramMap))
					{
						return;
					}
				}
			}
			BaseHttpHandler baseHttpHandler = baseHandler(command);
			if (baseHttpHandler == null)
			{
				LogHelper.Warn("http cmd handler 不存在：" + command);
				await context.Response.WriteAsync(HttpJsonResult.NotFoundString());
				return;
			}
			if (!baseHttpHandler.CheckSign(paramMap, out var error))
			{
				await context.Response.WriteAsync(error);
				return;
			}
			if (isProtoBuf)
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				MessageObject messageObject = await baseHttpHandler.Action(ip, url, paramMap, message);
				stopwatch.Stop();
				LogHelper.Debug($"{logHeader},执行时间：{stopwatch.ElapsedMilliseconds}ms, 结果: {messageObject}");
				if (messageObject.IsNotNull())
				{
					ReadOnlyMemory<byte> readOnlyMemory = ProtoBufSerializerHelper.Serialize(messageObject);
					byte[] array = ProtoBufSerializerHelper.Serialize(new MessageHttpObject
					{
						Id = MessageProtoHelper.GetMessageIdByType(messageObject),
						UniqueId = message.UniqueId,
						Body = readOnlyMemory.ToArray()
					});
					context.Response.ContentLength = array.Length;
					await context.Response.BodyWriter.WriteAsync(array);
				}
				return;
			}
			HttpMessageRequestAttribute customAttribute = baseHttpHandler.GetType().GetCustomAttribute<HttpMessageRequestAttribute>();
			if (customAttribute != null)
			{
				customAttribute.MessageType.CheckNotNull("MessageType");
				HttpMessageRequestBase httpMessageRequestBase = (HttpMessageRequestBase)JsonHelper.Deserialize(JsonHelper.Serialize(paramMap), customAttribute.MessageType);
				List<ValidationResult> list = new List<ValidationResult>();
				ValidationContext validationContext = new ValidationContext(httpMessageRequestBase, null, null);
				if (Validator.TryValidateObject(httpMessageRequestBase, validationContext, list, validateAllProperties: true))
				{
					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					string text = await baseHttpHandler.Action(ip, url, httpMessageRequestBase);
					stopwatch.Stop();
					LogHelper.Debug($"{logHeader}, 执行时间：{stopwatch.ElapsedMilliseconds}ms, 结果: {text}");
					await context.Response.WriteAsync(text);
				}
				else if (list.Count <= 0)
				{
					await context.Response.WriteAsync(HttpJsonResult.ErrorString(400, "data verification failed"));
				}
				else
				{
					await context.Response.WriteAsync(HttpJsonResult.ErrorString(400, list[0].ErrorMessage));
				}
			}
			else
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				string text2 = await baseHttpHandler.Action(ip, url, paramMap);
				stopwatch.Stop();
				LogHelper.Debug($"{logHeader}, 执行时间：{stopwatch.ElapsedMilliseconds}ms, 结果: {text2}");
				await context.Response.WriteAsync(text2);
			}
		}
		catch (Exception ex)
		{
			LogHelper.Error(logHeader + ", 发生异常. {0} {1}", ex.Message, new object[1] { ex.StackTrace });
			await context.Response.WriteAsync(HttpJsonResult.ServerErrorString());
		}
	}
}
