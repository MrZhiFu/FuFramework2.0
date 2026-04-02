using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using FuFramework.Core.Editor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
    /// <summary>
    /// 配置表导入器
    /// </summary>
    public static class ConfigImporter
    {
        /// <summary>
        /// 数据目标格式
        /// </summary>
        public enum DataTarget
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
            ExportConfig(DataTarget.Json);
        }

        /// <summary>
        /// 导出 二进制格式的配置表
        /// </summary>
        [MenuItem("FuFramework/配置表/导出配置表—Bin", false, 1001)]
        public static void ExportConfigToBin()
        {
            ExportConfig(DataTarget.Bin);
        }

        /// <summary>
        /// 导出配置表
        /// </summary>
        /// <param name="target">数据目标格式</param>
        /// <returns>是否导出成功</returns>
        private static void ExportConfig(DataTarget target)
        {
            var configDir = GetConfigPath();
            var now       = DateTime.Now.Ticks;
            var targetStr = target.ToString().ToLower();
            var scriptName = Application.platform == RuntimePlatform.WindowsEditor
                ? $"gen-client-{targetStr}.bat"
                : $"gen-client-{targetStr}.sh";
            var scriptPath = Path.Combine(configDir, scriptName);

            // 执行批处理命令
            var success = BatchRunner.RunBatch(scriptPath, configDir);

            if (success)
            {
                Debug.LogFormat("导入配置成功，耗时: {0} s.", ((DateTime.Now.Ticks - now) / TimeSpan.TicksPerSecond));

                // 如果导出 JSON 格式的配置表，则移除启用二进制配置表的环境变量符号，否则添加启用二进制配置表的环境变量符号
                if (target == DataTarget.Json)
                    ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableBinaryConfigSymbol);
                else
                    ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableBinaryConfigSymbol);

                // 刷新 Unity 资源
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("成功", "配置表导入成功！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("失败", "配置表导入失败，请查看控制台日志", "确定");
            }
        }

        /// <summary>
        /// 获取与项目根目录同级的 Config 目录路径, 如D:\_WorkSpace\Unity\FuFramework2.0\Config
        /// </summary>
        private static string GetConfigPath()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); // 项目根目录：Assets 的上一级
            var parentDir   = Path.GetFullPath(Path.Combine(projectRoot,          "..")); // 项目父目录：项目根目录的上一级

            return Path.Combine(parentDir, "Config"); // Config 目录
        }
    }
}