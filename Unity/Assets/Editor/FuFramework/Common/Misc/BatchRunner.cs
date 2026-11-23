using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEditor;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// 批处理执行工具类。
	/// 功能：
	///     1. 执行批处理命令(.bat或.sh)。
	///     2. 标准输出逐行镜像到 Unity Console（按行内容分级：警告黄/错误红/普通白），进度条同步展示当前行。
	/// </summary>
	public static class BatchRunner
	{
		/// <summary>
		/// 0 ~ 0.1 表示模拟执行前的准备时间(视觉效果，不影响功能)
		/// </summary>
		private const float READY_TIME = 0.1f;

		/// <summary>
		/// ANSI 转义序列（颜色码等）——子进程输出里混入会导致 Console 显示乱码标记，转发前剥离
		/// </summary>
		private static readonly Regex s_ansiRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

		/// <summary>
		/// Unity Console rich text 黄色标记（警告行内嵌用；Debug.Log 文本支持 &lt;color&gt; 标签，需 Console 开启 rich text——默认开启）
		/// </summary>
		private const string WARNING_COLOR_OPEN  = "<color=#FFD54F>";
		private const string WARNING_COLOR_CLOSE = "</color>";

		/// <summary>
		/// 执行批处理命令(.bat或.sh)
		/// </summary>
		/// <param name="cmdPath">命令路径</param>
		/// <param name="workDir">工作目录</param>
		/// <returns>是否执行成功</returns>
		public static bool RunBatch(string cmdPath, string workDir)
		{
			EditorUtility.DisplayProgressBar("执行批处理", "准备执行: " + cmdPath, READY_TIME);

			try
			{
				// 获取平台对应的命令执行工具和前缀参数
				var (runner, preArg) = GetPlatformCommand();

				// 执行进程（完整输出已在 RunProcess 内合并为一条 Log 级日志，此处不再重复打完成日志）
				ExecuteProcess(cmdPath, workDir, runner, preArg);

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
			var fProgress    = READY_TIME;
			var errorBuilder = new System.Text.StringBuilder();

			process.Start();

			// 异步读取批处理的错误输出，避免死锁
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null) errorBuilder.AppendLine(e.Data);
			};
			process.BeginErrorReadLine();

			// 同步读取批处理的标准输出：进度条逐行刷新，Console 在进程结束后合并输出一条完整日志
			var logBuilder = new System.Text.StringBuilder();
			var lineCount  = 0;
			while (true)
			{
				var line = process.StandardOutput.ReadLine();
				if (line == null) break;

				// ANSI 转义码剥离后收集；警告行内嵌 rich text 黄色标记（Console 中该行黄色显示，日志级别不变）
				var cleanLine = s_ansiRegex.Replace(line, string.Empty);
				if (!string.IsNullOrWhiteSpace(cleanLine))
				{
					logBuilder.AppendLine(ContainsWarning(cleanLine) ? WARNING_COLOR_OPEN + cleanLine + WARNING_COLOR_CLOSE : cleanLine);
				}

				// 模拟视觉上的进度感，每读取批处理的标准输出一行，进度条增加0.01f
				EditorUtility.DisplayProgressBar("执行批处理", cleanLine, fProgress);
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

			// 完整输出合并为一条 Log 级日志；警告行已内嵌黄色 rich text，不改变日志级别
			var fullLog = logBuilder.ToString().TrimEnd();
			if (fullLog.Length > 0)
			{
				Debug.Log(fullLog);
			}

			if (process.ExitCode != 0)
			{
				throw new Exception("[执行批处理错误]: " + errorBuilder);
			}
		}

		/// <summary>
		/// 判断行是否为警告特征（命中则该行在 Console 中以黄色显示，日志级别不变）。
		/// 判定基于中英文关键词（与 gen 脚本/Luban/同步工具的输出约定对齐），不追求完备——漏判只是普通白色显示。
		/// </summary>
		private static bool ContainsWarning(string line)
		{
			return line.Contains("警告") || line.Contains("WARNING") || line.Contains("Warning");
		}
	}
}