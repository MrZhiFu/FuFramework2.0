using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FuFramework.Editor.Network
{
	/// <summary>
	/// 推送绑定注册表过期守卫：每次编译前比对「业务源码里的 [NotifyMessageHandler] 声明」与
	/// 「生成物 MessageRegistry.gen.cs 里的 BindNotify 绑定」，不一致即报错提示重跑生成脚本。
	/// 解决"新增推送监听后忘跑 gen-proto-registry.bat → 该推送运行时静默失效"的坑。
	/// 比对键为（消息类型名, 方法名）二元组，不涉及宿主全名解析——宿主可达性错误由生成脚本自身报错拦截。
	/// </summary>
	[InitializeOnLoad]
	public static class NotifyRegistryGuard
	{
		/// <summary>生成物：注册表（gen-proto-registry.py 的输出）</summary>
		private const string GenFilePath = "Assets/Scripts/Hotfix/Framework/Network/Register/AutoGen/MessageRegistry.gen.cs";

		/// <summary>业务源码扫描根（跳过 AutoGen 与 Editor）</summary>
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

			var missingText = missingInGen.Count > 0 ? "\n  缺少绑定（新增未生成）：" + string.Join("、", missingInGen) : string.Empty;
			var staleText = staleInGen.Count > 0 ? "\n  多余绑定（已删除未清理）：" + string.Join("、", staleInGen) : string.Empty;
			Debug.LogError(
				"[推送注册表过期] 业务源码的 [NotifyMessageHandler] 与 MessageRegistry.gen.cs 不一致："
				+ missingText + staleText
				+ "\n请运行菜单 Tools/网络/重新生成推送注册表（或 Protobuf/gen-proto-registry.bat），否则这些推送将静默失效（服务器不会补发）。");
		}

		/// <summary>手动校验：立即比对并输出结果（绿色即一致）。</summary>
		[MenuItem("Tools/网络/校验推送注册表一致性")]
		private static void ValidateManually()
		{
			if (!TryCompare(out var missingInGen, out var staleInGen))
			{
				Debug.LogError("[推送注册表] 比对失败（生成物或源码缺失），详见上一条日志。");
				return;
			}

			if (missingInGen.Count == 0 && staleInGen.Count == 0)
			{
				Debug.Log($"<color=green>[推送注册表] 一致：{sr_bindPattern.Matches(File.ReadAllText(GenFilePath)).Count} 条绑定与业务源码声明完全匹配。</color>");
				return;
			}

			Debug.LogError($"[推送注册表过期] 缺少绑定 {missingInGen.Count} 条、多余绑定 {staleInGen.Count} 条，请运行 Tools/网络/重新生成推送注册表。");
		}

		/// <summary>一键重生成：调用 Protobuf/gen-proto-registry.py 并刷新（随后自动再编译，守卫会复验）。</summary>
		[MenuItem("Tools/网络/重新生成推送注册表")]
		private static void Regenerate()
		{
			var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
			if (!File.Exists(Path.Combine(repoRoot, "Protobuf", "gen-proto-registry.py")))
			{
				Debug.LogError($"[推送注册表] 未找到生成脚本：{Path.Combine(repoRoot, "Protobuf", "gen-proto-registry.py")}");
				return;
			}

			var startInfo = new ProcessStartInfo
			{
				FileName = "python",
				Arguments = "Protobuf/gen-proto-registry.py",
				WorkingDirectory = repoRoot,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			using var process = Process.Start(startInfo);
			var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
			process.WaitForExit(15000);

			if (process.ExitCode != 0)
			{
				Debug.LogError($"[推送注册表] 生成脚本失败（退出码 {process.ExitCode}）：\n{output}");
				return;
			}

			Debug.Log($"<color=green>[推送注册表] 已重新生成，触发刷新编译。</color>\n{output.TrimEnd()}");
			AssetDatabase.Refresh();
		}

		/// <summary>执行比对：missingInGen=业务有声明而生成物缺绑定；staleInGen=生成物有绑定而业务已无声明。</summary>
		private static bool TryCompare(out HashSet<string> missingInGen, out HashSet<string> staleInGen)
		{
			missingInGen = new HashSet<string>();
			staleInGen = new HashSet<string>();
			var genFullPath = Path.GetFullPath(GenFilePath);
			if (!File.Exists(genFullPath))
			{
				Debug.LogError($"[推送注册表] 生成物不存在：{GenFilePath}——请运行 Tools/网络/重新生成推送注册表。");
				return false;
			}

			// 生成物侧：BindNotify 对集合
			var bindings = new HashSet<string>();
			foreach (Match match in sr_bindPattern.Matches(File.ReadAllText(genFullPath)))
			{
				bindings.Add($"{match.Groups[1].Value}.{match.Groups[2].Value}");
			}

			// 业务侧：声明对集合（跳过 AutoGen 生成物自身）
			var declarations = new HashSet<string>();
			var hotfixFullPath = Path.GetFullPath(HotfixRoot);
			foreach (var file in Directory.EnumerateFiles(hotfixFullPath, "*.cs", SearchOption.AllDirectories))
			{
				if (file.Contains($"{Path.DirectorySeparatorChar}AutoGen{Path.DirectorySeparatorChar}"))
				{
					continue;
				}

				foreach (Match match in sr_declPattern.Matches(File.ReadAllText(file)))
				{
					declarations.Add($"{match.Groups[1].Value}.{match.Groups[2].Value}");
				}
			}

			foreach (var declaration in declarations)
			{
				if (!bindings.Contains(declaration))
				{
					missingInGen.Add(declaration);
				}
			}

			foreach (var binding in bindings)
			{
				if (!declarations.Contains(binding))
				{
					staleInGen.Add(binding);
				}
			}

			return true;
		}
	}
}
