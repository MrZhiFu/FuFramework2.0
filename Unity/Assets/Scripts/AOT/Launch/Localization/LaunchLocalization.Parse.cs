using System.Collections.Generic;
using Luban;

namespace AOT.Launch.Localization
{
	/// <summary>
	/// LaunchLocalization 的数据解析分部。
	///     拆分产物每行仅 2 字段：key + value（目标语种译文），数据为 bin（Luban ByteBuf）。
	/// </summary>
	public partial class LaunchLocalization
	{
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
