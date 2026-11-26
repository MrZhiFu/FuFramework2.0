using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using FuFramework.Core.Editor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
	/// <summary>
	/// 配置表导入器。
	/// 功能：
	///     1. 在 Unity Editor 中直接执行导表脚本，导出 bin（`.bytes`）格式的配置表。
	/// </summary>
	public static class ConfigImporter
	{
		/// <summary>
		/// 导出配置表（bin 唯一数据变体：.bytes + ByteBuf）
		/// </summary>
		[MenuItem("FuFramework/配置表/导出配置表", false, FuMenuPriority.CONFIG_EXPORT)]
		public static void ExportConfig()
		{
			var configDir = GetConfigPath();
			var stopwatch = Stopwatch.StartNew();

			var scriptName = Application.platform == RuntimePlatform.WindowsEditor
				? "gen-client-bin.bat"
				: "gen-client-bin.sh";
			var scriptPath = Path.Combine(configDir, scriptName);

			// 执行批处理命令
			var success = BatchRunner.RunBatch(scriptPath, configDir);

			if (success)
			{
				// 刷新并保存Unity资源
				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
				EditorUtility.DisplayDialog("成功", $"配置表导入成功, 耗时{stopwatch.Elapsed.TotalSeconds:F2}s", "确定");
			}
			else
			{
				EditorUtility.DisplayDialog("失败", "配置表导入失败，请查看控制台日志", "确定");
			}
		}

		/// <summary>
		/// 获取与项目根目录同级的 Config 目录路径, 如 D:\_WorkSpace\Unity\FuFramework2.0\Config
		/// </summary>
		private static string GetConfigPath()
		{
			// Assets 的上级即项目根目录（如 FuFramework2.0/Unity），再上一级即为 FuFramework2.0
			var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			var repoRoot    = Path.GetFullPath(Path.Combine(projectRoot,          ".."));

			return Path.Combine(repoRoot, "Config");
		}
	}
}
