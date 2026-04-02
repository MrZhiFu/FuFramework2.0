using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Debug = UnityEngine.Debug;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 批处理执行工具
    /// </summary>
    public static class BatchRunner
    {
        /// <summary>
        /// 执行批处理命令(.bat或.sh)
        /// </summary>
        /// <param name="cmdPath">命令路径</param>
        /// <param name="workDir">工作目录</param>
        /// <param name="showProgress">是否显示进度条</param>
        /// <param name="timeoutMs">超时时间（毫秒），-1表示无限等待</param>
        /// <returns>是否执行成功</returns>
        public static bool RunBatch(string cmdPath, string workDir, bool showProgress = true, int timeoutMs = -1)
        {
            if (showProgress)
            {
                EditorUtility.DisplayProgressBar("执行批处理", "准备执行: " + cmdPath, 0f);
            }

            try
            {
                // 获取平台相关的命令执行工具和前缀参数
                var (runner, preArg) = GetPlatformCommand();

                // 执行进程并收集错误
                var error = ExecuteProcess(cmdPath, workDir, runner, preArg, showProgress);

                // 检查执行结果
                if (error.Length > 0)
                {
                    Debug.LogError($"[执行批处理错误] {error}");
                    return false;
                }

                Debug.Log($"[执行批处理完成] {cmdPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[批处理执行异常] {ex.Message}\n命令: {cmdPath}");
                return false;
            }
            finally
            {
                if (showProgress)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        /// <summary>
        /// 获取平台相关的命令执行工具
        /// </summary>
        /// <returns>runner: 命令执行工具路径, preArg: 前缀参数，用于执行后自动退出进程</returns>
        private static (string runner, string preArg) GetPlatformCommand()
        {
            var os = Environment.OSVersion;

            if (os.Platform is PlatformID.Win32NT or PlatformID.Win32Windows)
            {
                return ("cmd.exe", "/C ");
            }

            return ("/bin/bash", "-c ");
        }

        /// <summary>
        /// 执行进程并收集发生的错误
        /// </summary>
        /// <param name="cmdPath">命令路径</param>
        /// <param name="workDir">工作目录</param>
        /// <param name="runner">命令执行工具</param>
        /// <param name="preArg">前缀参数</param>
        /// <param name="showProgress">是否显示进度条</param>
        /// <returns>错误输出内容</returns>
        private static StringBuilder ExecuteProcess(string cmdPath, string workDir, string runner, string preArg, bool showProgress)
        {
            var error = new StringBuilder();

            using var process = CreateProcess(cmdPath, workDir, runner, preArg);
            SetupDataHandlers(process, error, showProgress);
            RunProcess(process);

            return error;
        }

        /// <summary>
        /// 创建并配置进程
        /// </summary>
        /// <param name="cmdPath">命令路径</param>
        /// <param name="workDir">工作目录</param>
        /// <param name="runner">命令执行工具</param>
        /// <param name="preArg">前缀参数</param>
        /// <returns>配置好的进程实例</returns>
        private static Process CreateProcess(string cmdPath, string workDir, string runner, string preArg)
        {
            var process = new Process();

            process.StartInfo.FileName               = runner;
            process.StartInfo.Arguments              = preArg + cmdPath;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.CreateNoWindow         = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;

            if (!string.IsNullOrEmpty(workDir) && Directory.Exists(workDir))
            {
                process.StartInfo.WorkingDirectory = workDir;
            }

            return process;
        }

        /// <summary>
        /// 设置数据接收处理器
        /// </summary>
        /// <param name="process">进程实例</param>
        /// <param name="error">错误输出收集器</param>
        /// <param name="showProgress">是否显示进度条</param>
        private static void SetupDataHandlers(Process process, StringBuilder error, bool showProgress)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                if (showProgress)
                {
                    EditorUtility.DisplayProgressBar("执行批处理", e.Data, 0.5f);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                error.AppendLine(e.Data);
                Debug.LogError("[执行批处理错误] " + e.Data);
            };
        }

        /// <summary>
        /// 运行进程并等待完成
        /// </summary>
        /// <param name="process">进程实例</param>
        private static void RunProcess(Process process)
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 使用循环等待避免死锁
            // 当输出缓冲区满时，WaitForExit() 会阻塞，而进程又在等待缓冲区被读取
            while (!process.HasExited)
            {
                process.WaitForExit(100);
            }
        }
    }
}