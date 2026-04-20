using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 可执行文件运行工具类。
    /// 功能：
    ///     1. 执行可执行文件(.exe)。
    ///     2. 支持带参数执行。
    ///     3. 支持设置工作目录。
    ///     4. 支持超时控制。
    /// </summary>
    public static class ExeRunner
    {
        /// <summary>
        /// 默认超时时间（毫秒）
        /// </summary>
        private const int DefaultTimeout = 60000;

        /// <summary>
        /// 0 ~ 0.1 表示模拟执行前的准备时间(视觉效果，不影响功能)
        /// </summary>
        private const float ReadyTime = 0.1f;

        /// <summary>
        /// 启动 HttpCDN 服务器
        /// </summary>
        /// <remarks>
        /// 执行路径：Unity工程同级目录/Tools/HttpCDN/miniserve.exe
        /// 服务目录：Unity工程同级目录/Tools/HttpCDN（miniserve 默认服务当前目录）
        /// </remarks>
        [MenuItem("FuFramework/启动HttpCDN服务器(用于模拟资源更新)", false, 1300)]
        public static void StartHttpCdnServer()
        {
            var exePath = GetHttpCdnExePath();
            if (string.IsNullOrEmpty(exePath))
            {
                Debug.LogError("[启动 HttpCDN 失败] 未找到 miniserve.exe，请确保 Tools/HttpCDN 目录存在");
                return;
            }

            // miniserve 不需要参数，默认服务当前目录
            StartProcessDetached(exePath, System.IO.Path.GetDirectoryName(exePath));
        }

        /// <summary>
        /// 启动独立进程（不等待退出，适用于服务器程序）
        /// </summary>
        /// <param name="exePath">可执行文件路径</param>
        /// <param name="workDir">工作目录</param>
        private static void StartProcessDetached(string exePath, string workDir)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.WorkingDirectory = workDir;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = false;

                process.Start();

                Debug.Log($"[HttpCDN 已启动] {exePath}");
                Debug.Log($"[HttpCDN 访问地址] http://localhost:8080");
            }
            catch (Exception e)
            {
                Debug.LogError($"[启动 HttpCDN 失败] {e.Message}");
            }
        }

        /// <summary>
        /// 获取 HttpCDN 可执行文件路径
        /// </summary>
        /// <returns>miniserve.exe 的完整路径，不存在则返回 null</returns>
        private static string GetHttpCdnExePath()
        {
            // Unity 工程路径：Assets 的上级目录
            var projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectPath))
            {
                return null;
            }

            // 工程同级目录下的 Tools/HttpCDN/miniserve.exe
            var parentPath = System.IO.Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(parentPath))
            {
                return null;
            }

            var exePath = System.IO.Path.Combine(parentPath, "Tools", "HttpCDN", "miniserve.exe");
            return System.IO.File.Exists(exePath) ? exePath : null;
        }

        /// <summary>
        /// 获取 HttpCDN 服务目录路径
        /// </summary>
        /// <returns>CDN 目录路径，不存在则返回 null</returns>
        private static string GetHttpCdnDirectory()
        {
            var projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectPath))
            {
                return null;
            }

            var parentPath = System.IO.Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(parentPath))
            {
                return null;
            }

            var cdnPath = System.IO.Path.Combine(parentPath, "Tools", "HttpCDN", "CDN");

            // 如果目录不存在，尝试创建
            if (!System.IO.Directory.Exists(cdnPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(cdnPath);
                    Debug.Log($"[HttpCDN] 已创建 CDN 目录: {cdnPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HttpCDN] 创建 CDN 目录失败: {e.Message}");
                    return null;
                }
            }

            return cdnPath;
        }

        /// <summary>
        /// 执行可执行文件
        /// </summary>
        /// <param name="exePath">可执行文件路径</param>
        /// <param name="arguments">命令行参数</param>
        /// <param name="workDir">工作目录</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        /// <returns>执行结果</returns>
        public static ExeRunResult Run(string exePath, string arguments = null, string workDir = null, int timeout = DefaultTimeout)
        {
            EditorUtility.DisplayProgressBar("执行程序", "准备执行: " + exePath, ReadyTime);

            var result = new ExeRunResult
            {
                ExePath   = exePath,
                Arguments = arguments,
                WorkDir   = workDir
            };

            try
            {
                using var process = CreateProcess(exePath, arguments, workDir);
                RunProcess(process, timeout, out var output, out var error);

                result.ExitCode = process.ExitCode;
                result.Output   = output;
                result.Error    = error;
                result.Success  = process.ExitCode == 0;

                if (result.Success)
                {
                    Debug.Log($"[执行程序完成] {exePath} {arguments}");
                }
                else
                {
                    Debug.LogWarning($"[执行程序退出码非零] ExitCode={process.ExitCode}, Exe={exePath}");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error   = ex.Message;
                Debug.LogError($"[执行程序异常] {ex.Message}\n程序: {exePath}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return result;
        }

        /// <summary>
        /// 创建并配置进程
        /// </summary>
        /// <param name="exePath">可执行文件路径</param>
        /// <param name="arguments">命令行参数</param>
        /// <param name="workDir">工作目录</param>
        /// <returns>配置好的进程实例</returns>
        private static Process CreateProcess(string exePath, string arguments, string workDir)
        {
            var process = new Process();

            process.StartInfo.FileName               = exePath;
            process.StartInfo.Arguments              = arguments ?? string.Empty;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.CreateNoWindow         = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;

            if (!string.IsNullOrEmpty(workDir))
            {
                process.StartInfo.WorkingDirectory = workDir;
            }

            return process;
        }

        /// <summary>
        /// 运行进程并等待完成
        /// </summary>
        /// <param name="process">进程实例</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        /// <param name="output">标准输出内容</param>
        /// <param name="error">错误输出内容</param>
        private static void RunProcess(Process process, int timeout, out string output, out string error)
        {
            var outputBuilder = new StringBuilder();
            var errorBuilder  = new StringBuilder();
            var fProgress     = ReadyTime;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 更新进度条
            var startTime = DateTime.Now;
            while (!process.HasExited)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                if (elapsed > timeout)
                {
                    process.Kill();
                    throw new TimeoutException($"程序执行超时（{timeout}ms）");
                }

                // 模拟进度增加
                fProgress = Math.Min(fProgress + 0.01f, 0.9f);
                EditorUtility.DisplayProgressBar("执行程序", $"运行中... {elapsed / 1000:F1}s", fProgress);

                System.Threading.Thread.Sleep(100);
            }

            process.WaitForExit();
            process.CancelOutputRead();
            process.CancelErrorRead();

            output = outputBuilder.ToString();
            error  = errorBuilder.ToString();
        }
    }

    /// <summary>
    /// 可执行文件执行结果
    /// </summary>
    public class ExeRunResult
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 退出码
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// 标准输出内容
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// 错误输出内容
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 可执行文件路径
        /// </summary>
        public string ExePath { get; set; }

        /// <summary>
        /// 命令行参数
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// 工作目录
        /// </summary>
        public string WorkDir { get; set; }
    }
}