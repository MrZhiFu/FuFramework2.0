using System;
using System.Diagnostics;
using System.Text;
using Serilog;

namespace FuFramework.Foundation.Logger;

/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
/// <summary>
/// 日志帮助类
/// </summary>
/// <remarks>
/// 提供了一系列静态方法用于记录不同级别的日志信息，包括调试信息、普通信息、警告和错误等。
/// 支持将日志输出到文件系统和控制台。
/// </remarks>
public static class LogHelper
{
	/// <summary>
	/// 内部日志记录器实例
	/// </summary>
	private static ILogger _logger;

	/// <summary>
	/// 将日志持久化。
	/// </summary>
	/// <remarks>
	/// 关闭日志记录器并将所有待处理的日志条目刷新到持久化存储中。
	/// </remarks>
	public static void FlushAndSave()
	{
		Log.CloseAndFlush();
	}

	/// <summary>
	/// 异步将日志持久化。
	/// </summary>
	/// <remarks>
	/// 关闭日志记录器并将所有待处理的日志条目刷新到持久化存储中。
	/// </remarks>
	public static async void CloseAndFlushAsync()
	{
		await Log.CloseAndFlushAsync();
	}

	/// <summary>
	/// 设置日志记录器
	/// </summary>
	/// <param name="logger">要设置的日志记录器实例</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出此异常</exception>
	public static void SetLogger(ILogger logger)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		_logger = logger;
	}

	/// <summary>
	/// 获取当前使用的日志记录器
	/// </summary>
	/// <returns>返回当前设置的日志记录器，如果未设置则返回Serilog的默认Logger</returns>
	private static ILogger GetLogger()
	{
		return _logger ?? Log.Logger;
	}

	/// <summary>
	/// 记录带有格式参数的信息消息。,只打印到控制台
	/// </summary>
	/// <param name="message">要记录的信息消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 仅将信息输出到控制台，不写入日志文件。
	/// 输出的消息会包含时间戳。
	/// </remarks>
	public static void Console(string message, params object[] args)
	{
		string text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]";
		if (args != null && args.Length > 0)
		{
			System.Console.WriteLine(text + message, args);
		}
		else
		{
			System.Console.WriteLine(text + message);
		}
	}

	/// <summary>
	/// 记录带有可选格式参数的调试消息。
	/// </summary>
	/// <param name="msg">要记录的调试消息。</param>
	/// <param name="args">消息的可选格式参数。</param>
	/// <remarks>
	/// 用于记录调试级别的日志信息，通常在开发和测试阶段使用。
	/// </remarks>
	public static void Debug(string msg, params object[] args)
	{
		GetLogger().Debug(msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录调试消息
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="msg">要记录的调试消息</param>
	/// <param name="args">消息的格式参数</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出此异常</exception>
	public static void Debug(ILogger logger, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Debug(msg, args);
	}

	/// <summary>
	/// 记录带有可选格式参数的调试消息。并控制台打印
	/// </summary>
	/// <param name="msg">要记录的调试消息。</param>
	/// <param name="args">消息的可选格式参数。</param>
	/// <remarks>
	/// 同时将调试信息输出到日志文件和控制台。
	/// </remarks>
	public static void DebugConsole(string msg, params object[] args)
	{
		Debug(msg, args);
		Console(msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录调试消息并输出到控制台
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="msg">要记录的调试消息</param>
	/// <param name="args">消息的格式参数</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出此异常</exception>
	public static void DebugConsole(ILogger logger, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Debug(msg, args);
		Console(msg, args);
	}

	/// <summary>
	/// 记录带有标签的调试消息。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的调试消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void Debug(string tag, string msg, params object[] args)
	{
		Debug("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带标签的调试消息
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的调试消息</param>
	/// <param name="args">消息的格式参数</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出此异常</exception>
	public static void Debug(ILogger logger, string tag, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Debug("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 记录带有标签的调试消息并输出到控制台。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的调试消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void DebugConsole(string tag, string msg, params object[] args)
	{
		Debug("[" + tag + "] " + msg, args);
		Console("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带标签的调试消息并输出到控制台
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的调试消息</param>
	/// <param name="args">消息的格式参数</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出此异常</exception>
	public static void DebugConsole(ILogger logger, string tag, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Debug("[" + tag + "] " + msg, args);
		Console("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 记录严重的异常错误
	/// </summary>
	/// <param name="exception">异常对象，包含错误的详细信息</param>
	/// <remarks>
	/// 记录异常作为错误级别的日志，包含异常信息和消息。
	/// 此方法会自动获取默认的日志记录器进行记录。
	/// </remarks>
	public static void Error(Exception exception)
	{
		GetLogger().Error(exception, exception.Message);
	}

	/// <summary>
	/// 使用指定的日志记录器记录异常错误
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="exception">要记录的异常对象</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出</exception>
	public static void Error(ILogger logger, Exception exception)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Error(exception, string.Empty);
	}

	/// <summary>
	/// 记录带有格式参数的错误消息
	/// </summary>
	/// <param name="message">要记录的错误消息</param>
	/// <param name="args">用于格式化消息的参数数组</param>
	/// <remarks>
	/// 记录错误级别的日志信息，并包含堆栈跟踪信息。
	/// 使用默认的日志记录器进行记录。
	/// </remarks>
	public static void Error(string message, params object[] args)
	{
		StackTrace value = new StackTrace(1, fNeedFileInfo: true);
		string messageTemplate = new StringBuilder().Append(string.Format(message, args)).Append('\n').Append(value)
			.ToString();
		GetLogger().Error(messageTemplate);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有格式参数的错误消息
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="message">要记录的错误消息</param>
	/// <param name="args">用于格式化消息的参数数组</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出</exception>
	public static void Error(ILogger logger, string message, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		StackTrace value = new StackTrace(1, fNeedFileInfo: true);
		string messageTemplate = new StringBuilder().Append(string.Format(message, args)).Append('\n').Append(value)
			.ToString();
		logger.Error(messageTemplate);
	}

	/// <summary>
	/// 记录带有格式参数的错误消息并同时输出到控制台
	/// </summary>
	/// <param name="message">要记录的错误消息</param>
	/// <param name="args">用于格式化消息的参数数组</param>
	/// <remarks>
	/// 同时将错误信息输出到日志文件和控制台。
	/// 控制台输出使用红色字体以突出显示错误信息。
	/// </remarks>
	public static void ErrorConsole(string message, params object[] args)
	{
		GetLogger().Error(message, args);
		System.Console.ForegroundColor = ConsoleColor.Red;
		Console(message, args);
		System.Console.ResetColor();
	}

	/// <summary>
	/// 记录带有标签的异常错误
	/// </summary>
	/// <param name="tag">用于标识日志来源或分类的标签</param>
	/// <param name="exception">要记录的异常对象</param>
	/// <remarks>
	/// 使用默认的日志记录器记录带有标签的异常信息。
	/// 标签会被添加在日志消息的开头，格式为 [标签]。
	/// </remarks>
	public static void Error(string tag, Exception exception)
	{
		GetLogger().Error(exception, $"[{tag}] {exception}");
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的异常错误
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="tag">用于标识日志来源或分类的标签</param>
	/// <param name="exception">要记录的异常对象</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出</exception>
	public static void Error(ILogger logger, string tag, Exception exception)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Error(exception, $"[{tag}] {exception}");
	}

	/// <summary>
	/// 记录带有标签的错误消息
	/// </summary>
	/// <param name="tag">用于标识日志来源或分类的标签</param>
	/// <param name="message">要记录的错误消息</param>
	/// <param name="args">用于格式化消息的参数数组</param>
	/// <remarks>
	/// 使用默认的日志记录器记录带有标签的错误消息。
	/// 包含完整的堆栈跟踪信息。
	/// </remarks>
	public static void Error(string tag, string message, params object[] args)
	{
		StackTrace value = new StackTrace(1, fNeedFileInfo: true);
		string messageTemplate = $"[{tag}] {string.Format(message, args)}\n{value}";
		GetLogger().Error(messageTemplate);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的错误消息
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例</param>
	/// <param name="tag">用于标识日志来源或分类的标签</param>
	/// <param name="message">要记录的错误消息</param>
	/// <param name="args">用于格式化消息的参数数组</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出</exception>
	public static void Error(ILogger logger, string tag, string message, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		StackTrace value = new StackTrace(1, fNeedFileInfo: true);
		string messageTemplate = $"[{tag}] {string.Format(message, args)}\n{value}";
		logger.Error(messageTemplate);
	}

	/// <summary>
	/// 记录带有标签的错误消息并输出到控制台
	/// </summary>
	/// <param name="tag">用于标识日志来源或分类的标签</param>
	/// <param name="message">要记录的错误消息</param>
	/// <param name="args">用于格式化消息的参数数组</param>
	/// <remarks>
	/// 同时将错误信息记录到日志文件并以红色字体显示在控制台上。
	/// 消息前会添加标签标识，格式为 [标签]。
	/// </remarks>
	public static void ErrorConsole(string tag, string message, params object[] args)
	{
		Error(tag, message, args);
		System.Console.ForegroundColor = ConsoleColor.Red;
		Console("[" + tag + "] " + message, args);
		System.Console.ResetColor();
	}

	/// <summary>
	/// 记录严重错误消息。
	/// </summary>
	/// <param name="message">要记录的严重错误消息。</param>
	/// <remarks>
	/// 记录致命错误级别的日志信息，并包含堆栈跟踪信息。
	/// </remarks>
	public static void Fatal(string message)
	{
		string messageTemplate = $"{message}\n{new StackTrace(1, fNeedFileInfo: true)}";
		GetLogger().Fatal(messageTemplate);
	}

	/// <summary>
	/// 使用指定的日志记录器记录严重错误消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="message">要记录的严重错误消息。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Fatal(ILogger logger, string message)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		string messageTemplate = $"{message}\n{new StackTrace(1, fNeedFileInfo: true)}";
		logger.Fatal(messageTemplate);
	}

	/// <summary>
	/// 记录严重的异常错误。
	/// </summary>
	/// <param name="exception">要记录的异常对象。</param>
	/// <remarks>
	/// 记录异常作为致命错误级别的日志，并包含堆栈跟踪信息。
	/// </remarks>
	public static void Fatal(Exception exception)
	{
		string messageTemplate = $"{exception}\n{new StackTrace(1, fNeedFileInfo: true)}";
		GetLogger().Fatal(messageTemplate);
	}

	/// <summary>
	/// 使用指定的日志记录器记录严重的异常错误。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="exception">要记录的异常对象。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Fatal(ILogger logger, Exception exception)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		string messageTemplate = $"{exception}\n{new StackTrace(1, fNeedFileInfo: true)}";
		logger.Fatal(messageTemplate);
	}

	/// <summary>
	/// 记录带有标签的严重错误消息。
	/// </summary>
	/// <param name="tag">日志标签，用于标识日志来源或分类。</param>
	/// <param name="message">要记录的严重错误消息。</param>
	/// <remarks>
	/// 记录的消息将包含标签前缀和堆栈跟踪信息。
	/// </remarks>
	public static void Fatal(string tag, string message)
	{
		string messageTemplate = $"[{tag}] {message}\n{new StackTrace(1, fNeedFileInfo: true)}";
		GetLogger().Fatal(messageTemplate);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的严重错误消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="tag">日志标签，用于标识日志来源或分类。</param>
	/// <param name="message">要记录的严重错误消息。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Fatal(ILogger logger, string tag, string message)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		string messageTemplate = $"[{tag}] {message}\n{new StackTrace(1, fNeedFileInfo: true)}";
		logger.Fatal(messageTemplate);
	}

	/// <summary>
	/// 记录带有标签的严重异常错误。
	/// </summary>
	/// <param name="tag">日志标签，用于标识日志来源或分类。</param>
	/// <param name="exception">要记录的异常对象。</param>
	/// <remarks>
	/// 记录的异常信息将包含标签前缀和堆栈跟踪信息。
	/// </remarks>
	public static void Fatal(string tag, Exception exception)
	{
		string messageTemplate = $"[{tag}] {exception}\n{new StackTrace(1, fNeedFileInfo: true)}";
		GetLogger().Fatal(messageTemplate);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的严重异常错误。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="tag">日志标签，用于标识日志来源或分类。</param>
	/// <param name="exception">要记录的异常对象。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Fatal(ILogger logger, string tag, Exception exception)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		string messageTemplate = $"[{tag}] {exception}\n{new StackTrace(1, fNeedFileInfo: true)}";
		logger.Fatal(messageTemplate);
	}

	/// <summary>
	/// 记录信息消息
	/// </summary>
	/// <param name="message">要记录的信息对象</param>
	/// <remarks>
	/// 将对象转换为字符串后记录为信息级别的日志。
	/// 如果对象为null，将记录"null object"。
	/// </remarks>
	public static void Info(object message)
	{
		GetLogger().Information(message?.ToString() ?? "null object");
	}

	/// <summary>
	/// 记录带有格式参数的信息消息。
	/// </summary>
	/// <param name="message">要记录的信息消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 用于记录一般信息级别的日志。
	/// </remarks>
	public static void Info(string message, params object[] args)
	{
		GetLogger().Information(message, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有格式参数的信息消息
	/// </summary>
	/// <param name="logger">要使用的日志记录器实例</param>
	/// <param name="message">要记录的信息消息</param>
	/// <param name="args">消息的格式参数</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出此异常</exception>
	public static void Info(ILogger logger, string message, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Information(message, args);
	}

	/// <summary>
	/// 记录带有格式参数的信息消息。并控制台打印
	/// </summary>
	/// <param name="message">要记录的信息消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 同时将信息输出到日志文件和控制台。
	/// </remarks>
	public static void InfoConsole(string message, params object[] args)
	{
		Info(message, args);
		Console(message, args);
	}

	/// <summary>
	/// 记录信息消息。
	/// </summary>
	/// <param name="msg">要记录的异常对象。</param>
	/// <remarks>
	/// 将异常的消息内容记录为信息级别的日志。
	/// </remarks>
	public static void Info(Exception msg)
	{
		GetLogger().Information(msg.ToString());
	}

	/// <summary>
	/// 使用指定的日志记录器记录异常信息
	/// </summary>
	/// <param name="logger">要使用的日志记录器实例</param>
	/// <param name="exception">要记录的异常对象</param>
	public static void Info(ILogger logger, Exception exception)
	{
		Info(exception.ToString());
	}

	/// <summary>
	/// 使用指定的日志记录器记录异常信息并输出到控制台
	/// </summary>
	/// <param name="logger">要使用的日志记录器实例</param>
	/// <param name="exception">要记录的异常对象</param>
	public static void InfoConsole(ILogger logger, Exception exception)
	{
		Info(exception.ToString());
		Console(exception.ToString());
	}

	/// <summary>
	/// 记录带有标签的信息消息。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的信息消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void Info(string tag, string message, params object[] args)
	{
		Info("[" + tag + "] " + message, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的信息消息
	/// </summary>
	/// <param name="logger">要使用的日志记录器实例</param>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的信息消息</param>
	/// <param name="args">消息的格式参数</param>
	public static void Info(ILogger logger, string tag, string message, params object[] args)
	{
		Info(logger, "[" + tag + "] " + message, args);
	}

	/// <summary>
	/// 记录带有标签的信息消息并输出到控制台。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的信息消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void InfoConsole(string tag, string message, params object[] args)
	{
		Info(tag, message, args);
		Console("[" + tag + "] " + message, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的信息消息并输出到控制台
	/// </summary>
	/// <param name="logger">要使用的日志记录器实例</param>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的信息消息</param>
	/// <param name="args">消息的格式参数</param>
	public static void InfoConsole(ILogger logger, string tag, string message, params object[] args)
	{
		Info(logger, tag, message, args);
		Console("[" + tag + "] " + message, args);
	}

	/// <summary>
	/// 记录带有标签的对象信息。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的信息对象</param>
	public static void Info(string tag, object message)
	{
		GetLogger().Information("[" + tag + "] " + (message?.ToString() ?? "null object"));
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的对象信息
	/// </summary>
	/// <param name="logger">要使用的日志记录器实例</param>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的信息对象</param>
	public static void Info(ILogger logger, string tag, object message)
	{
		logger.Information("[" + tag + "] " + (message?.ToString() ?? "null object"));
	}

	/// <summary>
	/// 使用指定的日志记录器记录详细级别的日志消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <remarks>
	/// 在记录日志之前会检查logger参数是否为null。
	/// </remarks>
	public static void Verbose(ILogger logger, string msg)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Verbose(msg);
	}

	/// <summary>
	/// 记录详细级别的日志消息。
	/// </summary>
	/// <param name="msg">要记录的详细消息。</param>
	/// <remarks>
	/// 用于记录最详细级别的日志信息，通常用于深入调试和跟踪。
	/// </remarks>
	public static void Verbose(string msg)
	{
		GetLogger().Verbose(msg);
	}

	/// <summary>
	/// 记录带有格式参数的详细级别日志消息。
	/// </summary>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 支持使用格式化字符串记录详细级别的日志信息。
	/// </remarks>
	public static void Verbose(string msg, params object[] args)
	{
		GetLogger().Verbose(msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有格式参数的详细级别日志消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 在记录日志之前会检查logger参数是否为null。
	/// </remarks>
	public static void Verbose(ILogger logger, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Verbose(msg, args);
	}

	/// <summary>
	/// 记录带有格式参数的详细级别日志消息，并同时输出到控制台。
	/// </summary>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 同时将详细信息输出到日志文件和控制台，便于实时查看和调试。
	/// </remarks>
	public static void VerboseConsole(string msg, params object[] args)
	{
		Verbose(msg, args);
		Console(msg, args);
	}

	/// <summary>
	/// 记录带有格式参数的详细级别日志消息，并同时输出到控制台。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 同时将详细信息输出到日志文件和控制台，便于实时查看和调试。
	/// </remarks>
	public static void VerboseConsole(ILogger logger, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		Verbose(logger, msg, args);
		Console(msg, args);
	}

	/// <summary>
	/// 记录带有标签的详细级别日志消息。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的详细消息。</param>
	public static void Verbose(string tag, string msg)
	{
		Verbose("[" + tag + "] " + msg);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的详细级别日志消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="tag">日志标签。</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <remarks>
	/// 在记录日志之前会检查logger参数是否为null。
	/// </remarks>
	public static void Verbose(ILogger logger, string tag, string msg)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		Verbose(logger, "[" + tag + "] " + msg);
	}

	/// <summary>
	/// 记录带有标签和格式参数的详细级别日志消息。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void Verbose(string tag, string msg, params object[] args)
	{
		Verbose("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签和格式参数的详细级别日志消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="tag">日志标签。</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 在记录日志之前会检查logger参数是否为null。
	/// </remarks>
	public static void Verbose(ILogger logger, string tag, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		Verbose(logger, "[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 记录带有标签和格式参数的详细级别日志消息，并同时输出到控制台。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void VerboseConsole(string tag, string msg, params object[] args)
	{
		Verbose(tag, msg, args);
		Console("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签和格式参数的详细级别日志消息，并同时输出到控制台。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="tag">日志标签。</param>
	/// <param name="msg">要记录的详细消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 在记录日志之前会检查logger参数是否为null，并将消息同时输出到日志文件和控制台。
	/// </remarks>
	public static void VerboseConsole(ILogger logger, string tag, string msg, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		Verbose(logger, tag, msg, args);
		Console("[" + tag + "] " + msg, args);
	}

	/// <summary>
	/// 记录警告消息。
	/// </summary>
	/// <param name="message">要记录的警告消息。</param>
	/// <remarks>
	/// 使用默认日志记录器记录警告级别的日志信息。
	/// </remarks>
	public static void Warn(string message)
	{
		GetLogger().Warning(message);
	}

	/// <summary>
	/// 使用指定的日志记录器记录警告消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="message">要记录的警告消息。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Warn(ILogger logger, string message)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Warning(message);
	}

	/// <summary>
	/// 记录带有格式参数的警告消息。
	/// </summary>
	/// <param name="message">要记录的警告消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 用于记录警告级别的日志信息。
	/// </remarks>
	public static void Warn(string message, params object[] args)
	{
		GetLogger().Warning(message, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有格式参数的警告消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="message">要记录的警告消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Warn(ILogger logger, string message, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Warning(message, args);
	}

	/// <summary>
	/// 记录带有格式参数的警告消息。
	/// </summary>
	/// <param name="message">要记录的警告消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <remarks>
	/// 同时将警告信息输出到日志文件和控制台。
	/// 控制台输出使用黄色字体以突出显示警告信息。
	/// </remarks>
	public static void WarnConsole(string message, params object[] args)
	{
		Warn(message, args);
		System.Console.ForegroundColor = ConsoleColor.Yellow;
		Console(message, args);
		System.Console.ResetColor();
	}

	/// <summary>
	/// 记录带有标签的警告消息。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的警告消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void Warn(string tag, string message, params object[] args)
	{
		GetLogger().Warning("[" + tag + "] " + message, args);
	}

	/// <summary>
	/// 使用指定的日志记录器记录带有标签的警告消息。
	/// </summary>
	/// <param name="logger">用于记录日志的ILogger实例。</param>
	/// <param name="tag">日志标签。</param>
	/// <param name="message">要记录的警告消息。</param>
	/// <param name="args">消息的格式参数。</param>
	/// <exception cref="T:System.ArgumentNullException">当logger参数为null时抛出。</exception>
	public static void Warn(ILogger logger, string tag, string message, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(logger, "logger");
		logger.Warning("[" + tag + "] " + message, args);
	}

	/// <summary>
	/// 记录带有标签的警告消息并输出到控制台。
	/// </summary>
	/// <param name="tag">日志标签</param>
	/// <param name="message">要记录的警告消息。</param>
	/// <param name="args">消息的格式参数。</param>
	public static void WarnConsole(string tag, string message, params object[] args)
	{
		Warn(tag, message, args);
		System.Console.ForegroundColor = ConsoleColor.Yellow;
		Console("[" + tag + "] " + message, args);
		System.Console.ResetColor();
	}
}
