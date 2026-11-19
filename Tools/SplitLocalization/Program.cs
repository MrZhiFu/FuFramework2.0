using System.Text;
using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace FuFramework.SplitLocalization
{
	/// <summary>
	/// 本地化表按语种拆分工具。
	///     Luban 导出整表后，把 tblocalization（json/bin 双变体）拆分为 15 个单语种产物
	///     （每语种一个文件，目标语种字段兜底英语值，其余语种字段置空串），
	///     供运行时按当前语言只加载单语种数据（见 Docs/superpowers/specs/2026-11-18-多语言按语种拆分导出与加载-design.md）。
	///     另负责：删除整表源文件（不进打包，核对走 git 历史）、清理非当前变体的历史残留。
	/// 自校验内置：拆分结果抽样断言，失败即非 0 退出码，bat 链路中断。
	/// </summary>
	internal static class Program
	{
		/// <summary>
		/// 仓库根 = 工具程序集位置（Tools/SplitLocalization/bin/Release/net8.0/）向上回溯到含 Unity/ 目录的层级。
		/// 以 AppContext.BaseDirectory 为锚点，bat 无论从哪个目录调用都能定位。
		/// 声明须在 SourceDir/OutputDir 之前（静态字段按声明顺序初始化，后声明者初始化时前者才就绪）。
		/// </summary>
		private static readonly string RepoRoot = FindRepoRoot();

		/// <summary>
		/// 15 语种列名，顺序 = Excel 列序 = bean 构造参数序（来源: Bundles/Config/tblocalization.json 键序）
		/// </summary>
		private static readonly string[] LanguageFields =
		{
			"ChineseSimplified", "ChineseTraditional", "English", "Japanese", "Korean",
			"Thai", "Indonesian", "French", "German", "Russian",
			"Italian", "PortugueseBrazil", "PortuguesePortugal", "Spanish", "Vietnamese",
		};

		/// <summary>
		/// 整表源文件所在目录（相对仓库根；运行时以仓库根为基准，不依赖 CWD——bat 从 Config/ 目录调用本工具）
		/// </summary>
		private static readonly string SourceDir = Path.Combine(RepoRoot, "Unity/Assets/Bundles/Config");

		/// <summary>
		/// 拆分产物输出目录（相对仓库根，YooAsset 独立收集组）
		/// </summary>
		private static readonly string OutputDir = Path.Combine(RepoRoot, "Unity/Assets/Bundles/Localization");

		/// <summary>
		/// 表名主干（产物文件名 = {TableStem}_{语言枚举名}）
		/// </summary>
		private const string TableStem = "tblocalization";

		/// <summary>
		/// 自校验抽样行数
		/// </summary>
		private const int SampleRowCount = 3;

		/// <summary>
		/// 入口：--variant json|bin
		/// </summary>
		/// <param name="args">命令行参数</param>
		/// <returns>成功 0；失败非 0（错误已打印）</returns>
		private static int Main(string[] args)
		{
			// 解析变体参数（PowerShell/bash 把 "--variant json" 透传为两个独立参数，故按元素匹配）
			string variant = null;
			for (var i = 0; i < args.Length; i++)
			{
				if (args[i] == "--variant" && i + 1 < args.Length)
				{
					variant   = args[i + 1].Trim();
					break;
				}
			}

			if (variant != "json" && variant != "bin")
			{
				Console.Error.WriteLine("[SplitLocalization] usage: --variant json|bin");
				return 1;
			}

			Console.WriteLine($"[SplitLocalization] variant={variant}");

			try
			{
				// 读入整表源数据
				var rows = variant == "json" ? LoadJsonRows() : LoadBinRows();
				Console.WriteLine($"[SplitLocalization] loaded {rows.Count} rows from {SourceDir}/{TableStem}.{(variant == "json" ? "json" : "bytes")}");

				// 逐语种拆分写出
				Directory.CreateDirectory(OutputDir);
				foreach (var language in LanguageFields)
				{
					SplitAndWrite(rows, language, variant);
				}

				// 自校验：抽样断言拆分正确性，失败中断链路
				if (!Verify(rows, variant))
				{
					Console.Error.WriteLine("[SplitLocalization] SELF-VERIFY FAILED");
					return 2;
				}

				// 清理：删除源文件 + 旧版 .json 残留
				CleanupSource(variant);
				CleanupOtherVariant(variant);

				Console.WriteLine($"[SplitLocalization] OK: {LanguageFields.Length} files -> {OutputDir}");
				return 0;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"[SplitLocalization] ERROR: {e.Message}");
				return 1;
			}
		}

		/// <summary>
		/// 从程序集位置向上回溯找仓库根（含 Unity/ 与 Tools/ 两个目录的层级）
		/// </summary>
		/// <returns>仓库根绝对路径；找不到抛异常（部署位置错误）</returns>
		private static string FindRepoRoot()
		{
			var dir = AppContext.BaseDirectory;
			for (var i = 0; i < 10; i++)
			{
				if (Directory.Exists(Path.Combine(dir, "Unity")) && Directory.Exists(Path.Combine(dir, "Tools")))
					return dir;
				var parent = Directory.GetParent(dir);
				if (parent == null) break;
				dir = parent.FullName;
			}

			throw new InvalidOperationException($"repo root not found from {AppContext.BaseDirectory} (expect a dir containing Unity/ and Tools/)");
		}

		/// <summary>
		/// 源表行数据（字段名与 json 键序一致）
		/// </summary>
		private sealed class SourceRow
		{
			public string Key;
			public bool IsCode;
			public readonly Dictionary<string, string> Texts = new();
		}

		/// <summary>
		/// 读取 json 变体整表
		/// </summary>
		/// <returns>行数据列表</returns>
		private static List<SourceRow> LoadJsonRows()
		{
			var path = Path.Combine(SourceDir, $"{TableStem}.json");
			var json = File.ReadAllText(path, Encoding.UTF8);
			using var doc = JsonDocument.Parse(json);

			var rows = new List<SourceRow>();
			foreach (var element in doc.RootElement.EnumerateArray())
			{
				var row = new SourceRow
				{
					Key    = GetStringProperty(element, "key"),
					IsCode = element.TryGetProperty("is_code", out var isCodeProp) && isCodeProp.ValueKind == JsonValueKind.True,
				};

				foreach (var field in LanguageFields)
				{
					row.Texts[field] = GetStringProperty(element, field);
				}

				rows.Add(row);
			}

			return rows;
		}

		/// <summary>
		/// 读取 bin 变体整表（Luban ByteBuf：记录数前置 + 逐行 key/is_code/15 语种字符串，读序对齐 LaunchLocalization.Parse.cs）
		/// </summary>
		/// <returns>行数据列表</returns>
		private static List<SourceRow> LoadBinRows()
		{
			var path  = Path.Combine(SourceDir, $"{TableStem}.bytes");
			var bytes = File.ReadAllBytes(path);
			var buf   = new SimpleByteBuf(bytes);

			var n = buf.ReadSize();
			var rows = new List<SourceRow>(n);
			for (var i = 0; i < n; i++)
			{
				var row = new SourceRow { Key = buf.ReadString(), IsCode = buf.ReadBool() };
				foreach (var field in LanguageFields)
				{
					row.Texts[field] = buf.ReadString();
				}
				rows.Add(row);
			}

			return rows;
		}

		/// <summary>
		/// 取对象属性字符串值；缺失或非字符串按空串（生成代码反序列化要求 17 字段齐全，空串合法）
		/// </summary>
		private static string GetStringProperty(JsonElement element, string name)
		{
			if (!element.TryGetProperty(name, out var prop)) return string.Empty;
			return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? string.Empty : string.Empty;
		}

		/// <summary>
		/// 按语种拆分写出单个产物文件。
		/// 目标语种字段 = 原值非空 ? 原值 : 英语值（英语也空则空串，与运行时兜底语义一致）；其余语种字段 = 空串。
		/// </summary>
		private static void SplitAndWrite(List<SourceRow> rows, string language, string variant)
		{
			if (variant == "json")
			{
				WriteJsonFile(rows, language);
			}
			else
			{
				WriteBinFile(rows, language);
			}
		}

		/// <summary>
		/// 写 bin 变体单语种产物（Luban ByteBuf 格式：WriteSize(行数) + 逐行 key/is_code/15 语种字符串，与 json 变体同一兜底语义）
		/// </summary>
		private static void WriteBinFile(List<SourceRow> rows, string language)
		{
			var buf = new SimpleByteBuf();
			buf.WriteSize(rows.Count);
			foreach (var row in rows)
			{
				buf.WriteString(row.Key);
				buf.WriteBool(row.IsCode);
				foreach (var field in LanguageFields)
				{
					buf.WriteString(field == language ? ResolveTargetValue(row, language) : string.Empty);
				}
			}

			var path = Path.Combine(OutputDir, $"{TableStem}_{language}.bytes");
			File.WriteAllBytes(path, buf.ToBytes());
		}

		/// <summary>
		/// Luban ByteBuf 最小读写实现（对齐 Tools/Luban/source/src/Luban.Core/Serialization/ByteBuf.cs 的编码）。
		/// 只覆盖本工具用到的操作：WriteSize/ReadSize（变长 uint）、WriteBool/ReadBool（1 字节）、WriteString/ReadString（变长长度前缀 + UTF-8）。
		/// Luban 3.x 变长 uint 编码：按值域 1-5 字节，最高位段作长度标志（0x80/0xc0/0xe0/0xf0），低段与后续字节按大端位宽拼接。
		/// </summary>
		private sealed class SimpleByteBuf
		{
			private byte[] m_Bytes;
			private int m_WriteIndex;
			private int m_ReadIndex;

			public SimpleByteBuf() => m_Bytes = new byte[256];

			public SimpleByteBuf(byte[] bytes) => m_Bytes = bytes;

			/// <summary>
			/// 导出写出缓冲（截断到实际写长度）
			/// </summary>
			public byte[] ToBytes()
			{
				var result = new byte[m_WriteIndex];
				Buffer.BlockCopy(m_Bytes, 0, result, 0, m_WriteIndex);
				return result;
			}

			public void WriteBool(bool b)
			{
				EnsureWrite(1);
				m_Bytes[m_WriteIndex++] = (byte)(b ? 1 : 0);
			}

			public bool ReadBool()
			{
				EnsureRead(1);
				return m_Bytes[m_ReadIndex++] != 0;
			}

			public void WriteSize(int n) => WriteUint((uint)n);

			public int ReadSize() => (int)ReadUint();

			public void WriteString(string x)
			{
				var n = x != null ? Encoding.UTF8.GetByteCount(x) : 0;
				WriteSize(n);
				if (n > 0)
				{
					EnsureWrite(n);
					Encoding.UTF8.GetBytes(x, 0, x.Length, m_Bytes, m_WriteIndex);
					m_WriteIndex += n;
				}
			}

			public string ReadString()
			{
				var n = ReadSize();
				if (n <= 0) return string.Empty;

				EnsureRead(n);
				var s = Encoding.UTF8.GetString(m_Bytes, m_ReadIndex, n);
				m_ReadIndex += n;
				return s;
			}

			/// <summary>
			/// 变长 uint（编码对齐 Luban ByteBuf.WriteUint：值域分档 + 高位段长度标志）
			/// </summary>
			private void WriteUint(uint x)
			{
				if (x < 0x80)
				{
					EnsureWrite(1);
					m_Bytes[m_WriteIndex++] = (byte)x;
				}
				else if (x < 0x4000)
				{
					EnsureWrite(2);
					m_Bytes[m_WriteIndex + 1] = (byte)x;
					m_Bytes[m_WriteIndex]     = (byte)((x >> 8) | 0x80);
					m_WriteIndex += 2;
				}
				else if (x < 0x20_0000)
				{
					EnsureWrite(3);
					m_Bytes[m_WriteIndex + 2] = (byte)x;
					m_Bytes[m_WriteIndex + 1] = (byte)(x >> 8);
					m_Bytes[m_WriteIndex]     = (byte)((x >> 16) | 0xc0);
					m_WriteIndex += 3;
				}
				else if (x < 0x1000_0000)
				{
					EnsureWrite(4);
					m_Bytes[m_WriteIndex + 3] = (byte)x;
					m_Bytes[m_WriteIndex + 2] = (byte)(x >> 16);
					m_Bytes[m_WriteIndex + 1] = (byte)(x >> 8);
					m_Bytes[m_WriteIndex]     = (byte)((x >> 24) | 0xe0);
					m_WriteIndex += 4;
				}
				else
				{
					EnsureWrite(5);
					m_Bytes[m_WriteIndex + 4] = (byte)x;
					m_Bytes[m_WriteIndex + 3] = (byte)(x >> 24);
					m_Bytes[m_WriteIndex + 2] = (byte)(x >> 16);
					m_Bytes[m_WriteIndex + 1] = (byte)(x >> 8);
					m_Bytes[m_WriteIndex]     = (byte)(0xf0 | (byte)(x >> 32));
					m_WriteIndex += 5;
				}
			}

			/// <summary>
			/// 变长 uint 读取（编码对齐 Luban ByteBuf.ReadUint）
			/// </summary>
			private uint ReadUint()
			{
				EnsureRead(1);
				uint h = m_Bytes[m_ReadIndex];
				if (h < 0x80)
				{
					m_ReadIndex++;
					return h;
				}
				if (h < 0xc0)
				{
					EnsureRead(2);
					uint x = ((h & 0x3f) << 8) | m_Bytes[m_ReadIndex + 1];
					m_ReadIndex += 2;
					return x;
				}
				if (h < 0xe0)
				{
					EnsureRead(3);
					uint x = ((h & 0x1f) << 16) | ((uint)m_Bytes[m_ReadIndex + 1] << 8) | m_Bytes[m_ReadIndex + 2];
					m_ReadIndex += 3;
					return x;
				}
				if (h < 0xf0)
				{
					EnsureRead(4);
					uint x = ((h & 0x0f) << 24) | ((uint)m_Bytes[m_ReadIndex + 1] << 16) | ((uint)m_Bytes[m_ReadIndex + 2] << 8) | m_Bytes[m_ReadIndex + 3];
					m_ReadIndex += 4;
					return x;
				}

				EnsureRead(5);
				uint y = ((uint)(h & 0x0f) << 32) | ((uint)m_Bytes[m_ReadIndex + 1] << 24) | ((uint)m_Bytes[m_ReadIndex + 2] << 16)
				       | ((uint)m_Bytes[m_ReadIndex + 3] << 8) | m_Bytes[m_ReadIndex + 4];
				m_ReadIndex += 5;
				return y;
			}

			private void EnsureWrite(int n)
			{
				if (m_WriteIndex + n > m_Bytes.Length)
				{
					var newSize = m_Bytes.Length * 2;
					while (m_WriteIndex + n > newSize) newSize *= 2;
					var newBytes = new byte[newSize];
					Buffer.BlockCopy(m_Bytes, 0, newBytes, 0, m_WriteIndex);
					m_Bytes = newBytes;
				}
			}

			private void EnsureRead(int n)
			{
				if (m_ReadIndex + n > m_Bytes.Length)
					throw new EndOfStreamException($"SimpleByteBuf read overflow: need {n} at index {m_ReadIndex}, total {m_Bytes.Length}");
			}
		}

		/// <summary>
		/// 写 json 变体单语种产物（17 字段齐全对齐生成代码反序列化；UTF-8 无 BOM）。
		/// 扩展名统一 .bytes（YooAsset 收集器路径与变体解耦的裁决：json 变体内容是 json 文本、
		/// bin 变体内容是 ByteBuf 二进制，两者同为 TextAsset，运行时按变体选解析方式）。
		/// </summary>
		private static void WriteJsonFile(List<SourceRow> rows, string language)
		{
			var sb = new StringBuilder();
			sb.Append('[');
			foreach (var row in rows)
			{
				var targetValue = ResolveTargetValue(row, language);
				sb.Append("\n  {");
				sb.Append($"\n    \"key\": {JsonString(row.Key)},");
				sb.Append($"\n    \"is_code\": {(row.IsCode ? "true" : "false")},");
				for (var i = 0; i < LanguageFields.Length; i++)
				{
					var field = LanguageFields[i];
					var value = field == language ? targetValue : string.Empty;
					var comma = i < LanguageFields.Length - 1 ? "," : string.Empty;
					sb.Append($"\n    \"{field}\": {JsonString(value)}{comma}");
				}
				sb.Append("\n  },");
			}

			// 去掉最后一行的尾逗号，闭合数组
			if (rows.Count > 0) sb.Remove(sb.Length - 1, 1);
			sb.Append(rows.Count > 0 ? "\n]" : "]");

			var path = Path.Combine(OutputDir, $"{TableStem}_{language}.bytes");
			File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
		}

		/// <summary>
		/// 目标语种字段值 = 原值非空 ? 原值 : 英语值（英语也空则空串，与运行时兜底语义一致）。
		/// json/bin 双变体兜底判定共用。
		/// </summary>
		private static string ResolveTargetValue(SourceRow row, string language)
		{
			if (row.Texts.TryGetValue(language, out var value) && value.Length > 0) return value;
			if (language == "English") return string.Empty; // 英语自身即兜底源，为空则空串
			return row.Texts.TryGetValue("English", out var english) ? english : string.Empty;
		}

		/// <summary>
		/// json 字符串转义（key/文本可能含引号与换行）。
		/// UnsafeRelaxedJsonEscaping：保留 UTF-8 中文原文不转 \uXXXX（与 Luban 整表导出风格一致，产物可肉眼核对/diff）；
		/// 该 Encoder 只用于此处手写的 JSON 产物写出，非 HTML 场景无安全影响。
		/// </summary>
		private static string JsonString(string value) => JsonSerializer.Serialize(value, JsonWriteOptions);

		/// <summary>
		/// 产物写出用序列化选项（非 ASCII 保留原文）
		/// </summary>
		private static readonly JsonSerializerOptions JsonWriteOptions = new()
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};

		/// <summary>
		/// 自校验：抽样断言输出行数一致、目标语种字段值正确（含兜底）、非目标语种字段全空串。
		/// json 产物用 JsonDocument 读回断言；bin 产物用 SimpleByteBuf 读回断言（同一行断言函数复用）。
		/// 产物扩展名统一 .bytes（见 WriteJsonFile 注释的变体解耦裁决），内容格式随变体。
		/// </summary>
		/// <returns>全部断言通过 true</returns>
		private static bool Verify(List<SourceRow> rows, string variant)
		{
			foreach (var language in LanguageFields)
			{
				var path = Path.Combine(OutputDir, $"{TableStem}_{language}.bytes");
				if (!File.Exists(path))
				{
					Console.Error.WriteLine($"[SplitLocalization] verify: missing {path}");
					return false;
				}

				// 行数断言
				var outputCount = variant == "json" ? CountJsonRows(path) : CountBinRows(path);
				if (outputCount != rows.Count)
				{
					Console.Error.WriteLine($"[SplitLocalization] verify({language}): row count {outputCount} != {rows.Count}");
					return false;
				}

				// 抽样断言（首/中/尾）：逐产物变体读回指定行
				foreach (var index in SampleIndexes(rows.Count))
				{
					var pass = variant == "json"
						? VerifyJsonRow(path, rows[index], language, index)
						: VerifyBinRow(path, rows[index], language, index);
					if (!pass) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// 数 json 产物行数
		/// </summary>
		private static int CountJsonRows(string path)
		{
			using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
			var count = 0;
			foreach (var _ in doc.RootElement.EnumerateArray()) count++;
			return count;
		}

		/// <summary>
		/// 数 bin 产物行数（读记录数前缀）
		/// </summary>
		private static int CountBinRows(string path)
		{
			var buf = new SimpleByteBuf(File.ReadAllBytes(path));
			return buf.ReadSize();
		}

		/// <summary>
		/// 读回 json 产物指定行并断言
		/// </summary>
		private static bool VerifyJsonRow(string path, SourceRow sourceRow, string language, int index)
		{
			using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
			var rowIndex = 0;
			foreach (var row in doc.RootElement.EnumerateArray())
			{
				if (rowIndex++ != index) continue;
				return VerifyRowFields(GetStringProperty(row, "key"), GetStringProperty(row, language), row, sourceRow, language, index);
			}
			return false;
		}

		/// <summary>
		/// 读回 bin 产物指定行并断言（跳过前 index 行）
		/// </summary>
		private static bool VerifyBinRow(string path, SourceRow sourceRow, string language, int index)
		{
			var buf = new SimpleByteBuf(File.ReadAllBytes(path));
			buf.ReadSize(); // 记录数前缀
			for (var i = 0; i < index; i++)
			{
				buf.ReadString(); // key
				buf.ReadBool();   // is_code
				for (var f = 0; f < LanguageFields.Length; f++) buf.ReadString();
			}

			var key      = buf.ReadString();
			var isCode   = buf.ReadBool();
			var nonTargetEmpty = true;
			var targetValue    = string.Empty;
			foreach (var field in LanguageFields)
			{
				var value = buf.ReadString();
				if (field == language) targetValue = value;
				else if (value.Length > 0) nonTargetEmpty = false;
			}

			if (!isCode && !sourceRow.IsCode) { /* is_code 一致 */ }
			else if (isCode != sourceRow.IsCode)
			{
				Console.Error.WriteLine($"[SplitLocalization] verify({language})[{index}]: is_code mismatch");
				return false;
			}

			return VerifyRowFields(key, targetValue, null, sourceRow, language, index, nonTargetEmpty);
		}

		/// <summary>
		/// 行字段断言（json/bin 共用）：key 一致、目标语种 = 兜底值、非目标语种全空串
		/// </summary>
		/// <param name="rowElement">json 行节点（bin 传 null）</param>
		/// <param name="binNonTargetEmpty">bin 变体的非目标字段空串标记</param>
		private static bool VerifyRowFields(string key, string targetValue, JsonElement? rowElement, SourceRow sourceRow, string language, int index, bool binNonTargetEmpty = true)
		{
			if (key != sourceRow.Key)
			{
				Console.Error.WriteLine($"[SplitLocalization] verify({language})[{index}]: key mismatch");
				return false;
			}

			if (targetValue != ResolveTargetValue(sourceRow, language))
			{
				Console.Error.WriteLine($"[SplitLocalization] verify({language})[{index}]: target field mismatch");
				return false;
			}

			// 非目标字段全空串检查：bin 已在读回时聚合为标记；json 逐字段检查
			if (rowElement.HasValue)
			{
				foreach (var field in LanguageFields)
				{
					if (field == language) continue;
					if (GetStringProperty(rowElement.Value, field).Length > 0)
					{
						Console.Error.WriteLine($"[SplitLocalization] verify({language})[{index}]: non-target field '{field}' not empty");
						return false;
					}
				}
			}
			else if (!binNonTargetEmpty)
			{
				Console.Error.WriteLine($"[SplitLocalization] verify({language})[{index}]: non-target field not empty");
				return false;
			}

			return true;
		}

		/// <summary>
		/// 抽样单行断言：key 一致、目标语种字段 = 兜底语义值、其余语种字段全空串
		/// </summary>
		/// <returns>通过 true；失败已打印原因</returns>
		private static bool VerifyOneRow(JsonElement outputRow, SourceRow sourceRow, string language)
		{
			if (GetStringProperty(outputRow, "key") != sourceRow.Key)
			{
				Console.Error.WriteLine($"[SplitLocalization] verify({language}): key mismatch");
				return false;
			}

			if (GetStringProperty(outputRow, language) != ResolveTargetValue(sourceRow, language))
			{
				Console.Error.WriteLine($"[SplitLocalization] verify({language}): target field mismatch");
				return false;
			}

			foreach (var field in LanguageFields)
			{
				if (field == language) continue;
				if (GetStringProperty(outputRow, field).Length > 0)
				{
					Console.Error.WriteLine($"[SplitLocalization] verify({language}): non-target field '{field}' not empty");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// 抽样行索引：首、中、尾各一（手写去重，小行数无重复场景，直接顺序返回）
		/// </summary>
		private static List<int> SampleIndexes(int rowCount)
		{
			var indexes = new List<int> { 0 };
			if (rowCount > 2 && rowCount / 2 != 0 && rowCount / 2 != rowCount - 1) indexes.Add(rowCount / 2);
			if (rowCount > 1) indexes.Add(rowCount - 1);
			return indexes;
		}

		/// <summary>
		/// 删除整表源文件（不进打包，核对走 git 历史）；不动 .meta
		/// </summary>
		private static void CleanupSource(string variant)
		{
			var ext = variant == "json" ? "json" : "bytes";
			var path = Path.Combine(SourceDir, $"{TableStem}.{ext}");
			if (File.Exists(path))
			{
				File.Delete(path);
				Console.WriteLine($"[SplitLocalization] removed source {path}");
			}
		}

		/// <summary>
		/// 清理输出目录中的旧版 .json 产物残留（扩展名统一 .bytes 裁决前的历史产物）。
		/// 当前产物全部为 .bytes 且按语种同名覆写，双变体天然互斥，无需按变体清理。
		/// </summary>
		private static void CleanupOtherVariant(string variant)
		{
			if (!Directory.Exists(OutputDir)) return;

			foreach (var file in Directory.GetFiles(OutputDir, "*.json"))
			{
				File.Delete(file);
				Console.WriteLine($"[SplitLocalization] cleaned stale {file}");
			}
		}
	}
}
