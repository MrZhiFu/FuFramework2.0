using System.Collections.Generic;
using Luban;
using SimpleJSON;

namespace AOT.Launch.Localization
{
	/// <summary>
	/// LaunchLocalization 的数据解析分部。
	///     拆分产物每行仅 2 字段：key + value（目标语种译文）。
	///     json 变体用 SimpleJSON 逐行取两键；bin 变体用 Luban.ByteBuf 按记录数 + 2 字段顺序读取。
	/// </summary>
	public partial class LaunchLocalization
	{
		/// <summary>
		/// 解析 json 变体数据：顶层数组，每个元素为 { key, value }。
		/// </summary>
		/// <param name="json">表数据 json 文本</param>
		/// <param name="rows">接收行数据的字典（key → 译文）</param>
		private static void ParseJson(string json, Dictionary<string, string> rows)
		{
			foreach (var node in JSON.Parse(json).Children)
			{
				if (!node.IsObject) continue;

				var key = node["key"];
				if (string.IsNullOrEmpty(key)) continue;

				// 目标语种缺失（未翻译）时宽容反序列化为空串，兜底语义由运行时二段查询接管
				rows.TryAdd(key, node["value"]);
			}
		}

		/// <summary>
		/// 解析 bin 变体数据：记录数前置，之后按 2 字段顺序逐条读取（key=ReadString，value=ReadString）。
		/// </summary>
		/// <param name="bytes">表数据二进制</param>
		/// <param name="rows">接收行数据的字典（key → 译文）</param>
		private static void ParseBin(byte[] bytes, Dictionary<string, string> rows)
		{
			var buf = new ByteBuf(bytes);

			for (var n = buf.ReadSize(); n > 0; --n)
			{
				var key = buf.ReadString();
				if (string.IsNullOrEmpty(key)) continue;

				rows.TryAdd(key, buf.ReadString());
			}
		}
	}
}
