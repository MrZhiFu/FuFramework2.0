using Luban;
using SimpleJSON;

namespace AOT.Launch.Localization
{
    /// <summary>
    /// LaunchLocalization 的数据解析分部。
    ///     json 变体用 SimpleJSON 逐行取字段；
    ///     bin 变体用 Luban.ByteBuf 按记录数 + Excel 列顺序读取（列序硬约定，调整表列须同步解析代码）。
    /// </summary>
    public partial class LaunchLocalization
    {
        /// <summary>
        /// 解析 json 变体数据：顶层数组，每个元素为一条行数据的对象。
        /// </summary>
        /// <param name="json">表数据 json 文本</param>
        private static void ParseJson(string json)
        {
            foreach (var node in JSON.Parse(json).Children)
            {
                if (!node.IsObject) continue;

                var row = new LocalizationAOT
                {
                    Key                = node["key"],
                    IsCode             = node["is_code"].AsBool,
                    ChineseSimplified  = node["ChineseSimplified"],
                    ChineseTraditional = node["ChineseTraditional"],
                    English            = node["English"],
                    Japanese           = node["Japanese"],
                    Korean             = node["Korean"],
                    Thai               = node["Thai"],
                    Indonesian         = node["Indonesian"],
                    French             = node["French"],
                    German             = node["German"],
                    Russian            = node["Russian"],
                    Italian            = node["Italian"],
                    PortugueseBrazil   = node["PortugueseBrazil"],
                    PortuguesePortugal = node["PortuguesePortugal"],
                    Spanish            = node["Spanish"],
                    Vietnamese         = node["Vietnamese"],
                };

                if (string.IsNullOrEmpty(row.Key)) continue;

                s_Rows.TryAdd(row.Key, row);
            }
        }

        /// <summary>
        /// 解析 bin 变体数据：记录数前置，之后按 Excel 列顺序逐条读取（字符串=ReadString，布尔=ReadBool）。
        /// </summary>
        /// <param name="bytes">表数据二进制</param>
        private static void ParseBin(byte[] bytes)
        {
            var buf = new ByteBuf(bytes);

            for (var n = buf.ReadSize(); n > 0; --n)
            {
                var row = new LocalizationAOT
                {
                    Key                = buf.ReadString(),
                    IsCode             = buf.ReadBool(),
                    ChineseSimplified  = buf.ReadString(),
                    ChineseTraditional = buf.ReadString(),
                    English            = buf.ReadString(),
                    Japanese           = buf.ReadString(),
                    Korean             = buf.ReadString(),
                    Thai               = buf.ReadString(),
                    Indonesian         = buf.ReadString(),
                    French             = buf.ReadString(),
                    German             = buf.ReadString(),
                    Russian            = buf.ReadString(),
                    Italian            = buf.ReadString(),
                    PortugueseBrazil   = buf.ReadString(),
                    PortuguesePortugal = buf.ReadString(),
                    Spanish            = buf.ReadString(),
                    Vietnamese         = buf.ReadString(),
                };

                if (string.IsNullOrEmpty(row.Key)) continue;

                s_Rows.TryAdd(row.Key, row);
            }
        }
    }
}