using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace FuFramework.Editor.Network
{
	/// <summary>
	/// 推送绑定注册表过期守卫：每次编译前比对「业务源码里的 [NotifyMessageHandler] 声明」与
	/// 「生成物 MessageRegistry.gen.cs 里的 BindNotify 绑定」，不一致即报错提示重跑生成脚本。
	/// 解决"新增推送监听后忘跑 gen-proto-registry → 该推送运行时静默失效"的坑。
	/// 比对键为（消息类型名, 方法名）二元组并**按出现次数计数比对**——支持多宿主监听同一消息
	/// （含不同宿主同名方法的场景），键不含宿主全名（宿主可达性错误由生成脚本自身报错拦截）。
	/// 手动入口（重新生成 / 校验）在 ProtoImporter 的"网络"菜单下。
	/// </summary>
	[InitializeOnLoad]
	public static class NotifyRegistryGuard
	{
		/// <summary>生成物：注册表（gen-proto-registry 的输出）</summary>
		private const string GenFilePath = "Assets/Scripts/Hotfix/Framework/Network/Register/AutoGen/MessageRegistry.gen.cs";

		/// <summary>业务源码扫描根（跳过 AutoGen）</summary>
		private const string HotfixRoot = "Assets/Scripts/Hotfix";

		/// <summary>业务源码里的声明：[NotifyMessageHandler(typeof(X), nameof(Y))]</summary>
		private static readonly Regex sr_declPattern = new(@"\[NotifyMessageHandler\(typeof\((\w+)\),\s*nameof\((\w+)\)\)", RegexOptions.Compiled);

		/// <summary>生成物里的绑定：BindNotify(typeof(X), static msgObj => Ns.Class.Y((X)msgObj))</summary>
		private static readonly Regex sr_bindPattern = new(@"BindNotify\(typeof\((\w+)\),\s*static\s+\w+\s*=>\s*[\w.]+\.(\w+)\(", RegexOptions.Compiled);

		static NotifyRegistryGuard()
		{
			CompilationPipeline.compilationStarted += OnCompilationStarted;
		}

		/// <summary>编译开始前比对注册表一致性，过期即报错（不阻断编译，本轮用旧表，跑完生成脚本后 refresh 即恢复一致）。</summary>
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
			var staleText   = staleInGen.Count   > 0 ? "\n  多余绑定（已删除未清理）：" + FormatCounts(staleInGen)   : string.Empty;
			Debug.LogError(
			               "[推送注册表过期] 业务源码的 [NotifyMessageHandler] 与 MessageRegistry.gen.cs 不一致："
			               + missingText + staleText
			               + "\n请运行菜单 FuFramework/网络/重新生成推送注册表（或 Protobuf/gen-proto-registry.bat），否则这些推送将静默失效（服务器不会补发）。");
		}

		/// <summary>
		/// 校验并输出结果（ProtoImporter"网络/校验推送注册表一致性"菜单的入口）：一致报绿含绑定条数，不一致报错分列缺失/多余。
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
				Debug.Log($"<color=green>[推送注册表] 一致：{sr_bindPattern.Matches(File.ReadAllText(Path.GetFullPath(GenFilePath))).Count} 条绑定与业务源码声明完全匹配。</color>");
				return;
			}

			Debug.LogError($"[推送注册表过期] 缺少绑定 {missingInGen.Count} 条、多余绑定 {staleInGen.Count} 条，请运行菜单 FuFramework/网络/重新生成推送注册表。");
		}

		/// <summary>执行比对（键按出现次数计数）：missingInGen=业务声明多于生成物绑定（差值）；staleInGen=生成物绑定多于业务声明（差值）。</summary>
		private static bool TryCompare(out Dictionary<string, int> missingInGen, out Dictionary<string, int> staleInGen)
		{
			missingInGen = new Dictionary<string, int>();
			staleInGen   = new Dictionary<string, int>();
			var genFullPath = Path.GetFullPath(GenFilePath);
			if (!File.Exists(genFullPath))
			{
				Debug.LogError($"[推送注册表] 生成物不存在：{GenFilePath}——请运行菜单 FuFramework/网络/重新生成推送注册表。");
				return false;
			}

			// 生成物侧：BindNotify 对计数（同键可出现多次——多宿主监听同一消息）
			var bindings = new Dictionary<string, int>();
			foreach (Match match in sr_bindPattern.Matches(File.ReadAllText(genFullPath)))
			{
				AddOrUpdate(bindings, $"{match.Groups[1].Value}.{match.Groups[2].Value}");
			}

			// 业务侧：声明对计数（跳过 AutoGen 生成物自身）
			var declarations   = new Dictionary<string, int>();
			var hotfixFullPath = Path.GetFullPath(HotfixRoot);
			foreach (var file in Directory.EnumerateFiles(hotfixFullPath, "*.cs", SearchOption.AllDirectories))
			{
				if (file.Contains($"{Path.DirectorySeparatorChar}AutoGen{Path.DirectorySeparatorChar}"))
				{
					continue;
				}

				foreach (Match match in sr_declPattern.Matches(File.ReadAllText(file)))
				{
					AddOrUpdate(declarations, $"{match.Groups[1].Value}.{match.Groups[2].Value}");
				}
			}

			foreach (var pair in declarations)
			{
				if (!bindings.TryGetValue(pair.Key, out var bindingCount))
				{
					missingInGen[pair.Key] = pair.Value;
				}
				else if (bindingCount < pair.Value)
				{
					missingInGen[pair.Key] = pair.Value - bindingCount;
				}
			}

			foreach (var pair in bindings)
			{
				if (!declarations.TryGetValue(pair.Key, out var declarationCount))
				{
					staleInGen[pair.Key] = pair.Value;
				}
				else if (declarationCount < pair.Value)
				{
					staleInGen[pair.Key] = pair.Value - declarationCount;
				}
			}

			return true;
		}

		/// <summary>键计数累加（同键出现多次时值 +1）。</summary>
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

		/// <summary>格式化计数差异条目（如 NotifyBagInfoChanged.OnX(差 1)）。</summary>
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
