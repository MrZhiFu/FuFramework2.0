using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuFramework.Foundation.Hash;
using FuFramework.Foundation.Http.Normalization;
using FuFramework.NetWork.Messages;
using FuFramework.Utility;

namespace FuFramework.NetWork.HTTP;

/// <summary>
/// 基础HTTP处理器，用于处理HTTP请求的基础逻辑。
/// </summary>
public abstract class BaseHttpHandler : IHttpHandler
{
	/// <summary>
	/// 校验时间差，用于生成签名时的时间偏移量。
	/// </summary>
	protected virtual int CheckCodeTime { get; } = 38848;

	/// <summary>
	/// 头校验码，用于生成签名时的头部校验码。
	/// </summary>
	protected virtual ushort CheckCodeStart { get; } = 88;

	/// <summary>
	/// 尾校验码，用于生成签名时的尾部校验码。
	/// </summary>
	protected virtual ushort CheckCodeEnd { get; } = 66;

	/// <summary>
	/// 是否需要校验签名，默认为不需要校验。
	/// </summary>
	public virtual bool IsCheckSign => false;

	/// <summary>
	/// 处理HTTP请求的异步操作，返回字符串结果。
	/// </summary>
	/// <param name="ip">客户端IP地址。</param>
	/// <param name="url">请求的URL。</param>
	/// <param name="paramMap">请求参数字典，键为参数名，值为参数值。</param>
	/// <returns>返回处理结果的字符串。</returns>
	public virtual Task<string> Action(string ip, string url, Dictionary<string, object> paramMap)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// 处理HTTP请求的异步操作，返回MessageObject对象。
	/// </summary>
	/// <param name="ip">客户端IP地址。</param>
	/// <param name="url">请求的URL。</param>
	/// <param name="paramMap">请求参数字典，键为参数名，值为参数值。</param>
	/// <param name="messageObject">消息对象，包含更多信息。</param>
	/// <returns>返回处理结果的MessageObject对象。</returns>
	public virtual Task<MessageObject> Action(string ip, string url, Dictionary<string, object> paramMap, MessageObject messageObject)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// 处理HTTP请求的异步操作，返回MessageObject对象。
	/// </summary>
	/// <param name="ip">客户端IP地址。</param>
	/// <param name="url">请求的URL。</param>
	/// <param name="request">请求参数对象。</param>
	/// <returns>返回处理结果的MessageObject对象。</returns>
	public virtual Task<string> Action(string ip, string url, HttpMessageRequestBase request)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// 获取签名字符串。
	/// </summary>
	/// <param name="str">待签名的字符串。</param>
	/// <returns>签名后的字符串。</returns>
	public string GetStringSign(string str)
	{
		string text = Md5Helper.Hash(str);
		ushort num = CheckCodeStart;
		ushort num2 = CheckCodeEnd;
		string text2 = text;
		foreach (char c in text2)
		{
			if (c >= 'a')
			{
				num += c;
			}
			else
			{
				num2 += c;
			}
		}
		return num + text + num2;
	}

	/// <summary>
	/// 校验签名是否有效。
	/// </summary>
	/// <param name="paramMap">请求参数字典。</param>
	/// <param name="error">错误消息，如果校验失败则返回具体的错误信息。</param>
	/// <returns>校验结果，true表示校验成功，false表示校验失败。</returns>
	public bool CheckSign(Dictionary<string, object> paramMap, out string error)
	{
		error = string.Empty;
		if (!IsCheckSign)
		{
			return true;
		}
		if (!paramMap.ContainsKey("sign") || !paramMap.ContainsKey("timestamp"))
		{
			error = HttpJsonResult.ValidationErrorString();
			return false;
		}
		string text = paramMap["sign"].ToString();
		string text2 = paramMap["timestamp"].ToString();
		long.TryParse(text2, out var result);
		if (TimeHelper.TimeSpanWithTimestamp(result).TotalMinutes > 5.0)
		{
			error = HttpJsonResult.IllegalString();
			return false;
		}
		string str = CheckCodeTime + text2;
		if (text == GetStringSign(str))
		{
			return true;
		}
		error = HttpJsonResult.ValidationErrorString();
		return false;
	}
}
