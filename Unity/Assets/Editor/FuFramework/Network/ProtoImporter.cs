using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using FuFramework.Core.Editor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Editor.Network
{
	/// <summary>
	/// Proto 协议导出器（"网络"菜单统一入口）。
	/// 功能：
	///     1. 执行 Proto 导出脚本，将 .proto 文件编译为 C# 代码（客户端/服务端/全部）。
	///     2. 重新生成推送注册表：执行 gen-proto-registry 脚本（NotifyRegistryGuard 编译守卫的手动修复入口）。
	///     3. 校验推送注册表一致性：即时比对业务声明与生成物绑定（NotifyRegistryGuard 的手动触发）。
	/// </summary>
	public static class ProtoImporter
	{
		/// <summary>
		/// 导出目标
		/// </summary>
		private enum EExportTarget
		{
			/// <summary>
			/// 客户端
			/// </summary>
			Client,

			/// <summary>
			/// 服务端
			/// </summary>
			Server,

			/// <summary>
			/// 全部（客户端 + 服务端）
			/// </summary>
			All
		}

		/// <summary>
		/// 导出 Proto — 客户端
		/// </summary>
		[MenuItem("网络/导出Proto—客户端", false, FuMenuPriority.PROTO_EXPORT_CLIENT)]
		public static void ExportProtoClient()
		{
			ExportProto(EExportTarget.Client);
		}

		/// <summary>
		/// 导出 Proto — 服务端
		/// </summary>
		[MenuItem("网络/导出Proto—服务端", false, FuMenuPriority.PROTO_EXPORT_SERVER)]
		public static void ExportProtoServer()
		{
			ExportProto(EExportTarget.Server);
		}

		/// <summary>
		/// 导出 Proto — 全部（客户端 + 服务端）
		/// </summary>
		[MenuItem("网络/导出Proto—全部", false, FuMenuPriority.PROTO_EXPORT_ALL)]
		public static void ExportProtoAll()
		{
			ExportProto(EExportTarget.All);
		}

		/// <summary>
		/// 重新生成推送注册表：执行 gen-proto-registry 脚本（扫描 [NotifyMessageHandler] 产出 BindNotify 绑定），
		/// 完成后刷新资源触发再编译——NotifyRegistryGuard 编译守卫会自动复验一致性。
		/// </summary>
		[MenuItem("网络/重新生成推送注册表", false, FuMenuPriority.PROTO_REGISTRY_REGEN)]
		public static void RegenerateNotifyRegistry()
		{
			var protoDir  = GetProtoPath();
			var isWin     = Application.platform == RuntimePlatform.WindowsEditor;
			var script    = Path.Combine(protoDir, isWin ? "gen-proto-registry.bat" : "gen-proto-registry.sh");
			var stopwatch = Stopwatch.StartNew();

			if (!File.Exists(script))
			{
				EditorUtility.DisplayDialog("失败", $"未找到生成脚本：{script}", "确定");
				return;
			}

			if (BatchRunner.RunBatch(script, protoDir))
			{
				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
				EditorUtility.DisplayDialog("成功", $"推送注册表已重新生成，耗时{stopwatch.Elapsed.TotalSeconds:F2}s", "确定");
			}
			else
			{
				EditorUtility.DisplayDialog("失败", "推送注册表生成失败，请查看控制台日志", "确定");
			}
		}

		/// <summary>
		/// 校验推送注册表一致性：即时比对业务源码 [NotifyMessageHandler] 声明与生成物 BindNotify 绑定（结果输出到控制台）。
		/// </summary>
		[MenuItem("网络/校验推送注册表一致性", false, FuMenuPriority.PROTO_REGISTRY_VALIDATE)]
		public static void ValidateNotifyRegistry()
		{
			NotifyRegistryGuard.ValidateAndReport();
		}

		/// <summary>
		/// 导出 Proto
		/// </summary>
		/// <param name="target">导出目标</param>
		private static void ExportProto(EExportTarget target)
		{
			var protoDir  = GetProtoPath();
			var stopwatch = Stopwatch.StartNew();

			var isWin = Application.platform == RuntimePlatform.WindowsEditor;

			if (target == EExportTarget.All)
			{
				// 全部导出：依次执行客户端和服务端脚本
				var clientScript = Path.Combine(protoDir, isWin ? "Proto2CsExport_Client.bat" : "Proto2CsExport_Client.sh");
				var serverScript = Path.Combine(protoDir, isWin ? "Proto2CsExport_Server.bat" : "Proto2CsExport_Server.sh");

				var clientSuccess = BatchRunner.RunBatch(clientScript, protoDir);
				var serverSuccess = BatchRunner.RunBatch(serverScript, protoDir);

				if (clientSuccess && serverSuccess)
				{
					AssetDatabase.Refresh();
					AssetDatabase.SaveAssets();
					EditorUtility.DisplayDialog("成功", $"Proto 全部导出成功, 耗时{stopwatch.Elapsed.TotalSeconds:F2}s", "确定");
				}
				else
				{
					var failedTarget = !clientSuccess && !serverSuccess ? "客户端、服务端"
						: !clientSuccess                      ? "客户端"
						:                                       "服务端";
					EditorUtility.DisplayDialog("失败", $"Proto 导出失败（{failedTarget}），请查看控制台日志", "确定");
				}

				return;
			}

			// 单个导出
			var scriptName = GetScriptName(target, isWin);
			var scriptPath = Path.Combine(protoDir, scriptName);

			var success = BatchRunner.RunBatch(scriptPath, protoDir);

			if (success)
			{
				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
				EditorUtility.DisplayDialog("成功", $"Proto 导出成功, 耗时{stopwatch.Elapsed.TotalSeconds:F2}s", "确定");
			}
			else
			{
				EditorUtility.DisplayDialog("失败", "Proto 导出失败，请查看控制台日志", "确定");
			}
		}

		/// <summary>
		/// 根据导出目标和平台获取脚本文件名
		/// </summary>
		private static string GetScriptName(EExportTarget target, bool isWin)
		{
			return target switch
			{
				EExportTarget.Client => isWin ? "Proto2CsExport_Client.bat" : "Proto2CsExport_Client.sh",
				EExportTarget.Server => isWin ? "Proto2CsExport_Server.bat" : "Proto2CsExport_Server.sh",
				_                    => "Proto2CsExport_Client.bat"
			};
		}

		/// <summary>
		/// 获取与项目根目录同级的 Protobuf 目录路径, 如 D:\_WorkSpace\Unity\FuFramework2.0\Protobuf
		/// </summary>
		private static string GetProtoPath()
		{
			// Assets 的上级即项目根目录（如 FuFramework2.0/Unity），再上一级即为 FuFramework2.0
			var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			var repoRoot    = Path.GetFullPath(Path.Combine(projectRoot,          ".."));

			return Path.Combine(repoRoot, "Protobuf");
		}
	}
}
