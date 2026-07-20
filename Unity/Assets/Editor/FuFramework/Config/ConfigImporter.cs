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
    ///     1. 在 Unity Editor 中直接执行导表脚本，导出 JSON 或二进制格式的配置表。
    /// </summary>
    public static class ConfigImporter
    {
        /// <summary>
        /// 数据目标格式
        /// </summary>
        public enum EDataTarget
        {
            /// <summary>
            /// JSON格式
            /// </summary>
            Json,

            /// <summary>
            /// 二进制格式
            /// </summary>
            Bin
        }

        /// <summary>
        /// 启用二进制配置表的环境变量符号
        /// </summary>
        private const string EnableBinaryConfigSymbol = "ENABLE_BINARY_CONFIG";

        /// <summary>
        /// 导入 JSON 格式的配置表
        /// </summary>
        [MenuItem("FuFramework/配置表/导出配置表—Json", false, 1000)]
        public static void ExportConfigToJson()
        {
            ExportConfig(EDataTarget.Json);
        }

        /// <summary>
        /// 导出 二进制格式的配置表
        /// </summary>
        [MenuItem("FuFramework/配置表/导出配置表—Bin", false, 1001)]
        public static void ExportConfigToBin()
        {
            ExportConfig(EDataTarget.Bin);
        }

        /// <summary>
        /// 导出配置表
        /// </summary>
        /// <param name="target">数据目标格式</param>
        private static void ExportConfig(EDataTarget target)
        {
            var configDir = GetConfigPath();
            var stopwatch = Stopwatch.StartNew();

            var targetStr = target.ToString().ToLower();
            var scriptName = Application.platform == RuntimePlatform.WindowsEditor
                ? $"gen-client-{targetStr}.bat"
                : $"gen-client-{targetStr}.sh";
            var scriptPath = Path.Combine(configDir, scriptName);

            // 执行批处理命令
            var success = BatchRunner.RunBatch(scriptPath, configDir);

            if (success)
            {
                // 如果导出 JSON 格式的配置表，则移除启用二进制配置表的环境变量符号，否则添加启用二进制配置表的环境变量符号
                if (target == EDataTarget.Json)
                {
                    ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableBinaryConfigSymbol);
                }
                else
                {
                    ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableBinaryConfigSymbol);
                }

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
