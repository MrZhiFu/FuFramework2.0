using System;
using System.Text.Json;

namespace FuFramework.Foundation.Http.Normalization;

/// <summary>
/// 提供用于处理HTTP JSON结果的辅助方法。
/// </summary>
public static class HttpJsonResultHelper
{
	public static HttpJsonResultData<T> ToHttpJsonResultData<T>(this string jsonResult) where T : class, new()
	{
		HttpJsonResultData<T> httpJsonResultData = new HttpJsonResultData<T>
		{
			IsSuccess = false
		};
		try
		{
			HttpJsonResult httpJsonResult = JsonSerializer.Deserialize<HttpJsonResult>(jsonResult);
			if (httpJsonResult.Code != 0)
			{
				httpJsonResultData.Code = httpJsonResult.Code;
				return httpJsonResultData;
			}
			httpJsonResultData.IsSuccess = true;
			httpJsonResultData.Data = (T)(string.IsNullOrEmpty(httpJsonResult.Data) ? ((object)new T()) : ((object)JsonSerializer.Deserialize<T>(httpJsonResult.Data)));
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
		return httpJsonResultData;
	}
}
