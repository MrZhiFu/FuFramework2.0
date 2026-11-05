using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

// ReSharper disable once CheckNamespace
namespace FuFramework.Editor.Network
{
	/// <summary>
	/// 推送绑定注册表过期守卫：每次编译前比对「业务源码里的 [NotifyMessageHandler] 声明」与
	/// 「生成物 MessageRegistry.gen.cs 里的 BindNotify 绑定」，不一致即报错提示重跑生成脚本。
	/// 解决“新增推送监听后忘跑 gen-proto-registry → 该推送运行时静默失效”的坑。
	/// 比对键为（消息类型名, 方法名）二元组并**按出现次数计数比对**——支持多宿主监听同一消息
	/// （含不同宿主同名方法的场景），键不含宿主全名（宿主可达性错误由生成脚本自身报错拦截）。
	/// 手动入口（重新生成 / 校验）在 ProtoImporter 的“网络”菜单下。
	/// </summary>
	[InitializeOnLoad]
	public static class NotifyRegistryGuard
	{
		/// <summary>
		/// 生成物：注册表（gen-proto-registry 的输出）
		/// </summary>
		private const string GEN_FILE_PATH = "Assets/Scripts/Hotfix/Framework/Network/Register/AutoGen/MessageRegistry.gen.cs";

		/// <summary>
		/// 业务源码扫描根（跳过 AutoGen）
		/// </summary>
		private const string HOTFIX_ROOT = "Assets/Scripts/Hotfix";

		/// <summary>
		/// 声明特性名（预筛关键字：正则匹配的必要子串，不含它的文件直接跳过剥注释与正则）
		/// </summary>
		private const string DECL_KEYWORD = "NotifyMessageHandler";

		/// <summary>
		/// 业务源码里的声明：[NotifyMessageHandler(typeof(X), nameof(Y)[, Priority = N])]
		/// </summary>
		private static readonly Regex sr_declPattern = new(@"\[NotifyMessageHandler\(typeof\((\w+)\),\s*nameof\((\w+)\)(?:,\s*Priority\s*=\s*(-?\d+))?\)", RegexOptions.Compiled);

		/// <summary>
		/// 生成物里的绑定：BindNotify(typeof(X), static msgObj => Ns.Class.Y((X)msgObj)[, priority: N]);
		/// </summary>
		private static readonly Regex sr_bindPattern = new(@"BindNotify\(typeof\((\w+)\),\s*static\s+\w+\s*=>\s*[\w.]+\.(\w+)\([^;]*?(?:,\s*priority:\s*(\d+))?\);", RegexOptions.Compiled);

		/// <summary>
		/// 订阅编译开始事件（InitializeOnLoad 触发一次）
		/// </summary>
		static NotifyRegistryGuard()
		{
			CompilationPipeline.compilationStarted += OnCompilationStarted;
		}

		/// <summary>
		/// 编译开始前比对注册表一致性，过期即报错（不阻断编译，本轮用旧表，跑完生成脚本后 refresh 即恢复一致）。
		/// </summary>
		/// <param name="_">编译开始事件参数（未使用）</param>
		private static void OnCompilationStarted(object _)
		{
			if (!TryCompare(out var missingInGen, out var staleInGen))
			{
				return;
			}

			if (missingInGen.Count == 0 && staleInGen.Count == 0)
			{
				return;
			}

			var missingText = missingInGen.Count > 0 ? "\n  缺少绑定（新增未生成）："  + FormatCounts(missingInGen) : string.Empty;
			var staleText   = staleInGen.Count   > 0 ? "\n  多余绑定（已删除未清理）：" + FormatCounts(staleInGen) : string.Empty;
			Debug.LogError("[推送注册表过期] 业务源码的 [NotifyMessageHandler] 与 MessageRegistry.gen.cs 不一致："
			               + missingText + staleText
			               + "\n请运行菜单 FuFramework/网络/重新生成推送注册表（或 Protobuf/gen-proto-registry.bat），否则这些推送将静默失效（服务器不会补发）。");
		}

		/// <summary>
		/// 校验并输出结果（ProtoImporter“网络/校验推送注册表一致性”菜单的入口）：一致报绿含绑定条数，不一致报错分列缺失/多余。
		/// </summary>
		public static void ValidateAndReport()
		{
			if (!TryCompare(out var missingInGen, out var staleInGen))
			{
				Debug.LogError("[推送注册表] 比对失败（生成物或源码缺失），详见上一条日志。");
				return;
			}

			if (missingInGen.Count == 0 && staleInGen.Count == 0)
			{
				var bindingCount = sr_bindPattern.Matches(File.ReadAllText(Path.GetFullPath(GEN_FILE_PATH))).Count;
				Debug.Log($"<color=green>[推送注册表] 一致：{bindingCount} 条绑定与业务源码声明完全匹配。</color>");
				return;
			}

			Debug.LogError($"[推送注册表过期] 缺少绑定 {missingInGen.Count} 条、多余绑定 {staleInGen.Count} 条，请运行菜单 FuFramework/网络/重新生成推送注册表。");
		}

		/// <summary>
		/// 执行比对（键按出现次数计数）：missingInGen=业务声明多于生成物绑定（差值）；staleInGen=生成物绑定多于业务声明（差值）。
		/// </summary>
		/// <param name="missingInGen">缺少绑定计数（业务声明多于生成物的差值）</param>
		/// <param name="staleInGen">多余绑定计数（生成物多于业务声明的差值）</param>
		/// <returns>生成物与业务源码均可读取返回 true；生成物缺失时报错并返回 false。</returns>
		private static bool TryCompare(out Dictionary<string, int> missingInGen, out Dictionary<string, int> staleInGen)
		{
			missingInGen = new Dictionary<string, int>();
			staleInGen   = new Dictionary<string, int>();
			if (!TryCountGenBindings(out var bindings))
			{
				return false;
			}

			var declarations = CountSourceDeclarations();
			ComputeDeficit(declarations, bindings,     missingInGen);
			ComputeDeficit(bindings,     declarations, staleInGen);
			return true;
		}

		/// <summary>
		/// 读取生成物并统计 BindNotify 绑定计数（键含优先级——改 Priority 数值也会被识别为差异；同键可出现多次——多宿主监听同一消息）。
		/// </summary>
		/// <param name="bindings">生成物绑定计数（键=消息类型.方法名:优先级，值=出现次数）</param>
		/// <returns>生成物存在返回 true；不存在时报错并返回 false。</returns>
		private static bool TryCountGenBindings(out Dictionary<string, int> bindings)
		{
			bindings = new Dictionary<string, int>();
			var genFullPath = Path.GetFullPath(GEN_FILE_PATH);
			if (!File.Exists(genFullPath))
			{
				Debug.LogError($"[推送注册表] 生成物不存在：{GEN_FILE_PATH}——请运行菜单 FuFramework/网络/重新生成推送注册表。");
				return false;
			}

			foreach (Match match in sr_bindPattern.Matches(File.ReadAllText(genFullPath)))
			{
				var priority = match.Groups[3].Success ? match.Groups[3].Value : "0";
				AddOrUpdate(bindings, $"{match.Groups[1].Value}.{match.Groups[2].Value}:P{priority}");
			}

			return true;
		}

		/// <summary>
		/// 扫描业务源码并统计 [NotifyMessageHandler] 声明计数（跳过 AutoGen 生成物自身；剥离行注释——防止 XML doc / 注释里的示例被当作真实声明）。
		/// </summary>
		/// <returns>业务源码声明计数（键=消息类型.方法名:优先级，值=出现次数）</returns>
		private static Dictionary<string, int> CountSourceDeclarations()
		{
			var declarations   = new Dictionary<string, int>();
			var hotfixFullPath = Path.GetFullPath(HOTFIX_ROOT);
			foreach (var file in Directory.EnumerateFiles(hotfixFullPath, "*.cs", SearchOption.AllDirectories))
			{
				if (file.Contains($"{Path.DirectorySeparatorChar}AutoGen{Path.DirectorySeparatorChar}"))
				{
					continue;
				}

				var text = File.ReadAllText(file);
				if (!text.Contains(DECL_KEYWORD)) // 廉价预筛：绝大多数业务文件不含推送声明，直接跳过剥注释与正则
				{
					continue;
				}

				foreach (Match match in sr_declPattern.Matches(StripLineComments(text)))
				{
					var priority = match.Groups[3].Success ? match.Groups[3].Value : "0";
					AddOrUpdate(declarations, $"{match.Groups[1].Value}.{match.Groups[2].Value}:P{priority}");
				}
			}

			return declarations;
		}

		/// <summary>
		/// 剥离各行 // 行注释（含 XML doc），返回拼接后的源码文本。
		/// </summary>
		/// <param name="text">单个源码文件全文</param>
		/// <returns>剥离行注释后按行拼接的源码文本</returns>
		private static string StripLineComments(string text)
		{
			var code = new StringBuilder(text.Length);
			var lines = text.Split('\n');
			foreach (var line in lines)
			{
				var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
				code.Append(commentIndex >= 0 ? line[..commentIndex] : line);
				code.Append('\n');
			}

			return code.ToString();
		}

		/// <summary>
		/// 计算两侧键计数的差值：source 某键多于 target 时，把差额记入 deficit（target 缺失或不足都算）。
		/// </summary>
		/// <param name="source">基准侧计数表</param>
		/// <param name="target">对照侧计数表</param>
		/// <param name="deficit">差值输出表（source 某键多于 target 时的差额）</param>
		private static void ComputeDeficit(Dictionary<string, int> source, Dictionary<string, int> target, Dictionary<string, int> deficit)
		{
			foreach (var pair in source)
			{
				if (!target.TryGetValue(pair.Key, out var targetCount))
				{
					deficit[pair.Key] = pair.Value;
				}
				else if (targetCount < pair.Value)
				{
					deficit[pair.Key] = pair.Value - targetCount;
				}
			}
		}

		/// <summary>
		/// 键计数累加（同键出现多次时值 +1）。
		/// </summary>
		/// <param name="dict">目标计数表</param>
		/// <param name="key">计数键（消息类型.方法名:优先级）</param>
		private static void AddOrUpdate(Dictionary<string, int> dict, string key)
		{
			if (dict.TryGetValue(key, out var count))
			{
				dict[key] = count + 1;
			}
			else
			{
				dict[key] = 1;
			}
		}

		/// <summary>
		/// 格式化计数差异条目（如 NotifyBagInfoChanged.OnX(差 1)）。
		/// </summary>
		/// <param name="counts">差值计数表</param>
		/// <returns>「键(差 N)」以顿号连接的文本</returns>
		private static string FormatCounts(Dictionary<string, int> counts)
		{
			var parts = new List<string>(counts.Count);
			foreach (var pair in counts)
			{
				parts.Add($"{pair.Key}(差 {pair.Value})");
			}

			return string.Join("、", parts);
		}
	}
}