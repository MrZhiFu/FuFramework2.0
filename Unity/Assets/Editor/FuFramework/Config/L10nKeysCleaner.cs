using System.IO;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
    /// <summary>
    /// 多语言配置表清理器。
    /// 功能：
    ///     1. 预览：执行清理脚本（报告模式），列出所有 代码/配置数据/FGUI 中未引用的多语言 key。
    ///     2. 执行：删除 Excel 中未引用 key 行（仅修改 Excel，不导表）。
    /// 动态拼接 key（如 "common_" + x）静态分析不可检测，执行前须先预览人工确认。
    /// 说明：不走 BatchRunner——它会吞掉子进程 stdout（仅喂进度条），而清理报告需要完整打印到控制台。
    /// </summary>
    public static class L10nKeysCleaner
    {
        /// <summary>
        /// 预览未引用的多语言 key（只输出报告，不修改 Excel）
        /// </summary>
        [MenuItem("FuFramework/配置表/清理多语言配置表—预览", false, 1002)]
        public static void Preview()
        {
            Run("clean-l10n-keys-preview.bat", true);
        }

        /// <summary>
        /// 清理未引用的多语言 key（删除 Excel 行，不导表）
        /// </summary>
        [MenuItem("FuFramework/配置表/清理多语言配置表—执行", false, 1003)]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog("清理多语言配置表",
                                             "将删除所有未被代码/配置数据/FGUI引用的多语言key。\n\n"                       +
                                             "注意：动态拼接的 key（如 \"common_\" + x）无法静态检测，请先执行「预览」人工确认清单。\n" +
                                             "清理完成后请重新导表。\n\n确定执行？",
                                             "执行清理", "取消"))
            {
                return;
            }

            Run("clean-l10n-keys-apply.bat", false);
        }

        /// <summary>
        /// 执行清理批处理脚本，并把脚本完整输出打印到 Unity 控制台。
        /// </summary>
        /// <param name="scriptName">Tools 目录下的脚本文件名</param>
        /// <param name="isPreview">是否为预览模式，预览模式下脚本会输出报告而不修改 Excel</param>
        private static void Run(string scriptName, bool isPreview)
        {
            var toolsDir   = GetToolsPath();
            var scriptPath = Path.Combine(toolsDir, scriptName);
            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("失败", $"清理脚本不存在：{scriptPath}", "确定");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var (success, output) = RunScript(scriptPath, toolsDir);

            AssetDatabase.Refresh();

            Debug.Log($"[L10nKeysCleaner] {scriptName} 执行{(success ? "完成" : "失败")}，" +
                      $"耗时 {stopwatch.Elapsed.TotalSeconds:F2}s，输出：\n{output}");

            var operationStr = isPreview ? "预览冗余多语言 key" : "执行清理冗余多语言 key";
            var msgContent   = success ? $"{operationStr}完成，耗时 {stopwatch.Elapsed.TotalSeconds:F2}s，详见控制台日志。" : $"{operationStr}失败，详见控制台日志。";
            EditorUtility.DisplayDialog(success ? "成功" : "失败", msgContent, "确定");
        }

        /// <summary>
        /// 运行批处理脚本并收集完整输出。
        /// stdout/stderr 与脚本约定为 UTF-8（脚本侧对管道输出 reconfigure 为 UTF-8，控制台直跑仍为系统编码）。
        /// </summary>
        /// <param name="scriptPath">脚本完整路径</param>
        /// <param name="workDir">工作目录</param>
        /// <returns>(是否成功退出, 合并后的完整输出)</returns>
        private static (bool success, string output) RunScript(string scriptPath, string workDir)
        {
            var builder = new StringBuilder();

            using var process = new Process();
            process.StartInfo.FileName               = "cmd.exe";
            process.StartInfo.Arguments              = "/C \"" + scriptPath + "\"";
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.CreateNoWindow         = true;
            process.StartInfo.WorkingDirectory       = workDir;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;
            process.StartInfo.RedirectStandardInput  = true; // bat 以 pause 结尾：关闭 stdin 让其立即返回
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding  = Encoding.UTF8;

            process.Start();
            process.StandardInput.Close();

            // 报告输出量小（几十行），同步读到 EOF 不会死锁
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            builder.AppendLine(stdout);
            if (!string.IsNullOrWhiteSpace(stderr)) builder.AppendLine(stderr);

            return (process.ExitCode == 0, builder.ToString().TrimEnd());
        }

        /// <summary>
        /// 获取清理工具目录路径, 如 D:\_WorkSpace\Unity\FuFramework2.0\Tools\CleanL10nKeys
        /// </summary>
        private static string GetToolsPath()
        {
            // Assets 的上级即项目根目录（如 FuFramework2.0/Unity），再上一级即为 FuFramework2.0
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var repoRoot    = Path.GetFullPath(Path.Combine(projectRoot,          ".."));

            return Path.Combine(repoRoot, "Tools", "CleanL10nKeys");
        }
    }
}