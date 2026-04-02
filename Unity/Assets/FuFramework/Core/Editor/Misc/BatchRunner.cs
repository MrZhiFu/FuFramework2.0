using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 批处理执行工具
    /// </summary>
    public static class BatchRunner
    {
        /// <summary>
        /// 0 ~ 0.1 表示模拟执行前的准备时间(视觉效果，不影响功能)
        /// </summary>
        private const float ReadyTime = 0.1f;

        /// <summary>
        /// 执行批处理命令(.bat或.sh)
        /// </summary>
        /// <param name="cmdPath">命令路径</param>
        /// <param name="workDir">工作目录</param>
        /// <returns>是否执行成功</returns>
        public static bool RunBatch(string cmdPath, string workDir)
        {
            EditorUtility.DisplayProgressBar("执行批处理", "准备执行: " + cmdPath, ReadyTime);

            try
            {
                // 获取平台对应的命令执行工具和前缀参数
                var (runner, preArg) = GetPlatformCommand();

                // 执行进程
                ExecuteProcess(cmdPath, workDir, runner, preArg);

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
                EditorUtility.ClearProgressBar();
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
        /// 执行进程
        /// </summary>
        /// <param name="cmdPath">命令路径</param>
        /// <param name="workDir">工作目录</param>
        /// <param name="runner">命令执行工具</param>
        /// <param name="preArg">前缀参数</param>
        /// <returns>错误输出内容</returns>
        private static void ExecuteProcess(string cmdPath, string workDir, string runner, string preArg)
        {
            using var process = CreateProcess(cmdPath, workDir, runner, preArg);
            RunProcess(process);
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
        /// 运行进程并等待完成
        /// </summary>
        /// <param name="process">进程实例</param>
        private static void RunProcess(Process process)
        {
            var fProgress    = ReadyTime;
            var errorBuilder = new System.Text.StringBuilder();

            process.Start();

            // 异步读取批处理的错误输出，避免死锁
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };
            process.BeginErrorReadLine();

            // 同步读取批处理的标准输出，实时更新进度
            var lineCount = 0;
            while (true)
            {
                var line = process.StandardOutput.ReadLine();
                if (line == null) break;

                // 模拟视觉上的进度感，每读取批处理的标准输出一行，进度条增加0.01f
                EditorUtility.DisplayProgressBar("执行批处理", line, fProgress);
                fProgress =  Math.Min(fProgress + 0.01f, 0.9f);
                lineCount += 1;

                // 每读取10行，给Unity进度条UI一次刷新机会
                if (lineCount % 10 == 0)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }

            // 等待进程退出（带超时检查）
            while (!process.HasExited)
            {
                process.WaitForExit(100);
            }

            if (process.ExitCode != 0)
            {
                throw new Exception("[执行批处理错误]: " + errorBuilder);
            }
        }
    }
}